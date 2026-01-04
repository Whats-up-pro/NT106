using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MessagingApp.Services;

public sealed class SmtpEmailService
{
    private sealed class SmtpConfig
    {
        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; } = 587;
        public string? FromName { get; set; }
        public string? FromEmail { get; set; }
        public string? AppPassword { get; set; }
    }

    private static SmtpEmailService? _instance;
    public static SmtpEmailService Instance => _instance ??= new SmtpEmailService();

    private readonly SmtpConfig _cfg;

    private SmtpEmailService()
    {
        _cfg = LoadConfig() ?? new SmtpConfig();
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_cfg.SmtpHost) &&
        _cfg.SmtpPort > 0 &&
        !string.IsNullOrWhiteSpace(_cfg.FromEmail) &&
        !string.IsNullOrWhiteSpace(_cfg.AppPassword);

    public async Task SendOtpEmailAsync(string toEmail, string otpCode, TimeSpan validFor)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "SMTP chưa được cấu hình. Hãy điền 3Mess/Config/smtp-config.json (fromEmail + appPassword) hoặc đặt env vars SMTP_HOST/SMTP_PORT/SMTP_USER/SMTP_APP_PASSWORD.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_cfg.FromName ?? "3Mess", _cfg.FromEmail));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = "Mã xác thực khôi phục mật khẩu - 3Mess";

        var minutes = Math.Max(1, (int)Math.Ceiling(validFor.TotalMinutes));
        message.Body = new BodyBuilder
        {
            HtmlBody = $@"<div style='font-family: Arial, sans-serif; line-height: 1.6'>
  <h2 style='color:#0ea5e9'>Khôi phục mật khẩu</h2>
  <p>Bạn (hoặc ai đó) vừa yêu cầu khôi phục mật khẩu cho tài khoản 3Mess.</p>
  <p>Mã xác thực của bạn là:</p>
  <div style='font-size:28px; letter-spacing:6px; font-weight:700; margin:16px 0'>{otpCode}</div>
  <p>Mã có hiệu lực trong <b>{minutes} phút</b>.</p>
  <p>Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
</div>"
        }.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_cfg.SmtpHost, _cfg.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_cfg.FromEmail, _cfg.AppPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            var msg = ex.Message ?? string.Empty;
            // Gmail common auth failure
            if (msg.Contains("5.7.8", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("BadCredentials", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("Username and Password not accepted", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Gửi email thất bại: Gmail từ chối đăng nhập SMTP (535 5.7.8). " +
                    "Hãy dùng 'App Password' của Gmail (không phải mật khẩu Gmail), bật 2-Step Verification, " +
                    "và đảm bảo fromEmail đúng là Gmail bạn dùng để gửi.");
            }

            throw new InvalidOperationException($"Gửi email thất bại: {ex.Message}");
        }
    }

    private static SmtpConfig? LoadConfig()
    {
        // 1) Try local shipped config file (3Mess/Config/smtp-config.json copied to output)
        var fromFile = TryLoadFromFile();
        if (fromFile != null) return fromFile;

        // 2) Fallback env vars
        // SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_APP_PASSWORD, SMTP_FROM_NAME
        var host = Environment.GetEnvironmentVariable("SMTP_HOST");
        var portRaw = Environment.GetEnvironmentVariable("SMTP_PORT");
        var user = Environment.GetEnvironmentVariable("SMTP_USER");
        var pass = Environment.GetEnvironmentVariable("SMTP_APP_PASSWORD");
        var name = Environment.GetEnvironmentVariable("SMTP_FROM_NAME");

        if (string.IsNullOrWhiteSpace(host) && string.IsNullOrWhiteSpace(user) && string.IsNullOrWhiteSpace(pass))
        {
            return null;
        }

        int port = 587;
        if (!string.IsNullOrWhiteSpace(portRaw) && int.TryParse(portRaw, out var parsed))
        {
            port = parsed;
        }

        return new SmtpConfig
        {
            SmtpHost = host?.Trim(),
            SmtpPort = port,
            FromEmail = user?.Trim(),
            AppPassword = pass?.Trim(),
            FromName = name?.Trim(),
        };
    }

    private static SmtpConfig? TryLoadFromFile()
    {
        const string fileName = "smtp-config.json";

        try
        {
            var baseDir = AppContext.BaseDirectory;
            var candidates = new List<string>
            {
                Path.Combine(baseDir, "Config", fileName),
                Path.Combine(baseDir, fileName),
            };

            DirectoryInfo? dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 8 && dir != null; i++)
            {
                candidates.Add(Path.Combine(dir.FullName, "Config", fileName));
                candidates.Add(Path.Combine(dir.FullName, "3Mess", "Config", fileName));
                candidates.Add(Path.Combine(dir.FullName, "MessagingApp.Core", "Config", fileName));
                dir = dir.Parent;
            }

            foreach (var path in candidates)
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    continue;

                var json = File.ReadAllText(path);
                var cfg = JsonSerializer.Deserialize<SmtpConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (cfg == null) continue;

                // Accept both camelCase and PascalCase variants
                cfg.SmtpHost ??= cfg.SmtpHost;

                if (!string.IsNullOrWhiteSpace(cfg.SmtpHost) &&
                    !string.IsNullOrWhiteSpace(cfg.FromEmail) &&
                    !string.IsNullOrWhiteSpace(cfg.AppPassword))
                {
                    return cfg;
                }
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }
}
