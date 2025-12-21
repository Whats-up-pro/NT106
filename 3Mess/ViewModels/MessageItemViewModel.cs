using System;
using ThreeMess.Infrastructure;
using ThreeMess.Models;

namespace ThreeMess.ViewModels;

public sealed class MessageItemViewModel : ObservableObject
{
    private string _text = string.Empty;
    private DateTime _time = DateTime.Now;
    private bool _isOutgoing;
    private MessageBubbleKind _kind = MessageBubbleKind.Text;

    public string MessageId { get; init; } = string.Empty;
    public string SenderId { get; init; } = string.Empty;

    public MessageBubbleKind Kind
    {
        get => _kind;
        set => SetProperty(ref _kind, value);
    }

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
    
    // File attachment properties
    public string? FileName { get; set; }
    public byte[]? FileBytes { get; set; }
    public string? StorageBucket { get; set; }
    public string? StorageObject { get; set; }
    
    // Image properties
    public System.Windows.Media.ImageSource? Image { get; set; }
    
    // Link properties
    public string? LinkText { get; set; }
    public System.Uri? LinkUri { get; set; }
}

