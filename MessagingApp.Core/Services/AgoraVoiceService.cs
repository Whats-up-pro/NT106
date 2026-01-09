using System;
using System.Threading.Tasks;
using MessagingApp.Config;

namespace MessagingApp.Services;

/// <summary>
/// Service for managing Agora voice calls.
/// Wraps Agora RTC SDK for voice-only calling functionality.
/// </summary>
public sealed class AgoraVoiceService : IDisposable
{
    private static AgoraVoiceService? _instance;
    public static AgoraVoiceService Instance => _instance ??= new AgoraVoiceService();

    // Agora RTC engine instance - will be initialized after adding Agora SDK
    private object? _rtcEngine; // Type will be IRtcEngine after SDK installation
    
    private string? _currentChannelName;
    private bool _isMuted;
    private bool _isInChannel;

    public event Action<string>? UserJoined;
    public event Action<string>? UserLeft;
    public event Action<bool>? ConnectionStateChanged;
    public event Action<string>? Error;

    private AgoraVoiceService()
    {
        // Constructor will initialize Agora engine after SDK is installed
    }

    /// <summary>
    /// Initialize Agora RTC Engine with App ID.
    /// Call this once before using the service.
    /// </summary>
    public bool Initialize()
    {
        try
        {
            if (_rtcEngine != null)
            {
                Console.WriteLine("Agora engine already initialized.");
                return true;
            }

            // TODO: After installing Agora SDK, uncomment and implement:
            /*
            _rtcEngine = AgoraRtcEngine.CreateAgoraRtcEngine();
            
            RtcEngineContext context = new RtcEngineContext();
            context.appId = AgoraConfig.AppId;
            context.logConfig.filePath = AgoraConfig.LogFilePath;
            context.logConfig.level = LOG_LEVEL.LOG_LEVEL_INFO;
            
            int result = _rtcEngine.Initialize(context);
            if (result != 0)
            {
                Error?.Invoke($"Failed to initialize Agora engine: {result}");
                return false;
            }

            // Register event handlers
            _rtcEngine.OnJoinChannelSuccess = OnJoinChannelSuccessHandler;
            _rtcEngine.OnUserJoined = OnUserJoinedHandler;
            _rtcEngine.OnUserOffline = OnUserOfflineHandler;
            _rtcEngine.OnConnectionStateChanged = OnConnectionStateChangedHandler;
            _rtcEngine.OnError = OnErrorHandler;

            // Enable audio only (disable video)
            _rtcEngine.EnableAudio();
            _rtcEngine.DisableVideo();
            
            Console.WriteLine("Agora engine initialized successfully.");
            */

            Console.WriteLine("Agora SDK not installed yet. Please follow installation steps.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing Agora: {ex.Message}");
            Error?.Invoke($"Initialization error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Join a voice channel. The channel name should be the callId from Firestore.
    /// </summary>
    public async Task<bool> JoinChannelAsync(string channelName, string userId)
    {
        try
        {
            if (_rtcEngine == null)
            {
                Console.WriteLine("Agora engine not initialized. Call Initialize() first.");
                return false;
            }

            if (_isInChannel)
            {
                Console.WriteLine("Already in a channel. Leave first before joining another.");
                return false;
            }

            // TODO: After installing Agora SDK, uncomment:
            /*
            // Use empty token for testing (or implement token generation for production)
            string token = string.Empty;
            
            int result = _rtcEngine.JoinChannel(token, channelName, userId, 0);
            if (result != 0)
            {
                Error?.Invoke($"Failed to join channel: {result}");
                return false;
            }

            _currentChannelName = channelName;
            Console.WriteLine($"Joining channel: {channelName} as user: {userId}");
            */

            await Task.Delay(100); // Simulate async operation
            Console.WriteLine($"[DEMO] Would join channel: {channelName}");
            _currentChannelName = channelName;
            _isInChannel = true;
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error joining channel: {ex.Message}");
            Error?.Invoke($"Join channel error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Leave the current voice channel.
    /// </summary>
    public async Task<bool> LeaveChannelAsync()
    {
        try
        {
            if (_rtcEngine == null || !_isInChannel)
            {
                return true;
            }

            // TODO: After installing Agora SDK, uncomment:
            /*
            int result = _rtcEngine.LeaveChannel();
            if (result != 0)
            {
                Error?.Invoke($"Failed to leave channel: {result}");
                return false;
            }
            */

            await Task.Delay(100);
            Console.WriteLine($"[DEMO] Left channel: {_currentChannelName}");
            
            _currentChannelName = null;
            _isInChannel = false;
            _isMuted = false;

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error leaving channel: {ex.Message}");
            Error?.Invoke($"Leave channel error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Mute or unmute local microphone.
    /// </summary>
    public void SetMicrophoneMuted(bool muted)
    {
        try
        {
            if (_rtcEngine == null)
            {
                return;
            }

            // TODO: After installing Agora SDK, uncomment:
            /*
            int result = _rtcEngine.MuteLocalAudioStream(muted);
            if (result != 0)
            {
                Error?.Invoke($"Failed to mute/unmute: {result}");
                return;
            }
            */

            _isMuted = muted;
            Console.WriteLine($"Microphone {(muted ? "muted" : "unmuted")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting mute state: {ex.Message}");
            Error?.Invoke($"Mute error: {ex.Message}");
        }
    }

    /// <summary>
    /// Adjust playback volume for remote users.
    /// </summary>
    public void SetPlaybackVolume(int volume)
    {
        try
        {
            if (_rtcEngine == null)
            {
                return;
            }

            // Clamp volume to 0-100
            volume = Math.Clamp(volume, 0, 100);

            // TODO: After installing Agora SDK, uncomment:
            /*
            int result = _rtcEngine.AdjustPlaybackSignalVolume(volume);
            if (result != 0)
            {
                Error?.Invoke($"Failed to adjust volume: {result}");
            }
            */

            Console.WriteLine($"Playback volume set to: {volume}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting volume: {ex.Message}");
        }
    }

    public bool IsMuted => _isMuted;
    public bool IsInChannel => _isInChannel;
    public string? CurrentChannelName => _currentChannelName;

    // Event handlers (will be implemented after SDK installation)
    /*
    private void OnJoinChannelSuccessHandler(RtcConnection connection, int elapsed)
    {
        _isInChannel = true;
        Console.WriteLine($"Joined channel successfully: {connection.channelId}");
        ConnectionStateChanged?.Invoke(true);
    }

    private void OnUserJoinedHandler(RtcConnection connection, uint uid, int elapsed)
    {
        Console.WriteLine($"User joined: {uid}");
        UserJoined?.Invoke(uid.ToString());
    }

    private void OnUserOfflineHandler(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
    {
        Console.WriteLine($"User left: {uid}, reason: {reason}");
        UserLeft?.Invoke(uid.ToString());
    }

    private void OnConnectionStateChangedHandler(RtcConnection connection, CONNECTION_STATE_TYPE state, CONNECTION_CHANGED_REASON_TYPE reason)
    {
        Console.WriteLine($"Connection state changed: {state}, reason: {reason}");
        ConnectionStateChanged?.Invoke(state == CONNECTION_STATE_TYPE.CONNECTION_STATE_CONNECTED);
    }

    private void OnErrorHandler(int error, string msg)
    {
        Console.WriteLine($"Agora error: {error} - {msg}");
        Error?.Invoke($"Error {error}: {msg}");
    }
    */

    public void Dispose()
    {
        try
        {
            if (_rtcEngine != null && _isInChannel)
            {
                _ = LeaveChannelAsync();
            }

            // TODO: After installing Agora SDK, uncomment:
            /*
            _rtcEngine?.Dispose();
            */
            
            _rtcEngine = null;
            Console.WriteLine("Agora service disposed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disposing Agora service: {ex.Message}");
        }
    }
}
