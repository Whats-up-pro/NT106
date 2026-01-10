# Tài Liệu Luồng Hoạt Động - Ứng Dụng 3Mess

## Tổng Quan Kiến Trúc

```
┌─────────────────┐     ┌──────────────────────────────────────┐
│   WPF UI        │◄───►│  ViewModels (MainViewModel, etc.)    │
│  (XAML Views)   │     └──────────────────────────────────────┘
└─────────────────┘                      │
                                         ▼
               ┌──────────────────────────────────────────────────────┐
               │              Services Layer (Singleton)              │
               ├──────────────────────────────────────────────────────┤
               │  FirebaseAuthService    │  FirestoreMessagingService │
               │  FirestoreFriendsService│  FirestoreCallingService   │
               │  FirebaseStorageService │  AgoraVoiceService         │
               └──────────────────────────────────────────────────────┘
                                         │
                                         ▼
               ┌──────────────────────────────────────────────────────┐
               │                    Firebase                          │
               │  Authentication │ Firestore │ Cloud Storage          │
               └──────────────────────────────────────────────────────┘
```

---

## 1. Đăng Nhập / Đăng Xuất / Đăng Ký / Quên Mật Khẩu

### Service: [FirebaseAuthService](file:///d:/HK1_2025-2026/NT106.Q11.ANTT%20-%20LTM%20c%C4%83n%20b%E1%BA%A3n/Project/NT106/MessagingApp.Core/Services/FirebaseAuthService.cs#53-59)

### Đăng Nhập
```
User nhập Email + Password → LoginViewModel.LoginCommand
  → FirebaseAuthService.SignInWithEmailPassword(email, password)
    → HTTP POST tới Firebase Identity Toolkit REST API
       (accounts:signInWithPassword)
    → Nhận IdToken, LocalId (userId)
    → Lưu CurrentUserId, CurrentIdToken
    → Gọi UpdateLastLogin() để cập nhật lastLogin trong Firestore
  → Thành công: Navigate → MainWindow
```

**Firestore collections:** `users/{userId}` - lưu lastLogin timestamp

### Đăng Ký
```
User nhập Email + Password + Username → RegisterViewModel.RegisterCommand
  → FirebaseAuthService.SignUpWithEmailPassword(email, password)
    → HTTP POST tới Firebase Identity Toolkit REST API
       (accounts:signUp)
    → Tạo document users/{userId} trong Firestore với:
       email, username, createdAt, status = "offline"
  → Thành công: Navigate → LoginWindow
```

### Đăng Xuất
```
User click Logout → MainViewModel.LogoutCommand
  → FirebaseAuthService.SignOut()
    → UpdateUserStatus("offline") → Firestore users/{userId}
    → Clear CurrentUserId, CurrentIdToken
  → Navigate → LoginWindow
```

### Quên Mật Khẩu
```
User nhập Email → ForgotPasswordViewModel.SendResetEmailCommand
  → FirebaseAuthService.SendPasswordResetEmail(email)
    → HTTP POST tới Firebase Identity Toolkit REST API
       (accounts:sendOobCode với requestType=PASSWORD_RESET)
    → Firebase gửi email khôi phục tới user
```

---

## 2. Nhắn Tin

### Service: [FirestoreMessagingService](file:///d:/HK1_2025-2026/NT106.Q11.ANTT%20-%20LTM%20c%C4%83n%20b%E1%BA%A3n/Project/NT106/MessagingApp.Core/Services/FirestoreMessagingService.cs#13-863)

### Gửi Tin Nhắn
```
User nhập tin nhắn → MainViewModel.SendMessageCommand
  → FirestoreMessagingService.SendMessage(conversationId, senderId, content, type)
    → Tạo document trong Firestore messages collection:
       {conversationId, senderId, content, type, timestamp, read: false}
    → Cập nhật conversations/{conversationId}.lastMessageId
  → UI cập nhật Messages collection
```

**Firestore collections:**
- `messages/{messageId}` - nội dung tin nhắn
- `conversations/{conversationId}` - metadata cuộc trò chuyện

### Nhận Tin Nhắn Real-time
```
MainViewModel.LoadChatAsync()
  → FirestoreMessagingService.ListenToMessages(conversationId, callback)
    → Firestore Query với OrderBy(timestamp) Descending
    → Đăng ký Snapshot Listener
    → Khi có tin nhắn mới: callback(documents)
      → Parse và thêm vào Messages ObservableCollection
      → UI tự động cập nhật qua data binding
```

---

## 3. Trạng Thái Hoạt Động (Online/Offline)

### Service: [FirestoreFriendsService](file:///d:/HK1_2025-2026/NT106.Q11.ANTT%20-%20LTM%20c%C4%83n%20b%E1%BA%A3n/Project/NT106/MessagingApp.Core/Services/FirestoreFriendsService.cs#20-24)

```
MainWindow.Loaded
  → Khởi tạo DispatcherTimer (25 giây)
  → Mỗi 25 giây gọi: MainViewModel.UpdatePresenceAsync(true)
    → FirestoreFriendsService.UpdatePresenceAsync(userId, isOnline)
      → Firestore Update users/{userId}:
         {isOnline: true, status: "online", lastSeen: ServerTimestamp}

MainWindow.Closing
  → MainViewModel.UpdatePresenceAsync(false)
    → Firestore Update: {isOnline: false, status: "offline", lastSeen: ...}
```

### Theo Dõi Trạng Thái Bạn Bè
```
FirestoreFriendsService.ListenToUserStatus(userId, callback)
  → Firestore Snapshot Listener trên users/{userId}
  → Khi status thay đổi: callback(isOnline, lastSeen)
    → UI hiển thị chấm xanh/xám
```

---

## 4. Chế Độ Giao Diện Sáng/Tối

### Service: [FirestoreFriendsService](file:///d:/HK1_2025-2026/NT106.Q11.ANTT%20-%20LTM%20c%C4%83n%20b%E1%BA%A3n/Project/NT106/MessagingApp.Core/Services/FirestoreFriendsService.cs#20-24) (UpdateUserSettingsAsync)

```
User toggle Theme → MainViewModel.ToggleThemeCommand
  → Application.Current.Resources.MergedDictionaries
    → Switch ResourceDictionary: Light.xaml ↔ Dark.xaml
  → FirestoreFriendsService.UpdateUserSettingsAsync(userId, theme: "dark"/"light")
    → Firestore Update users/{userId}: {theme: "dark"}
  → UI tự động cập nhật màu sắc
```

**Local storage:** `Themes/Light.xaml`, `Themes/Dark.xaml`

---

## 5. Toggle Thông Báo

### Service: [FirestoreFriendsService](file:///d:/HK1_2025-2026/NT106.Q11.ANTT%20-%20LTM%20c%C4%83n%20b%E1%BA%A3n/Project/NT106/MessagingApp.Core/Services/FirestoreFriendsService.cs#20-24) (UpdateUserSettingsAsync)

```
User toggle Notifications → MainViewModel.ToggleNotificationsCommand
  → Cập nhật IsNotificationsEnabled property
  → FirestoreFriendsService.UpdateUserSettingsAsync(userId, notificationsEnabled: bool)
    → Firestore Update users/{userId}: {notificationsEnabled: true/false}
  → Áp dụng logic: nếu false, không hiện thông báo khi có tin mới
```

---

## 6. Tạo Nhóm

### Service: [FirestoreMessagingService](file:///d:/HK1_2025-2026/NT106.Q11.ANTT%20-%20LTM%20c%C4%83n%20b%E1%BA%A3n/Project/NT106/MessagingApp.Core/Services/FirestoreMessagingService.cs#13-863)

```
User click Tạo nhóm + chọn thành viên → MainViewModel.CreateGroupCommand
  → FirestoreMessagingService.CreateGroupConversation(name, memberIds, creatorId)
    → Tạo document conversations/{conversationId}:
       {
         type: "group",
         name: "Tên nhóm",
         members: [userId1, userId2, ...],
         createdBy: creatorId,
         createdAt: ServerTimestamp
       }
  → Trả về conversationId
  → UI thêm vào Conversations list
```

### Kick Thành Viên
```
Owner click Kick → FirestoreMessagingService.RemoveMemberFromGroupAsync()
  → Kiểm tra if requesterId == createdBy
  → Firestore Update: ArrayRemove(targetUserId) from members
```

---

## 7. Tìm và Thêm Bạn

### Service: [FirestoreFriendsService](file:///d:/HK1_2025-2026/NT106.Q11.ANTT%20-%20LTM%20c%C4%83n%20b%E1%BA%A3n/Project/NT106/MessagingApp.Core/Services/FirestoreFriendsService.cs#20-24)

### Tìm Kiếm User
```
User nhập keyword → MainViewModel.SearchUsersCommand
  → FirestoreFriendsService.SearchUsers(keyword, currentUserId)
    → Firestore Query users collection:
       WhereGreaterThanOrEqualTo("email", keyword)
       HOẶC WhereGreaterThanOrEqualTo("username", keyword)
    → Loại trừ currentUserId và existing friends
  → Trả về List<UserSearchResult>
  → UI hiển thị kết quả
```

### Gửi Lời Mời Kết Bạn
```
User click Kết bạn → MainViewModel.SendFriendRequestCommand
  → FirestoreFriendsService.SendFriendRequest(fromUserId, toUserId)
    → Kiểm tra đã có request/friendship chưa
    → Tạo document friendRequests/{requestId}:
       {fromUser, toUser, status: "pending", sentAt: ServerTimestamp}
  → UI cập nhật button → "Đã gửi"
```

### Chấp Nhận Lời Mời
```
User click Chấp nhận → MainViewModel.AcceptFriendRequestCommand
  → FirestoreFriendsService.AcceptFriendRequest(requestId)
    → Cập nhật friendRequests/{requestId}.status = "accepted"
    → Tạo document friendships/{canonicalPairId}:
       {user1, user2, createdAt: ServerTimestamp}
    → Xóa friendRequest document
  → UI cập nhật: thêm bạn vào Conversations
```

---

## 8. Xem Trang Cá Nhân

### Service: [FirestoreFriendsService](file:///d:/HK1_2025-2026/NT106.Q11.ANTT%20-%20LTM%20c%C4%83n%20b%E1%BA%A3n/Project/NT106/MessagingApp.Core/Services/FirestoreFriendsService.cs#20-24)

```
User click Avatar/Tên → MainViewModel.ViewProfileCommand
  → FirestoreFriendsService.GetUserAsync(targetUserId)
    → Firestore Get users/{targetUserId}
    → Trả về: email, username, bio, avatarDataUrl, coverDataUrl, isOnline
  → Hiển thị Profile Panel với thông tin
```

### Cập Nhật Profile
```
User edit Bio/Avatar → MainViewModel.SaveProfileCommand
  → FirestoreFriendsService.UpdateUserProfileAsync(userId, bio, avatarDataUrl, coverDataUrl)
    → Firestore Update users/{userId}: {bio, avatarDataUrl, coverDataUrl}
  → UI cập nhật hiển thị
```

---

## 9. Ghim Tin Nhắn

### Service: [FirestoreMessagingService](file:///d:/HK1_2025-2026/NT106.Q11.ANTT%20-%20LTM%20c%C4%83n%20b%E1%BA%A3n/Project/NT106/MessagingApp.Core/Services/FirestoreMessagingService.cs#13-863)

### Ghim
```
User chuột phải → Ghim tin nhắn → MainViewModel.PinMessageCommand
  → FirestoreMessagingService.PinMessageAsync(conversationId, messageId, userId)
    → Kiểm tra đã có 10 tin ghim chưa (limit)
    → Lấy content từ messages/{messageId}
    → Tạo document conversations/{conversationId}/pinnedMessages/{messageId}:
       {messageId, messageContent, messageType, senderId, pinnedAt, pinnedBy}
  → LoadPinnedMessagesAsync() → Cập nhật PinnedMessages collection
  → UI hiển thị icon 📌 và panel tin ghim
```

### Bỏ Ghim
```
User chuột phải → Bỏ ghim → MainViewModel.UnpinMessageCommand
  → FirestoreMessagingService.UnpinMessageAsync(conversationId, messageId)
    → Firestore Delete: conversations/{conversationId}/pinnedMessages/{messageId}
  → LoadPinnedMessagesAsync() → Cập nhật collection
```

---

## 10. Gửi File/Link

### Service: [FirebaseStorageService](file:///d:/HK1_2025-2026/NT106.Q11.ANTT%20-%20LTM%20c%C4%83n%20b%E1%BA%A3n/Project/NT106/MessagingApp.Core/Services/FirebaseStorageService.cs#22-36) + [FirestoreMessagingService](file:///d:/HK1_2025-2026/NT106.Q11.ANTT%20-%20LTM%20c%C4%83n%20b%E1%BA%A3n/Project/NT106/MessagingApp.Core/Services/FirestoreMessagingService.cs#13-863)

### Gửi File
```
User click Đính kèm → Chọn file → MainViewModel.SendFileAsync(filePath)
  → FirebaseStorageService.UploadFileAsync(localPath, bucket, objectName)
    → Google Cloud Storage API: UploadObjectAsync()
    → Trả về (bucket, objectName)
  → FirestoreMessagingService.SendMessage(conversationId, senderId, content, type: "file",
       extras: {fileName, fileSize, storageBucket, storageObject})
    → Tạo messages document với metadata file
  → UI hiển thị file bubble với nút Tải xuống
```

### Gửi Ảnh
```
User chọn ảnh → MainViewModel.SendImageFileAsync(imagePath)
  → Convert ảnh → Base64 DataUrl (hoặc upload Storage)
  → FirestoreMessagingService.SendMessage(..., type: "image", content: dataUrl)
  → UI hiển thị ảnh trong bubble
```

### Gửi Link
```
User nhập URL → MainViewModel.SendMessageCommand
  → Detect URL pattern (regex)
  → FirestoreMessagingService.SendMessage(..., type: "link", content: url)
  → UI hiển thị link có thể click
```

---

## 11. Gọi Điện (Voice Call)

### Services: [FirestoreCallingService](file:///d:/HK1_2025-2026/NT106.Q11.ANTT%20-%20LTM%20c%C4%83n%20b%E1%BA%A3n/Project/NT106/MessagingApp.Core/Services/FirestoreCallingService.cs#13-397) + [AgoraVoiceService](file:///d:/HK1_2025-2026/NT106.Q11.ANTT%20-%20LTM%20c%C4%83n%20b%E1%BA%A3n/Project/NT106/MessagingApp.Core/Services/AgoraVoiceService.cs#28-32)

### Khởi Tạo Cuộc Gọi
```
User click Gọi → MainViewModel.InitiateCallCommand
  → FirestoreCallingService.InitiateCall(callerId, receiverIds, type: "voice")
    → Tạo document calls/{callId}:
       {
         callerId, participants: [...], type: "voice",
         status: "ringing", startedAt: ServerTimestamp
       }
  → AgoraVoiceService.Initialize() → Khởi tạo Agora RTC Engine
  → AgoraVoiceService.JoinChannelAsync(callId, userId)
    → Agora SDK: JoinChannel(channelName=callId)
  → UI hiển thị màn hình gọi đi
```

### Nhận Cuộc Gọi
```
FirestoreCallingService.ListenToIncomingCalls(userId, callback)
  → Firestore Query: calls where participants contains userId AND status == "ringing"
  → Khi có cuộc gọi mới: callback(callData)
    → UI hiển thị popup "Cuộc gọi đến"

User click Nghe máy → FirestoreCallingService.AnswerCall(callId, userId)
  → Firestore Update calls/{callId}: status = "active", add to participants
  → AgoraVoiceService.JoinChannelAsync(callId, userId)
  → Bắt đầu đàm thoại
```

### Kết Thúc Cuộc Gọi
```
User click Kết thúc → MainViewModel.EndCallCommand
  → AgoraVoiceService.LeaveChannelAsync()
  → FirestoreCallingService.EndCall(callId, userId)
    → Firestore Update calls/{callId}: status = "ended", endedAt: ServerTimestamp
  → UI quay lại màn hình chat
```

---

## Tổng Kết Collections Firestore

| Collection | Mô tả |
|------------|-------|
| `users/{userId}` | Thông tin user: email, username, bio, avatar, status, settings |
| `friendRequests/{requestId}` | Lời mời kết bạn: fromUser, toUser, status |
| `friendships/{pairId}` | Quan hệ bạn bè: user1, user2 |
| `conversations/{conversationId}` | Cuộc trò chuyện: type, members, name, lastMessage |
| `conversations/{id}/pinnedMessages/{msgId}` | Tin nhắn ghim |
| `messages/{messageId}` | Nội dung tin nhắn |
| `calls/{callId}` | Cuộc gọi: participants, status, type |
