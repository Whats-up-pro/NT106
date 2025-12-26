using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ThreeMess;

public partial class ImagePreviewWindow : Window
{
    private readonly ImageSource? _singleSource;
    private readonly IList<ImageSource>? _sources;
    private int _index;

    public ImagePreviewWindow(ImageSource source)
    {
        InitializeComponent();

        _singleSource = source;
        PreviewImage.Source = source;

        PrevButton.Visibility = Visibility.Collapsed;
        NextButton.Visibility = Visibility.Collapsed;
        IndexBadge.Visibility = Visibility.Collapsed;

        Focusable = true;
        Loaded += (_, _) => Keyboard.Focus(this);
        KeyDown += ImagePreviewWindow_KeyDown;
    }

    public ImagePreviewWindow(IList<ImageSource> sources, int startIndex)
    {
        InitializeComponent();

        _sources = sources ?? throw new ArgumentNullException(nameof(sources));
        if (_sources.Count == 0)
            throw new ArgumentException("Sources cannot be empty.", nameof(sources));

        _index = Math.Clamp(startIndex, 0, _sources.Count - 1);
        PreviewImage.Source = _sources[_index];

        UpdateNavButtons();

        Focusable = true;
        Loaded += (_, _) => Keyboard.Focus(this);
        KeyDown += ImagePreviewWindow_KeyDown;
    }

    private void PrevButton_Click(object sender, RoutedEventArgs e)
    {
        if (_sources == null || _sources.Count <= 1) return;
        if (_index <= 0) return;

        _index--;
        PreviewImage.Source = _sources[_index];
        UpdateNavButtons();
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_sources == null || _sources.Count <= 1) return;
        if (_index >= _sources.Count - 1) return;

        _index++;
        PreviewImage.Source = _sources[_index];
        UpdateNavButtons();
    }

    private void UpdateNavButtons()
    {
        if (_sources == null || _sources.Count <= 1)
        {
            PrevButton.IsEnabled = false;
            NextButton.IsEnabled = false;
            IndexBadge.Visibility = Visibility.Collapsed;
            return;
        }

        PrevButton.IsEnabled = _index > 0;
        NextButton.IsEnabled = _index < _sources.Count - 1;

        IndexBadge.Visibility = Visibility.Visible;
        IndexText.Text = $"{_index + 1}/{_sources.Count}";
    }

    private void ImagePreviewWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        if (_sources == null || _sources.Count <= 1) return;

        if (e.Key == Key.Left)
        {
            PrevButton_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.Right)
        {
            NextButton_Click(sender, e);
            e.Handled = true;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (ContentHost == null)
        {
            Close();
            return;
        }

        if (e.OriginalSource is DependencyObject d && IsDescendantOf(d, ContentHost))
        {
            return;
        }

        Close();
    }

    private static bool IsDescendantOf(DependencyObject child, DependencyObject ancestor)
    {
        DependencyObject? current = child;
        while (current != null)
        {
            if (ReferenceEquals(current, ancestor))
                return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    private BitmapSource? GetCurrentBitmapSource()
    {
        ImageSource? src = _sources != null ? _sources[_index] : _singleSource;
        return src as BitmapSource;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var bitmap = GetCurrentBitmapSource();
        if (bitmap == null)
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
