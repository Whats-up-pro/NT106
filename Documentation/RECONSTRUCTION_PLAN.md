# 📋 Kế Hoạch Reconstruction - Messaging App với Firebase

## 🎯 Mục Tiêu
Xây dựng lại ứng dụng nhắn tin C# Windows Forms với:
- **Backend**: Firebase (Authentication + Firestore Database)
- **Theme**: Light/Dark mode với màu xanh da trời (#0ea5e9 sky-500) hoặc xanh lá cây (#10b981 emerald-500)
- **Architecture**: Clean Architecture với Services Layer
- **Priority**: Authentication (Login, Register, Forgot Password) + Database cơ bản

---

## 🏗️ Kiến Trúc Mới

### Cấu Trúc Thư Mục
```
MessagingApp/
├── Config/
│   ├── FirebaseConfig.cs           # Firebase configuration
│   └── firebase-credentials.json   # Service account key (gitignored)
├── Services/
│   ├── FirebaseAuthService.cs      # Authentication service
│   ├── FirestoreService.cs         # Database operations
│   └── ThemeService.cs             # Theme management
├── Models/
│   ├── User.cs                     # User model
│   ├── Message.cs                  # Message model
│   ├── Conversation.cs             # Conversation model
│   ├── Friendship.cs               # Friendship model
│   └── CallHistory.cs              # Call history model
├── Forms/
│   ├── Auth/
│   │   ├── LoginForm.cs           # Login UI
│   │   ├── RegisterForm.cs        # Register UI
│   │   └── ForgotPasswordForm.cs  # Password reset UI
│   ├── Main/
│   │   ├── MainForm.cs            # Main dashboard
│   │   ├── ProfileForm.cs         # User profile
│   │   └── SettingsForm.cs        # App settings (theme toggle)
│   ├── Social/
│   │   └── FriendsForm.cs         # Friends list
│   ├── Messaging/
│   │   └── MessageForm.cs         # Chat interface
│   └── Calls/
│       └── CallForm.cs            # Call interface
├── Utils/
│   ├── ThemeColors.cs             # Color definitions
│   └── Validators.cs              # Input validation
└── Documentation/
    ├── FIREBASE_SETUP.md          # Firebase setup guide
    └── THEME_GUIDE.md             # Theme customization guide
```

---

## 🎨 Theme Design

### Option 1: Xanh Da Trời (Sky Blue) - RECOMMENDED
**Light Mode**
- Primary: `#0ea5e9` (Sky 500)
- Background: `#f8fafc` (Slate 50)
- Surface: `#ffffff` (White)
- Text: `#0f172a` (Slate 950)
- Border: `#e2e8f0` (Slate 200)
- Accent: `#38bdf8` (Sky 400)

**Dark Mode**
- Primary: `#38bdf8` (Sky 400)
- Background: `#0f172a` (Slate 950)
- Surface: `#1e293b` (Slate 800)
- Text: `#f1f5f9` (Slate 100)
- Border: `#334155` (Slate 700)
- Accent: `#7dd3fc` (Sky 300)

### Option 2: Xanh Lá Cây (Emerald Green)
**Light Mode**
- Primary: `#10b981` (Emerald 500)
- Background: `#f0fdf4` (Green 50)
- Surface: `#ffffff` (White)
- Text: `#064e3b` (Emerald 950)
- Border: `#d1fae5` (Emerald 200)

**Dark Mode**
- Primary: `#34d399` (Emerald 400)
- Background: `#064e3b` (Emerald 950)
- Surface: `#065f46` (Emerald 800)
- Text: `#d1fae5` (Emerald 100)
- Border: `#047857` (Emerald 700)

---

## 🔥 Firebase Setup

### 1. Firebase Console Setup
1. Tạo project tại https://console.firebase.google.com
2. Enable **Authentication** → Email/Password provider
3. Enable **Cloud Firestore** → Production mode
4. Tạo Service Account:
   - Project Settings → Service Accounts
   - Generate new private key (JSON)
   - Download → rename thành `firebase-credentials.json`

### 2. Firestore Database Structure

#### Collection: `users`
```json
{
  "userId": "string (Auth UID)",
  "username": "string (unique)",
  "email": "string",
  "fullName": "string",
  "phoneNumber": "string?",
  "avatarUrl": "string?",
  "bio": "string?",
  "status": "online|away|busy|offline",
  "createdAt": "timestamp",
  "lastLogin": "timestamp",
  "isActive": "boolean",
  "theme": "light|dark"
}
```

#### Collection: `friendships`
```json
{
  "friendshipId": "string (auto-generated)",
  "userId1": "string",
  "userId2": "string",
  "status": "pending|accepted|blocked",
  "createdAt": "timestamp",
  "acceptedAt": "timestamp?"
}
```

#### Collection: `conversations`
```json
{
  "conversationId": "string (auto-generated)",
  "name": "string?",
  "type": "direct|group",
  "participants": ["userId1", "userId2", ...],
  "lastMessage": "string?",
  "lastMessageTime": "timestamp?",
  "createdAt": "timestamp"
}
```

#### Subcollection: `conversations/{id}/messages`
```json
{
  "messageId": "string (auto-generated)",
  "senderId": "string",
  "content": "string",
  "type": "text|image|file",
  "sentAt": "timestamp",
  "readBy": ["userId1", ...],
  "isEdited": "boolean",
  "isDeleted": "boolean"
}
```

#### Collection: `callHistory`
```json
{
  "callId": "string (auto-generated)",
  "callerId": "string",
  "receiverId": "string",
  "type": "voice|video",
  "status": "completed|missed|declined|failed",
  "duration": "number (seconds)",
  "startTime": "timestamp",
  "endTime": "timestamp?"
}
```

### 3. Firestore Indexes
```
- users: email (ascending)
- users: username (ascending)
- friendships: [userId1, userId2] (composite)
- conversations: participants (array-contains)
- messages: [conversationId, sentAt] (composite)
- callHistory: [callerId, startTime] (composite)
```

### 4. Security Rules (Basic)
```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // Users can read/write their own data
    match /users/{userId} {
      allow read: if request.auth != null;
      allow write: if request.auth.uid == userId;
    }
    
    // Authenticated users can read friendships
    match /friendships/{friendshipId} {
      allow read: if request.auth != null;
      allow create: if request.auth != null;
      allow update, delete: if request.auth.uid in resource.data.values();
    }
    
    // Conversation participants can read/write
    match /conversations/{conversationId} {
      allow read: if request.auth.uid in resource.data.participants;
      allow create: if request.auth != null;
      allow update: if request.auth.uid in resource.data.participants;
      
      match /messages/{messageId} {
        allow read: if request.auth.uid in get(/databases/$(database)/documents/conversations/$(conversationId)).data.participants;
        allow create: if request.auth != null;
        allow update: if request.auth.uid == resource.data.senderId;
      }
    }
    
    // Call history - participants can read
    match /callHistory/{callId} {
      allow read: if request.auth.uid == resource.data.callerId || 
                     request.auth.uid == resource.data.receiverId;
      allow create: if request.auth != null;
    }
  }
}
```

---

## 📦 NuGet Packages Cần Thiết

```xml
<PackageReference Include="FirebaseAdmin" Version="3.0.0" />
<PackageReference Include="Google.Cloud.Firestore" Version="3.7.0" />
<PackageReference Include="Google.Apis.Auth" Version="1.68.0" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

---

## 🚀 Phase 1: Authentication & Database (PRIORITY)

### Step 1: Setup Firebase
- [x] Tạo kế hoạch
- [ ] Cài đặt NuGet packages
- [ ] Tạo FirebaseConfig.cs
- [ ] Add firebase-credentials.json
- [ ] Test connection

### Step 2: Theme System
- [ ] Tạo ThemeColors.cs với Sky Blue theme
- [ ] Tạo ThemeService.cs
- [ ] Implement Light/Dark toggle
- [ ] Test theme switching

### Step 3: Authentication Forms
- [ ] LoginForm (UI + Firebase Auth)
- [ ] RegisterForm (UI + Firebase Auth + Firestore)
- [ ] ForgotPasswordForm (UI + Firebase Auth)
- [ ] Test authentication flow

### Step 4: Services Layer
- [ ] FirebaseAuthService.cs
  - SignIn(email, password)
  - SignUp(email, password, userData)
  - ResetPassword(email)
  - SignOut()
  - GetCurrentUser()
  
- [ ] FirestoreService.cs
  - CreateUser(user)
  - GetUser(userId)
  - UpdateUser(userId, data)
  - CreateConversation(participants)
  - SendMessage(conversationId, message)
  - GetMessages(conversationId)

### Step 5: Models
- [ ] User.cs with Firestore attributes
- [ ] Message.cs
- [ ] Conversation.cs
- [ ] Friendship.cs

### Step 6: Basic UI Flow
- [ ] Program.cs → LoginForm
- [ ] Login success → MainForm
- [ ] Register → Auto login → MainForm
- [ ] Logout → LoginForm

---

## 📝 Phase 2: Core Features (Future)

### MainForm
- [ ] User info header
- [ ] Conversations list (real-time)
- [ ] Navigation sidebar
- [ ] Theme toggle button

### ProfileForm
- [ ] Display user info
- [ ] Edit profile
- [ ] Upload avatar (Firebase Storage)
- [ ] Change status

### FriendsForm
- [ ] Friends list (real-time)
- [ ] Search users
- [ ] Send friend request
- [ ] Accept/Reject requests
- [ ] Online status indicators

### MessageForm
- [ ] Real-time messaging
- [ ] Message history
- [ ] Typing indicators
- [ ] Read receipts
- [ ] File/Image sharing

---

## 🔐 Security Considerations

### Client-Side (C# App)
- ✅ Use Firebase Admin SDK (server-side SDK)
- ✅ Secure credential storage (encrypt firebase-credentials.json)
- ✅ Input validation
- ✅ XSS prevention in UI

### Firebase
- ✅ Security rules enforcement
- ✅ Email verification
- ✅ Rate limiting
- ✅ Audit logging

---

## 📊 Success Criteria

### Phase 1 (Current)
- [x] Kế hoạch hoàn chỉnh
- [ ] Firebase connected successfully
- [ ] User can login with email/password
- [ ] User can register new account
- [ ] User data saved to Firestore
- [ ] Password reset works
- [ ] Theme toggle Light/Dark works
- [ ] UI matches theme design

### Phase 2 (Future)
- [ ] Real-time messaging
- [ ] Friends management
- [ ] Profile management
- [ ] Call features
- [ ] Notifications

---

## 🛠️ Development Commands

```bash
# Restore packages
dotnet restore

# Build project
dotnet build

# Run application (Windows only)
dotnet run

# Publish release
dotnet publish -c Release -r win-x64 --self-contained
```

---

## 📚 Documentation To Create

1. **FIREBASE_SETUP.md** - Chi tiết setup Firebase Console
2. **THEME_GUIDE.md** - Hướng dẫn customize theme
3. **API_REFERENCE.md** - Services API documentation
4. **DEPLOYMENT.md** - Deploy guide với Firebase credentials

---

## ⚠️ Important Notes

### Firebase Credentials Security
```gitignore
# Add to .gitignore
**/firebase-credentials.json
**/Config/firebase-credentials.json
```

### Environment Variables (Optional)
```csharp
// For production, use environment variables
string credentialPath = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS") 
                        ?? "Config/firebase-credentials.json";
```

### Error Handling
```csharp
try {
    await FirebaseAuthService.SignIn(email, password);
}
catch (FirebaseAuthException ex) {
    // Handle auth errors
    MessageBox.Show($"Lỗi đăng nhập: {ex.Message}");
}
catch (Exception ex) {
    // Handle general errors
    MessageBox.Show($"Lỗi: {ex.Message}");
}
```

---

## 🎯 Next Steps

1. ✅ **Hoàn thành kế hoạch này**
2. Update .gitignore
3. Install NuGet packages
4. Create FirebaseConfig.cs
5. Create ThemeColors.cs & ThemeService.cs
6. Build LoginForm with new theme
7. Implement FirebaseAuthService
8. Test authentication

---

**Tạo bởi**: GitHub Copilot  
**Ngày**: 2025-10-26  
**Phiên bản**: 1.0  
**Status**: 📝 Planning Complete → Ready for Implementation
