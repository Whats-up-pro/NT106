using System;
using System.Linq;
using System.Windows;

namespace ThreeMess.Services;

public static class ThemeManager
{
    public enum ThemeMode
    {
        Light,
        Dark
    }

    private static readonly Uri LightUri = new("Themes/Light.xaml", UriKind.Relative);
    private static readonly Uri DarkUri = new("Themes/Dark.xaml", UriKind.Relative);

    public static ThemeMode CurrentTheme { get; private set; } = ThemeMode.Light;

    public static void ApplyTheme(ThemeMode theme)
    {
        var app = Application.Current;
        if (app == null) return;

        var dictionaries = app.Resources.MergedDictionaries;

        // Remove any existing theme dictionary.
        var existing = dictionaries
            .Where(d => d.Source != null && (UriEquals(d.Source, LightUri) || UriEquals(d.Source, DarkUri)))
            .ToList();

        foreach (var d in existing)
        {
            dictionaries.Remove(d);
        }

        dictionaries.Insert(0, new ResourceDictionary { Source = theme == ThemeMode.Dark ? DarkUri : LightUri });
        CurrentTheme = theme;
    }

    private static bool UriEquals(Uri a, Uri b)
    {
        // Normalize to string compare; WPF will usually keep relative URIs as-is.
        return string.Equals(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
