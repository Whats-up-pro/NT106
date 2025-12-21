using System.Windows;
using System.Windows.Controls;

namespace ThreeMess.Infrastructure;

public static class PasswordBoxAssist
{
    public static readonly DependencyProperty HasPasswordProperty =
        DependencyProperty.RegisterAttached(
            "HasPassword",
            typeof(bool),
            typeof(PasswordBoxAssist),
            new FrameworkPropertyMetadata(false));

    public static bool GetHasPassword(DependencyObject obj)
        => (bool)obj.GetValue(HasPasswordProperty);

    public static void SetHasPassword(DependencyObject obj, bool value)
        => obj.SetValue(HasPasswordProperty, value);

    public static void UpdateHasPassword(PasswordBox box)
    {
        SetHasPassword(box, !string.IsNullOrEmpty(box.Password));
    }
}

