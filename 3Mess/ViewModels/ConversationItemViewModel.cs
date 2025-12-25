using System.Collections.Generic;
using System.Windows.Media;
using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class ConversationItemViewModel : ObservableObject
{
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private string _avatarText = "?";
    private ImageSource? _avatarImage;
    private bool _isGroup;
    private List<string> _participantIds = new();

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

    public bool IsGroup
    {
        get => _isGroup;
        set => SetProperty(ref _isGroup, value);
    }

    public List<string> ParticipantIds
    {
        get => _participantIds;
        set => SetProperty(ref _participantIds, value ?? new List<string>());
    }

    public string ConversationId { get; init; } = string.Empty;
    public string OtherUserId { get; init; } = string.Empty;
}

