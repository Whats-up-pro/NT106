using System;
using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class RightSidebarLinkItemViewModel : ObservableObject
{
    private string _text = string.Empty;
    private Uri? _uri;

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    public Uri? Uri
    {
        get => _uri;
        set => SetProperty(ref _uri, value);
    }

    public DateTime Time { get; init; }
}
