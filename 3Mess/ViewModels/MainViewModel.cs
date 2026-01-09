using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Google.Cloud.Firestore;
using MessagingApp.Services;
using ThreeMess.Services;
using ThreeMess.Infrastructure;
using ThreeMess.Models;

namespace ThreeMess.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private static void Forget(Task task) { }

    private static bool ComputeIsOnlineFromUserDoc(Dictionary<string, object> data)
    {
        bool onlineFlag;
        if (data.TryGetValue("isOnline", out var isOnlineObj) && isOnlineObj is bool b)
        {
            onlineFlag = b;
        }
        else if (data.TryGetValue("status", out var statusObj) && statusObj != null)
        {
            onlineFlag = string.Equals(statusObj.ToString(), "online", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            onlineFlag = false;
        }

        // Presence correctness rule:
        // - "Online" requires BOTH an online flag AND a recent lastSeen.
        // - If lastSeen is missing, treat as offline to avoid stuck/incorrect online.
        if (!onlineFlag) return false;

        if (data.TryGetValue("lastSeen", out var lastSeenObj) && lastSeenObj is Timestamp ts)
        {
            var lastSeenUtc = ts.ToDateTime();
            var age = DateTime.UtcNow - lastSeenUtc;
            return age <= TimeSpan.FromSeconds(90);
        }

        return false;
    }
    private readonly FirebaseAuthService _authService;
    private readonly FirestoreFriendsService _friendsService;
    private readonly FirestoreMessagingService _messagingService;
    private readonly FirebaseStorageService _storageService;
    private readonly FirestoreCallingService _callingService;
    private FirestoreChangeListener? _messageListener;
    private FirestoreChangeListener? _typingListener;
    private FirestoreChangeListener? _incomingCallsListener;
    private readonly Dictionary<string, FirestoreChangeListener> _friendStatusListeners = new();

    private object? _selectedSidebarItem;
    private ConversationItemViewModel? _selectedConversation;
    private string _searchText = string.Empty;
    private string _draftMessage = string.Empty;
    private string _incomingAvatarText = "A";
    private bool _showGroups;

    private bool _showRightLinks;

    private bool _isOtherTyping;
    private bool _localIsTyping;
    private DateTime _lastTypingSentUtc = DateTime.MinValue;
    private readonly DispatcherTimer _typingIdleTimer;

    private bool _isFriendFinderMode;
    private bool _isProfileMode;
    private bool _isEditingMyProfile;
    private string _friendFinderSearchText = string.Empty;
    private bool _friendFinderIsBusy;
    private string _friendFinderStatusText = "Nhập email hoặc username để tìm người dùng.";
    private readonly DispatcherTimer _friendFinderSearchTimer;
    private UserSearchResultViewModel? _selectedFriendFinderUser;
    private UserProfileViewModel? _selectedProfile;

    private ImageSource? _currentUserAvatarImage;
    private string _currentUserAvatarText = "U";

    private string _editBio = string.Empty;
    private ImageSource? _editAvatarPreview;
    private ImageSource? _editCoverPreview;
    private string? _editAvatarDataUrl;
    private string? _editCoverDataUrl;

    private Rect _editAvatarViewbox = new(0, 0, 1, 1);
    private Rect _editCoverViewbox = new(0, 0, 1, 1);
    private double _editAvatarImageAspect = 1.0;
    private double _editCoverImageAspect = 1.0;
    private double _editCoverContainerAspect = 16.0 / 9.0;

    private string? _selectedProfileAvatarRaw;
    private string? _selectedProfileCoverRaw;

    private bool _rightImagesExpanded;
    private bool _rightFilesExpanded;

    private bool _isToastVisible;
    private string _toastMessage = string.Empty;
    private Brush _toastAccentBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2563EB"));
    private readonly DispatcherTimer _toastTimer;

    private bool _isConfirmVisible;
    private string _confirmMessage = string.Empty;
    private string _confirmOkText = "OK";
    private string _confirmCancelText = "Hủy";
    private ConfirmKind _pendingConfirmKind = ConfirmKind.None;
    private UserSearchResultViewModel? _pendingConfirmUser;

    private string? _lastSelectedFriendUserId;
    private string? _lastSelectedGroupConversationId;

    private bool _suppressSidebarSelectionHandling;

    private bool _isDarkMode;
    private bool _isActivityStatusEnabled = true;
    private bool _suppressSettingsPersist;

    public ObservableCollection<object> SidebarItems { get; } = new();
    public ObservableCollection<MessageItemViewModel> Messages { get; } = new();
    public ObservableCollection<MemberItemViewModel> Members { get; } = new();

    public ObservableCollection<RightSidebarAttachmentItemViewModel> RightImages { get; } = new();
    public ObservableCollection<RightSidebarAttachmentItemViewModel> RightFiles { get; } = new();
    public ObservableCollection<RightSidebarLinkItemViewModel> RightLinks { get; } = new();

    public ObservableCollection<UserSearchResultViewModel> FriendFinderResults { get; } = new();
    public ObservableCollection<FriendRequestItemViewModel> PendingFriendRequests { get; } = new();

    private readonly List<FriendItemViewModel> _allFriends = new();
    private readonly List<GroupChatItemViewModel> _allGroups = new();
    private readonly HashSet<string> _locallyHiddenMessageIds = new(StringComparer.Ordinal);
    
    private FirestoreChangeListener? _friendRequestsListener;
    private int _pendingFriendRequestsCount;
    private bool _isNotificationPopupOpen;

    private readonly Dictionary<string, List<MessageItemViewModel>> _messageCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTime> _pendingOutgoingByTempId = new(StringComparer.Ordinal);

    private readonly Dictionary<string, (bool pinned, bool muted, bool hidden, DateTime? clearedAtUtc, DateTime? lastActivityUtc)> _conversationSettings = new(StringComparer.Ordinal);

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value)) return;
            ApplySidebarFilter();
        }
    }

    public bool ShowGroups
    {
        get => _showGroups;
        set
        {
            if (!SetProperty(ref _showGroups, value)) return;
            OnPropertyChanged(nameof(ShowFriends));
            ApplySidebarFilter();
        }
    }

    public bool ShowFriends => !ShowGroups;

    public bool ShowRightLinks
    {
        get => _showRightLinks;
        set
        {
            if (!SetProperty(ref _showRightLinks, value)) return;
            OnPropertyChanged(nameof(ShowRightDocs));
        }
    }

    public bool ShowRightDocs => !ShowRightLinks;

    public bool RightImagesExpanded
    {
        get => _rightImagesExpanded;
        set => SetProperty(ref _rightImagesExpanded, value);
    }

    public bool RightFilesExpanded
    {
        get => _rightFilesExpanded;
        set => SetProperty(ref _rightFilesExpanded, value);
    }

    public object? SelectedSidebarItem
    {
        get => _selectedSidebarItem;
        set
        {
            if (!SetProperty(ref _selectedSidebarItem, value)) return;

            if (_suppressSidebarSelectionHandling) return;

            if (value is FriendItemViewModel f)
            {
                IsProfileMode = false;
                _lastSelectedFriendUserId = f.UserId;
                _ = OpenChatWithFriendAsync(f);
            }
            else if (value is GroupChatItemViewModel g)
            {
                IsProfileMode = false;
                _lastSelectedGroupConversationId = g.ConversationId;
                _ = OpenChatWithGroupAsync(g);
            }
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
            ((RelayCommand)StartVoiceCallCommand).RaiseCanExecuteChanged();
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
                Forget(HandleDraftTypingChangedAsync());
            }
        }
    }

    public bool IsOtherTyping
    {
        get => _isOtherTyping;
        private set => SetProperty(ref _isOtherTyping, value);
    }

    public string OtherTypingText => "Đang nhập...";

    public ICommand SendMessageCommand { get; }
    public ICommand RevokeMessageCommand { get; }
    public ICommand HideMessageLocallyCommand { get; }
    public ICommand OpenAddFriendCommand { get; }
    public ICommand OpenCreateGroupCommand { get; }
    public ICommand GoHomeCommand { get; }
    public ICommand ShowFriendsCommand { get; }
    public ICommand ShowGroupsCommand { get; }

    public ICommand ShowRightDocsCommand { get; }
    public ICommand ShowRightLinksCommand { get; }

    public ICommand ToggleRightImagesExpandedCommand { get; }
    public ICommand ToggleRightFilesExpandedCommand { get; }

    public ICommand ToggleFriendFinderModeCommand { get; }
    public ICommand SendFriendFinderRequestCommand { get; }
    public ICommand AcceptFriendFinderRequestCommand { get; }
    public ICommand DeclineFriendFinderRequestCommand { get; }

    public ICommand OpenMyProfileCommand { get; }
    public ICommand BeginEditMyProfileCommand { get; }
    public ICommand CancelEditMyProfileCommand { get; }
    public ICommand SaveMyProfileCommand { get; }
    public ICommand PickMyAvatarCommand { get; }
    public ICommand PickMyCoverCommand { get; }

    public ICommand ConfirmOkCommand { get; }
    public ICommand ConfirmCancelCommand { get; }

    public ICommand DeleteConversationCommand { get; }
    public ICommand TogglePinConversationCommand { get; }
    public ICommand UpdatePinnedCommand { get; }
    public ICommand UpdateNotificationsCommand { get; }

    public ICommand LogoutCommand { get; }
    public ICommand ToggleNotificationPopupCommand { get; }
    public ICommand AcceptFriendRequestFromNotificationCommand { get; }
    public ICommand DeclineFriendRequestFromNotificationCommand { get; }
    public ICommand StartVoiceCallCommand { get; }

    public event Action? LogoutRequested;

    public MainViewModel()
    {
        _authService = FirebaseAuthService.Instance;
        _friendsService = FirestoreFriendsService.Instance;
        _messagingService = FirestoreMessagingService.Instance;
        _storageService = FirebaseStorageService.Instance;
        _callingService = FirestoreCallingService.Instance;

        _typingIdleTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _typingIdleTimer.Tick += (_, _) =>
        {
            _typingIdleTimer.Stop();
            _ = SetLocalTypingAsync(false);
        };

        _friendFinderSearchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _friendFinderSearchTimer.Tick += (_, _) =>
        {
            _friendFinderSearchTimer.Stop();
            Forget(SearchFriendFinderAsync());
        };

        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            IsToastVisible = false;
        };

        SendMessageCommand = new RelayCommand(
            () => _ = SendMessageAsync(),
            () => SelectedConversation != null && !string.IsNullOrWhiteSpace(DraftMessage));

        RevokeMessageCommand = new RelayCommand<MessageItemViewModel>(
            msg => _ = RevokeMessageAsync(msg),
            msg => msg != null
                   && msg.IsOutgoing
                   && !string.IsNullOrWhiteSpace(msg.MessageId)
                   && SelectedConversation != null);

        HideMessageLocallyCommand = new RelayCommand<MessageItemViewModel>(
            msg => _ = HideMessageLocallyAsync(msg),
            msg => msg != null
                   && !msg.IsOutgoing
                   && !string.IsNullOrWhiteSpace(msg.MessageId)
                   && SelectedConversation != null);

        OpenAddFriendCommand = new RelayCommand(OpenAddFriend);

        OpenCreateGroupCommand = new RelayCommand(OpenCreateGroup);

        GoHomeCommand = new RelayCommand(GoHome);

        ShowFriendsCommand = new RelayCommand(() => ShowGroups = false);
        ShowGroupsCommand = new RelayCommand(() => ShowGroups = true);

        ShowRightDocsCommand = new RelayCommand(() => ShowRightLinks = false);
        ShowRightLinksCommand = new RelayCommand(() => ShowRightLinks = true);

        ToggleRightImagesExpandedCommand = new RelayCommand(() => RightImagesExpanded = !RightImagesExpanded);
        ToggleRightFilesExpandedCommand = new RelayCommand(() => RightFilesExpanded = !RightFilesExpanded);

        ToggleFriendFinderModeCommand = new RelayCommand(() => IsFriendFinderMode = !IsFriendFinderMode);
        SendFriendFinderRequestCommand = new RelayCommand<object>(o => Forget(SendFriendRequestAsync(o as UserSearchResultViewModel)), o => o is UserSearchResultViewModel);
        AcceptFriendFinderRequestCommand = new RelayCommand<object>(o => Forget(AcceptFriendFinderRequestAsync(o as UserSearchResultViewModel)), o => o is UserSearchResultViewModel);
        DeclineFriendFinderRequestCommand = new RelayCommand<object>(o => Forget(DeclineFriendFinderRequestAsync(o as UserSearchResultViewModel)), o => o is UserSearchResultViewModel);

        OpenMyProfileCommand = new RelayCommand(() => Forget(OpenMyProfileAsync()));
        BeginEditMyProfileCommand = new RelayCommand(BeginEditMyProfile, () => IsViewingOwnProfile && !IsEditingMyProfile);
        CancelEditMyProfileCommand = new RelayCommand(CancelEditMyProfile, () => IsEditingMyProfile);
        SaveMyProfileCommand = new RelayCommand(() => Forget(SaveMyProfileAsync()), () => IsEditingMyProfile);
        PickMyAvatarCommand = new RelayCommand(() => Forget(PickMyAvatarAsync()), () => IsEditingMyProfile);
        PickMyCoverCommand = new RelayCommand(() => Forget(PickMyCoverAsync()), () => IsEditingMyProfile);

        ConfirmOkCommand = new RelayCommand(() => Forget(ExecuteConfirmOkAsync()), () => IsConfirmVisible);
        ConfirmCancelCommand = new RelayCommand(() => HideConfirm());

        DeleteConversationCommand = new RelayCommand<object>(o => _ = DeleteConversationAsync(o), o => o != null);
        TogglePinConversationCommand = new RelayCommand<object>(o => _ = TogglePinAsync(o), o => o != null);
        UpdatePinnedCommand = new RelayCommand<object>(o => _ = UpdatePinnedAsync(o), o => o != null);
        UpdateNotificationsCommand = new RelayCommand<object>(o => _ = UpdateNotificationsAsync(o), o => o != null);

        LogoutCommand = new RelayCommand(() => Forget(LogoutAsync()));

        ToggleNotificationPopupCommand = new RelayCommand(() => IsNotificationPopupOpen = !IsNotificationPopupOpen);
        AcceptFriendRequestFromNotificationCommand = new RelayCommand<object>(o => Forget(AcceptFriendRequestFromNotificationAsync(o as FriendRequestItemViewModel)), o => o is FriendRequestItemViewModel);
        DeclineFriendRequestFromNotificationCommand = new RelayCommand<object>(o => Forget(DeclineFriendRequestFromNotificationAsync(o as FriendRequestItemViewModel)), o => o is FriendRequestItemViewModel);
        StartVoiceCallCommand = new RelayCommand(() => Forget(StartCallAsync()), () => SelectedConversation != null);

        _ = LoadFriendsAsync();
        _ = LoadGroupsAsync();
        _ = StartFriendRequestsListenerAsync();
        _ = StartIncomingCallsListenerAsync();

        // Best-effort: preload current user's avatar for the top-right button.
        Forget(RefreshCurrentUserAvatarAsync());

        // Best-effort: load persisted settings (theme + presence visibility).
        Forget(RefreshCurrentUserSettingsAsync());
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (!SetProperty(ref _isDarkMode, value)) return;
            ApplyThemeFromSettings();
            if (!_suppressSettingsPersist) Forget(PersistCurrentUserSettingsAsync());
        }
    }

    public bool IsActivityStatusEnabled
    {
        get => _isActivityStatusEnabled;
        set
        {
            if (!SetProperty(ref _isActivityStatusEnabled, value)) return;
            if (!_suppressSettingsPersist) Forget(PersistCurrentUserSettingsAsync());

            // Immediately reflect the toggle in Firestore presence.
            Forget(UpdatePresenceAsync(true));
        }
    }

    public int PendingFriendRequestsCount
    {
        get => _pendingFriendRequestsCount;
        private set => SetProperty(ref _pendingFriendRequestsCount, value);
    }

    public bool IsNotificationPopupOpen
    {
        get => _isNotificationPopupOpen;
        set => SetProperty(ref _isNotificationPopupOpen, value);
    }

    public bool HasPendingFriendRequests => PendingFriendRequestsCount > 0;

    private void ApplyThemeFromSettings()
    {
        try
        {
            ThemeManager.ApplyTheme(IsDarkMode ? ThemeManager.ThemeMode.Dark : ThemeManager.ThemeMode.Light);
        }
        catch
        {
            // ignore
        }
    }

    private async Task RefreshCurrentUserSettingsAsync()
    {
        try
        {
            string? currentUserId = _authService.CurrentUserId;
            if (string.IsNullOrWhiteSpace(currentUserId)) return;

            var data = await _friendsService.GetUserAsync(currentUserId);
            if (data == null) return;

            bool isDark = false;
            if (data.TryGetValue("theme", out var themeObj) && themeObj != null)
            {
                isDark = string.Equals(themeObj.ToString(), "dark", StringComparison.OrdinalIgnoreCase);
            }

            bool showActivity = true;
            if (data.TryGetValue("showOnlineStatus", out var showObj) && showObj is bool b)
            {
                showActivity = b;
            }

            _suppressSettingsPersist = true;
            try
            {
                IsDarkMode = isDark;
                IsActivityStatusEnabled = showActivity;
            }
            finally
            {
                _suppressSettingsPersist = false;
            }
        }
        catch
        {
            // ignore
        }
    }

    private async Task PersistCurrentUserSettingsAsync()
    {
        try
        {
            string? currentUserId = _authService.CurrentUserId;
            if (string.IsNullOrWhiteSpace(currentUserId)) return;

            await _friendsService.UpdateUserSettingsAsync(
                currentUserId,
                theme: IsDarkMode ? "dark" : "light",
                showOnlineStatus: IsActivityStatusEnabled);
        }
        catch
        {
            // ignore
        }
    }

    private async Task LogoutAsync()
    {
        try
        {
            await UpdatePresenceAsync(false);
        }
        catch { }

        try
        {
            await _authService.SignOut();
        }
        catch { }

        try
        {
            Application.Current.Dispatcher.Invoke(() => LogoutRequested?.Invoke());
        }
        catch
        {
            LogoutRequested?.Invoke();
        }
    }

    private enum ConfirmKind
    {
        None = 0,
        Unfriend = 1,
        CancelOutgoingRequest = 2
    }

    public bool IsToastVisible
    {
        get => _isToastVisible;
        private set => SetProperty(ref _isToastVisible, value);
    }

    public string ToastMessage
    {
        get => _toastMessage;
        private set => SetProperty(ref _toastMessage, value);
    }

    public Brush ToastAccentBrush
    {
        get => _toastAccentBrush;
        private set => SetProperty(ref _toastAccentBrush, value);
    }

    public bool IsConfirmVisible
    {
        get => _isConfirmVisible;
        private set
        {
            if (SetProperty(ref _isConfirmVisible, value))
            {
                try { ((RelayCommand)ConfirmOkCommand).RaiseCanExecuteChanged(); } catch { }
            }
        }
    }

    public string ConfirmMessage
    {
        get => _confirmMessage;
        private set => SetProperty(ref _confirmMessage, value);
    }

    public string ConfirmOkText
    {
        get => _confirmOkText;
        private set => SetProperty(ref _confirmOkText, value);
    }

    public string ConfirmCancelText
    {
        get => _confirmCancelText;
        private set => SetProperty(ref _confirmCancelText, value);
    }

    private void ShowToast(string message, string kind = "info")
    {
        ToastMessage = message;

        // Reuse existing palette colors already used in the app.
        var accentHex = kind switch
        {
            "success" => "#FF10B981",
            "error" => "#FFEF4444",
            "warn" => "#FFEF4444",
            _ => "#FF2563EB"
        };

        try
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentHex));
            brush.Freeze();
            ToastAccentBrush = brush;
        }
        catch { }

        IsToastVisible = true;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void ShowConfirm(ConfirmKind kind, UserSearchResultViewModel user, string message, string okText = "OK", string cancelText = "Hủy")
    {
        _pendingConfirmKind = kind;
        _pendingConfirmUser = user;
        ConfirmMessage = message;
        ConfirmOkText = okText;
        ConfirmCancelText = cancelText;
        IsConfirmVisible = true;
    }

    private void HideConfirm()
    {
        IsConfirmVisible = false;
        _pendingConfirmKind = ConfirmKind.None;
        _pendingConfirmUser = null;
    }

    private async Task ExecuteConfirmOkAsync()
    {
        var user = _pendingConfirmUser;
        var kind = _pendingConfirmKind;
        HideConfirm();

        if (user == null) return;
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            ShowToast("Bạn chưa đăng nhập.", "error");
            return;
        }

        try
        {
            if (kind == ConfirmKind.Unfriend)
            {
                var (success, message) = await _friendsService.UnfriendAsync(currentUserId, user.UserId);
                if (success)
                {
                    user.IsFriend = false;
                    user.IsRequestPending = false;
                    try { await LoadFriendsAsync(); } catch { }
                    ShowToast(message, "success");
                }
                else
                {
                    ShowToast(message, "error");
                }
            }
            else if (kind == ConfirmKind.CancelOutgoingRequest)
            {
                var (success, message) = await _friendsService.CancelFriendRequest(currentUserId, user.UserId);
                if (success)
                {
                    user.IsRequestPending = false;
                    ShowToast(message, "success");
                }
                else
                {
                    ShowToast(message, "error");
                }
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Lỗi: {ex.Message}", "error");
        }
    }

    public bool IsFriendFinderMode
    {
        get => _isFriendFinderMode;
        set
        {
            if (SetProperty(ref _isFriendFinderMode, value))
            {
                if (_isFriendFinderMode)
                {
                    IsProfileMode = false;
                    FriendFinderSearchText = string.Empty;
                    FriendFinderResults.Clear();
                    SelectedFriendFinderUser = null;
                    SelectedProfile = null;
                    FriendFinderStatusText = "Nhập email hoặc username để tìm người dùng.";
                }
            }
        }
    }

    private void GoHome()
    {
        // Return to the main chat UI from any alternate mode.
        if (IsEditingMyProfile)
        {
            try { CancelEditMyProfile(); } catch { }
        }

        IsFriendFinderMode = false;
        IsProfileMode = false;

        // Clear any profile/friend-finder context so the center panel doesn't stay in profile mode.
        SelectedFriendFinderUser = null;
        SelectedProfile = null;
    }

    public bool IsProfileMode
    {
        get => _isProfileMode;
        set
        {
            if (SetProperty(ref _isProfileMode, value))
            {
                if (_isProfileMode)
                {
                    // Keep friend-finder UI off when user explicitly opens their own profile.
                    IsFriendFinderMode = false;
                }
                else
                {
                    IsEditingMyProfile = false;
                }
            }
        }
    }

    public bool IsViewingOwnProfile
    {
        get
        {
            var currentUserId = _authService.CurrentUserId;
            var profileUserId = SelectedProfile?.UserId;
            return !string.IsNullOrWhiteSpace(currentUserId)
                   && !string.IsNullOrWhiteSpace(profileUserId)
                   && string.Equals(currentUserId, profileUserId, StringComparison.Ordinal);
        }
    }

    public bool IsEditingMyProfile
    {
        get => _isEditingMyProfile;
        private set
        {
            if (SetProperty(ref _isEditingMyProfile, value))
            {
                try { ((RelayCommand)BeginEditMyProfileCommand).RaiseCanExecuteChanged(); } catch { }
                try { ((RelayCommand)CancelEditMyProfileCommand).RaiseCanExecuteChanged(); } catch { }
                try { ((RelayCommand)SaveMyProfileCommand).RaiseCanExecuteChanged(); } catch { }
                try { ((RelayCommand)PickMyAvatarCommand).RaiseCanExecuteChanged(); } catch { }
                try { ((RelayCommand)PickMyCoverCommand).RaiseCanExecuteChanged(); } catch { }
            }
        }
    }

    public ImageSource? CurrentUserAvatarImage
    {
        get => _currentUserAvatarImage;
        private set => SetProperty(ref _currentUserAvatarImage, value);
    }

    public string CurrentUserAvatarText
    {
        get => _currentUserAvatarText;
        private set => SetProperty(ref _currentUserAvatarText, value);
    }

    public ImageSource? EditAvatarPreview
    {
        get => _editAvatarPreview;
        private set => SetProperty(ref _editAvatarPreview, value);
    }

    public ImageSource? EditCoverPreview
    {
        get => _editCoverPreview;
        private set => SetProperty(ref _editCoverPreview, value);
    }

    public Rect EditAvatarViewbox
    {
        get => _editAvatarViewbox;
        set => SetProperty(ref _editAvatarViewbox, ClampViewbox(value));
    }

    public Rect EditCoverViewbox
    {
        get => _editCoverViewbox;
        set => SetProperty(ref _editCoverViewbox, ClampViewbox(value));
    }

    public void UpdateEditCoverContainerSize(double width, double height)
    {
        if (width <= 1 || height <= 1) return;
        var aspect = width / height;
        if (double.IsNaN(aspect) || double.IsInfinity(aspect) || aspect <= 0) return;

        _editCoverContainerAspect = aspect;
        if (!IsEditingMyProfile) return;

        // Recompute viewbox (keep current center) when container aspect changes.
        RecomputeCoverViewbox(keepCenter: true);
    }

    public string EditBio
    {
        get => _editBio;
        set => SetProperty(ref _editBio, value);
    }

    public string FriendFinderSearchText
    {
        get => _friendFinderSearchText;
        set
        {
            if (SetProperty(ref _friendFinderSearchText, value))
            {
                if (!_isFriendFinderMode) return;
                _friendFinderSearchTimer.Stop();
                _friendFinderSearchTimer.Start();
            }
        }
    }

    public bool FriendFinderIsBusy
    {
        get => _friendFinderIsBusy;
        private set => SetProperty(ref _friendFinderIsBusy, value);
    }

    public string FriendFinderStatusText
    {
        get => _friendFinderStatusText;
        private set => SetProperty(ref _friendFinderStatusText, value);
    }

    public UserSearchResultViewModel? SelectedFriendFinderUser
    {
        get => _selectedFriendFinderUser;
        set
        {
            if (SetProperty(ref _selectedFriendFinderUser, value))
            {
                if (value != null)
                {
                    Forget(LoadProfileAsync(value.UserId));
                }
            }
        }
    }

    public UserProfileViewModel? SelectedProfile
    {
        get => _selectedProfile;
        private set => SetProperty(ref _selectedProfile, value);
    }

    private async Task OpenMyProfileAsync()
    {
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            ShowToast("Bạn chưa đăng nhập.", "error");
            return;
        }

        IsProfileMode = true;
        SelectedFriendFinderUser = null;
        await LoadProfileAsync(currentUserId);
    }

    private void BeginEditMyProfile()
    {
        if (!IsViewingOwnProfile || SelectedProfile == null) return;

        IsEditingMyProfile = true;
        EditBio = SelectedProfile.About ?? string.Empty;
        EditAvatarPreview = SelectedProfile.AvatarImage;
        EditCoverPreview = SelectedProfile.CoverImage;

        _editAvatarDataUrl = null;
        _editCoverDataUrl = null;

        // Initialize viewboxes (for drag-to-position).
        _editAvatarImageAspect = TryGetAspectFromImageSource(EditAvatarPreview) ?? 1.0;
        _editCoverImageAspect = TryGetAspectFromImageSource(EditCoverPreview) ?? 1.0;
        EditAvatarViewbox = ComputeViewbox(_editAvatarImageAspect, containerAspect: 1.0);
        EditCoverViewbox = ComputeViewbox(_editCoverImageAspect, _editCoverContainerAspect);
    }

    private void CancelEditMyProfile()
    {
        IsEditingMyProfile = false;
        _editAvatarDataUrl = null;
        _editCoverDataUrl = null;

        // revert previews to current loaded profile
        if (SelectedProfile != null)
        {
            EditBio = SelectedProfile.About ?? string.Empty;
            EditAvatarPreview = SelectedProfile.AvatarImage;
            EditCoverPreview = SelectedProfile.CoverImage;

            _editAvatarImageAspect = TryGetAspectFromImageSource(EditAvatarPreview) ?? 1.0;
            _editCoverImageAspect = TryGetAspectFromImageSource(EditCoverPreview) ?? 1.0;
            EditAvatarViewbox = ComputeViewbox(_editAvatarImageAspect, containerAspect: 1.0);
            EditCoverViewbox = ComputeViewbox(_editCoverImageAspect, _editCoverContainerAspect);
        }
    }

    private async Task SaveMyProfileAsync()
    {
        if (!IsViewingOwnProfile) return;
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            ShowToast("Bạn chưa đăng nhập.", "error");
            return;
        }

        try
        {
            // If we're editing, crop according to the current drag-selected viewbox.
            var avatarSource = _editAvatarDataUrl ?? _selectedProfileAvatarRaw;
            var coverSource = _editCoverDataUrl ?? _selectedProfileCoverRaw;

            var avatarToSave = TryCropAndEncodeIfDataUrl(avatarSource, EditAvatarViewbox, maxPixels: 256, qualityLevel: 85) ?? avatarSource;
            var coverToSave = TryCropAndEncodeIfDataUrl(coverSource, EditCoverViewbox, maxPixels: 1280, qualityLevel: 85) ?? coverSource;

            var bioToSave = (EditBio ?? string.Empty).Trim();

            var (success, message) = await _friendsService.UpdateUserProfileAsync(currentUserId, avatarToSave, coverToSave, bioToSave);
            ShowToast(message, success ? "success" : "error");
            if (!success) return;

            IsEditingMyProfile = false;

            // Refresh profile + top-right avatar from server.
            await LoadProfileAsync(currentUserId);
            await RefreshCurrentUserAvatarAsync();
        }
        catch (Exception ex)
        {
            ShowToast($"Lỗi: {ex.Message}", "error");
        }
    }

    private async Task PickMyAvatarAsync()
    {
        var path = PickImageFilePath();
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            var dataUrl = await Task.Run(() => EncodeImageToJpegDataUrl(path, maxPixels: 256, qualityLevel: 85));
            if (string.IsNullOrWhiteSpace(dataUrl))
            {
                ShowToast("Không thể đọc ảnh.", "error");
                return;
            }

            _editAvatarDataUrl = dataUrl;
            EditAvatarPreview = TryDecodeDataUrlOrUriToImageSource(dataUrl);

            _editAvatarImageAspect = TryGetAspectFromImageSource(EditAvatarPreview) ?? 1.0;
            EditAvatarViewbox = ComputeViewbox(_editAvatarImageAspect, containerAspect: 1.0);
        }
        catch (Exception ex)
        {
            ShowToast($"Lỗi: {ex.Message}", "error");
        }
    }

    private async Task PickMyCoverAsync()
    {
        var path = PickImageFilePath();
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            var dataUrl = await Task.Run(() => EncodeImageToJpegDataUrl(path, maxPixels: 1280, qualityLevel: 85));
            if (string.IsNullOrWhiteSpace(dataUrl))
            {
                ShowToast("Không thể đọc ảnh.", "error");
                return;
            }

            _editCoverDataUrl = dataUrl;
            EditCoverPreview = TryDecodeDataUrlOrUriToImageSource(dataUrl);

            _editCoverImageAspect = TryGetAspectFromImageSource(EditCoverPreview) ?? 1.0;
            EditCoverViewbox = ComputeViewbox(_editCoverImageAspect, _editCoverContainerAspect);
        }
        catch (Exception ex)
        {
            ShowToast($"Lỗi: {ex.Message}", "error");
        }
    }

    private static string? PickImageFilePath()
    {
        try
        {
            var dlg = new OpenFileDialog
            {
                Title = "Chọn ảnh",
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.webp;*.bmp;*.gif|All files|*.*",
                Multiselect = false
            };

            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? EncodeImageToJpegDataUrl(string filePath, int maxPixels, int qualityLevel)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return null;

        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.CreateOptions = BitmapCreateOptions.PreservePixelFormat;

        // Downscale by bounding box: cap the larger dimension to maxPixels.
        bmp.UriSource = new Uri(filePath, UriKind.Absolute);
        bmp.DecodePixelWidth = maxPixels;
        bmp.EndInit();
        bmp.Freeze();

        var encoder = new JpegBitmapEncoder { QualityLevel = Math.Clamp(qualityLevel, 30, 95) };
        encoder.Frames.Add(BitmapFrame.Create(bmp));

        using var ms = new MemoryStream();
        encoder.Save(ms);
        var base64 = Convert.ToBase64String(ms.ToArray());
        return "data:image/jpeg;base64," + base64;
    }

    private static double? TryGetAspectFromImageSource(ImageSource? src)
    {
        try
        {
            if (src is BitmapSource bs)
            {
                if (bs.PixelWidth <= 0 || bs.PixelHeight <= 0) return null;
                return bs.PixelWidth / (double)bs.PixelHeight;
            }
        }
        catch { }
        return null;
    }

    private static Rect ClampViewbox(Rect vb)
    {
        double w = vb.Width <= 0 ? 1 : vb.Width;
        double h = vb.Height <= 0 ? 1 : vb.Height;
        if (w > 1) w = 1;
        if (h > 1) h = 1;

        double x = vb.X;
        double y = vb.Y;
        if (x < 0) x = 0;
        if (y < 0) y = 0;
        if (x > 1 - w) x = 1 - w;
        if (y > 1 - h) y = 1 - h;
        return new Rect(x, y, w, h);
    }

    private static Rect ComputeViewbox(double imageAspect, double containerAspect)
    {
        if (imageAspect <= 0 || containerAspect <= 0)
        {
            return new Rect(0, 0, 1, 1);
        }

        // UniformToFill-style crop: keep the largest possible portion without empty bars.
        if (imageAspect > containerAspect)
        {
            // Image is wider -> crop left/right
            double fracW = containerAspect / imageAspect;
            if (fracW > 1) fracW = 1;
            double x = (1 - fracW) / 2;
            return new Rect(x, 0, fracW, 1);
        }

        // Image is taller -> crop top/bottom
        double fracH = imageAspect / containerAspect;
        if (fracH > 1) fracH = 1;
        double y = (1 - fracH) / 2;
        return new Rect(0, y, 1, fracH);
    }

    private void RecomputeCoverViewbox(bool keepCenter)
    {
        var current = EditCoverViewbox;
        var next = ComputeViewbox(_editCoverImageAspect, _editCoverContainerAspect);

        if (!keepCenter)
        {
            EditCoverViewbox = next;
            return;
        }

        double cx = current.X + current.Width / 2;
        double cy = current.Y + current.Height / 2;
        double nx = cx - next.Width / 2;
        double ny = cy - next.Height / 2;
        EditCoverViewbox = ClampViewbox(new Rect(nx, ny, next.Width, next.Height));
    }

    private static string? TryCropAndEncodeIfDataUrl(string? dataUrl, Rect viewbox, int maxPixels, int qualityLevel)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dataUrl)) return null;
            if (!dataUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return null;
            if (viewbox.Width <= 0 || viewbox.Height <= 0) return null;

            var bmp = DecodeDataUrlToBitmapSource(dataUrl);
            if (bmp == null) return null;

            // Crop in pixel space
            int pw = bmp.PixelWidth;
            int ph = bmp.PixelHeight;
            int x = (int)Math.Round(viewbox.X * pw);
            int y = (int)Math.Round(viewbox.Y * ph);
            int w = (int)Math.Round(viewbox.Width * pw);
            int h = (int)Math.Round(viewbox.Height * ph);

            if (w < 1) w = 1;
            if (h < 1) h = 1;
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (x + w > pw) w = pw - x;
            if (y + h > ph) h = ph - y;
            if (w < 1 || h < 1) return null;

            var cropped = new CroppedBitmap(bmp, new Int32Rect(x, y, w, h));
            cropped.Freeze();

            var resized = ResizeBitmapMaxPixels(cropped, maxPixels);
            return EncodeBitmapToJpegDataUrl(resized, qualityLevel);
        }
        catch
        {
            return null;
        }
    }

    private static BitmapSource? DecodeDataUrlToBitmapSource(string dataUrl)
    {
        try
        {
            int idx = dataUrl.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            string b64 = dataUrl[(idx + "base64,".Length)..];
            var bytes = Convert.FromBase64String(b64);
            using var ms = new MemoryStream(bytes);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    private static BitmapSource ResizeBitmapMaxPixels(BitmapSource src, int maxPixels)
    {
        if (src.PixelWidth <= 0 || src.PixelHeight <= 0) return src;
        int maxDim = Math.Max(src.PixelWidth, src.PixelHeight);
        if (maxDim <= maxPixels) return src;

        double scale = maxPixels / (double)maxDim;
        var tb = new TransformedBitmap(src, new ScaleTransform(scale, scale));
        tb.Freeze();
        return tb;
    }

    private static string? EncodeBitmapToJpegDataUrl(BitmapSource bmp, int qualityLevel)
    {
        try
        {
            var encoder = new JpegBitmapEncoder { QualityLevel = Math.Clamp(qualityLevel, 30, 95) };
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            return "data:image/jpeg;base64," + Convert.ToBase64String(ms.ToArray());
        }
        catch
        {
            return null;
        }
    }

    private async Task RefreshCurrentUserAvatarAsync()
    {
        try
        {
            string? currentUserId = _authService.CurrentUserId;
            if (string.IsNullOrWhiteSpace(currentUserId)) return;

            var data = await _friendsService.GetUserAsync(currentUserId);
            if (data == null) return;

            string display = TryGetString(data, "fullName", "username", "email");
            if (string.IsNullOrWhiteSpace(display)) display = "User";

            string avatarValue = TryGetString(data, "avatarDataUrl", "avatar", "avatarUrl", "photoUrl");
            var avatarImg = TryDecodeDataUrlOrUriToImageSource(avatarValue);

            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentUserAvatarText = string.IsNullOrWhiteSpace(display) ? "U" : display.Substring(0, 1).ToUpperInvariant();
                CurrentUserAvatarImage = avatarImg;
            });
        }
        catch
        {
            // ignore
        }
    }

    private async Task SearchFriendFinderAsync()
    {
        if (!_isFriendFinderMode) return;
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            FriendFinderStatusText = "Bạn chưa đăng nhập.";
            return;
        }

        string query = (FriendFinderSearchText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            FriendFinderResults.Clear();
            SelectedFriendFinderUser = null;
            SelectedProfile = null;
            FriendFinderStatusText = "Nhập email hoặc username để tìm người dùng.";
            return;
        }

        try
        {
            FriendFinderIsBusy = true;
            FriendFinderStatusText = "Đang tìm kiếm...";

            var raw = await _friendsService.SearchUsers(query, currentUserId);
            var friendIds = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                foreach (var f in _allFriends)
                {
                    if (!string.IsNullOrWhiteSpace(f?.UserId))
                        friendIds.Add(f.UserId);
                }
            }
            catch { }

            HashSet<string> outgoingPendingToIds = new(StringComparer.Ordinal);
            try
            {
                outgoingPendingToIds = await _friendsService.GetOutgoingPendingRequestToUserIdsAsync(currentUserId);
            }
            catch { }

            Dictionary<string, string> incomingPendingFromToRequestId = new(StringComparer.Ordinal);
            try
            {
                incomingPendingFromToRequestId = await _friendsService.GetIncomingPendingRequestFromUserIdToRequestIdAsync(currentUserId);
            }
            catch { }

            var mapped = raw
                .Select(d =>
                {
                    string id = d.TryGetValue("userId", out var uid) ? uid?.ToString() ?? string.Empty : string.Empty;
                    string email = d.TryGetValue("email", out var em) ? em?.ToString() ?? string.Empty : string.Empty;
                    string username = d.TryGetValue("username", out var un) ? un?.ToString() ?? string.Empty : string.Empty;
                    string fullName = d.TryGetValue("fullName", out var fn) ? fn?.ToString() ?? string.Empty : string.Empty;

                    string display = string.IsNullOrWhiteSpace(fullName)
                        ? (string.IsNullOrWhiteSpace(username) ? (string.IsNullOrWhiteSpace(email) ? "(Không tên)" : email) : username)
                        : fullName;

                    string subtitle = !string.IsNullOrWhiteSpace(username)
                        ? $"@{username}" + (!string.IsNullOrWhiteSpace(email) ? $" • {email}" : "")
                        : email;

                    return new UserSearchResultViewModel
                    {
                        UserId = id,
                        DisplayName = display,
                        Subtitle = subtitle,
                        AvatarText = string.IsNullOrWhiteSpace(display) ? "?" : display.Substring(0, 1).ToUpperInvariant(),
                        IsSelf = string.Equals(id, currentUserId, StringComparison.Ordinal),
                        IsFriend = !string.IsNullOrWhiteSpace(id) && !string.Equals(id, currentUserId, StringComparison.Ordinal) && friendIds.Contains(id),
                        IsIncomingRequestPending = !string.IsNullOrWhiteSpace(id)
                                                   && !string.Equals(id, currentUserId, StringComparison.Ordinal)
                                                   && incomingPendingFromToRequestId.ContainsKey(id),
                        IncomingRequestId = !string.IsNullOrWhiteSpace(id) && incomingPendingFromToRequestId.TryGetValue(id, out var rid) ? rid : string.Empty,
                        IsRequestPending = !string.IsNullOrWhiteSpace(id)
                                          && !string.Equals(id, currentUserId, StringComparison.Ordinal)
                                          && !friendIds.Contains(id)
                                          && !incomingPendingFromToRequestId.ContainsKey(id)
                                          && outgoingPendingToIds.Contains(id)
                    };
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
                .ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                FriendFinderResults.Clear();
                foreach (var r in mapped)
                {
                    FriendFinderResults.Add(r);
                }
                SelectedFriendFinderUser = FriendFinderResults.FirstOrDefault();
            });

            FriendFinderStatusText = FriendFinderResults.Count == 0 ? "Không tìm thấy người dùng." : $"Tìm thấy {FriendFinderResults.Count} người dùng.";
        }
        catch (Exception ex)
        {
            FriendFinderStatusText = $"Lỗi khi tìm kiếm: {ex.Message}";
        }
        finally
        {
            FriendFinderIsBusy = false;
        }
    }

    private async Task SendFriendRequestAsync(UserSearchResultViewModel? user)
    {
        if (user == null) return;
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            ShowToast("Bạn chưa đăng nhập.", "error");
            return;
        }

        if (user.IsSelf)
        {
            return;
        }

        if (user.IsFriend)
        {
            ShowConfirm(ConfirmKind.Unfriend, user, "Bạn muốn hủy kết bạn với người này?", "OK", "Hủy");
            return;
        }

        if (user.IsRequestPending)
        {
            ShowConfirm(ConfirmKind.CancelOutgoingRequest, user, "Bạn muốn hủy gửi lời mời kết bạn?", "OK", "Hủy");
            return;
        }

        if (user.IsIncomingRequestPending)
        {
            ShowToast("Người này đã gửi lời mời kết bạn cho bạn. Hãy chọn Chấp nhận/Từ chối.", "info");
            return;
        }

        try
        {
            var (success, message) = await _friendsService.SendFriendRequest(currentUserId, user.UserId);
            if (success)
            {
                user.IsRequestPending = true;
            }
            ShowToast(message, success ? "success" : "error");
        }
        catch (Exception ex)
        {
            ShowToast($"Lỗi: {ex.Message}", "error");
        }
    }

    private async Task AcceptFriendFinderRequestAsync(UserSearchResultViewModel? user)
    {
        if (user == null) return;
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            ShowToast("Bạn chưa đăng nhập.", "error");
            return;
        }

        if (!user.IsIncomingRequestPending || string.IsNullOrWhiteSpace(user.IncomingRequestId))
            return;

        try
        {
            var (success, message) = await _friendsService.AcceptFriendRequest(user.IncomingRequestId, user.UserId, currentUserId);
            ShowToast(message, success ? "success" : "error");
            if (success)
            {
                user.IsIncomingRequestPending = false;
                user.IncomingRequestId = string.Empty;
                user.IsFriend = true;
                user.IsRequestPending = false;
                try { await LoadFriendsAsync(); } catch { }
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Lỗi: {ex.Message}", "error");
        }
    }

    private async Task DeclineFriendFinderRequestAsync(UserSearchResultViewModel? user)
    {
        if (user == null) return;
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            ShowToast("Bạn chưa đăng nhập.", "error");
            return;
        }

        if (!user.IsIncomingRequestPending || string.IsNullOrWhiteSpace(user.IncomingRequestId))
            return;

        try
        {
            var (success, message) = await _friendsService.DeclineFriendRequest(user.IncomingRequestId);
            ShowToast(message, success ? "success" : "error");
            if (success)
            {
                user.IsIncomingRequestPending = false;
                user.IncomingRequestId = string.Empty;
                user.IsFriend = false;
                user.IsRequestPending = false;
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Lỗi: {ex.Message}", "error");
        }
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

    private static ImageSource? TryDecodeDataUrlOrUriToImageSource(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return TryDecodeDataUrlToImageSource(value);
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = uri;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { }
        }

        return null;
    }

    private async Task LoadProfileAsync(string userId)
    {
        if (!_isFriendFinderMode && !_isProfileMode) return;
        if (string.IsNullOrWhiteSpace(userId)) return;

        var profile = SelectedProfile;
        if (profile == null || !string.Equals(profile.UserId, userId, StringComparison.Ordinal))
        {
            profile = new UserProfileViewModel { UserId = userId, StatusText = "Đang tải...", IsBusy = true };
            SelectedProfile = profile;
        }

        try
        {
            profile.IsBusy = true;
            profile.StatusText = "Đang tải...";

            var data = await _friendsService.GetUserAsync(userId);
            if (data == null)
            {
                profile.StatusText = "Không tìm thấy người dùng.";
                return;
            }

            string email = TryGetString(data, "email");
            string username = TryGetString(data, "username");
            string fullName = TryGetString(data, "fullName");
            string display = string.IsNullOrWhiteSpace(fullName)
                ? (string.IsNullOrWhiteSpace(username) ? (string.IsNullOrWhiteSpace(email) ? "(Không tên)" : email) : username)
                : fullName;

            string subtitle = !string.IsNullOrWhiteSpace(username)
                ? $"@{username}" + (!string.IsNullOrWhiteSpace(email) ? $" • {email}" : "")
                : email;

            string about = TryGetString(data, "bio", "about", "description");
            string avatarValue = TryGetString(data, "avatarDataUrl", "avatar", "avatarUrl", "photoUrl");
            string coverValue = TryGetString(data, "coverDataUrl", "coverPhotoDataUrl", "coverUrl", "cover");

            _selectedProfileAvatarRaw = avatarValue;
            _selectedProfileCoverRaw = coverValue;

            var avatarImg = TryDecodeDataUrlOrUriToImageSource(avatarValue);
            var coverImg = TryDecodeDataUrlOrUriToImageSource(coverValue);

            int friendsCount = await _friendsService.GetFriendsCountAsync(userId);

            Application.Current.Dispatcher.Invoke(() =>
            {
                profile.DisplayName = display;
                profile.Subtitle = subtitle;
                profile.About = string.IsNullOrWhiteSpace(about) ? "" : about;
                profile.FriendsCount = friendsCount;
                profile.AvatarText = string.IsNullOrWhiteSpace(display) ? "?" : display.Substring(0, 1).ToUpperInvariant();
                profile.AvatarImage = avatarImg;
                profile.CoverImage = coverImg;
                profile.StatusText = string.Empty;
            });

            // Notify dependent UI bits (self edit button visibility).
            OnPropertyChanged(nameof(IsViewingOwnProfile));
            try { ((RelayCommand)BeginEditMyProfileCommand).RaiseCanExecuteChanged(); } catch { }
        }
        catch (Exception ex)
        {
            profile.StatusText = $"Lỗi: {ex.Message}";
        }
        finally
        {
            profile.IsBusy = false;
        }
    }

    public async Task UpdatePresenceAsync(bool isOnline)
    {
        try
        {
            string? currentUserId = _authService.CurrentUserId;
            if (string.IsNullOrWhiteSpace(currentUserId)) return;
            // If user disabled activity status, always publish offline even while active.
            bool publishOnline = isOnline && IsActivityStatusEnabled;
            await _friendsService.UpdatePresenceAsync(currentUserId, publishOnline);
        }
        catch
        {
            // ignore
        }
    }

    private async Task SyncFriendStatusListenersAsync()
    {
        try
        {
            var currentIds = _allFriends
                .Select(f => f.UserId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet(StringComparer.Ordinal);

            // Remove listeners for users no longer in the list
            var toRemove = _friendStatusListeners.Keys.Where(id => !currentIds.Contains(id)).ToList();
            foreach (var id in toRemove)
            {
                try { await _friendStatusListeners[id].StopAsync(); } catch { }
                _friendStatusListeners.Remove(id);
            }

            // Add listeners for new friends
            foreach (var id in currentIds)
            {
                if (_friendStatusListeners.ContainsKey(id)) continue;

                var listener = _friendsService.ListenToUserStatus(id, () =>
                {
                    Forget(RefreshFriendPresenceAsync(id));
                });
                _friendStatusListeners[id] = listener;
                Forget(RefreshFriendPresenceAsync(id));
            }
        }
        catch
        {
            // ignore
        }
    }

    private async Task RefreshFriendPresenceAsync(string userId)
    {
        try
        {
            var data = await _friendsService.GetUserAsync(userId);
            if (data == null) return;

            var isOnline = ComputeIsOnlineFromUserDoc(data);
            var text = isOnline ? "Đang hoạt động" : "Không hoạt động";

            Application.Current.Dispatcher.Invoke(() =>
            {
                var friend = _allFriends.FirstOrDefault(f => string.Equals(f.UserId, userId, StringComparison.Ordinal));
                if (friend != null)
                {
                    friend.Status = text;
                }
            });
        }
        catch
        {
            // ignore
        }
    }

    private async Task HandleDraftTypingChangedAsync()
    {
        var conv = SelectedConversation;
        string? currentUserId = _authService.CurrentUserId;
        if (conv == null || string.IsNullOrWhiteSpace(conv.ConversationId) || string.IsNullOrWhiteSpace(currentUserId))
            return;

        // For now, keep typing indicator behavior consistent in both 1-1 and group chats.
        if (string.IsNullOrWhiteSpace(DraftMessage))
        {
            _typingIdleTimer.Stop();
            await SetLocalTypingAsync(false);
            return;
        }

        // Send "typing" with throttling (avoid spamming Firestore on every keystroke)
        var nowUtc = DateTime.UtcNow;
        if (!_localIsTyping || (nowUtc - _lastTypingSentUtc) > TimeSpan.FromSeconds(1))
        {
            await SetLocalTypingAsync(true);
            _lastTypingSentUtc = nowUtc;
        }

        // Reset idle timer; when it fires, we clear typing.
        _typingIdleTimer.Stop();
        _typingIdleTimer.Start();
    }

    private async Task SetLocalTypingAsync(bool isTyping)
    {
        var conv = SelectedConversation;
        string? currentUserId = _authService.CurrentUserId;
        if (conv == null || string.IsNullOrWhiteSpace(conv.ConversationId) || string.IsNullOrWhiteSpace(currentUserId))
            return;

        bool stateChanged = _localIsTyping != isTyping;
        if (stateChanged)
        {
            _localIsTyping = isTyping;
        }
        else if (!isTyping)
        {
            // Already not typing; no-op.
            return;
        }

        try
        {
            await _messagingService.SetTypingAsync(conv.ConversationId, currentUserId, isTyping);
        }
        catch
        {
            // ignore
        }
    }

    private void OpenCreateGroup()
    {
        // Snapshot friend list for selection UI
        var friendsSnapshot = _allFriends.ToList();

        Application.Current.Dispatcher.Invoke(() =>
        {
            var win = new CreateGroupWindow(friendsSnapshot)
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            bool? result = win.ShowDialog();
            if (result == true && !string.IsNullOrWhiteSpace(win.CreatedConversationId))
            {
                ShowGroups = true;
                _ = LoadGroupsAsync(selectConversationId: win.CreatedConversationId);
            }
        });
    }

    private void OpenAddFriend()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var win = new AddFriendWindow
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            win.ShowDialog();
        });

        // If the user accepted a request, a new friendship may have been created.
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
                    bool isOnline = ComputeIsOnlineFromUserDoc(d);
                    string status = isOnline ? "Đang hoạt động" : "Không hoạt động";
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

                // Apply conversation settings (pinned/muted/hidden) to friend items
                ApplyFriendConversationSettings();
                ApplySidebarFilter();
            });

            await SyncFriendStatusListenersAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadFriends failed: {ex.Message}");
        }
    }

    private void ApplyFriendsFilter()
    {
        ApplySidebarFilter();
    }

    private void ApplySidebarFilter()
    {
        string needle = (SearchText ?? string.Empty).Trim();

        var previousConversation = SelectedConversation;

        _suppressSidebarSelectionHandling = true;
        try
        {
            SidebarItems.Clear();

            if (ShowGroups)
            {
                IEnumerable<GroupChatItemViewModel> groups = _allGroups;
                if (!string.IsNullOrWhiteSpace(needle))
                {
                    groups = groups.Where(g => !string.IsNullOrWhiteSpace(g.Name)
                                               && g.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                // Hide deleted conversations by default, but still allow finding via search.
                if (string.IsNullOrWhiteSpace(needle))
                {
                    groups = groups.Where(g => !g.IsHidden);
                }

                foreach (var g in groups
                             .OrderByDescending(g => g.IsPinned)
                             .ThenByDescending(g => g.LastActivityUtc ?? DateTime.MinValue)
                             .ThenBy(g => g.Name))
                {
                    SidebarItems.Add(g);
                }

                if (!string.IsNullOrWhiteSpace(_lastSelectedGroupConversationId))
                {
                    var toSelect = SidebarItems.OfType<GroupChatItemViewModel>()
                        .FirstOrDefault(x => string.Equals(x.ConversationId, _lastSelectedGroupConversationId, StringComparison.Ordinal));
                    if (toSelect != null)
                    {
                        SelectedSidebarItem = toSelect;
                    }
                }
            }
            else
            {
                IEnumerable<FriendItemViewModel> friends = _allFriends;
                if (!string.IsNullOrWhiteSpace(needle))
                {
                    friends = friends.Where(f =>
                        (!string.IsNullOrWhiteSpace(f.Name) && f.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                        || (!string.IsNullOrWhiteSpace(f.Username) && f.Username.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0));
                }

                // Hide deleted conversations by default, but still allow finding via search.
                if (string.IsNullOrWhiteSpace(needle))
                {
                    friends = friends.Where(f => !f.IsHidden);
                }

                foreach (var f in friends
                             .OrderByDescending(f => f.IsPinned)
                             .ThenByDescending(f => f.LastActivityUtc ?? DateTime.MinValue)
                             .ThenBy(f => f.Name))
                {
                    SidebarItems.Add(f);
                }

                if (!string.IsNullOrWhiteSpace(_lastSelectedFriendUserId))
                {
                    var toSelect = SidebarItems.OfType<FriendItemViewModel>()
                        .FirstOrDefault(x => string.Equals(x.UserId, _lastSelectedFriendUserId, StringComparison.Ordinal));
                    if (toSelect != null)
                    {
                        SelectedSidebarItem = toSelect;
                    }
                }
            }

            // Default selection (per current section)
            if (SelectedSidebarItem == null || !SidebarItems.Contains(SelectedSidebarItem))
            {
                SelectedSidebarItem = SidebarItems.FirstOrDefault();
            }
        }
        finally
        {
            _suppressSidebarSelectionHandling = false;
        }

        // If filtering changed the selection and there's no matching open conversation, open it once.
        if (SelectedSidebarItem != null && !SidebarItemMatchesSelectedConversation(SelectedSidebarItem, previousConversation))
        {
            if (SelectedSidebarItem is FriendItemViewModel f)
            {
                _lastSelectedFriendUserId = f.UserId;
                _ = OpenChatWithFriendAsync(f);
            }
            else if (SelectedSidebarItem is GroupChatItemViewModel g)
            {
                _lastSelectedGroupConversationId = g.ConversationId;
                _ = OpenChatWithGroupAsync(g);
            }
        }
    }

    private bool SidebarItemMatchesSelectedConversation(object sidebarItem, ConversationItemViewModel? conversation)
    {
        if (conversation == null) return false;
        if (sidebarItem is GroupChatItemViewModel g)
        {
            return string.Equals(g.ConversationId, conversation.ConversationId, StringComparison.Ordinal);
        }

        if (sidebarItem is FriendItemViewModel f)
        {
            string? currentUserId = _authService.CurrentUserId;
            if (string.IsNullOrWhiteSpace(currentUserId) || string.IsNullOrWhiteSpace(f.UserId)) return false;
            string pairId = GetCanonicalPairId(currentUserId, f.UserId);
            return string.Equals(pairId, conversation.ConversationId, StringComparison.Ordinal);
        }

        return false;
    }

    private async Task LoadGroupsAsync(string? selectConversationId = null)
    {
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        try
        {
            var raw = await _messagingService.GetConversations(currentUserId);

            // Cache per-user conversation settings from raw results
            _conversationSettings.Clear();
            foreach (var d in raw)
            {
                string cid = d.TryGetValue("conversationId", out var cidObj) ? cidObj?.ToString() ?? string.Empty : string.Empty;
                if (string.IsNullOrWhiteSpace(cid)) continue;
                bool pinned = d.TryGetValue("userPinned", out var p) && p is bool pb && pb;
                bool muted = d.TryGetValue("userMuted", out var m) && m is bool mb && mb;
                bool hidden = d.TryGetValue("userHidden", out var h) && h is bool hb && hb;

                DateTime? clearedAtUtc = null;
                if (d.TryGetValue("userClearedAt", out var ca) && ca is Timestamp ts)
                {
                    clearedAtUtc = ts.ToDateTime().ToUniversalTime();
                }

                DateTime? lastActivityUtc = null;
                if (d.TryGetValue("lastMessageAt", out var lma) && lma is Timestamp lts)
                {
                    lastActivityUtc = lts.ToDateTime().ToUniversalTime();
                }
                else if (d.TryGetValue("createdAt", out var cra) && cra is Timestamp cts)
                {
                    lastActivityUtc = cts.ToDateTime().ToUniversalTime();
                }

                _conversationSettings[cid] = (pinned, muted, hidden, clearedAtUtc, lastActivityUtc);
            }

            var groups = raw
                .Where(d => d.TryGetValue("isGroup", out var isg) && isg is bool b && b)
                .Select(d =>
                {
                    string id = d.TryGetValue("conversationId", out var cid) ? cid?.ToString() ?? string.Empty : string.Empty;
                    string name = d.TryGetValue("groupName", out var gn) ? gn?.ToString() ?? "Nhóm chat" : "Nhóm chat";
                    string avatarDataUrl = d.TryGetValue("groupAvatarDataUrl", out var au) ? au?.ToString() ?? string.Empty : string.Empty;

                    bool pinned = d.TryGetValue("userPinned", out var p) && p is bool pb && pb;
                    bool muted = d.TryGetValue("userMuted", out var m) && m is bool mb && mb;
                    bool hidden = d.TryGetValue("userHidden", out var h) && h is bool hb && hb;

                    DateTime? lastActivityUtc = null;
                    if (d.TryGetValue("lastMessageAt", out var lma) && lma is Timestamp lts)
                    {
                        lastActivityUtc = lts.ToDateTime().ToUniversalTime();
                    }
                    else if (d.TryGetValue("createdAt", out var cra) && cra is Timestamp cts)
                    {
                        lastActivityUtc = cts.ToDateTime().ToUniversalTime();
                    }

                    var participants = new List<string>();
                    if (d.TryGetValue("participants", out var pObj) && pObj is IEnumerable<string> ps)
                    {
                        participants = ps.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToList();
                    }

                    var vm = new GroupChatItemViewModel
                    {
                        ConversationId = id,
                        Name = name,
                        AvatarText = string.IsNullOrWhiteSpace(name) ? "G" : name.Substring(0, 1).ToUpperInvariant(),
                        AvatarImage = TryDecodeDataUrlToImageSource(string.IsNullOrWhiteSpace(avatarDataUrl) ? null : avatarDataUrl),
                        ParticipantIds = participants,
                        IsPinned = pinned,
                        NotificationsEnabled = !muted,
                        IsHidden = hidden,
                        LastActivityUtc = lastActivityUtc
                    };
                    return vm;
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.ConversationId))
                .ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                _allGroups.Clear();
                _allGroups.AddRange(groups);

                // Apply settings to friends now that we have fresh conversation settings
                ApplyFriendConversationSettings();
                ApplySidebarFilter();

                if (!string.IsNullOrWhiteSpace(selectConversationId))
                {
                    var toSelect = _allGroups.FirstOrDefault(g => g.ConversationId == selectConversationId);
                    if (toSelect != null)
                    {
                        SelectedSidebarItem = toSelect;
                    }
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadGroups failed: {ex.Message}");
        }
    }

    private void ApplyFriendConversationSettings()
    {
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        foreach (var f in _allFriends)
        {
            if (string.IsNullOrWhiteSpace(f.UserId)) continue;

            string convId = GetCanonicalPairId(currentUserId, f.UserId);
            if (_conversationSettings.TryGetValue(convId, out var s))
            {
                f.IsPinned = s.pinned;
                f.NotificationsEnabled = !s.muted;
                f.IsHidden = s.hidden;
                f.LastActivityUtc = s.lastActivityUtc;
            }
            else
            {
                // Default UI state if no conversation exists yet
                f.IsPinned = false;
                f.NotificationsEnabled = true;
                f.IsHidden = false;
                f.LastActivityUtc = null;
            }
        }
    }

    private static string GetCanonicalPairId(string userId1, string userId2)
    {
        if (string.CompareOrdinal(userId1, userId2) < 0)
            return $"{userId1}_{userId2}";
        return $"{userId2}_{userId1}";
    }

    private async Task<string?> ResolveConversationIdAsync(object? item)
    {
        if (item == null) return null;

        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return null;

        if (item is GroupChatItemViewModel g)
        {
            return string.IsNullOrWhiteSpace(g.ConversationId) ? null : g.ConversationId;
        }

        if (item is FriendItemViewModel f)
        {
            if (string.IsNullOrWhiteSpace(f.UserId)) return null;
            // Ensure conversation doc exists before updating settings.
            return await _messagingService.GetOrCreateConversation(currentUserId, f.UserId);
        }

        return null;
    }

    private async Task TogglePinAsync(object? item)
    {
        if (item == null) return;
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        try
        {
            bool newPinned;
            if (item is GroupChatItemViewModel g)
            {
                newPinned = !g.IsPinned;
                g.IsPinned = newPinned;
            }
            else if (item is FriendItemViewModel f)
            {
                newPinned = !f.IsPinned;
                f.IsPinned = newPinned;
            }
            else
            {
                return;
            }

            var convId = await ResolveConversationIdAsync(item);
            if (string.IsNullOrWhiteSpace(convId)) return;

            await _messagingService.UpdateConversationUserSettings(convId, currentUserId, pinned: newPinned);

            if (_conversationSettings.TryGetValue(convId, out var s))
            {
                _conversationSettings[convId] = (pinned: newPinned, s.muted, s.hidden, s.clearedAtUtc, s.lastActivityUtc);
            }
            ApplySidebarFilter();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TogglePin failed: {ex.Message}");
        }
    }

    private async Task UpdatePinnedAsync(object? item)
    {
        if (item == null) return;
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        try
        {
            bool pinned;
            if (item is GroupChatItemViewModel g)
            {
                pinned = g.IsPinned;
            }
            else if (item is FriendItemViewModel f)
            {
                pinned = f.IsPinned;
            }
            else
            {
                return;
            }

            var convId = await ResolveConversationIdAsync(item);
            if (string.IsNullOrWhiteSpace(convId)) return;

            await _messagingService.UpdateConversationUserSettings(convId, currentUserId, pinned: pinned);
            if (_conversationSettings.TryGetValue(convId, out var s))
            {
                _conversationSettings[convId] = (pinned: pinned, s.muted, s.hidden, s.clearedAtUtc, s.lastActivityUtc);
            }
            ApplySidebarFilter();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdatePinned failed: {ex.Message}");
        }
    }

    private async Task UpdateNotificationsAsync(object? item)
    {
        if (item == null) return;
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        try
        {
            bool enabled;
            if (item is GroupChatItemViewModel g)
            {
                enabled = g.NotificationsEnabled;
            }
            else if (item is FriendItemViewModel f)
            {
                enabled = f.NotificationsEnabled;
            }
            else
            {
                return;
            }

            bool muted = !enabled;

            var convId = await ResolveConversationIdAsync(item);
            if (string.IsNullOrWhiteSpace(convId)) return;

            await _messagingService.UpdateConversationUserSettings(convId, currentUserId, muted: muted);

            if (_conversationSettings.TryGetValue(convId, out var s))
            {
                _conversationSettings[convId] = (s.pinned, muted: muted, s.hidden, s.clearedAtUtc, s.lastActivityUtc);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateNotifications failed: {ex.Message}");
        }
    }

    private async Task DeleteConversationAsync(object? item)
    {
        if (item == null) return;
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        var result = MessageBox.Show(
            "Bạn có chắc chắn muốn xóa cuộc trò chuyện này không?\n(Lưu ý: thao tác này chỉ ẩn ở phía bạn)",
            "Xóa cuộc trò chuyện",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            // Optimistically: hide and clear history for this user
            if (item is GroupChatItemViewModel g)
            {
                g.IsHidden = true;
                g.IsPinned = false;
                g.NotificationsEnabled = true;
            }
            if (item is FriendItemViewModel f)
            {
                f.IsHidden = true;
                f.IsPinned = false;
                f.NotificationsEnabled = true;
            }

            var convId = await ResolveConversationIdAsync(item);
            if (string.IsNullOrWhiteSpace(convId))
            {
                ApplySidebarFilter();
                return;
            }

            await _messagingService.UpdateConversationUserSettings(convId, currentUserId, pinned: false, muted: false, hidden: true, clearHistory: true);

            // Update local cache
            _conversationSettings[convId] = (pinned: false, muted: false, hidden: true, clearedAtUtc: DateTime.UtcNow, lastActivityUtc: DateTime.UtcNow);

            if (SelectedSidebarItem == item)
            {
                SelectedSidebarItem = null;
                SelectedConversation = null;
            }

            ApplySidebarFilter();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DeleteConversation failed: {ex.Message}");
        }
    }

    private Task OpenChatWithFriendAsync(FriendItemViewModel? friend)
    {
        if (friend == null) return Task.CompletedTask;

        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return Task.CompletedTask;

        // Open immediately using canonical id (avoid network roundtrip on click).
        var conversationId = GetCanonicalPairId(currentUserId, friend.UserId);
        SelectedConversation = new ConversationItemViewModel
        {
            ConversationId = conversationId,
            OtherUserId = friend.UserId,
            Title = friend.Name,
            Subtitle = string.Empty,
            AvatarText = friend.AvatarText
        };

        // Background: ensure conversation exists and revive if previously hidden.
        _ = EnsureFriendConversationExistsAndReviveAsync(friend, currentUserId, conversationId);

        return Task.CompletedTask;
    }

    private async Task EnsureFriendConversationExistsAndReviveAsync(FriendItemViewModel friend, string currentUserId, string conversationId)
    {
        try
        {
            // Ensure conversation document exists (id is deterministic).
            await _messagingService.GetOrCreateConversation(currentUserId, friend.UserId);

            if (!friend.IsHidden) return;

            friend.IsHidden = false;
            await _messagingService.UpdateConversationUserSettings(conversationId, currentUserId, hidden: false);
            if (_conversationSettings.TryGetValue(conversationId, out var s))
            {
                _conversationSettings[conversationId] = (s.pinned, s.muted, hidden: false, s.clearedAtUtc, s.lastActivityUtc);
            }
            ApplySidebarFilter();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EnsureFriendConversationExistsAndRevive failed: {ex.Message}");
        }
    }

    private Task OpenChatWithGroupAsync(GroupChatItemViewModel? group)
    {
        if (group == null) return Task.CompletedTask;

        bool needsSidebarRefresh = false;

        // If the user explicitly opens a hidden group via search, revive it.
        if (group.IsHidden)
        {
            string? currentUserId = _authService.CurrentUserId;
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                group.IsHidden = false;
                _ = _messagingService.UpdateConversationUserSettings(group.ConversationId, currentUserId, hidden: false);
                if (_conversationSettings.TryGetValue(group.ConversationId, out var s))
                {
                    _conversationSettings[group.ConversationId] = (s.pinned, s.muted, hidden: false, s.clearedAtUtc, s.lastActivityUtc);
                }
                needsSidebarRefresh = true;
            }
        }

        SelectedConversation = new ConversationItemViewModel
        {
            ConversationId = group.ConversationId,
            OtherUserId = string.Empty,
            Title = group.Name,
            Subtitle = string.Empty,
            AvatarText = group.AvatarText,
            AvatarImage = group.AvatarImage,
            IsGroup = true,
            ParticipantIds = group.ParticipantIds
        };

        if (needsSidebarRefresh)
        {
            ApplySidebarFilter();
        }

        return Task.CompletedTask;
    }

    private async Task SwitchConversationAsync(ConversationItemViewModel? conversation)
    {
        // Stop typing updates for the previous conversation.
        try
        {
            _typingIdleTimer.Stop();
            if (_localIsTyping && !string.IsNullOrWhiteSpace(_authService.CurrentUserId) && !string.IsNullOrWhiteSpace(SelectedConversation?.ConversationId))
            {
                await _messagingService.SetTypingAsync(SelectedConversation.ConversationId, _authService.CurrentUserId!, false);
            }
        }
        catch { }
        _localIsTyping = false;

        if (_messageListener != null)
        {
            try
            {
                await _messageListener.StopAsync();
            }
            catch { }
            _messageListener = null;
        }

        if (_typingListener != null)
        {
            try
            {
                await _typingListener.StopAsync();
            }
            catch { }
            _typingListener = null;
        }

        IsOtherTyping = false;

        string? targetConversationId = conversation?.ConversationId;

        _ = Application.Current.Dispatcher.BeginInvoke(() =>
        {
            Messages.Clear();
            Members.Clear();
            RightImages.Clear();
            RightFiles.Clear();
            RightLinks.Clear();

            RightImagesExpanded = false;
            RightFilesExpanded = false;

            if (!string.IsNullOrWhiteSpace(targetConversationId)
                && _messageCache.TryGetValue(targetConversationId, out var cached)
                && cached.Count > 0)
            {
                foreach (var vm in cached)
                {
                    Messages.Add(vm);
                }

                RebuildRightSidebarArtifactsFromMessages(cached);
            }
        });

        _locallyHiddenMessageIds.Clear();

        if (conversation == null) return;

        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        // Members
        Application.Current.Dispatcher.Invoke(() =>
        {
            Members.Clear();

            if (conversation.IsGroup && conversation.ParticipantIds.Count > 0)
            {
                foreach (var uid in conversation.ParticipantIds.Distinct(StringComparer.Ordinal))
                {
                    if (string.IsNullOrWhiteSpace(uid)) continue;

                    if (string.Equals(uid, currentUserId, StringComparison.Ordinal))
                    {
                        Members.Add(new MemberItemViewModel { Name = "Bạn", AvatarText = "B" });
                        continue;
                    }

                    var friend = _allFriends.FirstOrDefault(f => string.Equals(f.UserId, uid, StringComparison.Ordinal));
                    string name = friend?.Name ?? uid;
                    string avatarText = friend?.AvatarText ?? (string.IsNullOrWhiteSpace(name) ? "?" : name.Substring(0, 1).ToUpperInvariant());
                    Members.Add(new MemberItemViewModel { Name = name, AvatarText = avatarText });
                }
            }
            else
            {
                Members.Add(new MemberItemViewModel { Name = "Bạn", AvatarText = "B" });
                if (!string.IsNullOrWhiteSpace(conversation.Title))
                {
                    Members.Add(new MemberItemViewModel { Name = conversation.Title, AvatarText = conversation.AvatarText });
                }
            }
        });

        try
        {
            // Realtime listener (will deliver an initial snapshot as well)
            _messageListener = _messagingService.ListenToMessages(conversation.ConversationId, msgs =>
            {
                if (string.IsNullOrWhiteSpace(_authService.CurrentUserId)) return;
                ApplyMessages(conversation.ConversationId, msgs, _authService.CurrentUserId);
            });

            // Typing listener (conversation metadata)
            _typingListener = _messagingService.ListenToConversation(conversation.ConversationId, data =>
            {
                try
                {
                    string? currentId = _authService.CurrentUserId;
                    if (string.IsNullOrWhiteSpace(currentId)) return;

                    bool otherTyping = false;
                    if (data.TryGetValue("typing", out var typingObj) && typingObj is Dictionary<string, object> typingMap)
                    {
                        var now = DateTime.UtcNow;
                        foreach (var kv in typingMap)
                        {
                            if (string.Equals(kv.Key, currentId, StringComparison.Ordinal)) continue;
                            if (kv.Value is Timestamp ts)
                            {
                                var dt = ts.ToDateTime();
                                if ((now - dt) <= TimeSpan.FromSeconds(6))
                                {
                                    otherTyping = true;
                                    break;
                                }
                            }
                        }
                    }

                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        IsOtherTyping = otherTyping;
                    });
                }
                catch
                {
                    // ignore
                }
            });

            await _messagingService.MarkMessagesAsRead(conversation.ConversationId, currentUserId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SwitchConversation failed: {ex.Message}");
        }
    }

    private void ApplyMessages(string conversationId, List<Dictionary<string, object>> rawMessages, string currentUserId)
    {
        DateTime? clearedAtUtc = null;
        if (!string.IsNullOrWhiteSpace(conversationId)
            && _conversationSettings.TryGetValue(conversationId, out var s)
            && s.clearedAtUtc.HasValue)
        {
            clearedAtUtc = s.clearedAtUtc.Value;
        }

        // Sort by timestamp ascending
        var ordered = rawMessages
            .Select(m => new { m, t = ExtractTimestamp(m) })
            .OrderBy(x => x.t)
            .Select(x => x.m)
            .ToList();

        if (clearedAtUtc.HasValue)
        {
            ordered = ordered
                .Where(m =>
                {
                    var ts = ExtractTimestamp(m);
                    if (ts == DateTime.MinValue) return true;
                    return ts.ToUniversalTime() > clearedAtUtc.Value;
                })
                .ToList();
        }

        // Update last activity so the sidebar reorders by recent messages.
        var latest = ordered
            .Select(ExtractTimestamp)
            .Where(d => d != DateTime.MinValue)
            .Select(d => d.ToUniversalTime())
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();

        if (latest != DateTime.MinValue)
        {
            BumpConversationActivity(conversationId, latest, currentUserId);
        }

        var viewModels = ordered
            .Where(m => m.ContainsKey("senderId") && m.ContainsKey("content"))
            .Select(m =>
            {
                string messageId = m.TryGetValue("messageId", out var mid) ? mid?.ToString() ?? string.Empty : string.Empty;
                string senderId = m["senderId"]?.ToString() ?? string.Empty;
                string content = m["content"]?.ToString() ?? string.Empty;
                string type = m.TryGetValue("type", out var tObj) ? (tObj?.ToString() ?? "text") : "text";
                var dt = ExtractTimestamp(m);
                bool outgoing = senderId == currentUserId;

                var vm = new MessageItemViewModel
                {
                    MessageId = messageId,
                    SenderId = senderId,
                    IsOutgoing = outgoing,
                    Time = dt == DateTime.MinValue ? DateTime.Now : dt,
                    SenderAvatarText = outgoing ? "B" : IncomingAvatarText,
                    IsRead = m.TryGetValue("read", out var rObj) && rObj is bool rb && rb
                };

                switch (type.Trim().ToLowerInvariant())
                {
                    case "image":
                        vm.Kind = MessageBubbleKind.Image;
                        vm.Image = TryDecodeDataUrlToImageSource(content);
                        break;
                    case "file":
                        vm.Kind = MessageBubbleKind.File;
                        vm.FileName = m.TryGetValue("fileName", out var fnObj) ? fnObj?.ToString() : "download";
                        vm.StorageBucket = m.TryGetValue("bucket", out var bObj) ? bObj?.ToString() : null;
                        vm.StorageObject = m.TryGetValue("object", out var oObj) ? oObj?.ToString() : (string.IsNullOrWhiteSpace(content) ? null : content);
                        break;
                    case "link":
                        // Backward compatibility if old messages were stored with type=link.
                        {
                            var trimmedLink = (content ?? string.Empty).Trim();
                            var linkUri = TryCreateHttpUri(trimmedLink);
                            if (linkUri != null)
                            {
                                vm.Kind = MessageBubbleKind.Link;
                                vm.LinkText = trimmedLink;
                                vm.LinkUri = linkUri;
                            }
                            else
                            {
                                vm.Kind = MessageBubbleKind.Text;
                                vm.Text = content ?? string.Empty;
                            }
                            break;
                        }
                    default:
                        // Treat pure URL as a clickable link without needing a separate "send link" feature.
                        var trimmed = content.Trim();
                        var uri = TryCreateHttpUri(trimmed);
                        if (uri != null)
                        {
                            vm.Kind = MessageBubbleKind.Link;
                            vm.LinkText = trimmed;
                            vm.LinkUri = uri;
                        }
                        else
                        {
                            vm.Kind = MessageBubbleKind.Text;
                            vm.Text = content ?? string.Empty;
                        }
                        break;
                }

                return vm;
            })
            .Where(vm => string.IsNullOrWhiteSpace(vm.MessageId) || !_locallyHiddenMessageIds.Contains(vm.MessageId))
            .ToList();

        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            SyncMessagesIncremental(viewModels);
            _messageCache[conversationId] = Messages.ToList();

            if (string.Equals(SelectedConversation?.ConversationId, conversationId, StringComparison.Ordinal))
            {
                RebuildRightSidebarArtifactsFromMessages(Messages);
            }
        });
    }

    private void RebuildRightSidebarArtifactsFromMessages(IEnumerable<MessageItemViewModel> source)
    {
        RightImages.Clear();
        RightFiles.Clear();
        RightLinks.Clear();

        foreach (var msg in source.OrderByDescending(m => m.Time))
        {
            if (msg.Kind == MessageBubbleKind.Image && msg.Image != null)
            {
                RightImages.Add(new RightSidebarAttachmentItemViewModel
                {
                    Kind = RightSidebarAttachmentKind.Image,
                    Thumbnail = msg.Image,
                    Title = "Ảnh",
                    Time = msg.Time
                });
                continue;
            }

            if (msg.Kind == MessageBubbleKind.File)
            {
                RightFiles.Add(new RightSidebarAttachmentItemViewModel
                {
                    Kind = RightSidebarAttachmentKind.File,
                    Title = string.IsNullOrWhiteSpace(msg.FileName) ? "File" : msg.FileName!,
                    StorageBucket = msg.StorageBucket,
                    StorageObject = msg.StorageObject,
                    Time = msg.Time
                });
                continue;
            }

            if (msg.Kind == MessageBubbleKind.Link && msg.LinkUri != null)
            {
                RightLinks.Add(new RightSidebarLinkItemViewModel
                {
                    Text = string.IsNullOrWhiteSpace(msg.LinkText) ? msg.LinkUri.ToString() : msg.LinkText!,
                    Uri = msg.LinkUri,
                    Time = msg.Time
                });
            }
        }
    }

    private void SyncMessagesIncremental(List<MessageItemViewModel> newItems)
    {
        // Fast-path: if nothing exists yet, add all.
        if (Messages.Count == 0)
        {
            foreach (var vm in newItems) Messages.Add(vm);
            return;
        }

        // Drop trailing optimistic messages before comparing with server snapshot.
        int optimisticCount = 0;
        for (int i = Messages.Count - 1; i >= 0; i--)
        {
            var id = Messages[i].MessageId ?? string.Empty;
            if (!id.StartsWith("local-", StringComparison.Ordinal)) break;
            optimisticCount++;
        }

        int trimmedCount = Messages.Count - optimisticCount;
        if (trimmedCount < 0) trimmedCount = 0;

        bool isPrefix = trimmedCount <= newItems.Count;
        if (isPrefix)
        {
            for (int i = 0; i < trimmedCount; i++)
            {
                if (!string.Equals(Messages[i].MessageId, newItems[i].MessageId, StringComparison.Ordinal))
                {
                    isPrefix = false;
                    break;
                }
            }
        }

        if (isPrefix)
        {
            // Remove optimistic tail (server snapshot now becomes source of truth)
            while (Messages.Count > trimmedCount)
            {
                Messages.RemoveAt(Messages.Count - 1);
            }

            // Append any new server messages
            for (int i = trimmedCount; i < newItems.Count; i++)
            {
                Messages.Add(newItems[i]);
            }
            return;
        }

        // Fallback: replace all (handles revoke/delete/edits/out-of-order snapshots)
        Messages.Clear();
        foreach (var vm in newItems)
        {
            Messages.Add(vm);
        }
    }

    private void BumpConversationActivity(string conversationId, DateTime lastActivityUtc, string currentUserId)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) return;

        if (_conversationSettings.TryGetValue(conversationId, out var s))
        {
            _conversationSettings[conversationId] = (s.pinned, s.muted, s.hidden, s.clearedAtUtc, lastActivityUtc);
        }
        else
        {
            _conversationSettings[conversationId] = (pinned: false, muted: false, hidden: false, clearedAtUtc: null, lastActivityUtc: lastActivityUtc);
        }

        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            // Group
            var g = _allGroups.FirstOrDefault(x => string.Equals(x.ConversationId, conversationId, StringComparison.Ordinal));
            if (g != null)
            {
                g.LastActivityUtc = lastActivityUtc;
                ApplySidebarFilter();
                return;
            }

            // Friend (canonical pair id)
            string other = TryGetOtherUserIdFromPairId(conversationId, currentUserId) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(other))
            {
                var f = _allFriends.FirstOrDefault(x => string.Equals(x.UserId, other, StringComparison.Ordinal));
                if (f != null)
                {
                    f.LastActivityUtc = lastActivityUtc;
                }
            }
            ApplySidebarFilter();
        });
    }

    private static string? TryGetOtherUserIdFromPairId(string conversationId, string currentUserId)
    {
        if (string.IsNullOrWhiteSpace(conversationId) || string.IsNullOrWhiteSpace(currentUserId)) return null;
        var parts = conversationId.Split('_');
        if (parts.Length != 2) return null;
        if (string.Equals(parts[0], currentUserId, StringComparison.Ordinal)) return parts[1];
        if (string.Equals(parts[1], currentUserId, StringComparison.Ordinal)) return parts[0];
        return null;
    }

    private async Task RevokeMessageAsync(MessageItemViewModel? msg)
    {
        if (msg == null) return;

        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        if (!msg.IsOutgoing || string.IsNullOrWhiteSpace(msg.MessageId)) return;

        var result = MessageBox.Show(
            "Bạn có chắc chắn muốn thu hồi tin nhắn không?",
            "Thu hồi tin nhắn",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var (success, message) = await _messagingService.DeleteMessageAsync(msg.MessageId, currentUserId);
            if (!success)
            {
                MessageBox.Show(message, "Không thể thu hồi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Make it disappear immediately; listener will keep it consistent.
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var existing = Messages.FirstOrDefault(m => m.MessageId == msg.MessageId);
                if (existing != null)
                {
                    Messages.Remove(existing);
                }
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Không thể thu hồi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Task HideMessageLocallyAsync(MessageItemViewModel? msg)
    {
        if (msg == null) return Task.CompletedTask;
        if (msg.IsOutgoing) return Task.CompletedTask;
        if (string.IsNullOrWhiteSpace(msg.MessageId)) return Task.CompletedTask;

        var result = MessageBox.Show(
            "Bạn có chắc chắn muốn xóa tin nhắn này khỏi khung chat không?\n(Lưu ý: thao tác này chỉ ẩn ở phía bạn, không ảnh hưởng người gửi)",
            "Xóa tin nhắn",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return Task.CompletedTask;

        _locallyHiddenMessageIds.Add(msg.MessageId);
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var existing = Messages.FirstOrDefault(m => m.MessageId == msg.MessageId);
            if (existing != null)
            {
                Messages.Remove(existing);
            }
        });

        return Task.CompletedTask;
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

        // Clear typing immediately when sending.
        _typingIdleTimer.Stop();
        _ = SetLocalTypingAsync(false);

        // Optimistic UI: show the outgoing message immediately.
        var tempId = $"local-{Guid.NewGuid():N}";
        var optimisticVm = new MessageItemViewModel
        {
            MessageId = tempId,
            SenderId = currentUserId,
            IsOutgoing = true,
            Time = DateTime.Now,
            SenderAvatarText = "B",
            Kind = MessageBubbleKind.Text,
            Text = text
        };

        _pendingOutgoingByTempId[tempId] = DateTime.UtcNow;

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Messages.Add(optimisticVm);
            if (!string.IsNullOrWhiteSpace(selected.ConversationId))
            {
                _messageCache[selected.ConversationId] = Messages.ToList();
            }
        });

        DraftMessage = string.Empty;

        try
        {
            var (success, message) = await _messagingService.SendMessage(selected.ConversationId, currentUserId, text);
            if (!success)
            {
                Console.WriteLine(message);
                // Roll back optimistic message if send failed.
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var existing = Messages.FirstOrDefault(m => string.Equals(m.MessageId, tempId, StringComparison.Ordinal));
                    if (existing != null) Messages.Remove(existing);
                });
                return;
            }

            // Move conversation to top immediately (pinned still stays above).
            BumpConversationActivity(selected.ConversationId, DateTime.UtcNow, currentUserId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SendMessage failed: {ex.Message}");
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var existing = Messages.FirstOrDefault(m => string.Equals(m.MessageId, tempId, StringComparison.Ordinal));
                if (existing != null) Messages.Remove(existing);
            });
        }
    }

    public async Task SendImageFileAsync(string filePath)
    {
        var selected = SelectedConversation;
        if (selected == null) return;

        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            MessageBox.Show("Không tìm thấy file hình ảnh.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var info = new FileInfo(filePath);
            const long maxBytes = 25L * 1024 * 1024;
            if (info.Length > maxBytes)
            {
                MessageBox.Show("File quá lớn. Giới hạn hiện tại: 25MB.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        catch
        {
            MessageBox.Show("Không thể đọc thông tin file.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Firestore document size is limited; keep images small by resizing + JPEG encoding.
        var attemptSettings = new (int maxDim, int quality)[]
        {
            (1024, 80),
            (768, 70),
            (512, 60)
        };

        byte[]? jpegBytes = null;
        foreach (var (maxDim, quality) in attemptSettings)
        {
            jpegBytes = TryLoadAndCompressToJpeg(filePath, maxDim, quality);
            if (jpegBytes == null) continue;
            if (jpegBytes.Length <= 900_000) break;
            jpegBytes = null;
        }

        if (jpegBytes == null)
        {
            MessageBox.Show("Ảnh quá lớn hoặc không thể xử lý. Vui lòng chọn ảnh nhỏ hơn.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string dataUrl = "data:image/jpeg;base64," + Convert.ToBase64String(jpegBytes);

        try
        {
            var (success, message) = await _messagingService.SendMessage(selected.ConversationId, currentUserId, dataUrl, "image");
            if (!success)
            {
                Console.WriteLine(message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SendImage failed: {ex.Message}");
        }
    }

    public async Task SendFileAsync(string filePath)
    {
        var selected = SelectedConversation;
        if (selected == null) return;

        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            MessageBox.Show("Không tìm thấy file.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var info = new FileInfo(filePath);
        const long maxBytes = 25L * 1024 * 1024;
        if (info.Length > maxBytes)
        {
            MessageBox.Show("File quá lớn. Giới hạn hiện tại: 25MB.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string name = Path.GetFileName(filePath);
        string objectName = $"uploads/{selected.ConversationId}/{Guid.NewGuid():N}_{name}";

        try
        {
            var upload = await _storageService.UploadFileAsync(filePath, bucket: MessagingApp.Config.FirebaseConfig.StorageBucket, objectName: objectName);
            if (!upload.success)
            {
                MessageBox.Show(upload.message, "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var extras = new Dictionary<string, object>
            {
                { "fileName", name },
                { "size", info.Length },
                { "bucket", upload.bucket },
                { "object", upload.objectName }
            };

            // Store the storage object path in content for backward compatibility.
            var (success, message) = await _messagingService.SendMessage(selected.ConversationId, currentUserId, upload.objectName, "file", extras);
            if (!success)
            {
                Console.WriteLine(message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SendFile failed: {ex.Message}");
            MessageBox.Show("Không thể gửi file.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static Uri? TryCreateHttpUri(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        string trimmed = input.Trim();

        // Don't auto-linkify phrases.
        if (trimmed.Any(char.IsWhiteSpace)) return null;

        // Trim common trailing punctuation from sentences.
        trimmed = trimmed.TrimEnd('.', ',', ';', ':', '!', '?', ')', ']', '}', '"', '\'');
        if (string.IsNullOrWhiteSpace(trimmed)) return null;

        // If it's already an absolute URL, validate scheme.
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute))
        {
            if (string.Equals(absolute.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(absolute.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return absolute;
            }
            return null;
        }

        // If there's no scheme, only auto-add one for domain-like strings.
        // This prevents single words like "alo" becoming https://alo.
        bool looksLikeDomain = trimmed.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                               || trimmed.Contains('.', StringComparison.Ordinal)
                               || string.Equals(trimmed, "localhost", StringComparison.OrdinalIgnoreCase);

        if (!looksLikeDomain) return null;

        if (!trimmed.Contains("://", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate("https://" + trimmed, UriKind.Absolute, out var withScheme))
        {
            if (string.Equals(withScheme.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(withScheme.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return withScheme;
            }
        }

        return null;
    }

    private static ImageSource? TryDecodeDataUrlToImageSource(string? dataUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dataUrl)) return null;
            int idx = dataUrl.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            string b64 = dataUrl[(idx + "base64,".Length)..];
            var bytes = Convert.FromBase64String(b64);
            using var ms = new MemoryStream(bytes);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool success, string message)> SaveFileMessageToPathAsync(MessageItemViewModel msg, string destinationPath)
    {
        if (msg.FileBytes is { Length: > 0 })
        {
            try
            {
                await File.WriteAllBytesAsync(destinationPath, msg.FileBytes);
                return (true, "Saved");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        var bucket = string.IsNullOrWhiteSpace(msg.StorageBucket) ? MessagingApp.Config.FirebaseConfig.StorageBucket : msg.StorageBucket;
        var obj = msg.StorageObject;
        if (string.IsNullOrWhiteSpace(obj))
        {
            return (false, "Thiếu thông tin file trên Storage.");
        }

        return await _storageService.DownloadToFileAsync(bucket!, obj!, destinationPath);
    }

    public Task<(bool success, string message)> SaveStorageObjectToPathAsync(string? bucket, string? storageObject, string destinationPath)
    {
        var resolvedBucket = string.IsNullOrWhiteSpace(bucket)
            ? MessagingApp.Config.FirebaseConfig.StorageBucket
            : bucket;

        if (string.IsNullOrWhiteSpace(storageObject))
        {
            return Task.FromResult<(bool success, string message)>((false, "Thiếu thông tin file trên Storage."));
        }

        return _storageService.DownloadToFileAsync(resolvedBucket!, storageObject!, destinationPath);
    }

    private async Task StartFriendRequestsListenerAsync()
    {
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        try
        {
            _friendRequestsListener = _friendsService.ListenToPendingRequests(currentUserId, async () =>
            {
                await LoadPendingFriendRequestsAsync();
            });

            // Initial load
            await LoadPendingFriendRequestsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start friend requests listener: {ex.Message}");
        }
    }

    private async Task LoadPendingFriendRequestsAsync()
    {
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        try
        {
            var requests = await _friendsService.GetPendingRequests(currentUserId);
            
            var mapped = requests.Select(d =>
            {
                string requestId = d.TryGetValue("requestId", out var rid) ? rid?.ToString() ?? string.Empty : string.Empty;
                string fromUserId = d.TryGetValue("fromUserId", out var fid) ? fid?.ToString() ?? string.Empty : string.Empty;
                string senderUsername = d.TryGetValue("senderUsername", out var su) ? su?.ToString() ?? string.Empty : string.Empty;
                string senderFullName = d.TryGetValue("senderFullName", out var sfn) ? sfn?.ToString() ?? string.Empty : string.Empty;
                string senderEmail = d.TryGetValue("senderEmail", out var se) ? se?.ToString() ?? string.Empty : string.Empty;

                string displayName = string.IsNullOrWhiteSpace(senderFullName)
                    ? (string.IsNullOrWhiteSpace(senderUsername) ? senderEmail : senderUsername)
                    : senderFullName;

                string subtitle = !string.IsNullOrWhiteSpace(senderUsername)
                    ? $"@{senderUsername}"
                    : senderEmail;

                return new FriendRequestItemViewModel
                {
                    RequestId = requestId,
                    FromUserId = fromUserId,
                    DisplayName = displayName,
                    Subtitle = subtitle,
                    AvatarText = string.IsNullOrWhiteSpace(displayName) ? "?" : displayName.Substring(0, 1).ToUpperInvariant()
                };
            }).ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                PendingFriendRequests.Clear();
                foreach (var r in mapped)
                {
                    PendingFriendRequests.Add(r);
                }
                PendingFriendRequestsCount = PendingFriendRequests.Count;
                OnPropertyChanged(nameof(HasPendingFriendRequests));
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load pending friend requests: {ex.Message}");
        }
    }

    private async Task AcceptFriendRequestFromNotificationAsync(FriendRequestItemViewModel? request)
    {
        if (request == null) return;
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            ShowToast("Bạn chưa đăng nhập.", "error");
            return;
        }

        try
        {
            var (success, message) = await _friendsService.AcceptFriendRequest(request.RequestId, request.FromUserId, currentUserId);
            ShowToast(message, success ? "success" : "error");
            if (success)
            {
                // Reload friends list
                await LoadFriendsAsync();
                // Request will be removed automatically by the listener
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Lỗi: {ex.Message}", "error");
        }
    }

    private async Task DeclineFriendRequestFromNotificationAsync(FriendRequestItemViewModel? request)
    {
        if (request == null) return;
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            ShowToast("Bạn chưa đăng nhập.", "error");
            return;
        }

        try
        {
            var (success, message) = await _friendsService.DeclineFriendRequest(request.RequestId);
            ShowToast(message, success ? "success" : "error");
            // Request will be removed automatically by the listener
        }
        catch (Exception ex)
        {
            ShowToast($"Lỗi: {ex.Message}", "error");
        }
    }

    private async Task StartIncomingCallsListenerAsync()
    {
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        try
        {
            _incomingCallsListener = _callingService.ListenToIncomingCalls(currentUserId, callData =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    HandleIncomingCall(callData);
                });
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start incoming calls listener: {ex.Message}");
        }
    }

    private void HandleIncomingCall(Dictionary<string, object> callData)
    {
        try
        {
            string callId = callData.TryGetValue("callId", out var cid) ? cid?.ToString() ?? string.Empty : string.Empty;
            string callerId = callData.TryGetValue("callerId", out var cidr) ? cidr?.ToString() ?? string.Empty : string.Empty;
            bool isVideo = callData.TryGetValue("isVideo", out var iv) && iv is bool b && b;

            var participants = new List<string>();
            if (callData.TryGetValue("participants", out var pObj) && pObj is List<object> pList)
            {
                participants = pList.Select(p => p?.ToString() ?? string.Empty).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            }

            // Show incoming call notification
            var result = MessageBox.Show(
                $"Cuộc gọi {(isVideo ? "video" : "thoại")} đến" + (participants.Count > 1 ? " (nhóm)" : ""),
                "Cuộc gọi đến",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                OpenCallWindow(callId, isVideo, true, callerId, participants);
            }
            else
            {
                // Reject call
                string? currentUserId = _authService.CurrentUserId;
                if (!string.IsNullOrWhiteSpace(currentUserId))
                {
                    Forget(_callingService.RejectCall(callId, currentUserId));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling incoming call: {ex.Message}");
        }
    }

    private async Task StartCallAsync()
    {
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            ShowToast("Bạn chưa đăng nhập.", "error");
            return;
        }

        if (SelectedConversation == null)
        {
            ShowToast("Vui lòng chọn cuộc trò chuyện.", "error");
            return;
        }

        try
        {
            List<string> participantIds;

            if (SelectedConversation.IsGroup)
            {
                // Group call
                participantIds = SelectedConversation.ParticipantIds
                    .Where(p => !string.IsNullOrWhiteSpace(p) && p != currentUserId)
                    .ToList();
            }
            else
            {
                // 1-1 call
                if (string.IsNullOrWhiteSpace(SelectedConversation.OtherUserId))
                {
                    ShowToast("Không thể xác định người nhận cuộc gọi.", "error");
                    return;
                }
                participantIds = new List<string> { SelectedConversation.OtherUserId };
            }

            if (participantIds.Count == 0)
            {
                ShowToast("Không có người tham gia cuộc gọi.", "error");
                return;
            }

            var (success, message, callId) = await _callingService.InitiateCall(currentUserId, participantIds, false);

            if (!success || string.IsNullOrWhiteSpace(callId))
            {
                ShowToast(message, "error");
                return;
            }

            // Open call window
            OpenCallWindow(callId, false, false, currentUserId, participantIds);
        }
        catch (Exception ex)
        {
            ShowToast($"Lỗi khi bắt đầu cuộc gọi: {ex.Message}", "error");
        }
    }

    private void OpenCallWindow(string callId, bool isVideo, bool isIncoming, string callerId, List<string> participantIds)
    {
        try
        {
            var callViewModel = new CallViewModel(callId, isVideo, isIncoming, callerId, participantIds);
            var callWindow = new CallWindow(callViewModel);
            callWindow.Show();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening call window: {ex.Message}");
            ShowToast("Không thể mở cửa sổ cuộc gọi.", "error");
        }
    }

    private static byte[]? TryLoadAndCompressToJpeg(string filePath, int maxDimension, int quality)
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(filePath, UriKind.Absolute);
            bmp.EndInit();
            bmp.Freeze();

            BitmapSource source = bmp;
            double scale = 1.0;
            int w = source.PixelWidth;
            int h = source.PixelHeight;
            int max = Math.Max(w, h);
            if (max > maxDimension)
            {
                scale = (double)maxDimension / max;
            }

            if (scale < 1.0)
            {
                var transform = new ScaleTransform(scale, scale);
                var transformed = new TransformedBitmap(source, transform);
                transformed.Freeze();
                source = transformed;
            }

            var encoder = new JpegBitmapEncoder { QualityLevel = Math.Clamp(quality, 1, 100) };
            encoder.Frames.Add(BitmapFrame.Create(source));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }
}

