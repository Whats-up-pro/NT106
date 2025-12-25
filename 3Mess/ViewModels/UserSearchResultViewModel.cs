using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class UserSearchResultViewModel : ObservableObject
{
    private string _userId = string.Empty;
    private string _displayName = string.Empty;
    private string _subtitle = string.Empty;
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

    public string Subtitle
    {
        get => _subtitle;
        set => SetProperty(ref _subtitle, value);
    }

    public string AvatarText
    {
        get => _avatarText;
        set => SetProperty(ref _avatarText, value);
    }
}
