using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class MemberItemViewModel : ObservableObject
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string AvatarText { get; init; } = "?";
}

