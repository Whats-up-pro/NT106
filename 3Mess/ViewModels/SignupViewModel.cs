using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Google.Cloud.Firestore;
using MessagingApp.Config;
using MessagingApp.Services;
using ThreeMess.Infrastructure;
using ThreeMess.Services;
using ThreeMess;

namespace ThreeMess.ViewModels;

public sealed class SignupViewModel : ObservableObject
{
    private int _step = 1;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _displayName = string.Empty;
    private string _status = string.Empty;
    private bool _isBusy;

    public int Step
    {
        get => _step;
        set
        {
            if (SetProperty(ref _step, value))
            {
                OnPropertyChanged(nameof(IsStep1));
                OnPropertyChanged(nameof(IsStep2));
            }
        }
    }

    public bool IsStep1 => Step == 1;
    public bool IsStep2 => Step == 2;

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

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
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
                ((RelayCommand)BackCommand).RaiseCanExecuteChanged();
                ((RelayCommand)SignupCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand NextCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand SignupCommand { get; }

    public SignupViewModel()
    {
        NextCommand = new RelayCommand(Next, () => !IsBusy);
        BackCommand = new RelayCommand(Back, () => !IsBusy);
        SignupCommand = new RelayCommand(() => _ = SignupAsync(), () => !IsBusy);
    }

    private void Next()
    {
        Status = string.Empty;

        if (!IsValidEmail(Email))
        {
            Status = "Email không hợp lệ.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
        {
            Status = "Mật khẩu tối thiểu 6 ký tự.";
            return;
        }
        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            Status = "Mật khẩu xác nhận không khớp.";
            return;
        }

        Step = 2;
    }

    private void Back()
    {
        Status = string.Empty;
        Step = 1;
    }

    private async Task SignupAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        Status = string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                Status = "Vui lòng nhập tên hiển thị.";
                return;
            }

            // Map to existing service signature: username + fullName
            string username = BuildUsernameFromEmail(Email);
            string fullName = DisplayName.Trim();

            var (success, message, userId) = await FirebaseAuthService.Instance.SignUpWithEmailPassword(
                Email.Trim(), Password, username, fullName);

            if (!success || string.IsNullOrWhiteSpace(userId))
            {
                Status = message;
                return;
            }

            // Generate RSA keys for new account
            var (publicPem, _) = RsaKeyService.GenerateAndStoreForUser(userId);

            // Store public key in Firestore user doc
            await SetUserPublicKeyAsync(userId, publicPem);

            Status = "Đăng ký thành công!";

            Application.Current.Dispatcher.Invoke(() =>
            {
                var main = new MainWindow();
                main.Show();

                foreach (Window w in Application.Current.Windows)
                {
                    if (w is SignupWindow)
                    {
                        w.Close();
                        break;
                    }
                }
            });
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

    private static async Task SetUserPublicKeyAsync(string userId, string publicPem)
    {
        // Use shared config to update users doc.
        FirebaseConfig.Initialize();
        var db = FirebaseConfig.GetFirestoreDb();
        await db.Collection("users").Document(userId).SetAsync(new
        {
            rsaPublicKeyPem = publicPem,
            rsaCreatedAt = Timestamp.GetCurrentTimestamp()
        }, SetOptions.MergeAll);
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return Regex.IsMatch(email.Trim(), "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$");
    }

    private static string BuildUsernameFromEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0) return "user";
        var baseName = email.Substring(0, at);
        baseName = Regex.Replace(baseName, "[^a-zA-Z0-9._-]", "");
        if (string.IsNullOrWhiteSpace(baseName)) baseName = "user";
        return baseName;
    }
}

