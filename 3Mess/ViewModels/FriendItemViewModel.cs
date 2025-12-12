using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class FriendItemViewModel : ObservableObject
{
    public string UserId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Status { get; init; } = "offline";
    public string AvatarText { get; init; } = "?";
}

