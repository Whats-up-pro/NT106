using System;
using System.Windows;
using System.Windows.Media.Animation;
using ThreeMess.Infrastructure;
using ThreeMess.ViewModels;

namespace ThreeMess;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            FadeIn();
            PasswordBox.PasswordChanged += (_, _) =>
            {
                PasswordBoxAssist.UpdateHasPassword(PasswordBox);
                if (DataContext is LoginViewModel vm)
                {
                    vm.Password = PasswordBox.Password;
                }
            };
        };
    }

    private void FadeIn()
    {
        var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(OpacityProperty, anim);
    }

    private void OpenSignup_Click(object sender, RoutedEventArgs e)
    {
        var w = new SignupWindow();
        w.Show();
        Close();
    }

    private void ForgotPassword_Click(object sender, RoutedEventArgs e)
    {
        var w = new ForgotPasswordWindow();
        w.Show();
        Close();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        var w = new LandingWindow();
        w.Show();
        Close();
    }
}

