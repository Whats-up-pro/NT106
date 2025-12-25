using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MessagingApp.Services;
using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class AddFriendViewModel : ObservableObject
{
    private readonly FirebaseAuthService _authService;
    private readonly FirestoreFriendsService _friendsService;

    private string _searchText = string.Empty;
    private UserSearchResultViewModel? _selectedUser;
    private string _statusText = "";
    private string _requestsStatusText = "";
    private bool _isBusy;

    public ObservableCollection<UserSearchResultViewModel> Results { get; } = new();
    public ObservableCollection<FriendRequestItemViewModel> PendingRequests { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                OnPropertyChanged(nameof(CanSearch));
            }
        }
    }

    public UserSearchResultViewModel? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value))
            {
                OnPropertyChanged(nameof(CanSend));
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string RequestsStatusText
    {
        get => _requestsStatusText;
        private set => SetProperty(ref _requestsStatusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanSearch));
                OnPropertyChanged(nameof(CanSend));
                ((RelayCommand)SearchCommand).RaiseCanExecuteChanged();
                ((RelayCommand)SendFriendRequestCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanSearch => !IsBusy && !string.IsNullOrWhiteSpace(SearchText);
    public bool CanSend => !IsBusy && SelectedUser != null;

    public ICommand SearchCommand { get; }
    public ICommand SendFriendRequestCommand { get; }
    public ICommand RefreshRequestsCommand { get; }
    public ICommand AcceptRequestCommand { get; }
    public ICommand DeclineRequestCommand { get; }

    public event Action? RequestClose;

    public AddFriendViewModel()
    {
        _authService = FirebaseAuthService.Instance;
        _friendsService = FirestoreFriendsService.Instance;

        SearchCommand = new RelayCommand(
            () => _ = SearchAsync(),
            () => CanSearch);

        SendFriendRequestCommand = new RelayCommand(
            () => _ = SendRequestAsync(),
            () => CanSend);

        RefreshRequestsCommand = new RelayCommand(
            () => _ = LoadPendingRequestsAsync(),
            () => !IsBusy);

        AcceptRequestCommand = new RelayCommand<FriendRequestItemViewModel>(
            req => _ = AcceptRequestAsync(req),
            req => !IsBusy && req != null && !string.IsNullOrWhiteSpace(req.RequestId) && !string.IsNullOrWhiteSpace(req.FromUserId));

        DeclineRequestCommand = new RelayCommand<FriendRequestItemViewModel>(
            req => _ = DeclineRequestAsync(req),
            req => !IsBusy && req != null && !string.IsNullOrWhiteSpace(req.RequestId));

        StatusText = "Nhập email hoặc username để tìm người dùng.";
        RequestsStatusText = "";
    }

    public void OnLoaded()
    {
        _ = LoadPendingRequestsAsync();
    }

    private async Task LoadPendingRequestsAsync()
    {
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            RequestsStatusText = "Bạn chưa đăng nhập.";
            return;
        }

        try
        {
            IsBusy = true;
            RequestsStatusText = "Đang tải lời mời...";

            List<Dictionary<string, object>> raw = await _friendsService.GetPendingRequests(currentUserId);

            var mapped = raw
                .Select(d =>
                {
                    string requestId = d.TryGetValue("requestId", out var rid) ? rid?.ToString() ?? string.Empty : string.Empty;
                    string fromUserId = d.TryGetValue("fromUserId", out var fuid) ? fuid?.ToString() ?? string.Empty : string.Empty;
                    string fullName = d.TryGetValue("fromUserFullName", out var fn) ? fn?.ToString() ?? string.Empty : string.Empty;
                    string username = d.TryGetValue("fromUserUsername", out var un) ? un?.ToString() ?? string.Empty : string.Empty;
                    string email = d.TryGetValue("fromUserEmail", out var em) ? em?.ToString() ?? string.Empty : string.Empty;

                    string display = string.IsNullOrWhiteSpace(fullName)
                        ? (string.IsNullOrWhiteSpace(username) ? (string.IsNullOrWhiteSpace(email) ? "(Không tên)" : email) : username)
                        : fullName;

                    string subtitle = !string.IsNullOrWhiteSpace(username)
                        ? $"@{username}" + (!string.IsNullOrWhiteSpace(email) ? $" • {email}" : "")
                        : email;

                    return new FriendRequestItemViewModel
                    {
                        RequestId = requestId,
                        FromUserId = fromUserId,
                        DisplayName = display,
                        Subtitle = subtitle,
                        AvatarText = string.IsNullOrWhiteSpace(display) ? "?" : display.Substring(0, 1).ToUpperInvariant()
                    };
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.RequestId) && !string.IsNullOrWhiteSpace(x.FromUserId))
                .ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                PendingRequests.Clear();
                foreach (var r in mapped)
                {
                    PendingRequests.Add(r);
                }
            });

            RequestsStatusText = PendingRequests.Count == 0 ? "Không có lời mời nào." : $"Có {PendingRequests.Count} lời mời.";
        }
        catch (Exception ex)
        {
            RequestsStatusText = $"Lỗi khi tải lời mời: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SearchAsync()
    {
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            StatusText = "Bạn chưa đăng nhập.";
            return;
        }

        string query = (SearchText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            StatusText = "Vui lòng nhập email hoặc username.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusText = "Đang tìm kiếm...";

            List<Dictionary<string, object>> raw = await _friendsService.SearchUsers(query, currentUserId);

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
                        AvatarText = string.IsNullOrWhiteSpace(display) ? "?" : display.Substring(0, 1).ToUpperInvariant()
                    };
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
                .ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Results.Clear();
                foreach (var r in mapped)
                {
                    Results.Add(r);
                }
                SelectedUser = Results.FirstOrDefault();
            });

            StatusText = Results.Count == 0 ? "Không tìm thấy người dùng." : $"Tìm thấy {Results.Count} người dùng.";
        }
        catch (Exception ex)
        {
            StatusText = $"Lỗi khi tìm kiếm: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SendRequestAsync()
    {
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            StatusText = "Bạn chưa đăng nhập.";
            return;
        }

        if (SelectedUser == null || string.IsNullOrWhiteSpace(SelectedUser.UserId))
        {
            StatusText = "Vui lòng chọn một người dùng.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusText = "Đang gửi lời mời...";

            var (success, message) = await _friendsService.SendFriendRequest(currentUserId, SelectedUser.UserId);
            StatusText = message;

            if (success)
            {
                MessageBox.Show(message, "Kết bạn", MessageBoxButton.OK, MessageBoxImage.Information);
                RequestClose?.Invoke();
            }
            else
            {
                MessageBox.Show(message, "Kết bạn", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Lỗi: {ex.Message}";
            MessageBox.Show(StatusText, "Kết bạn", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AcceptRequestAsync(FriendRequestItemViewModel? req)
    {
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            RequestsStatusText = "Bạn chưa đăng nhập.";
            return;
        }

        if (req == null || string.IsNullOrWhiteSpace(req.RequestId) || string.IsNullOrWhiteSpace(req.FromUserId))
        {
            RequestsStatusText = "Lời mời không hợp lệ.";
            return;
        }

        try
        {
            IsBusy = true;
            RequestsStatusText = "Đang chấp nhận...";

            var (success, message) = await _friendsService.AcceptFriendRequest(req.RequestId, req.FromUserId, currentUserId);
            RequestsStatusText = message;

            if (success)
            {
                MessageBox.Show(message, "Kết bạn", MessageBoxButton.OK, MessageBoxImage.Information);
                RequestClose?.Invoke();
                return;
            }

            MessageBox.Show(message, "Kết bạn", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            RequestsStatusText = $"Lỗi: {ex.Message}";
            MessageBox.Show(RequestsStatusText, "Kết bạn", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeclineRequestAsync(FriendRequestItemViewModel? req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.RequestId))
        {
            RequestsStatusText = "Lời mời không hợp lệ.";
            return;
        }

        try
        {
            IsBusy = true;
            RequestsStatusText = "Đang từ chối...";

            var (success, message) = await _friendsService.DeclineFriendRequest(req.RequestId);
            RequestsStatusText = message;

            if (success)
            {
                await LoadPendingRequestsAsync();
                return;
            }

            MessageBox.Show(message, "Kết bạn", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            RequestsStatusText = $"Lỗi: {ex.Message}";
            MessageBox.Show(RequestsStatusText, "Kết bạn", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
