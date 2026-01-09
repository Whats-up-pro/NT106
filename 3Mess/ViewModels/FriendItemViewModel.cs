using System;
using System.Windows.Media;
using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class FriendItemViewModel : ObservableObject
{
    private string _userId = string.Empty;
    private string _name = string.Empty;
    private string _username = string.Empty;
    private string _status = "offline";
    private string _avatarText = "?";
    private ImageSource? _avatarImage;
    private bool _isPinned;
    private bool _notificationsEnabled = true;
    private bool _isHidden;
    private DateTime? _lastActivityUtc;

    public string UserId
    {
        get => _userId;
        set => SetProperty(ref _userId, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string AvatarText
    {
        get => _avatarText;
        set => SetProperty(ref _avatarText, value);
    }

    public ImageSource? AvatarImage
    {
        get => _avatarImage;
        set => SetProperty(ref _avatarImage, value);
    }

    public bool IsPinned
    {
        get => _isPinned;
        set => SetProperty(ref _isPinned, value);
    }

    // True = notifications ON (switch to the right)
    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set => SetProperty(ref _notificationsEnabled, value);
    }

    // True = hidden from sidebar (used by "Xóa cuộc trò chuyện")
    public bool IsHidden
    {
        get => _isHidden;
        set => SetProperty(ref _isHidden, value);
    }

    public DateTime? LastActivityUtc
    {
        get => _lastActivityUtc;
        set => SetProperty(ref _lastActivityUtc, value);
    }
}

