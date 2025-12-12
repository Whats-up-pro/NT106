using System;
using ThreeMess.Infrastructure;
using ThreeMess.Models;

namespace ThreeMess.ViewModels;

public sealed class MessageItemViewModel : ObservableObject
{
    private string _text = string.Empty;
    private DateTime _time = DateTime.Now;
    private bool _isOutgoing;

    public MessageBubbleKind Kind { get; init; } = MessageBubbleKind.Text;

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    public DateTime Time
    {
        get => _time;
        set => SetProperty(ref _time, value);
    }

    public bool IsOutgoing
    {
        get => _isOutgoing;
        set => SetProperty(ref _isOutgoing, value);
    }

    public string SenderAvatarText { get; init; } = "?";
    public string TimeText => Time.ToString("HH:mm");
}

