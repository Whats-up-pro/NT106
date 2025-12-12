using System;
using System.Windows.Input;
using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class LandingViewModel : ObservableObject
{
    public event Action? OpenLoginRequested;
    public event Action? OpenSignupRequested;

    public ICommand OpenLoginCommand { get; }
    public ICommand OpenSignupCommand { get; }

    public LandingViewModel()
    {
        OpenLoginCommand = new RelayCommand(() => OpenLoginRequested?.Invoke());
        OpenSignupCommand = new RelayCommand(() => OpenSignupRequested?.Invoke());
    }
}

