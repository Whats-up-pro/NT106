using System;
using System.Windows;
using System.Windows.Media.Animation;
using ThreeMess.ViewModels;

namespace ThreeMess;

public partial class LandingWindow : Window
{
    public LandingWindow()
    {
        InitializeComponent();

        if (DataContext is LandingViewModel vm)
        {
            vm.OpenLoginRequested += () => Navigate(new LoginWindow());
            vm.OpenSignupRequested += () => Navigate(new SignupWindow());
        }

        Loaded += (_, _) => FadeIn();
    }

    private void Navigate(Window next)
    {
        next.Show();
        Close();
    }

    private void FadeIn()
    {
        var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(OpacityProperty, anim);
    }
}

