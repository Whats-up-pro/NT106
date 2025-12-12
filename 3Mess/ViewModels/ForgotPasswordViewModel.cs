using System;
using System.Threading.Tasks;
using System.Windows.Input;
using MessagingApp.Services;
using ThreeMess.Infrastructure;

namespace ThreeMess.ViewModels;

public sealed class ForgotPasswordViewModel : ObservableObject
{
    private string _email = string.Empty;
    private string _status = string.Empty;
    private bool _isBusy;

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ((RelayCommand)SendCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand SendCommand { get; }

    public ForgotPasswordViewModel()
    {
        SendCommand = new RelayCommand(() => _ = SendAsync(), () => !IsBusy);
    }

    private async Task SendAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Status = string.Empty;

        try
        {
            var (success, message) = await FirebaseAuthService.Instance.SendPasswordResetEmail(Email.Trim());
            Status = success ? message : $"Lá»—i: {message}";
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

