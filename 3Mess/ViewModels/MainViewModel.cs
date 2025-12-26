using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Google.Cloud.Firestore;
using MessagingApp.Services;
using ThreeMess.Infrastructure;
using ThreeMess.Models;

namespace ThreeMess.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly FirebaseAuthService _authService;
    private readonly FirestoreFriendsService _friendsService;
    private readonly FirestoreMessagingService _messagingService;
    private readonly FirebaseStorageService _storageService;
    private FirestoreChangeListener? _messageListener;

    private object? _selectedSidebarItem;
    private ConversationItemViewModel? _selectedConversation;
    private string _searchText = string.Empty;
    private string _draftMessage = string.Empty;
    private string _incomingAvatarText = "A";
    private bool _showGroups;

    private bool _showRightLinks;

    private bool _rightImagesExpanded;
    private bool _rightFilesExpanded;

    private string? _lastSelectedFriendUserId;
    private string? _lastSelectedGroupConversationId;

    private bool _suppressSidebarSelectionHandling;

    public ObservableCollection<object> SidebarItems { get; } = new();
    public ObservableCollection<MessageItemViewModel> Messages { get; } = new();
    public ObservableCollection<MemberItemViewModel> Members { get; } = new();

    public ObservableCollection<RightSidebarAttachmentItemViewModel> RightImages { get; } = new();
    public ObservableCollection<RightSidebarAttachmentItemViewModel> RightFiles { get; } = new();
    public ObservableCollection<RightSidebarLinkItemViewModel> RightLinks { get; } = new();

    private readonly List<FriendItemViewModel> _allFriends = new();
    private readonly List<GroupChatItemViewModel> _allGroups = new();
    private readonly HashSet<string> _locallyHiddenMessageIds = new(StringComparer.Ordinal);

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
                _lastSelectedFriendUserId = f.UserId;
                _ = OpenChatWithFriendAsync(f);
            }
            else if (value is GroupChatItemViewModel g)
            {
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
    public ICommand RevokeMessageCommand { get; }
    public ICommand HideMessageLocallyCommand { get; }
    public ICommand OpenAddFriendCommand { get; }
    public ICommand OpenCreateGroupCommand { get; }
    public ICommand ShowFriendsCommand { get; }
    public ICommand ShowGroupsCommand { get; }

    public ICommand ShowRightDocsCommand { get; }
    public ICommand ShowRightLinksCommand { get; }

    public ICommand ToggleRightImagesExpandedCommand { get; }
    public ICommand ToggleRightFilesExpandedCommand { get; }

    public ICommand DeleteConversationCommand { get; }
    public ICommand TogglePinConversationCommand { get; }
    public ICommand UpdatePinnedCommand { get; }
    public ICommand UpdateNotificationsCommand { get; }

    public MainViewModel()
    {
        _authService = FirebaseAuthService.Instance;
        _friendsService = FirestoreFriendsService.Instance;
        _messagingService = FirestoreMessagingService.Instance;
        _storageService = FirebaseStorageService.Instance;

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

        ShowFriendsCommand = new RelayCommand(() => ShowGroups = false);
        ShowGroupsCommand = new RelayCommand(() => ShowGroups = true);

        ShowRightDocsCommand = new RelayCommand(() => ShowRightLinks = false);
        ShowRightLinksCommand = new RelayCommand(() => ShowRightLinks = true);

        ToggleRightImagesExpandedCommand = new RelayCommand(() => RightImagesExpanded = !RightImagesExpanded);
        ToggleRightFilesExpandedCommand = new RelayCommand(() => RightFilesExpanded = !RightFilesExpanded);

        DeleteConversationCommand = new RelayCommand<object>(o => _ = DeleteConversationAsync(o), o => o != null);
        TogglePinConversationCommand = new RelayCommand<object>(o => _ = TogglePinAsync(o), o => o != null);
        UpdatePinnedCommand = new RelayCommand<object>(o => _ = UpdatePinnedAsync(o), o => o != null);
        UpdateNotificationsCommand = new RelayCommand<object>(o => _ = UpdateNotificationsAsync(o), o => o != null);

        _ = LoadFriendsAsync();
        _ = LoadGroupsAsync();
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

                // Apply conversation settings (pinned/muted/hidden) to friend items
                ApplyFriendConversationSettings();
                ApplySidebarFilter();
            });
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
        if (_messageListener != null)
        {
            try
            {
                await _messageListener.StopAsync();
            }
            catch { }
            _messageListener = null;
        }

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
                    SenderAvatarText = outgoing ? "B" : IncomingAvatarText
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

