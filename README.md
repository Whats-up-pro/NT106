# NT106
Ứng dụng nhắn tin và gọi điện đơn giản - Messaging & Calling Application

## 📱 Giới Thiệu
Đây là ứng dụng nhắn tin và gọi điện được xây dựng bằng C# Windows Forms với **Firebase backend** và giao diện **Sky Blue theme** (Light/Dark mode). Ứng dụng cung cấp các tính năng cơ bản cho việc giao tiếp và kết nối.

**🔥 Reconstruction Phase 1 Complete** - Đã chuyển từ SQL Server sang Firebase hoàn toàn!

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
- **Backend**: Firebase (Authentication + Cloud Firestore)
- **Language**: C# 12.0
- **Architecture**: Clean Architecture (Services Layer)

## 🎨 Thiết Kế
- Giao diện màu Sky Blue Light/Dark mode (#0ea5e9, #38bdf8)
- Theme toggle (Light/Dark)
- Thiết kế thân thiện, dễ sử dụng
- Responsive và hiện đại

## 📋 Yêu Cầu Hệ Thống
- Windows 10 hoặc mới hơn
- .NET 8.0 SDK hoặc Runtime
- Firebase account (miễn phí)
- Internet connection (cho Firebase)

## 🚀 Cài Đặt và Chạy

### Bước 1: Clone Repository
```bash
git clone https://github.com/Whats-up-pro/NT106.git
cd NT106/MessagingApp
```

### Bước 2: Setup Firebase (QUAN TRỌNG!)
**Xem hướng dẫn chi tiết:** [Documentation/FIREBASE_SETUP.md](Documentation/FIREBASE_SETUP.md)

**Tóm tắt:**
1. Tạo Firebase project tại https://console.firebase.google.com
2. Enable **Authentication** (Email/Password provider)
3. Enable **Cloud Firestore**
4. Download Service Account Key → Đổi tên thành `firebase-credentials.json`
5. Copy vào `MessagingApp/Config/firebase-credentials.json`
6. Update `ProjectId` trong `Config/FirebaseConfig.cs`

### Bước 3: Build và Chạy
```bash
dotnet restore
dotnet build
dotnet run
```

## 👨‍💻 Tài Khoản
Không có tài khoản mẫu - Đăng ký tài khoản mới qua RegisterForm khi chạy app.

## 📁 Cấu Trúc Dự Án
```
MessagingApp/
├── Config/                        # Firebase configuration
│   ├── FirebaseConfig.cs          # Firebase initialization
│   └── firebase-credentials.json  # Service account key (gitignored)
├── Services/                      # Business logic layer
│   ├── FirebaseAuthService.cs     # Authentication service
│   └── ThemeService.cs            # Theme management (Light/Dark)
├── Forms/                         # Giao diện Windows Forms
│   ├── Auth/                      # Authentication forms
│   │   ├── LoginForm.cs           # Login UI
│   │   ├── RegisterForm.cs        # Register UI
│   │   └── ForgotPasswordForm.cs  # Password reset UI
│   ├── Main/                      # Main application
│   │   └── MainForm.cs            # Main dashboard
│   ├── Social/                    # Friends management (future)
│   ├── Messaging/                 # Chat interface (future)
│   └── Calls/                     # Call interface (future)
├── Models/                        # Data models
│   └── User.cs
├── Utils/                         # Utilities
│   └── ThemeColors.cs             # (Legacy - replaced by ThemeService)
├── Documentation/                 # Tài liệu
│   ├── FIREBASE_SETUP.md          # Firebase setup guide
│   ├── FunctionalDecomposition.md
│   └── DatabaseDesign.md
└── Program.cs                     # Entry point
```

## 📚 Tài Liệu
- **[Firebase Setup Guide](Documentation/FIREBASE_SETUP.md)** - QUAN TRỌNG! Hướng dẫn cấu hình Firebase
- [Reconstruction Plan](RECONSTRUCTION_PLAN.md) - Kế hoạch architecture mới
- [Reconstruction Summary](RECONSTRUCTION_SUMMARY.md) - Tổng kết Phase 1
- [Quick Start](MessagingApp/README_RECONSTRUCTION.md) - Hướng dẫn nhanh
- [Sơ Đồ Phân Rã Chức Năng](MessagingApp/Documentation/FunctionalDecomposition.md)
- [Thiết Kế Cơ Sở Dữ Liệu](MessagingApp/Documentation/DatabaseDesign.md) (Legacy SQL Server)

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
- Firebase Team - Amazing cloud platform
- .NET Team for the amazing framework
- Windows Forms community
- Tailwind CSS - Color palette inspiration (Sky Blue)
- All contributors

---

## 📊 Technology Stack Details

### Backend
- **Firebase Authentication** - Email/password authentication
- **Cloud Firestore** - NoSQL database
- **Firebase Admin SDK** - Server-side operations
- **Google Cloud APIs** - Authentication & authorization

### Frontend
- **Windows Forms** - Desktop UI framework
- **.NET 8.0** - Latest .NET framework
- **C# 12.0** - Modern C# features

### Architecture
- **Clean Architecture** - Separation of concerns
- **Services Layer** - Business logic isolation
- **Singleton Pattern** - Service instances
- **Event-Driven** - Theme change notifications

### NuGet Packages
```xml
<PackageReference Include="FirebaseAdmin" Version="3.0.0" />
<PackageReference Include="Google.Cloud.Firestore" Version="3.7.0" />
<PackageReference Include="Google.Apis.Auth" Version="1.68.0" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

---

## 🎯 Project Status

**Phase 1: ✅ COMPLETE** (Authentication + Theme System)
- ✅ Firebase Authentication integration
- ✅ Login/Register/ForgotPassword forms
- ✅ Sky Blue Light/Dark theme
- ✅ ThemeService with toggle
- ✅ FirebaseAuthService
- ✅ Clean Architecture setup
- ✅ Comprehensive documentation

**Phase 2: 🔜 PLANNED** (Core Features)
- 🔜 MainForm with conversations list
- 🔜 ProfileForm - Edit profile + avatar
- 🔜 FriendsForm - Friends management
- 🔜 MessageForm - Real-time chat
- 🔜 CallForm - Voice/Video calls
- 🔜 Real-time Firestore listeners
- 🔜 Offline support

---

## 🔒 Security Notes

**⚠️ IMPORTANT:**
- `firebase-credentials.json` is **gitignored** - NEVER commit this file
- Service Account Key must be kept secure
- Firestore Security Rules are implemented (see FIREBASE_SETUP.md)
- All user input is validated
- Password hashing handled by Firebase Authentication

---

Made with ❤️ by 614_2U0C Team

