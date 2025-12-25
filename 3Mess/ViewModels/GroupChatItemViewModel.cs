using System.Collections.Generic;
using System;
using System.Windows.Media;
using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class GroupChatItemViewModel : ObservableObject
{
    private string _conversationId = string.Empty;
    private string _name = string.Empty;
    private string _avatarText = "G";
    private ImageSource? _avatarImage;
    private bool _isPinned;
    private bool _notificationsEnabled = true;
    private bool _isHidden;
    private DateTime? _lastActivityUtc;

    public string ConversationId
    {
        get => _conversationId;
        set => SetProperty(ref _conversationId, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
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

    public List<string> ParticipantIds { get; init; } = new();
}
