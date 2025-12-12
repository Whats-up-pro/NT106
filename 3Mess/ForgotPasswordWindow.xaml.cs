using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace ThreeMess;

public partial class ForgotPasswordWindow : Window
{
    public ForgotPasswordWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => FadeIn();
    }

    private void FadeIn()
    {
        var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(OpacityProperty, anim);
    }

    private void OpenLogin_Click(object sender, RoutedEventArgs e)
    {
        var w = new LoginWindow();
        w.Show();
        Close();
    }
}

