# 🔥 Hướng Dẫn Cấu Hình Firebase (3Mess)

Tài liệu này hướng dẫn thiết lập Firebase cho **3Mess (WPF + Firebase/Firestore/Storage)**.

> Lưu ý quan trọng: bản demo/đồ án hiện tại dùng **Firebase Admin SDK (service account JSON) ngay trên máy client** để thao tác Firestore/Storage.
> Đây là cách làm phù hợp cho đồ án/demo nội bộ, nhưng **không khuyến nghị** cho production.

---

## 1) Tạo Firebase Project
1. Vào https://console.firebase.google.com → **Add project**
2. Lưu lại **Project ID** (Project settings → General → Project ID)

---

## 2) Bật Firebase Authentication (Email/Password)
1. Authentication → Sign-in method
2. Enable **Email/Password** → Save

### (Khuyến nghị) Tùy chỉnh email reset mật khẩu
Authentication → Templates → **Password reset**

---

## 3) Bật Cloud Firestore
1. Firestore Database → Create database
2. Chọn location (gợi ý: `asia-southeast1`)

Các collection chính app sẽ tự tạo khi chạy:
- `users`
- `friendRequests`
- `friendships`
- `conversations`
- `messages`

---

## 4) Bật Firebase Storage (nếu dùng gửi file)
1. Build → Storage → Get started
2. Tạo bucket mặc định

Bucket mặc định thường là: `{projectId}.appspot.com`.
Nếu bucket của bạn khác, có thể set env var `FIREBASE_STORAGE_BUCKET`.

---

## 5) Tạo Service Account Key (Admin SDK)
⚠️ **KHÔNG BAO GIỜ commit file JSON này lên Git**.

1. Project settings → Service accounts
2. Generate new private key → tải file JSON
3. Đổi tên thành `firebase-credentials.json`

### Cách cấu hình cho app
Bạn có 2 cách:

**Cách A (khuyến nghị): biến môi trường `FIREBASE_CREDENTIALS`**

PowerShell:
```powershell
$env:FIREBASE_CREDENTIALS="C:\path\to\firebase-credentials.json"
```

**Cách B: đặt file trong repo (đã gitignore)**
- `3Mess/Config/firebase-credentials.json` hoặc `MessagingApp.Core/Config/firebase-credentials.json`

> App sẽ auto-detect `projectId` từ service account JSON (field `project_id`) nên thường **không cần sửa code**.

---

## 6) Cấu hình Firebase Web API Key (bắt buộc cho Login/Forgot Password)
3Mess đăng nhập email/password qua Firebase Auth REST API (`signInWithPassword`) và gửi email reset (`sendOobCode`), nên cần **Web API Key**.

Lấy key:
1. Project settings → General
2. Trong phần “Your apps”, tạo **Web App** (chỉ để lấy key)
3. Copy **Web API Key**

Điền vào file: `3Mess/Config/firebase-client-config.json`
```json
{
  "webApiKey": "YOUR_FIREBASE_WEB_API_KEY",
  "projectId": "your-project-id"
}
```

`projectId` là tùy chọn.

---

## 7) Chạy app
Tại root repo:
```powershell
dotnet restore
dotnet build .\NT106.sln
dotnet run --project .\3Mess\3Mess.csproj
```

---

## 8) Troubleshooting nhanh
- **Lỗi không tìm thấy credentials**: kiểm tra `FIREBASE_CREDENTIALS` hoặc đặt đúng `firebase-credentials.json` vào `3Mess/Config/`.
- **Đăng nhập/Quên mật khẩu báo thiếu API key**: điền `webApiKey` trong `3Mess/Config/firebase-client-config.json`.
- **Upload Storage bị NotFound bucket**: bật Storage trong Firebase Console hoặc set `FIREBASE_STORAGE_BUCKET` đúng.
        allow update, delete: if isAuthenticated() && 
          request.auth.uid == resource.data.senderId;
      }
    }
    
    // Call history collection
    match /callHistory/{callId} {
      // Participants can read call history
      allow read: if isAuthenticated() && (
        request.auth.uid == resource.data.callerId ||
        request.auth.uid == resource.data.receiverId
      );
      
      // Authenticated users can create call records
      allow create: if isAuthenticated();
      
      // Participants can update call records
      allow update: if isAuthenticated() && (
        request.auth.uid == resource.data.callerId ||
        request.auth.uid == resource.data.receiverId
      );
    }
  }
}
```

### Bước 6.3: Publish Rules
1. Click nút **"Publish"**
2. Chờ vài giây để rules được áp dụng

---

## 7. Kiểm tra Kết nối

### Bước 7.1: Test trong Code
Thêm code test vào `Program.cs`:

```csharp
using MessagingApp.Config;

// Test Firebase connection
try
{
    FirebaseConfig.Initialize();
    bool connected = FirebaseConfig.TestConnection();
    
    if (connected)
    {
        MessageBox.Show("✅ Firebase connected successfully!", "Success");
    }
    else
    {
        MessageBox.Show("❌ Firebase connection failed!", "Error");
    }
}
catch (Exception ex)
{
    MessageBox.Show($"❌ Error: {ex.Message}", "Error");
}
```

### Bước 7.2: Run Application
```bash
dotnet run
```

Nếu thấy message "✅ Firebase connected successfully!" → Thành công!

### Bước 7.3: Kiểm tra Firestore Console
1. Vào **Firestore Database** trong Firebase Console
2. Bạn sẽ thấy collections và documents được tạo bởi app

---

## 🔐 Bảo Mật

### ⚠️ KHÔNG BAO GIỜ:
- ❌ Commit `firebase-credentials.json` vào Git
- ❌ Chia sẻ Service Account Key công khai
- ❌ Upload file JSON lên GitHub, Discord, email, v.v.

### ✅ NÊN:
- ✅ Thêm `firebase-credentials.json` vào `.gitignore`
- ✅ Lưu backup file JSON ở nơi an toàn (1Password, Bitwarden, USB, etc.)
- ✅ Sử dụng environment variables cho production
- ✅ Rotate (tạo mới) service account key định kỳ

---

## 🐛 Troubleshooting

### Lỗi: "Credentials file not found"
**Giải pháp:**
1. Kiểm tra file `firebase-credentials.json` có trong `MessagingApp/Config/`
2. Kiểm tra tên file chính xác (không có khoảng trắng, dấu ngoặc)
3. Kiểm tra quyền đọc file (Windows: Right-click → Properties → Security)

### Lỗi: "Failed to initialize Firebase"
**Giải pháp:**
1. Kiểm tra Project ID trong `FirebaseConfig.cs` đúng chưa
2. Kiểm tra file JSON có valid không (mở bằng text editor)
3. Thử tạo lại Service Account Key mới

### Lỗi: "Permission denied" khi access Firestore
**Giải pháp:**
1. Kiểm tra Security Rules đã publish chưa
2. Kiểm tra user đã authenticated chưa
3. Kiểm tra rules có cho phép operation này không

### Lỗi: "The Application Default Credentials are not available"
**Giải pháp:**
1. Kiểm tra biến môi trường `GOOGLE_APPLICATION_CREDENTIALS` (nếu dùng)
2. Hoặc đảm bảo file JSON ở đúng đường dẫn trong code
3. Restart Visual Studio/IDE sau khi thêm file

### App chạy chậm khi connect Firebase
**Giải pháp:**
1. Firebase có thể chậm lần đầu khởi tạo (cold start)
2. Sau lần đầu sẽ nhanh hơn
3. Cân nhắc thêm loading screen

---

## 📚 Tài Liệu Tham Khảo

### Official Documentation
- Firebase Console: https://console.firebase.google.com
- Firebase Admin SDK (.NET): https://firebase.google.com/docs/admin/setup
- Cloud Firestore: https://firebase.google.com/docs/firestore
- Firebase Authentication: https://firebase.google.com/docs/auth

### Code Examples
- Firestore C# Examples: https://cloud.google.com/firestore/docs/samples
- Firebase Admin .NET: https://github.com/firebase/firebase-admin-dotnet

---

## ✅ Checklist Hoàn Thành

- [ ] Tạo Firebase Project
- [ ] Enable Authentication (Email/Password)
- [ ] Thiết lập Cloud Firestore
- [ ] Download Service Account Key
- [ ] Đổi tên file thành `firebase-credentials.json`
- [ ] Move file vào `MessagingApp/Config/`
- [ ] Update Project ID trong `FirebaseConfig.cs`
- [ ] Restore NuGet packages
- [ ] Thêm Security Rules vào Firestore
- [ ] Test connection thành công
- [ ] Verify `.gitignore` đã có `firebase-credentials.json`

---

**Hoàn thành**: Khi tất cả checkbox đều được tick ✅  
**Thời gian ước tính**: 15-30 phút

**Cần hỗ trợ?** Mở issue trên GitHub repo hoặc liên hệ team.

---

Made with ❤️ by 614_2U0C Team
