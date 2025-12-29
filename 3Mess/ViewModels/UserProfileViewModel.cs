using System.Windows.Media;
using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class UserProfileViewModel : ObservableObject
{
    private string _userId = string.Empty;
    private string _displayName = string.Empty;
    private string _subtitle = string.Empty;
    private string _about = string.Empty;
    private int _friendsCount;
    private string _avatarText = "?";
    private ImageSource? _avatarImage;
    private ImageSource? _coverImage;
    private bool _isBusy;
    private string _statusText = string.Empty;

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

    public string Subtitle
    {
        get => _subtitle;
        set => SetProperty(ref _subtitle, value);
    }

    public string About
    {
        get => _about;
        set => SetProperty(ref _about, value);
    }

    public int FriendsCount
    {
        get => _friendsCount;
        set => SetProperty(ref _friendsCount, value);
    }

    public string FriendsCountText => $"Bạn bè · {FriendsCount}";

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

    public ImageSource? CoverImage
    {
        get => _coverImage;
        set => SetProperty(ref _coverImage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }
}
