using System;
using System.Windows;
using System.Windows.Media.Animation;
using ThreeMess.Infrastructure;
using ThreeMess.ViewModels;

namespace ThreeMess;

public partial class SignupWindow : Window
{
    public SignupWindow()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            FadeIn();

            PasswordBox.PasswordChanged += (_, _) =>
            {
                if (DataContext is SignupViewModel vm)
                {
                    PasswordBoxAssist.UpdateHasPassword(PasswordBox);
                    vm.Password = PasswordBox.Password;
                }
            };

            ConfirmPasswordBox.PasswordChanged += (_, _) =>
            {
                if (DataContext is SignupViewModel vm)
                {
                    PasswordBoxAssist.UpdateHasPassword(ConfirmPasswordBox);
                    vm.ConfirmPassword = ConfirmPasswordBox.Password;
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

    private void OpenLogin_Click(object sender, RoutedEventArgs e)
    {
        var w = new LoginWindow();
        w.Show();
        Close();
    }

    private void BackToLanding_Click(object sender, RoutedEventArgs e)
    {
        var w = new LandingWindow();
        w.Show();
        Close();
    }
}

