# Hướng Dẫn Cài Đặt - Messaging App

## Mục Lục
1. [Yêu Cầu Hệ Thống](#yêu-cầu-hệ-thống)
2. [Cài Đặt SQL Server](#cài-đặt-sql-server)
3. [Cài Đặt .NET SDK](#cài-đặt-net-sdk)
4. [Clone Repository](#clone-repository)
5. [Tạo Database](#tạo-database)
6. [Cấu Hình Connection String](#cấu-hình-connection-string)
7. [Build và Chạy](#build-và-chạy)
8. [Xử Lý Sự Cố](#xử-lý-sự-cố)

---

## Yêu Cầu Hệ Thống

### Hệ Điều Hành
- Windows 10 (version 1809 trở lên)
- Windows 11
- Windows Server 2019 trở lên

### Phần Mềm Cần Thiết
- .NET 8.0 SDK hoặc Runtime
- SQL Server (một trong các phiên bản sau):
  - SQL Server 2019 Express (miễn phí)
  - SQL Server 2022 Express (miễn phí)
  - SQL Server LocalDB (miễn phí)
  - SQL Server Developer Edition (miễn phí)
  - SQL Server Standard/Enterprise

### Phần Cứng Đề Xuất
- **CPU**: 2 GHz trở lên
- **RAM**: 4 GB trở lên (8 GB khuyến nghị)
- **Disk Space**: 2 GB trống
- **Display**: 1024x768 trở lên

---

## Cài Đặt SQL Server

### Tùy Chọn 1: SQL Server Express (Khuyến Nghị)

#### Bước 1: Download
1. Truy cập: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
2. Tải **SQL Server 2022 Express** hoặc **SQL Server 2019 Express**

#### Bước 2: Cài Đặt
1. Chạy file setup đã tải
2. Chọn **"Basic"** installation type
3. Chấp nhận license terms
4. Chọn thư mục cài đặt (hoặc để mặc định)
5. Nhấn **"Install"**
6. Đợi quá trình cài đặt hoàn tất (5-10 phút)

#### Bước 3: Cài SQL Server Management Studio (SSMS)
1. Sau khi cài SQL Server, nhấn **"Install SSMS"**
2. Hoặc tải từ: https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms
3. Chạy installer và làm theo hướng dẫn
4. Khởi động lại máy nếu cần

### Tùy Chọn 2: SQL Server LocalDB (Nhẹ Nhất)

#### Download và Cài Đặt
```bash
# Download SQL Server Express với LocalDB
# Hoặc cài qua Visual Studio Installer
```

LocalDB connection string:
```
Server=(localdb)\MSSQLLocalDB;Database=MessagingAppDB;Integrated Security=True;TrustServerCertificate=True;
```

### Kiểm Tra Cài Đặt
1. Mở **SQL Server Management Studio (SSMS)**
2. Server name: `localhost` hoặc `.\SQLEXPRESS`
3. Authentication: **Windows Authentication**
4. Nhấn **Connect**
5. Nếu kết nối thành công, SQL Server đã sẵn sàng!

---

## Cài Đặt .NET SDK

### Bước 1: Download
1. Truy cập: https://dotnet.microsoft.com/download
2. Tải **.NET 8.0 SDK** (không phải Runtime)
3. Chọn phiên bản phù hợp với hệ điều hành

### Bước 2: Cài Đặt
1. Chạy installer đã tải
2. Làm theo hướng dẫn on-screen
3. Đợi cài đặt hoàn tất

### Bước 3: Kiểm Tra
Mở Command Prompt hoặc PowerShell:
```bash
dotnet --version
```
Kết quả mong đợi: `8.0.xxx`

Nếu lỗi "dotnet is not recognized":
- Khởi động lại Command Prompt
- Khởi động lại máy tính
- Kiểm tra PATH environment variable

---

## Clone Repository

### Tùy Chọn 1: Git Command Line

#### Cài Git (nếu chưa có)
1. Download từ: https://git-scm.com/download/win
2. Cài đặt với các tùy chọn mặc định

#### Clone Repository
```bash
# Mở Command Prompt hoặc PowerShell
cd C:\Projects  # Hoặc thư mục bạn muốn

# Clone repository
git clone https://github.com/Whats-up-pro/NT106.git

# Di chuyển vào thư mục
cd NT106\MessagingApp
```

### Tùy Chọn 2: GitHub Desktop (Dễ hơn)

1. Download GitHub Desktop: https://desktop.github.com/
2. Cài đặt và đăng nhập
3. File → Clone Repository
4. Nhập: `Whats-up-pro/NT106`
5. Chọn thư mục lưu
6. Nhấn **Clone**

### Tùy Chọn 3: Download ZIP

1. Truy cập: https://github.com/Whats-up-pro/NT106
2. Nhấn **Code** → **Download ZIP**
3. Giải nén file ZIP
4. Di chuyển vào thư mục `NT106\MessagingApp`

---

## Tạo Database

### Bước 1: Mở SQL Script
1. Mở **SQL Server Management Studio (SSMS)**
2. File → Open → File
3. Chọn file: `MessagingApp\Database\CreateDatabase.sql`

### Bước 2: Chạy Script
1. Nhấn **Execute** (hoặc F5)
2. Đợi script hoàn tất
3. Refresh danh sách databases (F5 trên Object Explorer)
4. Kiểm tra database **MessagingAppDB** đã được tạo

### Bước 3: Kiểm Tra Tables
Expand `MessagingAppDB` → `Tables`, nên thấy:
- Users
- Friendships
- Conversations
- ConversationParticipants
- Messages
- CallHistory
- MessageReadStatus

### Bước 4: Kiểm Tra Sample Data
```sql
-- Chạy query này để xem users mẫu
SELECT * FROM Users
```
Kết quả: 3 users (admin, user1, user2)

---

## Cấu Hình Connection String

### Bước 1: Xác Định Server Name
Trong SSMS, xem **Server name** bạn đang kết nối.

Thông thường:
- `localhost`
- `.\SQLEXPRESS`
- `(local)`
- `(localdb)\MSSQLLocalDB` (nếu dùng LocalDB)

### Bước 2: Cập Nhật Code
1. Mở file: `MessagingApp\Utils\DatabaseConnection.cs`
2. Tìm dòng:
```csharp
private static readonly string connectionString = 
    @"Server=localhost;Database=MessagingAppDB;Integrated Security=True;TrustServerCertificate=True;";
```

3. Thay `localhost` bằng server name của bạn:

**Ví dụ với SQL Express:**
```csharp
@"Server=.\SQLEXPRESS;Database=MessagingAppDB;Integrated Security=True;TrustServerCertificate=True;";
```

**Ví dụ với LocalDB:**
```csharp
@"Server=(localdb)\MSSQLLocalDB;Database=MessagingAppDB;Integrated Security=True;TrustServerCertificate=True;";
```

**Ví dụ với SQL Authentication:**
```csharp
@"Server=localhost;Database=MessagingAppDB;User ID=sa;Password=YourPassword;TrustServerCertificate=True;";
```

### Bước 3: Lưu File
Nhấn Ctrl+S để lưu thay đổi

---

## Build và Chạy

### Phương Pháp 1: Command Line

#### Restore Dependencies
```bash
cd C:\Projects\NT106\MessagingApp
dotnet restore
```

#### Build Project
```bash
dotnet build
```
Kiểm tra output: `Build succeeded. 0 Warning(s). 0 Error(s)`

#### Chạy Ứng Dụng
```bash
dotnet run
```

### Phương Pháp 2: Visual Studio

#### Mở Project
1. Mở Visual Studio 2022
2. File → Open → Project/Solution
3. Chọn file: `MessagingApp\MessagingApp.csproj`

#### Build
1. Build → Build Solution (Ctrl+Shift+B)
2. Xem Output window để kiểm tra lỗi

#### Run
1. Nhấn **Start** (F5) hoặc **Start Without Debugging** (Ctrl+F5)
2. Ứng dụng sẽ mở

### Phương Pháp 3: Visual Studio Code

#### Mở Folder
```bash
cd C:\Projects\NT106\MessagingApp
code .
```

#### Cài Extension
- C# Dev Kit
- .NET Extension Pack

#### Run
1. Nhấn F5
2. Chọn "C# - .NET" configuration

---

## Đăng Nhập Lần Đầu

### Sử Dụng Tài Khoản Mẫu

Sau khi ứng dụng mở, đăng nhập với:

**Tài khoản Admin:**
```
Username: admin
Password: password123
```

**Tài khoản User 1:**
```
Username: user1
Password: password123
```

**Tài khoản User 2:**
```
Username: user2
Password: password123
```

### Hoặc Tạo Tài Khoản Mới
1. Nhấn **"Đăng Ký Tài Khoản Mới"**
2. Điền thông tin
3. Nhấn **"Đăng Ký"**
4. Quay lại và đăng nhập

---

## Xử Lý Sự Cố

### Lỗi: "Cannot connect to database"

**Nguyên nhân**: SQL Server không chạy hoặc connection string sai

**Giải pháp**:
1. Kiểm tra SQL Server đang chạy:
   - Mở **Services** (Win+R → `services.msc`)
   - Tìm **SQL Server (SQLEXPRESS)** hoặc **SQL Server (MSSQLSERVER)**
   - Status phải là **Running**
   - Nếu không, nhấn chuột phải → **Start**

2. Kiểm tra connection string:
   - Mở `DatabaseConnection.cs`
   - Đảm bảo server name đúng
   - Test kết nối trong SSMS

3. Kiểm tra database exists:
   ```sql
   SELECT name FROM sys.databases WHERE name = 'MessagingAppDB'
   ```

### Lỗi: "The type or namespace name 'Forms' does not exist"

**Nguyên nhân**: Project không nhận diện Windows Forms

**Giải pháp**:
1. Kiểm tra `MessagingApp.csproj`:
   ```xml
   <UseWindowsForms>true</UseWindowsForms>
   ```
2. Clean và rebuild:
   ```bash
   dotnet clean
   dotnet restore
   dotnet build
   ```

### Lỗi: "Login failed for user"

**Nguyên nhân**: SQL Authentication không được enable

**Giải pháp**:
1. Mở SSMS
2. Chuột phải vào server → Properties
3. Security → SQL Server and Windows Authentication mode
4. Restart SQL Server service

### Lỗi: Database already exists

**Giải pháp**:
```sql
-- Xóa database cũ
USE master;
GO
ALTER DATABASE MessagingAppDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE MessagingAppDB;
GO

-- Chạy lại CreateDatabase.sql
```

### Ứng dụng không hiển thị gì

**Giải pháp**:
1. Kiểm tra `Program.cs`:
   ```csharp
   Application.Run(new LoginForm());
   ```
2. Rebuild project
3. Kiểm tra Event Viewer cho errors

### Lỗi Font/Display

**Nguyên nhân**: DPI scaling issues

**Giải pháp**:
1. Chuột phải vào exe → Properties → Compatibility
2. Check "Override high DPI scaling behavior"
3. Chọn "System (Enhanced)"

---

## Gỡ Cài Đặt

### Gỡ Ứng Dụng
```bash
# Xóa thư mục project
cd C:\Projects
rmdir /s /q NT106
```

### Gỡ Database
```sql
USE master;
GO
DROP DATABASE MessagingAppDB;
GO
```

### Gỡ SQL Server
1. Control Panel → Programs and Features
2. Tìm "Microsoft SQL Server"
3. Uninstall

### Gỡ .NET SDK
1. Control Panel → Programs and Features
2. Tìm "Microsoft .NET SDK"
3. Uninstall

---

## Cập Nhật Ứng Dụng

### Pull Latest Changes
```bash
cd C:\Projects\NT106
git pull origin main
```

### Update Dependencies
```bash
cd MessagingApp
dotnet restore
```

### Rebuild
```bash
dotnet clean
dotnet build
```

---

## Tiếp Theo

Sau khi cài đặt thành công:
1. Đọc [User Guide](UserGuide.md) để học cách sử dụng
2. Đọc [Technical Documentation](TechnicalDocumentation.md) để hiểu cấu trúc code
3. Xem [SCREENSHOTS](SCREENSHOTS.md) để biết giao diện

---

## Hỗ Trợ

Nếu gặp vấn đề:
1. Kiểm tra phần [Xử Lý Sự Cố](#xử-lý-sự-cố) ở trên
2. Tạo issue: https://github.com/Whats-up-pro/NT106/issues
3. Đính kèm:
   - Lỗi message
   - Screenshot (nếu có)
   - Phiên bản Windows
   - Phiên bản .NET SDK
   - Phiên bản SQL Server

---

**Chúc bạn cài đặt thành công! 🎉**
