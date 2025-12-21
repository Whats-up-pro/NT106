using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class ConversationItemViewModel : ObservableObject
{
    private string _title = string.Empty;
    private string _subtitle = string.Empty;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Subtitle
    {
        get => _subtitle;
        set => SetProperty(ref _subtitle, value);
    }

    public string AvatarText { get; init; } = "?";

    public string ConversationId { get; init; } = string.Empty;
    public string OtherUserId { get; init; } = string.Empty;
}

