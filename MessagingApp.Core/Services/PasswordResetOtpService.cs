using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using MessagingApp.Config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MessagingApp.Services;

public sealed class PasswordResetOtpService
{
    private sealed record OtpEntry(string Code, DateTimeOffset ExpiresAt, int Attempts);

    private static PasswordResetOtpService? _instance;
    public static PasswordResetOtpService Instance => _instance ??= new PasswordResetOtpService();

    private readonly FirebaseAuth _auth;
    private readonly FirestoreDb _db;
    private readonly ConcurrentDictionary<string, OtpEntry> _otpByEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, bool> _verifiedByEmail = new(StringComparer.OrdinalIgnoreCase);

    private static readonly TimeSpan OtpValidFor = TimeSpan.FromMinutes(5);
    private const int MaxAttempts = 5;

    private PasswordResetOtpService()
    {
        FirebaseConfig.Initialize();
        _auth = FirebaseAuth.GetAuth(FirebaseConfig.GetApp());
        _db = FirebaseConfig.GetFirestoreDb();
    }

    public async Task<(bool success, string message, Dictionary<string, object>? userData)> LookupAccountAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return (false, "Vui lòng nhập email.", null);

        try
        {
            var user = await _auth.GetUserByEmailAsync(email.Trim());
            var snap = await _db.Collection("users").Document(user.Uid).GetSnapshotAsync();
            if (!snap.Exists)
            {
                return (false, "Không tìm thấy dữ liệu người dùng.", null);
            }

            return (true, "OK", snap.ToDictionary());
        }
        catch (FirebaseAuthException ex) when (
            ex.AuthErrorCode == AuthErrorCode.UserNotFound ||
            ex.AuthErrorCode == AuthErrorCode.EmailNotFound)
        {
            return (false, "Không tìm thấy tài khoản với email này.", null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool success, string message)> SendOtpAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return (false, "Vui lòng nhập email.");

        // Ensure account exists
        try
        {
            await _auth.GetUserByEmailAsync(email.Trim());
        }
        catch
        {
            // Keep generic to avoid leaking account existence
            return (false, "Email hoặc mật khẩu không đúng.");
        }

        var code = Generate6DigitCode();
        var expires = DateTimeOffset.UtcNow.Add(OtpValidFor);
        _otpByEmail[email.Trim()] = new OtpEntry(code, expires, 0);
        _verifiedByEmail[email.Trim()] = false;

        await SmtpEmailService.Instance.SendOtpEmailAsync(email.Trim(), code, OtpValidFor);
        return (true, "Mã xác thực đã được gửi về email. Vui lòng kiểm tra hộp thư (và Spam)." );
    }

    public (bool success, string message) VerifyOtp(string email, string otpCode)
    {
        if (string.IsNullOrWhiteSpace(email))
            return (false, "Vui lòng nhập email.");
        if (string.IsNullOrWhiteSpace(otpCode) || otpCode.Trim().Length != 6)
            return (false, "Vui lòng nhập đủ 6 số mã xác thực.");

        var key = email.Trim();
        if (!_otpByEmail.TryGetValue(key, out var entry))
        {
            return (false, "Chưa gửi mã xác thực hoặc mã đã hết hạn. Vui lòng gửi lại mã.");
        }

        if (DateTimeOffset.UtcNow > entry.ExpiresAt)
        {
            _otpByEmail.TryRemove(key, out _);
            _verifiedByEmail.TryRemove(key, out _);
            return (false, "Mã đã hết hạn. Vui lòng gửi lại mã.");
        }

        if (entry.Attempts >= MaxAttempts)
        {
            _otpByEmail.TryRemove(key, out _);
            _verifiedByEmail.TryRemove(key, out _);
            return (false, "Bạn đã nhập sai quá nhiều lần. Vui lòng gửi lại mã.");
        }

        if (!string.Equals(entry.Code, otpCode.Trim(), StringComparison.Ordinal))
        {
            _otpByEmail[key] = entry with { Attempts = entry.Attempts + 1 };
            return (false, "Mã xác thực không đúng.");
        }

        _verifiedByEmail[key] = true;
        return (true, "Xác thực thành công.");
    }

    public async Task<(bool success, string message)> ResetPasswordAsync(string email, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(email))
            return (false, "Vui lòng nhập email.");
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.");

        var key = email.Trim();
        if (!_verifiedByEmail.TryGetValue(key, out var ok) || !ok)
        {
            return (false, "Bạn chưa xác thực mã. Vui lòng nhập mã xác thực trước.");
        }

        try
        {
            var user = await _auth.GetUserByEmailAsync(key);
            await _auth.UpdateUserAsync(new UserRecordArgs
            {
                Uid = user.Uid,
                Password = newPassword
            });

            _otpByEmail.TryRemove(key, out _);
            _verifiedByEmail.TryRemove(key, out _);

            return (true, "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập lại.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static string Generate6DigitCode()
    {
        // 000000 - 999999
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        int value = BitConverter.ToInt32(bytes);
        value = Math.Abs(value % 1_000_000);
        return value.ToString("D6");
    }
}
