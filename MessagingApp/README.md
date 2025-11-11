# Ứng Dụng Nhắn Tin và Gọi Điện - Messaging App

## Mô Tả
Ứng dụng nhắn tin và gọi điện cơ bản được xây dựng bằng C# Windows Forms với giao diện màu xanh dương đen thân thiện với người dùng.

## Tính Năng

### 1. Xác Thực Người Dùng
- ✅ Đăng nhập
- ✅ Đăng ký tài khoản mới
- ✅ Quên mật khẩu

### 2. Quản Lý Hồ Sơ
- ✅ Xem và chỉnh sửa thông tin cá nhân
- ✅ Cập nhật trạng thái (Online, Away, Busy, Offline)
- ✅ Thay đổi thông tin liên hệ

### 3. Bạn Bè
- ✅ Xem danh sách bạn bè
- ✅ Tìm kiếm bạn bè
- ⏳ Thêm bạn mới (đang phát triển)

### 4. Tin Nhắn
- ✅ Giao diện nhắn tin cơ bản
- ⏳ Gửi và nhận tin nhắn thời gian thực (đang phát triển)
- ⏳ Lịch sử tin nhắn (đang phát triển)

### 5. Cuộc Gọi
- ✅ Xem lịch sử cuộc gọi
- ⏳ Gọi thoại (đang phát triển)
- ⏳ Gọi video (đang phát triển)

## Cơ Sở Dữ Liệu

### Yêu Cầu
- Microsoft SQL Server (LocalDB hoặc SQL Server Express)

### Cài Đặt Database
1. Mở SQL Server Management Studio
2. Chạy script `MessagingApp/Database/CreateDatabase.sql`
3. Cập nhật connection string trong `MessagingApp/Utils/DatabaseConnection.cs` nếu cần

### Cấu Trúc Database
- **Users**: Thông tin người dùng
- **Friendships**: Quan hệ bạn bè
- **Conversations**: Cuộc trò chuyện
- **ConversationParticipants**: Thành viên cuộc trò chuyện
- **Messages**: Tin nhắn
- **CallHistory**: Lịch sử cuộc gọi
- **MessageReadStatus**: Trạng thái đã đọc tin nhắn

## Công Nghệ Sử Dụng
- **Framework**: .NET 8.0
- **UI**: Windows Forms
- **Database**: Microsoft SQL Server
- **Language**: C# 12.0

## Màu Sắc Giao Diện
- **Primary Blue**: #2563EB
- **Dark Blue**: #1E3A8A
- **Light Blue**: #3B82F6
- **Background Dark**: #111827
- **Background Medium**: #1F2937

## Hướng Dẫn Chạy Ứng Dụng

### Yêu Cầu Hệ Thống
- Windows 10 hoặc mới hơn
- .NET 8.0 SDK
- SQL Server (LocalDB, Express, hoặc Full)

### Các Bước
1. Clone repository
```bash
git clone https://github.com/Whats-up-pro/NT106.git
cd NT106/MessagingApp
```

2. Cài đặt database
- Chạy script `Database/CreateDatabase.sql` trong SQL Server

3. Cấu hình connection string
- Mở file `Utils/DatabaseConnection.cs`
- Cập nhật connection string theo cấu hình SQL Server của bạn

4. Build và chạy ứng dụng
```bash
dotnet restore
dotnet build
dotnet run
```

## Tài Khoản Mẫu
- **Username**: admin
- **Password**: password123

- **Username**: user1
- **Password**: password123

- **Username**: user2
- **Password**: password123

## Cấu Trúc Thư Mục
```
MessagingApp/
├── Database/           # SQL scripts
├── Documentation/      # Tài liệu thiết kế
├── Forms/             # Windows Forms
├── Models/            # Data models
├── Utils/             # Utilities
└── Program.cs         # Entry point
```

## Tài Liệu Thiết Kế
- [Sơ Đồ Phân Rã Chức Năng](Documentation/FunctionalDecomposition.md)
- [Thiết Kế Cơ Sở Dữ Liệu](Documentation/DatabaseDesign.md)

## Đóng Góp
Mọi đóng góp đều được chào đón! Vui lòng tạo issue hoặc pull request.

## Giấy Phép
MIT License - Xem file [LICENSE](../LICENSE) để biết thêm chi tiết.

## Tác Giả
614_2U0C Team

## Liên Hệ
- GitHub: [Whats-up-pro](https://github.com/Whats-up-pro)
- Repository: [NT106](https://github.com/Whats-up-pro/NT106)
