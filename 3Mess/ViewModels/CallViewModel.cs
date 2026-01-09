using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Google.Cloud.Firestore;
using MessagingApp.Services;
using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class CallViewModel : ObservableObject
{
    private static void Forget(Task task) { }

    private readonly FirebaseAuthService _authService;
    private readonly FirestoreCallingService _callingService;
    private readonly FirestoreFriendsService _friendsService;
    private readonly AgoraVoiceService _agoraService;
    
    private readonly DispatcherTimer _durationTimer;
    private DateTime? _callStartTime;
    
    private FirestoreChangeListener? _callStateListener;
    private FirestoreChangeListener? _signalingListener;

    private string _callId = string.Empty;
    private string _callerId = string.Empty;
    private List<string> _participantIds = new();
    private bool _isVideoCall;
    private bool _isIncoming;

    private string _callStatusText = "Đang kết nối...";
    private string _callDuration = "00:00";
    private bool _isConnecting = true;
    private bool _isCallActive;
    private bool _isMicrophoneMuted;

    private string _remoteDisplayName = string.Empty;
    private string _remoteAvatarText = "?";

    public ObservableCollection<CallParticipantViewModel> CallParticipants { get; } = new();

    public ICommand ToggleMicrophoneCommand { get; }
    public ICommand EndCallCommand { get; }
    public ICommand AddParticipantCommand { get; }

    public event Action? CallEnded;

    public CallViewModel(string callId, bool isVideoCall, bool isIncoming, string callerId, List<string> participantIds)
    {
        _authService = FirebaseAuthService.Instance;
        _callingService = FirestoreCallingService.Instance;
        _friendsService = FirestoreFriendsService.Instance;
        _agoraService = AgoraVoiceService.Instance;

        _callId = callId;
        _isVideoCall = isVideoCall;
        _isIncoming = isIncoming;
        _callerId = callerId;
        _participantIds = participantIds ?? new List<string>();

        _durationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _durationTimer.Tick += (_, _) => UpdateCallDuration();

        ToggleMicrophoneCommand = new RelayCommand(() =>
        {
            IsMicrophoneMuted = !IsMicrophoneMuted;
            _agoraService.SetMicrophoneMuted(IsMicrophoneMuted);
        });
        EndCallCommand = new RelayCommand(() => Forget(EndCallAsync()));
        AddParticipantCommand = new RelayCommand(() => { /* TODO: Implement add participant */ });

        // Register Agora event handlers
        RegisterAgoraEventHandlers();

        _ = InitializeCallAsync();
    }

    public string CallId
    {
        get => _callId;
        set => SetProperty(ref _callId, value);
    }

    public bool IsVideoCall
    {
        get => _isVideoCall;
        set => SetProperty(ref _isVideoCall, value);
    }

    public bool IsVoiceCall => !IsVideoCall;

    public bool IsGroupCall => _participantIds.Count > 1;
    public bool IsOneToOneCall => !IsGroupCall;

    public bool CanAddParticipants => IsGroupCall; // For now, only allow adding to group calls

    public string CallStatusText
    {
        get => _callStatusText;
        set => SetProperty(ref _callStatusText, value);
    }

    public string CallDuration
    {
        get => _callDuration;
        set => SetProperty(ref _callDuration, value);
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        set => SetProperty(ref _isConnecting, value);
    }

    public bool IsCallActive
    {
        get => _isCallActive;
        set
        {
            if (SetProperty(ref _isCallActive, value))
            {
                if (value && !_durationTimer.IsEnabled)
                {
                    _callStartTime = DateTime.UtcNow;
                    _durationTimer.Start();
                }
                else if (!value && _durationTimer.IsEnabled)
                {
                    _durationTimer.Stop();
                }
            }
        }
    }

    public bool IsMicrophoneMuted
    {
        get => _isMicrophoneMuted;
        set => SetProperty(ref _isMicrophoneMuted, value);
    }

    public string RemoteDisplayName
    {
        get => _remoteDisplayName;
        set => SetProperty(ref _remoteDisplayName, value);
    }

    public string RemoteAvatarText
    {
        get => _remoteAvatarText;
        set => SetProperty(ref _remoteAvatarText, value);
    }

    private async Task InitializeCallAsync()
    {
        try
        {
            string? currentUserId = _authService.CurrentUserId;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                CallStatusText = "Lỗi: Chưa đăng nhập";
                await Task.Delay(2000);
                CallEnded?.Invoke();
                return;
            }

            // Load participant information
            await LoadParticipantsAsync();

            if (_isIncoming)
            {
                CallStatusText = "Cuộc gọi đến...";
                // Auto-answer for now (in production, show answer/reject UI first)
                await _callingService.AnswerCall(_callId, currentUserId);
            }
            else
            {
                CallStatusText = "Đang gọi...";
            }

            // Listen to call state changes
            _callStateListener = _callingService.ListenToCallState(_callId, OnCallStateChanged);

            // Listen to WebRTC signaling
            _signalingListener = _callingService.ListenToSignaling(_callId, currentUserId, OnSignalingDataReceived);

            // Initialize and join Agora voice channel
            bool agoraInitialized = _agoraService.Initialize();
            if (agoraInitialized)
            {
                bool joined = await _agoraService.JoinChannelAsync(_callId, currentUserId);
                if (joined)
                {
                    CallStatusText = "Đang kết nối...";
                    await Task.Delay(1000);
                    IsConnecting = false;
                    IsCallActive = true;
                    CallStatusText = "Đã kết nối";
                }
                else
                {
                    CallStatusText = "Không thể kết nối Agora";
                }
            }
            else
            {
                // Fallback: simulate connection if Agora not available
                CallStatusText = "Đang kết nối...";
                await Task.Delay(2000);
                IsConnecting = false;
                IsCallActive = true;
                CallStatusText = "Đã kết nối (Demo mode)";
            }
        }
        catch (Exception ex)
        {
            CallStatusText = $"Lỗi: {ex.Message}";
            await Task.Delay(2000);
            CallEnded?.Invoke();
        }
    }

    private async Task LoadParticipantsAsync()
    {
        try
        {
            CallParticipants.Clear();

            // For 1-1 calls
            if (IsOneToOneCall && _participantIds.Count > 0)
            {
                string otherUserId = _participantIds[0];
                var userData = await _friendsService.GetUserAsync(otherUserId);
                if (userData != null)
                {
                    string displayName = TryGetString(userData, "fullName", "username", "email");
                    RemoteDisplayName = displayName;
                    RemoteAvatarText = string.IsNullOrWhiteSpace(displayName) ? "?" : displayName.Substring(0, 1).ToUpperInvariant();
                }
                else
                {
                    RemoteDisplayName = "Người dùng";
                    RemoteAvatarText = "?";
                }
            }
            // For group calls
            else
            {
                foreach (var userId in _participantIds)
                {
                    var userData = await _friendsService.GetUserAsync(userId);
                    string displayName = "Người dùng";
                    if (userData != null)
                    {
                        displayName = TryGetString(userData, "fullName", "username", "email");
                    }

                    CallParticipants.Add(new CallParticipantViewModel
                    {
                        UserId = userId,
                        DisplayName = displayName,
                        AvatarText = string.IsNullOrWhiteSpace(displayName) ? "?" : displayName.Substring(0, 1).ToUpperInvariant()
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading participants: {ex.Message}");
        }
    }

    private void OnCallStateChanged(Dictionary<string, object> callData)
    {
        try
        {
            if (callData.TryGetValue("status", out var statusObj) && statusObj is string status)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (status == "active")
                    {
                        IsConnecting = false;
                        IsCallActive = true;
                        CallStatusText = "Đã kết nối";
                    }
                    else if (status == "ended" || status == "rejected" || status == "missed")
                    {
                        IsCallActive = false;
                        CallStatusText = status == "ended" ? "Cuộc gọi đã kết thúc" : "Cuộc gọi không thành công";
                        Task.Delay(1500).ContinueWith(_ => CallEnded?.Invoke());
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling call state change: {ex.Message}");
        }
    }

    private void OnSignalingDataReceived(Dictionary<string, object> signalData)
    {
        // In a real implementation, this would handle WebRTC offer/answer/ICE candidates
        // For this demonstration, we're just showing the UI structure
        try
        {
            if (signalData.TryGetValue("type", out var typeObj) && typeObj is string type)
            {
                Console.WriteLine($"Received signaling data: {type}");
                // Handle offer, answer, ice-candidate
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling signaling data: {ex.Message}");
        }
    }

    private void RegisterAgoraEventHandlers()
    {
        // Subscribe to Agora events for real-time updates
        _agoraService.UserJoined += OnAgoraUserJoined;
        _agoraService.UserLeft += OnAgoraUserLeft;
        _agoraService.ConnectionStateChanged += OnAgoraConnectionStateChanged;
        _agoraService.Error += OnAgoraError;
    }

    private void UnregisterAgoraEventHandlers()
    {
        _agoraService.UserJoined -= OnAgoraUserJoined;
        _agoraService.UserLeft -= OnAgoraUserLeft;
        _agoraService.ConnectionStateChanged -= OnAgoraConnectionStateChanged;
        _agoraService.Error -= OnAgoraError;
    }

    private void OnAgoraUserJoined(string userId)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Console.WriteLine($"User joined Agora channel: {userId}");
            
            // Update participant status or UI
            var participant = CallParticipants.FirstOrDefault(p => p.UserId == userId);
            if (participant != null)
            {
                // Mark as joined/active
                Console.WriteLine($"Participant {participant.DisplayName} is now in the call");
            }

            // Update call status if this is the first participant to join
            if (IsConnecting)
            {
                IsConnecting = false;
                IsCallActive = true;
                CallStatusText = "Đã kết nối";
            }
        });
    }

    private void OnAgoraUserLeft(string userId)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Console.WriteLine($"User left Agora channel: {userId}");
            
            // Update participant status or remove from list
            var participant = CallParticipants.FirstOrDefault(p => p.UserId == userId);
            if (participant != null)
            {
                Console.WriteLine($"Participant {participant.DisplayName} left the call");
            }

            // If all participants left and we're not the only one
            if (CallParticipants.Count > 1 && CallParticipants.All(p => p.UserId == _authService.CurrentUserId))
            {
                CallStatusText = "Người khác đã rời cuộc gọi";
            }
        });
    }

    private void OnAgoraConnectionStateChanged(bool isConnected)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (isConnected)
            {
                IsConnecting = false;
                IsCallActive = true;
                CallStatusText = "Đã kết nối";
                Console.WriteLine("Agora connection established");
            }
            else
            {
                CallStatusText = "Mất kết nối...";
                Console.WriteLine("Agora connection lost");
            }
        });
    }

    private void OnAgoraError(string error)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Console.WriteLine($"Agora error: {error}");
            CallStatusText = $"Lỗi: {error}";
        });
    }

    private async Task EndCallAsync()
    {
        try
        {
            string? currentUserId = _authService.CurrentUserId;
            if (string.IsNullOrWhiteSpace(currentUserId)) return;

            IsCallActive = false;
            CallStatusText = "Đang kết thúc...";

            // Leave Agora channel
            await _agoraService.LeaveChannelAsync();

            // Unregister Agora event handlers
            UnregisterAgoraEventHandlers();

            await _callingService.EndCall(_callId, currentUserId);

            // Clean up listeners
            if (_callStateListener != null)
            {
                await _callStateListener.StopAsync();
                _callStateListener = null;
            }

            if (_signalingListener != null)
            {
                await _signalingListener.StopAsync();
                _signalingListener = null;
            }

            _durationTimer.Stop();

            await Task.Delay(500);
            CallEnded?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error ending call: {ex.Message}");
            CallEnded?.Invoke();
        }
    }

    private void UpdateCallDuration()
    {
        if (!_callStartTime.HasValue) return;

        var elapsed = DateTime.UtcNow - _callStartTime.Value;
        CallDuration = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
    }

    private static string TryGetString(Dictionary<string, object> d, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (d.TryGetValue(k, out var v) && v != null)
            {
                var s = v.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
        }
        return string.Empty;
    }
}

public sealed class CallParticipantViewModel : ObservableObject
{
    private string _userId = string.Empty;
    private string _displayName = string.Empty;
    private string _avatarText = "?";

    public string UserId
    {
        get => _userId;
        set => SetProperty(ref _userId, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string AvatarText
    {
        get => _avatarText;
        set => SetProperty(ref _avatarText, value);
    }
}
