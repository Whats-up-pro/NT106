# 📧 EMAIL_SETUP (Không còn dùng)

Tài liệu này thuộc luồng cũ (OTP/SMTP).

Hiện tại 3Mess dùng **Firebase Authentication gửi email reset mật khẩu trực tiếp** qua REST API `sendOobCode(PASSWORD_RESET)`.
Vì vậy bạn **không cần cấu hình SMTP/Gmail** trong ứng dụng.

Bạn chỉ cần:
- Bật **Authentication → Email/Password**
- (Tuỳ chọn) Tuỳ chỉnh nội dung email trong **Authentication → Templates → Password reset**

Hướng dẫn chuẩn: xem `Documentation/FIREBASE_SETUP.md`.

---

## (Tham khảo) Các phương án gửi email khác
Nếu bạn muốn tự vận hành hệ thống email riêng (không khuyến nghị cho đồ án), bạn có thể cân nhắc các phương án như SendGrid/SMTP.

Phần nội dung dưới đây được giữ lại để tham khảo, nhưng **không còn là yêu cầu của app hiện tại**.

## 🎯 Phương Pháp 1: Sử dụng Firebase Email Extension (Khuyến nghị)

### Bước 1: Cài đặt Extension trong Firebase Console

1. Vào **Firebase Console** → Chọn project của bạn
2. Vào **Extensions** (menu bên trái)
3. Tìm và cài đặt: **"Trigger Email"** hoặc **"SendGrid Email"**
4. Làm theo hướng dẫn cấu hình SMTP

### Bước 2: Bật Email Templates

1. Vào **Authentication** → **Templates**
2. Chọn **Password reset**
3. Tùy chỉnh nội dung email (Vietnamese)
4. Lưu template

### Ưu điểm:
- ✅ Tự động gửi email khi gọi `GeneratePasswordResetLinkAsync()`
- ✅ Không cần code thêm
- ✅ Miễn phí (quota: 25,000 emails/ngày)

---

## 🎯 Phương Pháp 2: Sử dụng SMTP (Gmail)

### Bước 1: Cài đặt Package

```bash
dotnet add package MailKit
```

### Bước 2: Tạo Service gửi email

**File: `Services/EmailService.cs`**

```csharp
using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace MessagingApp.Services
{
    public class EmailService
    {
        private static EmailService? _instance;
        public static EmailService Instance => _instance ??= new EmailService();

        private readonly string _smtpHost = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _fromEmail = "your-email@gmail.com"; // TODO: Thay đổi
        private readonly string _fromPassword = "your-app-password"; // TODO: Thay đổi (App Password, không phải mật khẩu Gmail)

        public async Task<bool> SendPasswordResetEmail(string toEmail, string resetLink)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Messaging App", _fromEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = "Khôi Phục Mật Khẩu - Messaging App";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                                <h2 style='color: #0ea5e9;'>Khôi Phục Mật Khẩu</h2>
                                <p>Xin chào,</p>
                                <p>Bạn đã yêu cầu khôi phục mật khẩu cho tài khoản Messaging App.</p>
                                <p>Click vào link dưới đây để đặt lại mật khẩu:</p>
                                <p>
                                    <a href='{resetLink}' 
                                       style='display: inline-block; padding: 12px 24px; 
                                              background-color: #0ea5e9; color: white; 
                                              text-decoration: none; border-radius: 5px;'>
                                        Đặt Lại Mật Khẩu
                                    </a>
                                </p>
                                <p><small>Link có hiệu lực trong 1 giờ.</small></p>
                                <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                                <hr>
                                <p><small>© 2025 Messaging App. All rights reserved.</small></p>
                            </div>
                        </body>
                        </html>
                    "
                };
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_fromEmail, _fromPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }
    }
}
```

### Bước 3: Cập nhật FirebaseAuthService

**File: `Services/FirebaseAuthService.cs`** (Dòng ~195-206)

```csharp
public async Task<(bool success, string message)> SendPasswordResetEmail(string email)
{
    try
    {
        // Check if user exists
        var userRecord = await _auth.GetUserByEmailAsync(email);

        if (userRecord == null)
        {
            return (false, "Không tìm thấy người dùng với email này.");
        }

        // Generate password reset link
        string resetLink = await _auth.GeneratePasswordResetLinkAsync(email);

        // Send email using EmailService
        var emailService = EmailService.Instance;
        bool emailSent = await emailService.SendPasswordResetEmail(email, resetLink);

        if (emailSent)
        {
            return (true, "Email khôi phục mật khẩu đã được gửi! Vui lòng kiểm tra hộp thư của bạn.");
        }
        else
        {
            // Fallback: Print to console if email fails
            Console.WriteLine($"Password reset link: {resetLink}");
            return (true, "Link khôi phục đã được tạo (kiểm tra console).");
        }
    }
    catch (FirebaseAuthException ex)
    {
        return (false, $"Lỗi: {ex.Message}");
    }
    catch (Exception ex)
    {
        return (false, $"Lỗi: {ex.Message}");
    }
}
```

### Bước 4: Tạo App Password cho Gmail

1. Vào **Google Account Settings**: https://myaccount.google.com/
2. **Security** → **2-Step Verification** (Bật nếu chưa có)
3. **App passwords** → Tạo mật khẩu cho "Mail"
4. Copy mật khẩu 16 ký tự → Dán vào `_fromPassword` trong `EmailService.cs`

### Ưu điểm:
- ✅ Tùy chỉnh email template hoàn toàn
- ✅ Miễn phí (Gmail: 500 emails/ngày)
- ✅ Độc lập với Firebase

### Nhược điểm:
- ❌ Cần bảo mật SMTP credentials
- ❌ Giới hạn 500 emails/ngày (Gmail)

---

## 🎯 Phương Pháp 3: Sử dụng SendGrid (Production)

### Bước 1: Đăng ký SendGrid

1. Vào: https://sendgrid.com/
2. Đăng ký Free Plan (100 emails/ngày miễn phí)
3. Tạo **API Key** trong Settings

### Bước 2: Cài đặt Package

```bash
dotnet add package SendGrid
```

### Bước 3: Tạo SendGrid Service

**File: `Services/SendGridService.cs`**

```csharp
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;

namespace MessagingApp.Services
{
    public class SendGridService
    {
        private static SendGridService? _instance;
        public static SendGridService Instance => _instance ??= new SendGridService();

        private readonly string _apiKey = "YOUR_SENDGRID_API_KEY"; // TODO: Thay đổi
        private readonly string _fromEmail = "noreply@yourdomain.com"; // TODO: Thay đổi
        private readonly string _fromName = "Messaging App";

        public async Task<bool> SendPasswordResetEmail(string toEmail, string resetLink)
        {
            try
            {
                var client = new SendGridClient(_apiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail);
                var subject = "Khôi Phục Mật Khẩu - Messaging App";
                
                var htmlContent = $@"
                    <h2>Khôi Phục Mật Khẩu</h2>
                    <p>Bạn đã yêu cầu khôi phục mật khẩu.</p>
                    <p><a href='{resetLink}' style='padding: 12px 24px; background-color: #0ea5e9; color: white; text-decoration: none;'>Đặt Lại Mật Khẩu</a></p>
                ";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                var response = await client.SendEmailAsync(msg);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendGrid error: {ex.Message}");
                return false;
            }
        }
    }
}
```

### Ưu điểm:
- ✅ Độ tin cậy cao (99.9% uptime)
- ✅ Analytics chi tiết
- ✅ Không bị spam filter
- ✅ Hỗ trợ template động

### Nhược điểm:
- ❌ Cần domain verification (production)
- ❌ Giới hạn 100 emails/ngày (free tier)

---

## 📊 So Sánh

| Tiêu Chí | Firebase Extension | Gmail SMTP | SendGrid |
|----------|-------------------|------------|----------|
| **Miễn phí** | ✅ 25,000/ngày | ✅ 500/ngày | ✅ 100/ngày |
| **Dễ setup** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Độ tin cậy** | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Tùy chỉnh** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Analytics** | ⭐⭐⭐ | ❌ | ⭐⭐⭐⭐⭐ |

---

## 🚀 Khuyến Nghị

1. **Development/Testing**: Dùng **Console Log** (hiện tại) hoặc **Gmail SMTP**
2. **Production nhỏ**: Dùng **Firebase Extension**
3. **Production lớn**: Dùng **SendGrid** hoặc **AWS SES**

---

## 🔒 Bảo Mật

⚠️ **QUAN TRỌNG**: Không commit credentials vào Git!

### Sử dụng Environment Variables:

**File: `appsettings.json`** (Gitignore)
```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "FromEmail": "your-email@gmail.com",
    "FromPassword": "your-app-password"
  },
  "SendGrid": {
    "ApiKey": "YOUR_SENDGRID_API_KEY"
  }
}
```

**File: `.gitignore`**
```
appsettings.json
*.env
```

---

## ✅ Testing

Sau khi cấu hình xong, test bằng cách:

1. Chạy app
2. Click "Quên mật khẩu?"
3. Nhập email của bạn
4. Kiểm tra:
   - Console log có in link không?
   - Email có đến inbox không?
   - Link có hoạt động không?

---

## 📝 Ghi Chú

- Link reset password có hiệu lực **1 giờ**
- Firebase tự động hash link để bảo mật
- Email có thể vào **Spam folder** (nếu dùng Gmail SMTP)
- Production nên dùng **verified domain** để tránh spam

---

**Cần hỗ trợ thêm?** Hãy cho tôi biết phương pháp nào bạn muốn triển khai! 🚀
