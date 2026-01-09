# NT106 – 3Mess
Ứng dụng nhắn tin realtime (WPF + Firebase/Firestore) cho đồ án NT106.

## 📱 Giới thiệu
`3Mess` là app desktop viết bằng C#/.NET 8 trên Windows, giao diện WPF theo mô hình MVVM.
Backend dùng Firebase (Authentication + Cloud Firestore) để đăng nhập/đăng ký và đồng bộ tin nhắn theo thời gian thực.

Hiện tại solution chính gồm:
- `3Mess`: WPF app (UI/MVVM)
- `MessagingApp.Core`: thư viện class library chứa config/models/services dùng chung (Firebase/Firestore)

> Lưu ý: Thư mục `MessagingApp/` (WinForms cũ) có thể còn trong repo để tham khảo, nhưng không còn nằm trong solution chính.

## ✨ Tính năng hiện có
- 🔐 Xác thực: Landing / Login / Signup (2 bước) / Forgot Password
- 👥 Bạn bè: danh sách bạn bè + tìm kiếm trong danh sách
- 💬 Chat 1-1: mở cuộc trò chuyện theo friend, load lịch sử + realtime listener
- 🔑 RSA khi đăng ký: tạo RSA-2048; public key lưu Firestore, private key lưu local (DPAPI)

## 🛠️ Công nghệ
- .NET 8 (`net8.0-windows`)
- WPF + MVVM (Binding, ICommand, ObservableObject)
- Firebase Admin SDK + Google Cloud Firestore
- Firebase Auth REST API (Identity Toolkit) cho đăng nhập email/password + gửi email reset mật khẩu

## 📋 Yêu cầu
- Windows 10+ / Windows 11
- .NET 8 SDK
- Firebase project có bật:
	- Authentication (Email/Password)
	- Cloud Firestore
	- Storage (nếu dùng gửi file)

## 🚀 Cài đặt & chạy

### 1) Clone repo
```bash
git clone https://github.com/Whats-up-pro/NT106.git
cd NT106
```

### 2) Setup Firebase credentials (QUAN TRỌNG)
Xem hướng dẫn chi tiết: `Documentation/FIREBASE_SETUP.md`.

App cần service account JSON (ví dụ `firebase-credentials.json`). Có 2 cách cấu hình:

**Cách A (khuyến nghị): set biến môi trường `FIREBASE_CREDENTIALS`**

PowerShell:
```powershell
$env:FIREBASE_CREDENTIALS="C:\path\to\firebase-credentials.json"
```

CMD:
```bat
set FIREBASE_CREDENTIALS=C:\path\to\firebase-credentials.json
```

**Cách B: đặt file vào repo**
- Copy JSON vào `3Mess/Config/firebase-credentials.json` hoặc `MessagingApp.Core/Config/firebase-credentials.json` (file này đã được `.gitignore`, tuyệt đối không commit).

Ghi chú:
- Project ID thường được auto-detect từ service account JSON (`project_id`), nên không cần sửa code.
- Nếu muốn override thủ công: set env var `FIREBASE_PROJECT_ID`.

### 3) Cấu hình Firebase Web API Key (để đăng nhập + quên mật khẩu)
App đăng nhập email/password qua Firebase Auth REST API nên cần **Web API Key**.

- Điền vào file `3Mess/Config/firebase-client-config.json` (file này được copy ra output khi build).

Ví dụ:
```json
{
	"webApiKey": "YOUR_FIREBASE_WEB_API_KEY",
	"projectId": "your-project-id" 
}
```

> `projectId` là tùy chọn (service account thường đã đủ). Nếu bạn dùng Storage và bucket không phải mặc định, bạn có thể set env var `FIREBASE_STORAGE_BUCKET`.

### 3) Build & run
```powershell
dotnet restore
dotnet build .\NT106.sln
dotnet run --project .\3Mess\3Mess.csproj
```

## 📁 Cấu trúc repo (tóm tắt)
```
NT106.sln
3Mess/                 # WPF app (UI + ViewModels)
MessagingApp.Core/     # Shared services/config/models (Firebase/Firestore)
Documentation/         # FIREBASE_SETUP.md, docs khác
MessagingApp/          # Legacy WinForms (không còn là app chính)
```

## 🔒 Lưu ý bảo mật
- Không commit service account key (`firebase-credentials.json`) lên Git.
- Web API Key không phải secret tuyệt đối, nhưng cũng không nên hardcode bừa bãi khi public repo.
- Nếu share repo cho người khác, chỉ share hướng dẫn setup, không share file JSON.

## 📝 License
MIT – xem `LICENSE`.

