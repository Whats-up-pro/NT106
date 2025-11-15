# Phân Tích và Thiết Kế Cơ Sở Dữ Liệu

## 1. Danh Sách Bảng (Tables)

### 1.1. Users (Người Dùng)
- **UserID** (INT, PRIMARY KEY, IDENTITY): ID người dùng
- **Username** (NVARCHAR(50), UNIQUE, NOT NULL): Tên đăng nhập
- **Email** (NVARCHAR(100), UNIQUE, NOT NULL): Email
- **PasswordHash** (NVARCHAR(255), NOT NULL): Mật khẩu đã mã hóa
- **FullName** (NVARCHAR(100)): Họ và tên
- **PhoneNumber** (NVARCHAR(20)): Số điện thoại
- **Avatar** (NVARCHAR(255)): Đường dẫn ảnh đại diện
- **Status** (NVARCHAR(50)): Trạng thái (Online, Offline, Away, Busy)
- **Bio** (NVARCHAR(500)): Tiểu sử
- **CreatedAt** (DATETIME, DEFAULT GETDATE()): Ngày tạo
- **LastLogin** (DATETIME): Lần đăng nhập cuối
- **IsActive** (BIT, DEFAULT 1): Trạng thái kích hoạt

### 1.2. Friendships (Quan Hệ Bạn Bè)
- **FriendshipID** (INT, PRIMARY KEY, IDENTITY): ID quan hệ
- **UserID1** (INT, FOREIGN KEY → Users.UserID): Người dùng 1
- **UserID2** (INT, FOREIGN KEY → Users.UserID): Người dùng 2
- **Status** (NVARCHAR(20)): Trạng thái (Pending, Accepted, Blocked)
- **RequestedBy** (INT, FOREIGN KEY → Users.UserID): Người gửi lời mời
- **CreatedAt** (DATETIME, DEFAULT GETDATE()): Ngày tạo
- **AcceptedAt** (DATETIME): Ngày chấp nhận

### 1.3. Conversations (Cuộc Trò Chuyện)
- **ConversationID** (INT, PRIMARY KEY, IDENTITY): ID cuộc trò chuyện
- **ConversationName** (NVARCHAR(100)): Tên cuộc trò chuyện (cho nhóm)
- **IsGroup** (BIT, DEFAULT 0): Cuộc trò chuyện nhóm
- **CreatedBy** (INT, FOREIGN KEY → Users.UserID): Người tạo
- **CreatedAt** (DATETIME, DEFAULT GETDATE()): Ngày tạo
- **LastMessageAt** (DATETIME): Thời gian tin nhắn cuối

### 1.4. ConversationParticipants (Thành Viên Cuộc Trò Chuyện)
- **ParticipantID** (INT, PRIMARY KEY, IDENTITY): ID thành viên
- **ConversationID** (INT, FOREIGN KEY → Conversations.ConversationID): ID cuộc trò chuyện
- **UserID** (INT, FOREIGN KEY → Users.UserID): ID người dùng
- **JoinedAt** (DATETIME, DEFAULT GETDATE()): Ngày tham gia
- **LeftAt** (DATETIME): Ngày rời khỏi
- **IsActive** (BIT, DEFAULT 1): Trạng thái hoạt động

### 1.5. Messages (Tin Nhắn)
- **MessageID** (INT, PRIMARY KEY, IDENTITY): ID tin nhắn
- **ConversationID** (INT, FOREIGN KEY → Conversations.ConversationID): ID cuộc trò chuyện
- **SenderID** (INT, FOREIGN KEY → Users.UserID): Người gửi
- **MessageText** (NVARCHAR(MAX)): Nội dung tin nhắn
- **MessageType** (NVARCHAR(20)): Loại tin nhắn (Text, Image, File, Audio, Video)
- **AttachmentPath** (NVARCHAR(255)): Đường dẫn file đính kèm
- **SentAt** (DATETIME, DEFAULT GETDATE()): Thời gian gửi
- **IsRead** (BIT, DEFAULT 0): Đã đọc
- **IsEdited** (BIT, DEFAULT 0): Đã chỉnh sửa
- **IsDeleted** (BIT, DEFAULT 0): Đã xóa

### 1.6. CallHistory (Lịch Sử Cuộc Gọi)
- **CallID** (INT, PRIMARY KEY, IDENTITY): ID cuộc gọi
- **CallerID** (INT, FOREIGN KEY → Users.UserID): Người gọi
- **ReceiverID** (INT, FOREIGN KEY → Users.UserID): Người nhận
- **CallType** (NVARCHAR(20)): Loại cuộc gọi (Voice, Video)
- **StartTime** (DATETIME): Thời gian bắt đầu
- **EndTime** (DATETIME): Thời gian kết thúc
- **Duration** (INT): Thời lượng (giây)
- **Status** (NVARCHAR(20)): Trạng thái (Completed, Missed, Rejected, Failed)

### 1.7. MessageReadStatus (Trạng Thái Đọc Tin Nhắn)
- **ReadStatusID** (INT, PRIMARY KEY, IDENTITY): ID trạng thái
- **MessageID** (INT, FOREIGN KEY → Messages.MessageID): ID tin nhắn
- **UserID** (INT, FOREIGN KEY → Users.UserID): ID người đọc
- **ReadAt** (DATETIME, DEFAULT GETDATE()): Thời gian đọc

## 2. Mối Quan Hệ (Relationships)

```
Users (1) ←→ (N) Friendships
Users (1) ←→ (N) Conversations (CreatedBy)
Users (1) ←→ (N) ConversationParticipants
Users (1) ←→ (N) Messages (SenderID)
Users (1) ←→ (N) CallHistory (CallerID, ReceiverID)
Users (1) ←→ (N) MessageReadStatus

Conversations (1) ←→ (N) ConversationParticipants
Conversations (1) ←→ (N) Messages

Messages (1) ←→ (N) MessageReadStatus
```

## 3. Indexes (Chỉ Mục)

```sql
-- Users
CREATE INDEX idx_users_username ON Users(Username);
CREATE INDEX idx_users_email ON Users(Email);
CREATE INDEX idx_users_status ON Users(Status);

-- Friendships
CREATE INDEX idx_friendships_user1 ON Friendships(UserID1);
CREATE INDEX idx_friendships_user2 ON Friendships(UserID2);
CREATE INDEX idx_friendships_status ON Friendships(Status);

-- ConversationParticipants
CREATE INDEX idx_participants_conversation ON ConversationParticipants(ConversationID);
CREATE INDEX idx_participants_user ON ConversationParticipants(UserID);

-- Messages
CREATE INDEX idx_messages_conversation ON Messages(ConversationID);
CREATE INDEX idx_messages_sender ON Messages(SenderID);
CREATE INDEX idx_messages_sentat ON Messages(SentAt);

-- CallHistory
CREATE INDEX idx_calls_caller ON CallHistory(CallerID);
CREATE INDEX idx_calls_receiver ON CallHistory(ReceiverID);
CREATE INDEX idx_calls_starttime ON CallHistory(StartTime);
```

## 4. Ràng Buộc (Constraints)

- Email phải có định dạng hợp lệ
- Username phải có ít nhất 3 ký tự
- Password phải có ít nhất 6 ký tự (kiểm tra tại ứng dụng)
- UserID1 ≠ UserID2 trong Friendships
- Không thể có hai quan hệ bạn bè giống nhau
- Status chỉ có thể là các giá trị được định nghĩa trước
