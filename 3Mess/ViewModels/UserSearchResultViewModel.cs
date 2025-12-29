using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class UserSearchResultViewModel : ObservableObject
{
    private string _userId = string.Empty;
    private string _displayName = string.Empty;
    private string _subtitle = string.Empty;
    private string _avatarText = "?";
    private bool _isFriend;
    private bool _isRequestPending;
    private bool _isSelf;
    private bool _isIncomingRequestPending;
    private string _incomingRequestId = string.Empty;

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

    public bool IsFriend
    {
        get => _isFriend;
        set
        {
            if (SetProperty(ref _isFriend, value))
            {
                OnPropertyChanged(nameof(ActionText));
                OnPropertyChanged(nameof(CanAction));
            }
        }
    }

    public bool IsRequestPending
    {
        get => _isRequestPending;
        set
        {
            if (SetProperty(ref _isRequestPending, value))
            {
                OnPropertyChanged(nameof(ActionText));
                OnPropertyChanged(nameof(CanAction));
            }
        }
    }

    public bool IsSelf
    {
        get => _isSelf;
        set
        {
            if (SetProperty(ref _isSelf, value))
            {
                OnPropertyChanged(nameof(ActionText));
                OnPropertyChanged(nameof(CanAction));
                OnPropertyChanged(nameof(ShowIncomingActions));
            }
        }
    }

    public bool IsIncomingRequestPending
    {
        get => _isIncomingRequestPending;
        set
        {
            if (SetProperty(ref _isIncomingRequestPending, value))
            {
                OnPropertyChanged(nameof(ActionText));
                OnPropertyChanged(nameof(CanAction));
                OnPropertyChanged(nameof(ShowIncomingActions));
            }
        }
    }

    public string IncomingRequestId
    {
        get => _incomingRequestId;
        set => SetProperty(ref _incomingRequestId, value);
    }

    public string ActionText
    {
        get
        {
            if (IsSelf) return "Bạn";
            if (IsIncomingRequestPending) return "Chấp nhận";
            if (IsFriend) return "Bạn bè";
            if (IsRequestPending) return "Đã gửi";
            return "Kết bạn";
        }
    }

    public bool CanAction => !IsSelf && (!IsIncomingRequestPending);

    public bool ShowIncomingActions => !IsSelf && IsIncomingRequestPending;
}
