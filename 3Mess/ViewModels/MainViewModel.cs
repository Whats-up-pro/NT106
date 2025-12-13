using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Google.Cloud.Firestore;
using MessagingApp.Services;
using ThreeMess.Infrastructure;
using ThreeMess.Models;

namespace ThreeMess.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly FirebaseAuthService _authService;
    private readonly FirestoreFriendsService _friendsService;
    private readonly FirestoreMessagingService _messagingService;
    private readonly FirebaseStorageService _storageService;
    private FirestoreChangeListener? _messageListener;

    private FriendItemViewModel? _selectedFriend;
    private ConversationItemViewModel? _selectedConversation;
    private string _searchText = string.Empty;
    private string _draftMessage = string.Empty;
    private string _incomingAvatarText = "A";

    public ObservableCollection<FriendItemViewModel> Friends { get; } = new();
    public ObservableCollection<MessageItemViewModel> Messages { get; } = new();
    public ObservableCollection<MemberItemViewModel> Members { get; } = new();

    private readonly List<FriendItemViewModel> _allFriends = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value)) return;
            ApplyFriendsFilter();
        }
    }

    public FriendItemViewModel? SelectedFriend
    {
        get => _selectedFriend;
        set
        {
            if (!SetProperty(ref _selectedFriend, value)) return;
            _ = OpenChatWithFriendAsync(value);
        }
    }

    public ConversationItemViewModel? SelectedConversation
    {
        get => _selectedConversation;
        set
        {
            if (!SetProperty(ref _selectedConversation, value)) return;
            IncomingAvatarText = string.IsNullOrWhiteSpace(value?.AvatarText) ? "A" : value!.AvatarText;
            ((RelayCommand)SendMessageCommand).RaiseCanExecuteChanged();
            _ = SwitchConversationAsync(value);
        }
    }

    public string IncomingAvatarText
    {
        get => _incomingAvatarText;
        private set => SetProperty(ref _incomingAvatarText, value);
    }

    public string DraftMessage
    {
        get => _draftMessage;
        set
        {
            if (SetProperty(ref _draftMessage, value))
            {
                ((RelayCommand)SendMessageCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand SendMessageCommand { get; }

    public MainViewModel()
    {
        _authService = FirebaseAuthService.Instance;
        _friendsService = FirestoreFriendsService.Instance;
        _messagingService = FirestoreMessagingService.Instance;
        _storageService = FirebaseStorageService.Instance;

        SendMessageCommand = new RelayCommand(
            () => _ = SendMessageAsync(),
            () => SelectedConversation != null && !string.IsNullOrWhiteSpace(DraftMessage));

        _ = LoadFriendsAsync();
    }

    private async Task LoadFriendsAsync()
    {
        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return;
        }

        try
        {
            var friendsRaw = await _friendsService.GetFriends(currentUserId);
            var items = friendsRaw
                .Select(d =>
                {
                    string id = d.TryGetValue("userId", out var uid) ? uid?.ToString() ?? string.Empty : string.Empty;
                    string fullName = d.TryGetValue("fullName", out var fn) ? fn?.ToString() ?? string.Empty : string.Empty;
                    string username = d.TryGetValue("username", out var un) ? un?.ToString() ?? string.Empty : string.Empty;
                    string status = d.TryGetValue("status", out var st) ? st?.ToString() ?? "offline" : "offline";
                    string name = string.IsNullOrWhiteSpace(fullName) ? (string.IsNullOrWhiteSpace(username) ? "(Không tên)" : username) : fullName;
                    return new FriendItemViewModel
                    {
                        UserId = id,
                        Name = name,
                        Username = username,
                        Status = status,
                        AvatarText = string.IsNullOrWhiteSpace(name) ? "?" : name.Substring(0, 1).ToUpperInvariant()
                    };
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
                .OrderBy(x => x.Name)
                .ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                _allFriends.Clear();
                _allFriends.AddRange(items);
                ApplyFriendsFilter();

                // Default selection
                SelectedFriend = Friends.FirstOrDefault();
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadFriends failed: {ex.Message}");
        }
    }

    private void ApplyFriendsFilter()
    {
        string needle = (SearchText ?? string.Empty).Trim();

        IEnumerable<FriendItemViewModel> filtered = _allFriends;
        if (!string.IsNullOrWhiteSpace(needle))
        {
            filtered = filtered.Where(f =>
                (!string.IsNullOrWhiteSpace(f.Name) && f.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                || (!string.IsNullOrWhiteSpace(f.Username) && f.Username.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        Friends.Clear();
        foreach (var f in filtered)
        {
            Friends.Add(f);
        }
    }

    private async Task OpenChatWithFriendAsync(FriendItemViewModel? friend)
    {
        if (friend == null) return;

        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        try
        {
            var conversationId = await _messagingService.GetOrCreateConversation(currentUserId, friend.UserId);
            SelectedConversation = new ConversationItemViewModel
            {
                ConversationId = conversationId,
                OtherUserId = friend.UserId,
                Title = friend.Name,
                Subtitle = string.Empty,
                AvatarText = friend.AvatarText
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OpenChatWithFriend failed: {ex.Message}");
        }
    }

    private async Task SwitchConversationAsync(ConversationItemViewModel? conversation)
    {
        _messageListener?.StopAsync();
        _messageListener = null;

        Application.Current.Dispatcher.Invoke(() =>
        {
            Messages.Clear();
            Members.Clear();
        });

        if (conversation == null) return;

        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        // Members (basic for 1-1 chat)
        Application.Current.Dispatcher.Invoke(() =>
        {
            Members.Add(new MemberItemViewModel { Name = "Bạn", AvatarText = "B" });
            if (!string.IsNullOrWhiteSpace(conversation.Title))
            {
                Members.Add(new MemberItemViewModel { Name = conversation.Title, AvatarText = conversation.AvatarText });
            }
        });

        try
        {
            // Initial load
            var raw = await _messagingService.GetMessages(conversation.ConversationId);
            ApplyMessages(raw, currentUserId);

            // Realtime listener
            _messageListener = _messagingService.ListenToMessages(conversation.ConversationId, msgs =>
            {
                if (string.IsNullOrWhiteSpace(_authService.CurrentUserId)) return;
                ApplyMessages(msgs, _authService.CurrentUserId);
            });

            await _messagingService.MarkMessagesAsRead(conversation.ConversationId, currentUserId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SwitchConversation failed: {ex.Message}");
        }
    }

    private void ApplyMessages(List<Dictionary<string, object>> rawMessages, string currentUserId)
    {
        // Sort by timestamp ascending
        var ordered = rawMessages
            .Select(m => new { m, t = ExtractTimestamp(m) })
            .OrderBy(x => x.t)
            .Select(x => x.m)
            .ToList();

        var viewModels = ordered
            .Where(m => m.ContainsKey("senderId") && m.ContainsKey("content"))
            .Select(m =>
            {
                string senderId = m["senderId"]?.ToString() ?? string.Empty;
                string content = m["content"]?.ToString() ?? string.Empty;
                string type = m.TryGetValue("type", out var tObj) ? (tObj?.ToString() ?? "text") : "text";
                var dt = ExtractTimestamp(m);
                bool outgoing = senderId == currentUserId;

                var vm = new MessageItemViewModel
                {
                    IsOutgoing = outgoing,
                    Time = dt == DateTime.MinValue ? DateTime.Now : dt,
                    SenderAvatarText = outgoing ? "B" : IncomingAvatarText
                };

                switch (type.Trim().ToLowerInvariant())
                {
                    case "image":
                        vm.Kind = MessageBubbleKind.Image;
                        vm.Image = TryDecodeDataUrlToImageSource(content);
                        break;
                    case "file":
                        vm.Kind = MessageBubbleKind.File;
                        vm.FileName = m.TryGetValue("fileName", out var fnObj) ? fnObj?.ToString() : "download";
                        vm.StorageBucket = m.TryGetValue("bucket", out var bObj) ? bObj?.ToString() : null;
                        vm.StorageObject = m.TryGetValue("object", out var oObj) ? oObj?.ToString() : (string.IsNullOrWhiteSpace(content) ? null : content);
                        break;
                    case "link":
                        // Backward compatibility if old messages were stored with type=link.
                        vm.Kind = MessageBubbleKind.Link;
                        vm.LinkText = content;
                        vm.LinkUri = TryCreateHttpUri(content);
                        break;
                    default:
                        // Treat pure URL as a clickable link without needing a separate "send link" feature.
                        var uri = TryCreateHttpUri(content);
                        if (uri != null && string.Equals(content.Trim(), uri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                        {
                            vm.Kind = MessageBubbleKind.Link;
                            vm.LinkText = uri.AbsoluteUri;
                            vm.LinkUri = uri;
                        }
                        else
                        {
                            vm.Kind = MessageBubbleKind.Text;
                            vm.Text = content;
                        }
                        break;
                }

                return vm;
            })
            .ToList();

        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            Messages.Clear();
            foreach (var vm in viewModels)
            {
                Messages.Add(vm);
            }
        });
    }

    private static DateTime ExtractTimestamp(Dictionary<string, object> msg)
    {
        try
        {
            if (msg.TryGetValue("timestamp", out var tsObj) && tsObj != null)
            {
                if (tsObj is Timestamp ts)
                {
                    return ts.ToDateTime().ToLocalTime();
                }
                if (tsObj is DateTime dt)
                {
                    return dt.ToLocalTime();
                }
            }
        }
        catch { }
        return DateTime.MinValue;
    }

    private async Task SendMessageAsync()
    {
        var text = DraftMessage.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        var selected = SelectedConversation;
        if (selected == null) return;

        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        try
        {
            var (success, message) = await _messagingService.SendMessage(selected.ConversationId, currentUserId, text);
            if (!success)
            {
                Console.WriteLine(message);
                return;
            }
            DraftMessage = string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SendMessage failed: {ex.Message}");
        }
    }

    public async Task SendImageFileAsync(string filePath)
    {
        var selected = SelectedConversation;
        if (selected == null) return;

        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            MessageBox.Show("Không tìm thấy file hình ảnh.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Firestore document size is limited; keep images small by resizing + JPEG encoding.
        var attemptSettings = new (int maxDim, int quality)[]
        {
            (1024, 80),
            (768, 70),
            (512, 60)
        };

        byte[]? jpegBytes = null;
        foreach (var (maxDim, quality) in attemptSettings)
        {
            jpegBytes = TryLoadAndCompressToJpeg(filePath, maxDim, quality);
            if (jpegBytes == null) continue;
            if (jpegBytes.Length <= 900_000) break;
            jpegBytes = null;
        }

        if (jpegBytes == null)
        {
            MessageBox.Show("Ảnh quá lớn hoặc không thể xử lý. Vui lòng chọn ảnh nhỏ hơn.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string dataUrl = "data:image/jpeg;base64," + Convert.ToBase64String(jpegBytes);

        try
        {
            var (success, message) = await _messagingService.SendMessage(selected.ConversationId, currentUserId, dataUrl, "image");
            if (!success)
            {
                Console.WriteLine(message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SendImage failed: {ex.Message}");
        }
    }

    public async Task SendFileAsync(string filePath)
    {
        var selected = SelectedConversation;
        if (selected == null) return;

        string? currentUserId = _authService.CurrentUserId;
        if (string.IsNullOrWhiteSpace(currentUserId)) return;

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            MessageBox.Show("Không tìm thấy file.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var info = new FileInfo(filePath);
        const long maxBytes = 50L * 1024 * 1024;
        if (info.Length > maxBytes)
        {
            MessageBox.Show("File quá lớn. Giới hạn hiện tại: 50MB.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string name = Path.GetFileName(filePath);
        string objectName = $"uploads/{selected.ConversationId}/{Guid.NewGuid():N}_{name}";

        try
        {
            var upload = await _storageService.UploadFileAsync(filePath, bucket: MessagingApp.Config.FirebaseConfig.StorageBucket, objectName: objectName);
            if (!upload.success)
            {
                MessageBox.Show(upload.message, "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var extras = new Dictionary<string, object>
            {
                { "fileName", name },
                { "size", info.Length },
                { "bucket", upload.bucket },
                { "object", upload.objectName }
            };

            // Store the storage object path in content for backward compatibility.
            var (success, message) = await _messagingService.SendMessage(selected.ConversationId, currentUserId, upload.objectName, "file", extras);
            if (!success)
            {
                Console.WriteLine(message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SendFile failed: {ex.Message}");
            MessageBox.Show("Không thể gửi file.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static Uri? TryCreateHttpUri(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        if (!Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri)) return null;
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        return uri;
    }

    private static ImageSource? TryDecodeDataUrlToImageSource(string dataUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dataUrl)) return null;
            int idx = dataUrl.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            string b64 = dataUrl[(idx + "base64,".Length)..];
            var bytes = Convert.FromBase64String(b64);
            using var ms = new MemoryStream(bytes);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool success, string message)> SaveFileMessageToPathAsync(MessageItemViewModel msg, string destinationPath)
    {
        if (msg.FileBytes is { Length: > 0 })
        {
            try
            {
                await File.WriteAllBytesAsync(destinationPath, msg.FileBytes);
                return (true, "Saved");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        var bucket = string.IsNullOrWhiteSpace(msg.StorageBucket) ? MessagingApp.Config.FirebaseConfig.StorageBucket : msg.StorageBucket;
        var obj = msg.StorageObject;
        if (string.IsNullOrWhiteSpace(obj))
        {
            return (false, "Thiếu thông tin file trên Storage.");
        }

        return await _storageService.DownloadToFileAsync(bucket!, obj!, destinationPath);
    }

    private static byte[]? TryLoadAndCompressToJpeg(string filePath, int maxDimension, int quality)
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(filePath, UriKind.Absolute);
            bmp.EndInit();
            bmp.Freeze();

            BitmapSource source = bmp;
            double scale = 1.0;
            int w = source.PixelWidth;
            int h = source.PixelHeight;
            int max = Math.Max(w, h);
            if (max > maxDimension)
            {
                scale = (double)maxDimension / max;
            }

            if (scale < 1.0)
            {
                var transform = new ScaleTransform(scale, scale);
                var transformed = new TransformedBitmap(source, transform);
                transformed.Freeze();
                source = transformed;
            }

            var encoder = new JpegBitmapEncoder { QualityLevel = Math.Clamp(quality, 1, 100) };
            encoder.Frames.Add(BitmapFrame.Create(source));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }
}

