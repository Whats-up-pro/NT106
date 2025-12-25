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
using Microsoft.Win32;
using MessagingApp.Services;
using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class CreateGroupViewModel : ObservableObject
{
    private readonly FirebaseAuthService _authService;
    private readonly FirestoreMessagingService _messagingService;
    private readonly List<FriendItemViewModel> _friends;

    private string _groupName = string.Empty;
    private string _friendSearchText = string.Empty;
    private FriendItemViewModel? _selectedFriend;
    private string _statusText = "";
    private bool _isBusy;

    private ImageSource? _avatarPreview;
    private string? _avatarDataUrl;

    public ObservableCollection<FriendItemViewModel> FilteredFriends { get; } = new();
    public ObservableCollection<FriendItemViewModel> SelectedMembers { get; } = new();

    public string GroupName
    {
        get => _groupName;
        set
        {
            if (SetProperty(ref _groupName, value))
            {
                OnPropertyChanged(nameof(CanCreate));
                ((RelayCommand)CreateGroupCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string FriendSearchText
    {
        get => _friendSearchText;
        set
        {
            if (SetProperty(ref _friendSearchText, value))
            {
                ApplyFriendsFilter();
            }
        }
    }

    public FriendItemViewModel? SelectedFriend
    {
        get => _selectedFriend;
        set => SetProperty(ref _selectedFriend, value);
    }

    public ImageSource? AvatarPreview
    {
        get => _avatarPreview;
        private set => SetProperty(ref _avatarPreview, value);
    }

    public string AvatarText
    {
        get
        {
            var name = (GroupName ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(name) ? "G" : name.Substring(0, 1).ToUpperInvariant();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanCreate));
                ((RelayCommand)CreateGroupCommand).RaiseCanExecuteChanged();
                ((RelayCommand)PickAvatarCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanCreate => !IsBusy
                             && !string.IsNullOrWhiteSpace(GroupName)
                             && SelectedMembers.Count >= 2;

    public ICommand AddMemberCommand { get; }
    public ICommand RemoveMemberCommand { get; }
    public ICommand PickAvatarCommand { get; }
    public ICommand CreateGroupCommand { get; }

    public event Action<string>? Created;

    public CreateGroupViewModel(IReadOnlyList<FriendItemViewModel> friends)
    {
        _authService = FirebaseAuthService.Instance;
        _messagingService = FirestoreMessagingService.Instance;
        _friends = friends?.ToList() ?? new List<FriendItemViewModel>();

        AddMemberCommand = new RelayCommand<FriendItemViewModel>(
            f => AddMember(f),
            f => !IsBusy && f != null);

        RemoveMemberCommand = new RelayCommand<FriendItemViewModel>(
            f => RemoveMember(f),
            f => !IsBusy && f != null);

        PickAvatarCommand = new RelayCommand(
            () => PickAvatar(),
            () => !IsBusy);

        CreateGroupCommand = new RelayCommand(
            () => _ = CreateGroupAsync(),
            () => CanCreate);

        StatusText = "Chọn ít nhất 2 bạn bè để tạo nhóm.";
        ApplyFriendsFilter();

        SelectedMembers.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CanCreate));
            ((RelayCommand)CreateGroupCommand).RaiseCanExecuteChanged();
        };
    }

    public void OnLoaded()
    {
        OnPropertyChanged(nameof(AvatarText));
    }

    private void ApplyFriendsFilter()
    {
        string needle = (FriendSearchText ?? string.Empty).Trim();
        IEnumerable<FriendItemViewModel> filtered = _friends;

        if (!string.IsNullOrWhiteSpace(needle))
        {
            filtered = filtered.Where(f =>
                (!string.IsNullOrWhiteSpace(f.Name) && f.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                || (!string.IsNullOrWhiteSpace(f.Username) && f.Username.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        // Hide already selected
        var selectedIds = new HashSet<string>(SelectedMembers.Select(x => x.UserId), StringComparer.Ordinal);
        filtered = filtered.Where(f => !selectedIds.Contains(f.UserId));

        FilteredFriends.Clear();
        foreach (var f in filtered.OrderBy(x => x.Name))
        {
            FilteredFriends.Add(f);
        }
    }

    private void AddMember(FriendItemViewModel? friend)
    {
        if (friend == null) return;
        if (SelectedMembers.Any(x => x.UserId == friend.UserId)) return;

        SelectedMembers.Add(friend);
        ApplyFriendsFilter();
        StatusText = $"Đã chọn {SelectedMembers.Count} thành viên.";
    }

    private void RemoveMember(FriendItemViewModel? friend)
    {
        if (friend == null) return;

        var existing = SelectedMembers.FirstOrDefault(x => x.UserId == friend.UserId);
        if (existing != null)
        {
            SelectedMembers.Remove(existing);
            ApplyFriendsFilter();
            StatusText = SelectedMembers.Count == 0 ? "Chọn ít nhất 2 bạn bè để tạo nhóm." : $"Đã chọn {SelectedMembers.Count} thành viên.";
        }
    }

    private void PickAvatar()
    {
        var ofd = new OpenFileDialog
        {
            Title = "Chọn avatar nhóm",
            Filter = "Hình ảnh (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp",
            FilterIndex = 1,
            Multiselect = false
        };

        if (ofd.ShowDialog() != true) return;

        // Keep avatar small (data URL stored on conversation doc)
        var jpeg = TryLoadAndCompressToJpeg(ofd.FileName, maxDim: 256, quality: 75);
        if (jpeg == null)
        {
            StatusText = "Không thể đọc ảnh avatar.";
            return;
        }

        _avatarDataUrl = "data:image/jpeg;base64," + Convert.ToBase64String(jpeg);
        AvatarPreview = TryDecodeDataUrlToImageSource(_avatarDataUrl);
        OnPropertyChanged(nameof(AvatarText));
        StatusText = "Đã chọn avatar.";
    }

    private async Task CreateGroupAsync()
    {
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            StatusText = "Bạn chưa đăng nhập.";
            return;
        }

        var name = (GroupName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusText = "Vui lòng nhập tên nhóm.";
            return;
        }

        if (SelectedMembers.Count < 2)
        {
            StatusText = "Cần ít nhất 2 bạn bè để tạo nhóm.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusText = "Đang tạo nhóm...";

            var participantIds = SelectedMembers.Select(m => m.UserId).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            var conversationId = await _messagingService.CreateGroupConversation(currentUserId, name, participantIds, _avatarDataUrl);

            StatusText = "Tạo nhóm thành công.";
            Created?.Invoke(conversationId);
        }
        catch (Exception ex)
        {
            StatusText = $"Lỗi: {ex.Message}";
            MessageBox.Show(StatusText, "Tạo nhóm", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static byte[]? TryLoadAndCompressToJpeg(string filePath, int maxDim, int quality)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = stream;
            bmp.EndInit();
            bmp.Freeze();

            int w = bmp.PixelWidth;
            int h = bmp.PixelHeight;
            if (w <= 0 || h <= 0) return null;

            double scale = Math.Min(1.0, (double)maxDim / Math.Max(w, h));
            var transformed = new TransformedBitmap(bmp, new ScaleTransform(scale, scale));
            transformed.Freeze();

            var encoder = new JpegBitmapEncoder { QualityLevel = Math.Clamp(quality, 30, 95) };
            encoder.Frames.Add(BitmapFrame.Create(transformed));

            using var ms = new MemoryStream();
            encoder.Save(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource? TryDecodeDataUrlToImageSource(string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl)) return null;
        const string Prefix = "base64,";
        int idx = dataUrl.IndexOf(Prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        try
        {
            string b64 = dataUrl[(idx + Prefix.Length)..];
            byte[] bytes = Convert.FromBase64String(b64);
            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = new MemoryStream(bytes);
            img.EndInit();
            img.Freeze();
            return img;
        }
        catch
        {
            return null;
        }
    }
}
