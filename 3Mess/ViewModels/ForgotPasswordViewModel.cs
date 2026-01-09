using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ThreeMess.Infrastructure;
using MessagingApp.Services;

namespace ThreeMess.ViewModels;

public sealed class ForgotPasswordViewModel : ObservableObject
{
    // Steps:
    // 0: Enter account email
    // 1: Confirm account
    // 2: Send reset email
    // 3: Done
    private int _step;
    private string _email = string.Empty;
    private string _accountName = string.Empty;
    private string _accountUsername = string.Empty;
    private string _status = string.Empty;
    private bool _isBusy;

    private string _confirmedAccountEmail = string.Empty;

    public int Step
    {
        get => _step;
        set => SetProperty(ref _step, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string AccountName
    {
        get => _accountName;
        set => SetProperty(ref _accountName, value);
    }

    public string AccountUsername
    {
        get => _accountUsername;
        set => SetProperty(ref _accountUsername, value);
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
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ConfirmAccountCommand).RaiseCanExecuteChanged();
                ((RelayCommand)SendResetEmailCommand).RaiseCanExecuteChanged();
                ((RelayCommand)BackCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand NextCommand { get; }
    public ICommand ConfirmAccountCommand { get; }
    public ICommand SendResetEmailCommand { get; }
    public ICommand BackCommand { get; }

    public ForgotPasswordViewModel()
    {
        Step = 0;
        NextCommand = new RelayCommand(() => _ = NextAsync(), () => !IsBusy);
        ConfirmAccountCommand = new RelayCommand(() => ConfirmAccount(), () => !IsBusy);
        SendResetEmailCommand = new RelayCommand(() => _ = SendResetEmailAsync(), () => !IsBusy);
        BackCommand = new RelayCommand(Back, () => !IsBusy);
    }

    private async Task NextAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Status = string.Empty;

        try
        {
            var (success, message, userData) = await PasswordResetOtpService.Instance.LookupAccountAsync(Email.Trim());
            if (!success)
            {
                Status = $"Lỗi: {message}";
                return;
            }

            AccountName = userData != null && userData.TryGetValue("fullName", out var fn) ? fn?.ToString() ?? string.Empty : string.Empty;
            AccountUsername = userData != null && userData.TryGetValue("username", out var un) ? un?.ToString() ?? string.Empty : string.Empty;
            _confirmedAccountEmail = Email.Trim();

            Step = 1;
        }
        catch (Exception ex)
        {
            Status = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ConfirmAccount()
    {
        Status = string.Empty;
        if (string.IsNullOrWhiteSpace(_confirmedAccountEmail))
        {
            Status = "Lỗi: Vui lòng nhập email trước.";
            Step = 0;
            return;
        }

        Step = 2;
    }

    private async Task SendResetEmailAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Status = string.Empty;

        try
        {
            var (success, message) = await FirebaseAuthService.Instance.SendPasswordResetEmail(_confirmedAccountEmail);
            Status = success ? message : $"Lỗi: {message}";
            if (success) Step = 3;
        }
        catch (Exception ex)
        {
            Status = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Back()
    {
        Status = string.Empty;
        if (Step <= 0)
        {
            Step = 0;
            return;
        }

        // Simple back behavior
        Step -= 1;
    }
}

