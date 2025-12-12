using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Google.Cloud.Firestore;
using MessagingApp.Services;
using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly FirebaseAuthService _authService;
    private readonly FirestoreFriendsService _friendsService;
    private readonly FirestoreMessagingService _messagingService;
    private FirestoreChangeListener? _messageListener;

    private FriendItemViewModel? _selectedFriend;
    private ConversationItemViewModel? _selectedConversation;
    private string _searchText = string.Empty;
    private string _draftMessage = string.Empty;
    private string _incomingAvatarText = "A";

    public ObservableCollection<FriendItemViewModel> Friends { get; } = new();
    public ObservableCollection<MessageItemViewModel> Messages { get; } = new();
    public ObservableCollection<MemberItemViewModel> Members { get; } = new();

    private readonly List<FriendItemViewModel> _allFriends = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value)) return;
            ApplyFriendsFilter();
        }
    }

    public FriendItemViewModel? SelectedFriend
    {
        get => _selectedFriend;
        set
        {
            if (!SetProperty(ref _selectedFriend, value)) return;
            _ = OpenChatWithFriendAsync(value);
        }
    }

    public ConversationItemViewModel? SelectedConversation
    {
        get => _selectedConversation;
        set
        {
            if (!SetProperty(ref _selectedConversation, value)) return;
            IncomingAvatarText = string.IsNullOrWhiteSpace(value?.AvatarText) ? "A" : value!.AvatarText;
            ((RelayCommand)SendMessageCommand).RaiseCanExecuteChanged();
            _ = SwitchConversationAsync(value);
        }
    }

    public string IncomingAvatarText
    {
        get => _incomingAvatarText;
        private set => SetProperty(ref _incomingAvatarText, value);
    }

    public string DraftMessage
    {
        get => _draftMessage;
        set
        {
            if (SetProperty(ref _draftMessage, value))
            {
                ((RelayCommand)SendMessageCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand SendMessageCommand { get; }

    public MainViewModel()
    {
        _authService = FirebaseAuthService.Instance;
        _friendsService = FirestoreFriendsService.Instance;
        _messagingService = FirestoreMessagingService.Instance;

        SendMessageCommand = new RelayCommand(
            () => _ = SendMessageAsync(),
            () => SelectedConversation != null && !string.IsNullOrWhiteSpace(DraftMessage));

        _ = LoadFriendsAsync();
    }

    private async Task LoadFriendsAsync()
    {
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return;
        }

        try
        {
            var friendsRaw = await _friendsService.GetFriends(currentUserId);
            var items = friendsRaw
                .Select(d =>
                {
                    string id = d.TryGetValue("userId", out var uid) ? uid?.ToString() ?? string.Empty : string.Empty;
                    string fullName = d.TryGetValue("fullName", out var fn) ? fn?.ToString() ?? string.Empty : string.Empty;
                    string username = d.TryGetValue("username", out var un) ? un?.ToString() ?? string.Empty : string.Empty;
                    string status = d.TryGetValue("status", out var st) ? st?.ToString() ?? "offline" : "offline";
                    string name = string.IsNullOrWhiteSpace(fullName) ? (string.IsNullOrWhiteSpace(username) ? "(Không tên)" : username) : fullName;
                    return new FriendItemViewModel
                    {
                        UserId = id,
                        Name = name,
                        Username = username,
                        Status = status,
                        AvatarText = string.IsNullOrWhiteSpace(name) ? "?" : name.Substring(0, 1).ToUpperInvariant()
                    };
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
                .OrderBy(x => x.Name)
                .ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                _allFriends.Clear();
                _allFriends.AddRange(items);
                ApplyFriendsFilter();

                // Default selection
                SelectedFriend = Friends.FirstOrDefault();
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadFriends failed: {ex.Message}");
        }
    }

    private void ApplyFriendsFilter()
    {
        string needle = (SearchText ?? string.Empty).Trim();

        IEnumerable<FriendItemViewModel> filtered = _allFriends;
        if (!string.IsNullOrWhiteSpace(needle))
        {
            filtered = filtered.Where(f =>
                (!string.IsNullOrWhiteSpace(f.Name) && f.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                || (!string.IsNullOrWhiteSpace(f.Username) && f.Username.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        Friends.Clear();
        foreach (var f in filtered)
        {
            Friends.Add(f);
        }
    }

    private async Task OpenChatWithFriendAsync(FriendItemViewModel? friend)
    {
        if (friend == null) return;

        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        try
        {
            var conversationId = await _messagingService.GetOrCreateConversation(currentUserId, friend.UserId);
            SelectedConversation = new ConversationItemViewModel
            {
                ConversationId = conversationId,
                OtherUserId = friend.UserId,
                Title = friend.Name,
                Subtitle = string.Empty,
                AvatarText = friend.AvatarText
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OpenChatWithFriend failed: {ex.Message}");
        }
    }

    private async Task SwitchConversationAsync(ConversationItemViewModel? conversation)
    {
        _messageListener?.StopAsync();
        _messageListener = null;

        Application.Current.Dispatcher.Invoke(() =>
        {
            Messages.Clear();
            Members.Clear();
        });

        if (conversation == null) return;

        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        // Members (basic for 1-1 chat)
        Application.Current.Dispatcher.Invoke(() =>
        {
            Members.Add(new MemberItemViewModel { Name = "Bạn", AvatarText = "B" });
            if (!string.IsNullOrWhiteSpace(conversation.Title))
            {
                Members.Add(new MemberItemViewModel { Name = conversation.Title, AvatarText = conversation.AvatarText });
            }
        });

        try
        {
            // Initial load
            var raw = await _messagingService.GetMessages(conversation.ConversationId);
            ApplyMessages(raw, currentUserId);

            // Realtime listener
            _messageListener = _messagingService.ListenToMessages(conversation.ConversationId, msgs =>
            {
                if (string.IsNullOrWhiteSpace(_authService.CurrentUserId)) return;
                ApplyMessages(msgs, _authService.CurrentUserId);
            });

            await _messagingService.MarkMessagesAsRead(conversation.ConversationId, currentUserId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SwitchConversation failed: {ex.Message}");
        }
    }

    private void ApplyMessages(List<Dictionary<string, object>> rawMessages, string currentUserId)
    {
        // Sort by timestamp ascending
        var ordered = rawMessages
            .Select(m => new { m, t = ExtractTimestamp(m) })
            .OrderBy(x => x.t)
            .Select(x => x.m)
            .ToList();

        var viewModels = ordered
            .Where(m => m.ContainsKey("senderId") && m.ContainsKey("content"))
            .Select(m =>
            {
                string senderId = m["senderId"]?.ToString() ?? string.Empty;
                string content = m["content"]?.ToString() ?? string.Empty;
                var dt = ExtractTimestamp(m);
                bool outgoing = senderId == currentUserId;

                return new MessageItemViewModel
                {
                    IsOutgoing = outgoing,
                    Text = content,
                    Time = dt == DateTime.MinValue ? DateTime.Now : dt,
                    SenderAvatarText = outgoing ? "B" : IncomingAvatarText
                };
            })
            .ToList();

        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            Messages.Clear();
            foreach (var vm in viewModels)
            {
                Messages.Add(vm);
            }
        });
    }

    private static DateTime ExtractTimestamp(Dictionary<string, object> msg)
    {
        try
        {
            if (msg.TryGetValue("timestamp", out var tsObj) && tsObj != null)
            {
                if (tsObj is Timestamp ts)
                {
                    return ts.ToDateTime().ToLocalTime();
                }
                if (tsObj is DateTime dt)
                {
                    return dt.ToLocalTime();
                }
            }
        }
        catch { }
        return DateTime.MinValue;
    }

    private async Task SendMessageAsync()
    {
        var text = DraftMessage.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        var selected = SelectedConversation;
        if (selected == null) return;

        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        try
        {
            var (success, message) = await _messagingService.SendMessage(selected.ConversationId, currentUserId, text);
            if (!success)
            {
                Console.WriteLine(message);
                return;
            }
            DraftMessage = string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SendMessage failed: {ex.Message}");
        }
    }
}

