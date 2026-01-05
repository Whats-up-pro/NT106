using ThreeMess.Infrastructure;
using System.Windows.Input;
using System.Windows.Media;

namespace ThreeMess.ViewModels;

public sealed class MemberItemViewModel : ObservableObject
{
    private string _userId = string.Empty;
    private string _name = string.Empty;
    private string _roleLabel = string.Empty;
    private ImageSource? _avatarImage;
    private bool _canKick;

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

    public string RoleLabel
    {
        get => _roleLabel;
        set => SetProperty(ref _roleLabel, value);
    }

    public ImageSource? AvatarImage
    {
        get => _avatarImage;
        set => SetProperty(ref _avatarImage, value);
    }

    public string AvatarText { get; init; } = "?";

    public bool CanKick
    {
        get => _canKick;
        set => SetProperty(ref _canKick, value);
    }

    public ICommand ViewProfileCommand { get; set; } = new RelayCommand(() => { });
    public ICommand MessageCommand { get; set; } = new RelayCommand(() => { });
    public ICommand KickCommand { get; set; } = new RelayCommand(() => { }, () => false);
}

