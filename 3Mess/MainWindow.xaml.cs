using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Media;
using Microsoft.Win32;
using ThreeMess.ViewModels;

namespace ThreeMess;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainViewModel();
        DataContext = vm;

        Loaded += (_, _) =>
        {
            UpdateEmptyState();

            if (vm.Messages is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += (_, _) => ScrollToBottom();
            }

            if (vm is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(MainViewModel.SelectedConversation))
                    {
                        UpdateEmptyState();
                    }
                };
            }

            DraftBox.PreviewKeyDown += (_, e) =>
            {
                if (e.Key != Key.Enter) return;
                if (vm.SendMessageCommand.CanExecute(null))
                {
                    vm.SendMessageCommand.Execute(null);
                }
                e.Handled = true;
            };
        };
    }

    private void UpdateEmptyState()
    {
        if (DataContext is not MainViewModel vm) return;
        EmptyState.Visibility = vm.SelectedConversation == null ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ScrollToBottom()
    {
        if (DataContext is not MainViewModel vm) return;
        var last = vm.Messages.LastOrDefault();
        if (last == null) return;
        Dispatcher.BeginInvoke(() => MessagesList.ScrollIntoView(last));
    }

    private static bool IsImagePath(string path)
    {
        var ext = Path.GetExtension(path)?.ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".webp" or ".bmp";
    }

    private async void AttachButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (vm.SelectedConversation == null)
        {
            MessageBox.Show("Vui lòng chọn cuộc trò chuyện trước.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var ofd = new OpenFileDialog
        {
            Title = "Chọn file hoặc hình ảnh để gửi",
            Filter = "Tất cả (*.*)|*.*|Hình ảnh (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp",
            FilterIndex = 1,
            Multiselect = false
        };

        if (ofd.ShowDialog(this) != true) return;

        try
        {
            var info = new FileInfo(ofd.FileName);
            const long maxBytes = 25L * 1024 * 1024;
            if (info.Length > maxBytes)
            {
                MessageBox.Show("File quá lớn. Giới hạn hiện tại: 25MB.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        catch
        {
            MessageBox.Show("Không thể đọc thông tin file.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (IsImagePath(ofd.FileName))
        {
            await vm.SendImageFileAsync(ofd.FileName);
        }
        else
        {
            await vm.SendFileAsync(ofd.FileName);
        }
    }

    private void MessageImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;
        if (fe.DataContext is not MessageItemViewModel msg) return;
        if (msg.Image is not ImageSource src) return;

        try
        {
            var win = new ImagePreviewWindow(src)
            {
                Owner = this
            };
            win.ShowDialog();
        }
        catch
        {
            MessageBox.Show("Không thể mở xem ảnh.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void SendImageMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (vm.SelectedConversation == null)
        {
            MessageBox.Show("Vui lòng chọn cuộc trò chuyện trước.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var ofd = new OpenFileDialog
        {
            Title = "Chọn hình ảnh để gửi",
            Filter = "Hình ảnh (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp|Tất cả (*.*)|*.*",
            Multiselect = false
        };

        if (ofd.ShowDialog(this) != true) return;

        await vm.SendImageFileAsync(ofd.FileName);
    }

    private async void SendFileMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (vm.SelectedConversation == null)
        {
            MessageBox.Show("Vui lòng chọn cuộc trò chuyện trước.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var ofd = new OpenFileDialog
        {
            Title = "Chọn file để gửi",
            Filter = "Tất cả (*.*)|*.*",
            Multiselect = false
        };

        if (ofd.ShowDialog(this) != true) return;

        await vm.SendFileAsync(ofd.FileName);
    }

    private async void SaveFileButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;
        if (fe.DataContext is not MessageItemViewModel msg) return;
        if (DataContext is not MainViewModel vm) return;

        var sfd = new SaveFileDialog
        {
            Title = "Lưu file",
            FileName = string.IsNullOrWhiteSpace(msg.FileName) ? "download" : msg.FileName
        };

        if (sfd.ShowDialog(this) != true) return;

        try
        {
            var (success, message) = await vm.SaveFileMessageToPathAsync(msg, sfd.FileName);
            if (!success)
            {
                MessageBox.Show(message, "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch
        {
            MessageBox.Show("Không thể lưu file.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch
        {
            MessageBox.Show("Không thể mở liên kết.", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        e.Handled = true;
    }
}
