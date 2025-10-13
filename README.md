# NT106
Ứng dụng nhắn tin và gọi điện đơn giản - Messaging & Calling Application

## 📱 Giới Thiệu
Đây là ứng dụng nhắn tin và gọi điện được xây dựng bằng C# Windows Forms với giao diện màu xanh dương đen thân thiện với người dùng. Ứng dụng cung cấp các tính năng cơ bản cho việc giao tiếp và kết nối.

## ✨ Tính Năng

### 🔐 Xác Thực
- Đăng nhập an toàn với mã hóa mật khẩu
- Đăng ký tài khoản mới
- Khôi phục mật khẩu qua email

### 👤 Quản Lý Hồ Sơ
- Xem và chỉnh sửa thông tin cá nhân
- Cập nhật trạng thái (Online, Away, Busy, Offline)
- Quản lý thông tin liên hệ

### 👥 Bạn Bè
- Danh sách bạn bè với trạng thái online/offline
- Tìm kiếm bạn bè
- Thêm và quản lý bạn bè

### 💬 Tin Nhắn
- Giao diện nhắn tin trực quan
- Lịch sử tin nhắn
- Gửi tin nhắn văn bản

### 📞 Cuộc Gọi
- Lịch sử cuộc gọi
- Hỗ trợ gọi thoại
- Hỗ trợ gọi video

## 🛠️ Công Nghệ
- **Framework**: .NET 8.0
- **UI**: Windows Forms
- **Database**: Microsoft SQL Server
- **Language**: C# 12.0

## 🎨 Thiết Kế
- Giao diện màu xanh dương đen (#1E3A8A, #2563EB)
- Thiết kế thân thiện, dễ sử dụng
- Responsive và hiện đại

## 📋 Yêu Cầu Hệ Thống
- Windows 10 hoặc mới hơn
- .NET 8.0 SDK hoặc Runtime
- SQL Server (LocalDB, Express, hoặc Full)

## 🚀 Cài Đặt và Chạy

### Bước 1: Clone Repository
```bash
git clone https://github.com/Whats-up-pro/NT106.git
cd NT106/MessagingApp
```

### Bước 2: Cài Đặt Database
1. Mở SQL Server Management Studio
2. Chạy script `Database/CreateDatabase.sql`
3. Database `MessagingAppDB` sẽ được tạo với dữ liệu mẫu

### Bước 3: Cấu Hình Connection String
Mở file `Utils/DatabaseConnection.cs` và cập nhật connection string:
```csharp
private static readonly string connectionString = 
    @"Server=localhost;Database=MessagingAppDB;Integrated Security=True;TrustServerCertificate=True;";
```

### Bước 4: Build và Chạy
```bash
dotnet restore
dotnet build
dotnet run
```

## 👨‍💻 Tài Khoản Mẫu
Sau khi chạy script database, bạn có thể đăng nhập với:

| Username | Password | Tên |
|----------|----------|-----|
| admin | password123 | Quản Trị Viên |
| user1 | password123 | Nguyễn Văn A |
| user2 | password123 | Trần Thị B |

## 📁 Cấu Trúc Dự Án
```
MessagingApp/
├── Database/              # SQL scripts và schema
│   └── CreateDatabase.sql
├── Documentation/         # Tài liệu thiết kế
│   ├── FunctionalDecomposition.md
│   └── DatabaseDesign.md
├── Forms/                # Giao diện Windows Forms
│   ├── LoginForm.cs
│   ├── RegisterForm.cs
│   ├── ForgotPasswordForm.cs
│   ├── MainForm.cs
│   ├── ProfileForm.cs
│   ├── FriendsForm.cs
│   ├── MessageForm.cs
│   └── CallForm.cs
├── Models/               # Data models
│   └── User.cs
├── Utils/                # Tiện ích
│   ├── DatabaseConnection.cs
│   ├── PasswordHelper.cs
│   └── ThemeColors.cs
└── Program.cs            # Entry point
```

## 📚 Tài Liệu
- [Sơ Đồ Phân Rã Chức Năng](MessagingApp/Documentation/FunctionalDecomposition.md)
- [Thiết Kế Cơ Sở Dữ Liệu](MessagingApp/Documentation/DatabaseDesign.md)
- [Hướng Dẫn Sử Dụng](MessagingApp/README.md)

## 🤝 Đóng Góp
Mọi đóng góp đều được chào đón! Vui lòng:
1. Fork repository
2. Tạo branch mới (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Tạo Pull Request

## 📝 License
Dự án được phân phối dưới giấy phép MIT. Xem file [LICENSE](LICENSE) để biết thêm chi tiết.

## 👥 Tác Giả
**614_2U0C Team**

## 📧 Liên Hệ
- GitHub: [@Whats-up-pro](https://github.com/Whats-up-pro)
- Repository: [NT106](https://github.com/Whats-up-pro/NT106)

## 🙏 Acknowledgments
- .NET Team for the amazing framework
- Windows Forms community
- All contributors

---

Made with ❤️ by 614_2U0C Team

