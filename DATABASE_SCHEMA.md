# Sơ Đồ Cơ Sở Dữ Liệu - Messaging App

## ER Diagram (Entity Relationship)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           MESSAGING APP DATABASE                            │
└─────────────────────────────────────────────────────────────────────────────┘

                              ┌──────────────────┐
                              │     USERS        │
                              ├──────────────────┤
                              │ PK: UserID       │
                              │ • Username       │
                              │ • Email          │
                              │ • PasswordHash   │
                              │ • FullName       │
                              │ • PhoneNumber    │
                              │ • Avatar         │
                              │ • Status         │
                              │ • Bio            │
                              │ • CreatedAt      │
                              │ • LastLogin      │
                              │ • IsActive       │
                              └──────────────────┘
                                     △
                    ┌────────────────┼────────────────┐
                    │                │                │
        ┌───────────┴────────┐   ┌───┴──────────┐   ┌──┴──────────────────┐
        │                    │   │              │   │                     │
        │  (1 User : N Friends)  │ (Creator)    │   │(Caller/Receiver)    │
        ▼                    │   │              │   │                     │
┌──────────────────────┐    │   ▼              │   ▼                     │
│   FRIENDSHIPS        │    │ ┌──────────────────────┐  ┌──────────────────┐
├──────────────────────┤    │ │   CONVERSATIONS      │  │   CALL HISTORY   │
│ PK: FriendshipID     │    │ ├──────────────────────┤  ├──────────────────┤
│ FK: UserID1          ├────┘ │ PK: ConversationID   │  │ PK: CallID       │
│ FK: UserID2          ├──┐   │ • ConversationName   │  │ FK: CallerID     │
│ FK: RequestedBy      ├──┴───┤ • IsGroup            │  │ FK: ReceiverID   │
│ • Status             │      │ • CreatedBy          │  │ • CallType       │
│ • CreatedAt          │      │ • CreatedAt          │  │ • StartTime      │
│ • AcceptedAt         │      │ • LastMessageAt      │  │ • EndTime        │
└──────────────────────┘      └──────────────────────┘  │ • Duration       │
                                      △                │ • Status         │
                                      │                └──────────────────┘
                                      │
                  (1 Conversation : N Participants)
                                      │
                              ┌───────┴──────────┐
                              │                  │
                              ▼                  ▼
                    ┌──────────────────────┐  ┌──────────────────────┐
                    │ CONVERSATION         │  │   MESSAGES           │
                    │ PARTICIPANTS         │  ├──────────────────────┤
                    ├──────────────────────┤  │ PK: MessageID        │
                    │ PK: ParticipantID    │  │ FK: ConversationID   │
                    │ FK: ConversationID   │  │ FK: SenderID         │
                    │ FK: UserID           │  │ • MessageText        │
                    │ • JoinedAt           │  │ • MessageType        │
                    │ • LeftAt             │  │ • AttachmentPath     │
                    │ • IsActive           │  │ • SentAt             │
                    └──────────────────────┘  │ • IsRead             │
                                              │ • IsEdited           │
                                              │ • IsDeleted          │
                                              └──────────────────────┘
                                                      △
                                (1 Message : N MessageReadStatus)
                                                      │
                                              ┌───────┴──────────┐
                                              │                  │
                                              ▼                  ▼
                                    ┌──────────────────────┐
                                    │ MESSAGE READ STATUS  │
                                    ├──────────────────────┤
                                    │ PK: ReadStatusID     │
                                    │ FK: MessageID        │
                                    │ FK: UserID           │
                                    │ • ReadAt             │
                                    └──────────────────────┘
```

---

## Mối Quan Hệ Chi Tiết

| Bảng 1 | Bảng 2 | Mối Quan Hệ | Mô Tả |
|--------|--------|-----------|-------|
| **USERS** | **FRIENDSHIPS** | 1 : N | Một user có nhiều quan hệ bạn bè |
| **USERS** | **CONVERSATIONS** | 1 : N | Một user tạo nhiều cuộc trò chuyện |
| **USERS** | **CONVERSATION_PARTICIPANTS** | 1 : N | Một user tham gia nhiều cuộc trò chuyện |
| **USERS** | **MESSAGES** | 1 : N | Một user gửi nhiều tin nhắn |
| **USERS** | **MESSAGE_READ_STATUS** | 1 : N | Một user đọc nhiều tin nhắn |
| **CONVERSATIONS** | **CONVERSATION_PARTICIPANTS** | 1 : N | Một cuộc trò chuyện có nhiều thành viên |
| **CONVERSATIONS** | **MESSAGES** | 1 : N | Một cuộc trò chuyện có nhiều tin nhắn |
| **MESSAGES** | **MESSAGE_READ_STATUS** | 1 : N | Một tin nhắn được nhiều user đọc |

---

## Quy Tắc Dữ Liệu (Data Rules)

### Users
- ✅ Username và Email phải **unique**
- ✅ Email phải có định dạng hợp lệ (@)
- ✅ Username tối thiểu 3 ký tự
- ✅ Status có các giá trị: `Online`, `Offline`, `Away`, `Busy`
- ✅ IsActive mặc định = true

### Friendships
- ✅ Status có giá trị: `Pending`, `Accepted`, `Blocked`
- ✅ UserID1 ≠ UserID2 (không thể là bạn của chính mình)
- ✅ RequestedBy phải là một trong hai user (UserID1 hoặc UserID2)

### Messages
- ✅ MessageType: `Text`, `Image`, `File`, `Audio`, `Video`
- ✅ IsDeleted = true thì ẩn tin nhắn nhưng vẫn giữ dữ liệu
- ✅ Chỉ SenderID tương ứng mới có thể chỉnh sửa tin nhắn

### Conversations
- ✅ IsGroup = false: cuộc trò chuyện 1-1
- ✅ IsGroup = true: cuộc trò chuyện nhóm
- ✅ LastMessageAt cập nhật mỗi khi có tin nhắn mới

---

## Các Index (Chỉ Mục) Để Tối Ưu

```sql
-- Users
CREATE INDEX idx_users_username ON Users(Username);
CREATE INDEX idx_users_email ON Users(Email);
CREATE INDEX idx_users_status ON Users(Status);

-- Friendships
CREATE INDEX idx_friendships_user1 ON Friendships(UserID1);
CREATE INDEX idx_friendships_user2 ON Friendships(UserID2);
CREATE INDEX idx_friendships_status ON Friendships(Status);

-- Conversations
CREATE INDEX idx_conversations_createdby ON Conversations(CreatedBy);
CREATE INDEX idx_conversations_lastmessageat ON Conversations(LastMessageAt);

-- ConversationParticipants
CREATE INDEX idx_participants_conversation ON ConversationParticipants(ConversationID);
CREATE INDEX idx_participants_user ON ConversationParticipants(UserID);

-- Messages
CREATE INDEX idx_messages_conversation ON Messages(ConversationID);
CREATE INDEX idx_messages_sender ON Messages(SenderID);
CREATE INDEX idx_messages_sentat ON Messages(SentAt);

-- MessageReadStatus
CREATE INDEX idx_readstatus_message ON MessageReadStatus(MessageID);
CREATE INDEX idx_readstatus_user ON MessageReadStatus(UserID);
```

---

## Thống Kê Bảng

| Bảng | Mục Đích | Khoá Chính | Khoá Ngoại |
|------|----------|-----------|-----------|
| **USERS** | Lưu thông tin người dùng | UserID | - |
| **FRIENDSHIPS** | Quản lý quan hệ bạn bè | FriendshipID | UserID1, UserID2, RequestedBy |
| **CONVERSATIONS** | Quản lý cuộc trò chuyện | ConversationID | CreatedBy |
| **CONVERSATION_PARTICIPANTS** | Quản lý thành viên cuộc trò chuyện | ParticipantID | ConversationID, UserID |
| **MESSAGES** | Lưu tin nhắn | MessageID | ConversationID, SenderID |
| **MESSAGE_READ_STATUS** | Trạng thái đọc tin nhắn | ReadStatusID | MessageID, UserID |

---

## Ghi Chú Thiết Kế

1. **Normalization**: Database theo nguyên tắc 3NF (Third Normal Form)
2. **Scalability**: Sử dụng Identity PK để dễ mở rộng
3. **Auditing**: Có CreatedAt, LastLogin để tracking
4. **Soft Delete**: Messages dùng IsDeleted để bảo toàn dữ liệu
5. **Relationships**: Sử dụng Foreign Keys để đảm bảo tính toàn vẹn dữ liệu
