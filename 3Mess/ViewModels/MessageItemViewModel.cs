using System;
using System.Windows.Media;
using ThreeMess.Infrastructure;
using ThreeMess.Models;

namespace ThreeMess.ViewModels;

public sealed class MessageItemViewModel : ObservableObject
{
    private MessageBubbleKind _kind = MessageBubbleKind.Text;
    private string _text = string.Empty;
    private DateTime _time = DateTime.Now;
    private bool _isOutgoing;
    private ImageSource? _image;
    private string? _fileName;
    private byte[]? _fileBytes;
    private string? _storageBucket;
    private string? _storageObject;
    private string? _linkText;
    private Uri? _linkUri;

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

    public ImageSource? Image
    {
        get => _image;
        set => SetProperty(ref _image, value);
    }

    public string? FileName
    {
        get => _fileName;
        set => SetProperty(ref _fileName, value);
    }

    public byte[]? FileBytes
    {
        get => _fileBytes;
        set => SetProperty(ref _fileBytes, value);
    }

    public string? StorageBucket
    {
        get => _storageBucket;
        set => SetProperty(ref _storageBucket, value);
    }

    public string? StorageObject
    {
        get => _storageObject;
        set => SetProperty(ref _storageObject, value);
    }

    public string? LinkText
    {
        get => _linkText;
        set => SetProperty(ref _linkText, value);
    }

    public Uri? LinkUri
    {
        get => _linkUri;
        set => SetProperty(ref _linkUri, value);
    }

    public string SenderAvatarText { get; init; } = "?";
    public string TimeText => Time.ToString("HH:mm");
}

