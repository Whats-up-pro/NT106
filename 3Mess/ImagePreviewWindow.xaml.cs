using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ThreeMess;

public partial class ImagePreviewWindow : Window
{
    public ImagePreviewWindow(ImageSource source)
    {
        InitializeComponent();
        PreviewImage.Source = source;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (PreviewImage.Source is not BitmapSource bitmap)
        {
            MessageBox.Show("Không thể lưu ảnh (định dạng ảnh không hỗ trợ).", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var sfd = new SaveFileDialog
        {
            Title = "Lưu ảnh",
            Filter = "PNG (*.png)|*.png|JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg",
            FileName = $"image_{DateTime.Now:yyyyMMdd_HHmmss}.png"
        };

        if (sfd.ShowDialog(this) != true) return;

        try
        {
            var ext = Path.GetExtension(sfd.FileName).ToLowerInvariant();
            BitmapEncoder encoder = ext is ".jpg" or ".jpeg"
                ? new JpegBitmapEncoder { QualityLevel = 90 }
                : new PngBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write);
            encoder.Save(fs);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể lưu ảnh: {ex.Message}", "3Mess", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
