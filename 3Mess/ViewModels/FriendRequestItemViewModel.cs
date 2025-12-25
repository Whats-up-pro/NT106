using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class FriendRequestItemViewModel : ObservableObject
{
    private string _requestId = string.Empty;
    private string _fromUserId = string.Empty;
    private string _displayName = string.Empty;
    private string _subtitle = string.Empty;
    private string _avatarText = "?";

    public string RequestId
    {
        get => _requestId;
        set => SetProperty(ref _requestId, value);
    }

    public string FromUserId
    {
        get => _fromUserId;
        set => SetProperty(ref _fromUserId, value);
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
