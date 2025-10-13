# Tài Liệu Kỹ Thuật - Messaging App

## Kiến Trúc Hệ Thống

### Tổng Quan
Ứng dụng được xây dựng theo mô hình 3-tier:
- **Presentation Layer**: Windows Forms UI
- **Business Logic Layer**: Form logic và utilities
- **Data Access Layer**: DatabaseConnection utility

### Công Nghệ Stack
```
Frontend: Windows Forms (.NET 8.0)
Backend: C# 12.0
Database: SQL Server
ORM: ADO.NET (SqlClient)
Security: SHA256 Password Hashing
```

## Cấu Trúc Dự Án

### Namespace Organization
```
MessagingApp
├── MessagingApp.Forms        # UI Forms
├── MessagingApp.Models       # Data Models
└── MessagingApp.Utils        # Utilities
```

### Thư Mục và File

#### /Forms
Chứa tất cả Windows Forms:
- `LoginForm.cs` - Đăng nhập
- `RegisterForm.cs` - Đăng ký
- `ForgotPasswordForm.cs` - Quên mật khẩu
- `MainForm.cs` - Màn hình chính
- `ProfileForm.cs` - Hồ sơ cá nhân
- `FriendsForm.cs` - Danh sách bạn bè
- `MessageForm.cs` - Tin nhắn
- `CallForm.cs` - Cuộc gọi

Mỗi form có 2 files:
- `.cs` - Logic code
- `.Designer.cs` - UI design code

#### /Models
```csharp
User.cs - User entity model
```

#### /Utils
```csharp
DatabaseConnection.cs - Database operations
PasswordHelper.cs - Password hashing
ThemeColors.cs - UI theming
```

#### /Database
```sql
CreateDatabase.sql - Database schema and seed data
```

#### /Documentation
```
FunctionalDecomposition.md - Sơ đồ phân rã chức năng
DatabaseDesign.md - Thiết kế CSDL
UserGuide.md - Hướng dẫn sử dụng
TechnicalDocumentation.md - Tài liệu kỹ thuật
```

## Database Schema

### Tables

#### Users
```sql
UserID INT PRIMARY KEY IDENTITY
Username NVARCHAR(50) UNIQUE NOT NULL
Email NVARCHAR(100) UNIQUE NOT NULL
PasswordHash NVARCHAR(255) NOT NULL
FullName NVARCHAR(100)
PhoneNumber NVARCHAR(20)
Avatar NVARCHAR(255)
Status NVARCHAR(50) DEFAULT 'Offline'
Bio NVARCHAR(500)
CreatedAt DATETIME DEFAULT GETDATE()
LastLogin DATETIME
IsActive BIT DEFAULT 1
```

#### Friendships
```sql
FriendshipID INT PRIMARY KEY IDENTITY
UserID1 INT FOREIGN KEY -> Users(UserID)
UserID2 INT FOREIGN KEY -> Users(UserID)
Status NVARCHAR(20) DEFAULT 'Pending'
RequestedBy INT FOREIGN KEY -> Users(UserID)
CreatedAt DATETIME DEFAULT GETDATE()
AcceptedAt DATETIME
```

#### Conversations
```sql
ConversationID INT PRIMARY KEY IDENTITY
ConversationName NVARCHAR(100)
IsGroup BIT DEFAULT 0
CreatedBy INT FOREIGN KEY -> Users(UserID)
CreatedAt DATETIME DEFAULT GETDATE()
LastMessageAt DATETIME
```

#### ConversationParticipants
```sql
ParticipantID INT PRIMARY KEY IDENTITY
ConversationID INT FOREIGN KEY -> Conversations
UserID INT FOREIGN KEY -> Users
JoinedAt DATETIME DEFAULT GETDATE()
LeftAt DATETIME
IsActive BIT DEFAULT 1
```

#### Messages
```sql
MessageID INT PRIMARY KEY IDENTITY
ConversationID INT FOREIGN KEY -> Conversations
SenderID INT FOREIGN KEY -> Users
MessageText NVARCHAR(MAX)
MessageType NVARCHAR(20) DEFAULT 'Text'
AttachmentPath NVARCHAR(255)
SentAt DATETIME DEFAULT GETDATE()
IsRead BIT DEFAULT 0
IsEdited BIT DEFAULT 0
IsDeleted BIT DEFAULT 0
```

#### CallHistory
```sql
CallID INT PRIMARY KEY IDENTITY
CallerID INT FOREIGN KEY -> Users
ReceiverID INT FOREIGN KEY -> Users
CallType NVARCHAR(20) DEFAULT 'Voice'
StartTime DATETIME DEFAULT GETDATE()
EndTime DATETIME
Duration INT DEFAULT 0
Status NVARCHAR(20) DEFAULT 'Completed'
```

#### MessageReadStatus
```sql
ReadStatusID INT PRIMARY KEY IDENTITY
MessageID INT FOREIGN KEY -> Messages
UserID INT FOREIGN KEY -> Users
ReadAt DATETIME DEFAULT GETDATE()
```

### Indexes
```sql
-- Performance indexes
idx_users_username ON Users(Username)
idx_users_email ON Users(Email)
idx_users_status ON Users(Status)
idx_friendships_user1 ON Friendships(UserID1)
idx_friendships_user2 ON Friendships(UserID2)
idx_participants_conversation ON ConversationParticipants(ConversationID)
idx_participants_user ON ConversationParticipants(UserID)
idx_messages_conversation ON Messages(ConversationID)
idx_messages_sender ON Messages(SenderID)
idx_messages_sentat ON Messages(SentAt)
idx_calls_caller ON CallHistory(CallerID)
idx_calls_receiver ON CallHistory(ReceiverID)
```

## API Reference

### DatabaseConnection Class

#### Methods

**GetConnection()**
```csharp
public static SqlConnection GetConnection()
```
Trả về một SqlConnection mới.

**TestConnection()**
```csharp
public static bool TestConnection()
```
Kiểm tra kết nối database.

**ExecuteNonQuery()**
```csharp
public static int ExecuteNonQuery(string query, SqlParameter[]? parameters = null)
```
Thực thi INSERT, UPDATE, DELETE query.

**ExecuteScalar()**
```csharp
public static object? ExecuteScalar(string query, SqlParameter[]? parameters = null)
```
Thực thi query trả về giá trị đơn.

**ExecuteQuery()**
```csharp
public static DataTable ExecuteQuery(string query, SqlParameter[]? parameters = null)
```
Thực thi SELECT query, trả về DataTable.

### PasswordHelper Class

**HashPassword()**
```csharp
public static string HashPassword(string password)
```
Mã hóa password với SHA256.

**VerifyPassword()**
```csharp
public static bool VerifyPassword(string password, string hash)
```
Xác minh password với hash.

### ThemeColors Class

#### Color Constants
```csharp
PrimaryDarkBlue = #1E3A8A
PrimaryBlue = #2563EB
PrimaryLightBlue = #3B82F6
SecondaryBlue = #60A5FA
TertiaryBlue = #93C5FD
Black = #000000
DarkGray = #1F2937
MediumGray = #374151
White = #FFFFFF
SuccessGreen = #22C55E
ErrorRed = #EF4444
WarningYellow = #EAB308
```

#### Methods

**ApplyTheme()**
```csharp
public static void ApplyTheme(Form form)
```
Áp dụng theme cho form.

**StylePrimaryButton()**
```csharp
public static void StylePrimaryButton(Button button)
```
Style nút chính.

**StyleSecondaryButton()**
```csharp
public static void StyleSecondaryButton(Button button)
```
Style nút phụ.

**StyleTextBox()**
```csharp
public static void StyleTextBox(TextBox textBox)
```
Style ô nhập text.

**StyleLabel()**
```csharp
public static void StyleLabel(Label label, bool isTitle = false)
```
Style nhãn text.

**StylePanel()**
```csharp
public static void StylePanel(Panel panel, bool isDark = true)
```
Style panel.

### CurrentUser Static Class
```csharp
public static class CurrentUser
{
    public static int UserID { get; set; }
    public static string Username { get; set; }
    public static string FullName { get; set; }
}
```
Lưu trữ thông tin người dùng hiện tại.

## Security

### Password Security
- **Hashing Algorithm**: SHA256
- **Storage**: Chỉ lưu hash, không lưu plaintext
- **Validation**: Kiểm tra độ dài tối thiểu 6 ký tự

### SQL Injection Prevention
```csharp
// Sử dụng parameterized queries
var parameters = new SqlParameter[]
{
    new SqlParameter("@Username", username),
    new SqlParameter("@PasswordHash", passwordHash)
};
DatabaseConnection.ExecuteQuery(query, parameters);
```

### Connection String Security
```csharp
// Sử dụng Integrated Security
Server=localhost;Database=MessagingAppDB;Integrated Security=True;TrustServerCertificate=True;
```

## Performance Optimization

### Database Indexes
- Tất cả foreign keys có index
- Username và Email có unique index
- SentAt có index cho sorting messages

### Connection Management
- Sử dụng `using` statements
- Connection pooling tự động
- Close connections sau mỗi operation

### UI Optimization
- Load data on demand
- ListView virtual mode (planned)
- Async operations (planned)

## Error Handling

### Try-Catch Blocks
```csharp
try
{
    // Database operation
}
catch (Exception ex)
{
    // User-friendly error message
    MessageBox.Show("Lỗi: " + ex.Message, "Lỗi",
        MessageBoxButtons.OK, MessageBoxIcon.Error);
}
```

### Validation
- Client-side validation trước khi gửi database
- Server-side constraints trong database
- User feedback qua labels

## Design Patterns

### Singleton Pattern
```csharp
// CurrentUser static class
public static class CurrentUser { ... }
```

### Factory Pattern
```csharp
// DatabaseConnection.GetConnection()
public static SqlConnection GetConnection() { ... }
```

### Utility Pattern
```csharp
// ThemeColors, PasswordHelper
public static class ThemeColors { ... }
```

## Configuration

### Connection String
Location: `Utils/DatabaseConnection.cs`
```csharp
private static readonly string connectionString = 
    @"Server=localhost;Database=MessagingAppDB;Integrated Security=True;TrustServerCertificate=True;";
```

### Project Settings
Location: `MessagingApp.csproj`
```xml
<TargetFramework>net8.0-windows</TargetFramework>
<UseWindowsForms>true</UseWindowsForms>
<EnableWindowsTargeting>true</EnableWindowsTargeting>
```

## Build and Deploy

### Build Commands
```bash
# Restore packages
dotnet restore

# Build project
dotnet build

# Build for release
dotnet build -c Release

# Publish
dotnet publish -c Release -r win-x64 --self-contained
```

### Requirements
- .NET 8.0 SDK
- Windows 10 or later
- SQL Server (any edition)

## Testing

### Manual Testing Checklist
- [ ] Login with valid credentials
- [ ] Login with invalid credentials
- [ ] Register new user
- [ ] Register with duplicate username
- [ ] Forgot password flow
- [ ] Update profile information
- [ ] View friends list
- [ ] Search friends
- [ ] View message interface
- [ ] View call history
- [ ] Logout

### Database Testing
```sql
-- Test user authentication
SELECT * FROM Users WHERE Username = 'admin'

-- Test friendship relationships
SELECT * FROM Friendships WHERE Status = 'Accepted'

-- Test messages
SELECT * FROM Messages ORDER BY SentAt DESC
```

## Future Enhancements

### Phase 2 (Planned)
- Real-time messaging với SignalR
- File và image sharing
- Voice và video calling với WebRTC
- Push notifications
- User avatars
- Group chat
- Message encryption

### Phase 3 (Planned)
- Mobile app (Xamarin/MAUI)
- Web interface (Blazor)
- Cloud deployment (Azure)
- Load balancing
- Caching (Redis)

## Troubleshooting

### Common Issues

**Database Connection Failed**
```
Solution:
1. Check SQL Server is running
2. Verify connection string
3. Check database exists
4. Verify user permissions
```

**Build Errors**
```
Solution:
1. dotnet clean
2. dotnet restore
3. dotnet build
```

**Form Not Displaying**
```
Solution:
1. Check Program.cs entry point
2. Verify form InitializeComponent()
3. Check designer.cs file exists
```

## Contributing Guidelines

### Code Style
- Use C# naming conventions
- Add XML comments for public methods
- Keep methods small and focused
- Use meaningful variable names

### Commit Messages
```
Format: <type>: <description>

Types:
- feat: New feature
- fix: Bug fix
- docs: Documentation
- style: Formatting
- refactor: Code restructuring
- test: Testing
```

### Pull Request Process
1. Fork repository
2. Create feature branch
3. Make changes
4. Test thoroughly
5. Submit PR with description

## License
MIT License - See LICENSE file

## Contact
- GitHub: https://github.com/Whats-up-pro
- Issues: https://github.com/Whats-up-pro/NT106/issues

---

**Document Version**: 1.0  
**Last Updated**: 2025-10-13  
**Author**: 614_2U0C Team
