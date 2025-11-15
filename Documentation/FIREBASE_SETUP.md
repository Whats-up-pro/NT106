# 🔥 Hướng Dẫn Cấu Hình Firebase

Tài liệu này hướng dẫn chi tiết cách thiết lập Firebase cho ứng dụng Messaging App.

---

## 📋 Mục Lục
1. [Tạo Firebase Project](#1-tạo-firebase-project)
2. [Enable Firebase Authentication](#2-enable-firebase-authentication)
3. [Thiết lập Cloud Firestore](#3-thiết-lập-cloud-firestore)
4. [Tạo Service Account Key](#4-tạo-service-account-key)
5. [Cấu hình Ứng dụng](#5-cấu-hình-ứng-dụng)
6. [Firestore Security Rules](#6-firestore-security-rules)
7. [Kiểm tra Kết nối](#7-kiểm-tra-kết-nối)

---

## 1. Tạo Firebase Project

### Bước 1.1: Truy cập Firebase Console
1. Mở trình duyệt và truy cập: https://console.firebase.google.com
2. Đăng nhập bằng tài khoản Google của bạn

### Bước 1.2: Tạo Project Mới
1. Click **"Add project"** hoặc **"Create a project"**
2. Nhập tên project (ví dụ: `MessagingApp` hoặc `nt106-messaging`)
3. (Tùy chọn) Tắt Google Analytics nếu không cần thiết
4. Click **"Create project"**
5. Đợi Firebase tạo project (khoảng 30 giây)
6. Click **"Continue"** khi hoàn tất

### Bước 1.3: Lưu Project ID
- Sau khi tạo xong, vào **Project Settings** (icon bánh răng ⚙️ bên cạnh "Project Overview")
- Phần **"General"**, copy **Project ID** (ví dụ: `messaging-app-123abc`)
- **LƯU LẠI** Project ID này, sẽ cần dùng sau

---

## 2. Enable Firebase Authentication

### Bước 2.1: Vào Authentication
1. Trong Firebase Console, click **"Authentication"** ở menu bên trái
2. Click **"Get started"** nếu lần đầu sử dụng

### Bước 2.2: Enable Email/Password Provider
1. Click tab **"Sign-in method"**
2. Tìm **"Email/Password"** trong danh sách providers
3. Click vào **"Email/Password"**
4. Toggle **"Enable"** sang ON
5. (Tùy chọn) Có thể bật **"Email link (passwordless sign-in)"** nếu muốn
6. Click **"Save"**

### Bước 2.3: (Tùy chọn) Tạo Test Users
1. Click tab **"Users"**
2. Click **"Add user"**
3. Nhập:
   - Email: `test@example.com`
   - Password: `Test123456`
4. Click **"Add user"**

---

## 3. Thiết lập Cloud Firestore

### Bước 3.1: Vào Firestore Database
1. Click **"Firestore Database"** ở menu bên trái
2. Click **"Create database"**

### Bước 3.2: Chọn Mode
1. Chọn **"Start in production mode"** (khuyến nghị)
   - Security rules sẽ được cấu hình sau
2. Click **"Next"**

### Bước 3.3: Chọn Location
1. Chọn location gần bạn nhất:
   - **asia-southeast1 (Singapore)** - Tốt nhất cho Việt Nam
   - **asia-east1 (Taiwan)**
   - **asia-northeast1 (Tokyo)**
2. Click **"Enable"**
3. Đợi Firestore khởi tạo (khoảng 30-60 giây)

### Bước 3.4: Tạo Collections (Tùy chọn - App sẽ tự tạo)
Ứng dụng sẽ tự động tạo các collections, nhưng nếu muốn tạo trước:

#### Collection: `users`
1. Click **"Start collection"**
2. Collection ID: `users`
3. Thêm document mẫu:
   - Document ID: (auto-generated)
   - Fields:
     ```
     userId: string = "sample_id"
     username: string = "testuser"
     email: string = "test@example.com"
     fullName: string = "Test User"
     status: string = "offline"
     createdAt: timestamp = (current time)
     ```
4. Click **"Save"**

Các collections khác sẽ tự động được tạo khi sử dụng app:
- `friendships`
- `conversations`
- `callHistory`

---

## 4. Tạo Service Account Key

⚠️ **QUAN TRỌNG**: Service Account Key chứa thông tin nhạy cảm. **KHÔNG BAO GIỜ** commit vào Git!

### Bước 4.1: Vào Project Settings
1. Click icon **⚙️ (Settings)** > **"Project settings"**
2. Chọn tab **"Service accounts"**

### Bước 4.2: Generate Private Key
1. Trong phần **"Firebase Admin SDK"**, chọn **C#** (hoặc bất kỳ)
2. Click nút **"Generate new private key"**
3. Một popup xuất hiện cảnh báo bảo mật
4. Click **"Generate key"**
5. File JSON sẽ được download tự động (tên dạng: `messaging-app-123abc-firebase-adminsdk-xxxxx-xxxxxxxxxx.json`)

### Bước 4.3: Lưu File JSON
1. **Đổi tên file** thành: `firebase-credentials.json`
2. **Di chuyển file** vào thư mục:
   ```
   MessagingApp/Config/firebase-credentials.json
   ```
3. **Kiểm tra .gitignore** đã có dòng:
   ```gitignore
   **/firebase-credentials.json
   **/Config/firebase-credentials.json
   firebase-adminsdk-*.json
   ```

---

## 5. Cấu hình Ứng dụng

### Bước 5.1: Cập nhật Project ID
Mở file `MessagingApp/Config/FirebaseConfig.cs`:

```csharp
public const string ProjectId = "your-firebase-project-id"; // TODO: Replace
```

Thay `"your-firebase-project-id"` bằng **Project ID** đã lưu ở Bước 1.3, ví dụ:

```csharp
public const string ProjectId = "messaging-app-123abc";
```

### Bước 5.2: Verify File Structure
Đảm bảo cấu trúc thư mục đúng:

```
MessagingApp/
├── Config/
│   ├── FirebaseConfig.cs
│   └── firebase-credentials.json  ← File này phải tồn tại
├── Services/
│   ├── FirebaseAuthService.cs
│   └── ThemeService.cs
└── ...
```

### Bước 5.3: Restore NuGet Packages
Mở terminal trong thư mục dự án và chạy:

```bash
cd MessagingApp
dotnet restore
```

Kiểm tra các packages đã được cài đặt:
- ✅ FirebaseAdmin (v3.0.0)
- ✅ Google.Cloud.Firestore (v3.7.0)
- ✅ Google.Apis.Auth (v1.68.0)
- ✅ Newtonsoft.Json (v13.0.3)

---

## 6. Firestore Security Rules

### Bước 6.1: Vào Rules Tab
1. Trong **Firestore Database**, click tab **"Rules"**
2. Xóa nội dung hiện tại

### Bước 6.2: Paste Security Rules

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    
    // Helper function: Check if user is authenticated
    function isAuthenticated() {
      return request.auth != null;
    }
    
    // Helper function: Check if user owns the document
    function isOwner(userId) {
      return isAuthenticated() && request.auth.uid == userId;
    }
    
    // Users collection
    match /users/{userId} {
      // Anyone authenticated can read user profiles
      allow read: if isAuthenticated();
      
      // Users can only write their own profile
      allow create: if isOwner(userId);
      allow update, delete: if isOwner(userId);
    }
    
    // Friendships collection
    match /friendships/{friendshipId} {
      // Users can read friendships they're part of
      allow read: if isAuthenticated() && (
        request.auth.uid == resource.data.userId1 ||
        request.auth.uid == resource.data.userId2
      );
      
      // Users can create friendship requests
      allow create: if isAuthenticated();
      
      // Users can update/delete their own friendships
      allow update, delete: if isAuthenticated() && (
        request.auth.uid == resource.data.userId1 ||
        request.auth.uid == resource.data.userId2
      );
    }
    
    // Conversations collection
    match /conversations/{conversationId} {
      // Participants can read the conversation
      allow read: if isAuthenticated() && 
        request.auth.uid in resource.data.participants;
      
      // Authenticated users can create conversations
      allow create: if isAuthenticated();
      
      // Participants can update the conversation
      allow update: if isAuthenticated() && 
        request.auth.uid in resource.data.participants;
      
      // Participants can delete (leave) the conversation
      allow delete: if isAuthenticated() && 
        request.auth.uid in resource.data.participants;
      
      // Messages subcollection
      match /messages/{messageId} {
        // Participants can read messages
        allow read: if isAuthenticated() && 
          request.auth.uid in get(/databases/$(database)/documents/conversations/$(conversationId)).data.participants;
        
        // Authenticated users can create messages
        allow create: if isAuthenticated();
        
        // Senders can update/delete their messages
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
