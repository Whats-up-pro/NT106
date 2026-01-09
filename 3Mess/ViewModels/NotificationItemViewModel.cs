using System;
using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class NotificationItemViewModel : ObservableObject
{
    private string _title = string.Empty;
    private string _message = string.Empty;
    private DateTime _timestampUtc = DateTime.UtcNow;
    private bool _isUnread = true;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public DateTime TimestampUtc
    {
        get => _timestampUtc;
        set
        {
            if (!SetProperty(ref _timestampUtc, value)) return;
            OnPropertyChanged(nameof(TimeText));
        }
    }

    public bool IsUnread
    {
        get => _isUnread;
        set => SetProperty(ref _isUnread, value);
    }

    public string TimeText
    {
        get
        {
            try
            {
                var local = TimestampUtc.ToLocalTime();
                return local.ToString("HH:mm dd/MM");
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public string Kind { get; init; } = string.Empty;
    public string SourceId { get; init; } = string.Empty;

    // Navigation targets (optional, depending on Kind)
    public string ConversationId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string RequestId { get; init; } = string.Empty;

}
