using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MessagingApp.Services;
using ThreeMess.Infrastructure;
using ThreeMess;

namespace ThreeMess.ViewModels;

public sealed class LoginViewModel : ObservableObject
{
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _status = string.Empty;
    private bool _isBusy;
    private bool _rememberMe;

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public bool RememberMe
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand LoginCommand { get; }

    public LoginViewModel()
    {
        LoginCommand = new RelayCommand(() => _ = LoginAsync(), () => !IsBusy);
    }

    private async Task LoginAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Status = string.Empty;

        try
        {
            var (success, message, userId) = await FirebaseAuthService.Instance.SignInWithEmailPassword(Email.Trim(), Password);
            Status = message;

            if (!success || string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                var main = new MainWindow();
                main.Show();

                foreach (Window w in Application.Current.Windows)
                {
                    if (w is LoginWindow)
                    {
                        w.Close();
                        break;
                    }
                }
            });
        }
        catch (Exception ex)
        {
            Status = $"Lá»—i: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

