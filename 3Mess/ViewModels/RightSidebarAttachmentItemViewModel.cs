using System;
using System.Windows.Media;
using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public enum RightSidebarAttachmentKind
{
    Image,
    File
}

public sealed class RightSidebarAttachmentItemViewModel : ObservableObject
{
    private RightSidebarAttachmentKind _kind;
    private ImageSource? _thumbnail;
    private string _title = string.Empty;

    public RightSidebarAttachmentKind Kind
    {
        get => _kind;
        set => SetProperty(ref _kind, value);
    }

    public ImageSource? Thumbnail
    {
        get => _thumbnail;
        set => SetProperty(ref _thumbnail, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string? StorageBucket { get; init; }
    public string? StorageObject { get; init; }
    public DateTime Time { get; init; }
}
