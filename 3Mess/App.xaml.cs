using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ThreeMess;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		TryOverrideAppMarkFromPng();
	}

	private void TryOverrideAppMarkFromPng()
	{
		try
		{
			// Prefer a logo shipped next to the executable (site-of-origin).
			// Place it at: 3Mess/Assets/logo.png (copied to output).
			string baseDir = AppDomain.CurrentDomain.BaseDirectory;
			string candidate = Path.Combine(baseDir, "Assets", "logo.png");

			if (!File.Exists(candidate))
				return;

			var bitmap = new BitmapImage();
			bitmap.BeginInit();
			bitmap.CacheOption = BitmapCacheOption.OnLoad;
			bitmap.UriSource = new Uri(candidate, UriKind.Absolute);
			bitmap.EndInit();
			bitmap.Freeze();

			Resources["AppMark"] = bitmap;
		}
		catch
		{
			// Fall back to the vector AppMark defined in App.xaml.
		}
	}
}


