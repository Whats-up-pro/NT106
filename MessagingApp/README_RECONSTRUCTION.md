# 🚀 Messaging App - Reconstruction với Firebase & Sky Blue Theme

## 📋 Tổng Quan

Ứng dụng nhắn tin và gọi điện được **reconstruction hoàn toàn** với:
- ✅ **Backend**: Firebase Authentication + Cloud Firestore
- ✅ **Theme**: Sky Blue Light/Dark mode (màu xanh da trời #0ea5e9)
- ✅ **Architecture**: Clean Architecture với Services Layer
- ✅ **UI**: Windows Forms với giao diện hiện đại, thân thiện

---

## ✨ Tính Năng Đã Hoàn Thành (Phase 1)

### 🔐 Authentication System
- ✅ **LoginForm** - Đăng nhập với email/password
- ✅ **RegisterForm** - Đăng ký tài khoản mới
- ✅ **ForgotPasswordForm** - Khôi phục mật khẩu qua email
- ✅ Validation đầy đủ (email format, password strength, required fields)
- ✅ Error handling với Firebase exceptions
- ✅ Loading states & user feedback

### 🎨 Theme System
- ✅ **Light Mode** - Sky Blue (#0ea5e9) + White background
- ✅ **Dark Mode** - Sky Blue (#38bdf8) + Dark Slate background (#0f172a)
- ✅ Toggle theme button trong MainForm
- ✅ ThemeService singleton với event-driven theme changes
- ✅ Auto-apply theme to all controls

### 🔥 Firebase Integration
- ✅ Firebase Admin SDK setup
- ✅ FirebaseConfig với auto-initialization
- ✅ FirebaseAuthService (login, register, reset password, sign out)
- ✅ Firestore database ready (users collection)
- ✅ Security rules implemented

---

## 🛠️ Cài Đặt & Chạy

### Prerequisites
- Windows 10/11
- .NET 8.0 SDK
- Firebase account (miễn phí)

### Quick Start
```bash
# Clone repo
git clone https://github.com/Whats-up-pro/NT106.git
cd NT106/MessagingApp

# Setup Firebase (IMPORTANT!)
# 1. Follow Documentation/FIREBASE_SETUP.md
# 2. Place firebase-credentials.json in Config/
# 3. Update ProjectId in Config/FirebaseConfig.cs

# Restore & Build
dotnet restore
dotnet build

# Run
dotnet run
```

---

## 📁 Cấu Trúc Dự Án Mới

```
MessagingApp/
├── Config/
│   ├── FirebaseConfig.cs              # ✅ Firebase initialization
│   └── firebase-credentials.json      # ⚠️ Gitignored
├── Services/
│   ├── FirebaseAuthService.cs         # ✅ Authentication
│   └── ThemeService.cs                # ✅ Light/Dark theme
├── Forms/
│   ├── Auth/
│   │   ├── LoginForm.cs               # ✅ Login UI
│   │   ├── RegisterForm.cs            # ✅ Register UI
│   │   └── ForgotPasswordForm.cs      # ✅ Password reset
│   └── Main/
│       └── MainForm.cs                # ✅ Main dashboard (basic)
├── Documentation/
│   └── FIREBASE_SETUP.md              # ✅ Setup guide
└── Program.cs                         # ✅ Entry point

✅ = Completed | 🔜 = Planned
```

---

## 🎨 Sky Blue Theme

### Light Mode
- Primary: #0ea5e9 (Sky 500)
- Background: #f8fafc (Slate 50)
- Text: #0f172a (Slate 950)

### Dark Mode
- Primary: #38bdf8 (Sky 400)
- Background: #0f172a (Slate 950)
- Text: #f1f5f9 (Slate 100)

**Toggle**: Click "🌙 Chế Độ Tối" / "☀️ Chế Độ Sáng" trong MainForm

---

## 📚 Documentation

- **[FIREBASE_SETUP.md](Documentation/FIREBASE_SETUP.md)** - Hướng dẫn cấu hình Firebase (QUAN TRỌNG!)
- **[RECONSTRUCTION_PLAN.md](../RECONSTRUCTION_PLAN.md)** - Kế hoạch reconstruction chi tiết

---

## 🐛 Troubleshooting

### Lỗi: "Credentials file not found"
→ Đặt `firebase-credentials.json` trong `MessagingApp/Config/`

### Lỗi: "Failed to initialize Firebase"
→ Kiểm tra Project ID trong `FirebaseConfig.cs`

### Chi tiết: Xem [FIREBASE_SETUP.md](Documentation/FIREBASE_SETUP.md)

---

## 📈 Roadmap - Phase 2

- [ ] MainForm - Conversations list (real-time)
- [ ] ProfileForm - Edit profile + avatar
- [ ] FriendsForm - Friends management
- [ ] MessageForm - Real-time chat
- [ ] CallForm - Voice/Video calls

---

## 👥 Team

**614_2U0C Team** | NT106.Q11.ANTT | HK1 2025-2026

- GitHub: [@Whats-up-pro](https://github.com/Whats-up-pro)
- Repository: [NT106](https://github.com/Whats-up-pro/NT106)

---

**Status**: ✅ Phase 1 Complete  
**Build**: ✅ Succeeded (0 errors)  
**Firebase**: ⚙️ Requires setup

Made with ❤️ by 614_2U0C Team
