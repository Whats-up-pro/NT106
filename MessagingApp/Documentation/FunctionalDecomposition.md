# Sơ Đồ Phân Rã Chức Năng - Ứng Dụng Nhắn Tin

## 1. Hệ Thống Xác Thực (Authentication)
```
├── Đăng Nhập (Login)
│   ├── Xác thực tài khoản và mật khẩu
│   ├── Kiểm tra trạng thái tài khoản
│   └── Tạo phiên đăng nhập
├── Đăng Ký (Registration)
│   ├── Nhập thông tin người dùng
│   ├── Kiểm tra tính hợp lệ
│   ├── Mã hóa mật khẩu
│   └── Tạo tài khoản mới
└── Quên Mật Khẩu (Forgot Password)
    ├── Xác minh email/số điện thoại
    ├── Tạo mã xác thực
    └── Đặt lại mật khẩu
```

## 2. Quản Lý Người Dùng (User Management)
```
├── Hồ Sơ Cá Nhân (Personal Profile)
│   ├── Xem thông tin cá nhân
│   ├── Chỉnh sửa thông tin
│   ├── Thay đổi ảnh đại diện
│   └── Cập nhật trạng thái
└── Cài Đặt Tài Khoản
    ├── Thay đổi mật khẩu
    ├── Cài đặt quyền riêng tư
    └── Cài đặt thông báo
```

## 3. Quản Lý Bạn Bè (Friends Management)
```
├── Danh Sách Bạn Bè
│   ├── Hiển thị danh sách
│   ├── Tìm kiếm bạn bè
│   └── Xem trạng thái online/offline
├── Thêm Bạn Bè
│   ├── Tìm kiếm người dùng
│   ├── Gửi lời mời kết bạn
│   └── Chấp nhận/Từ chối lời mời
└── Quản Lý Quan Hệ
    ├── Xóa bạn bè
    ├── Chặn người dùng
    └── Báo cáo người dùng
```

## 4. Nhắn Tin (Messaging)
```
├── Trò Chuyện 1-1
│   ├── Gửi tin nhắn văn bản
│   ├── Gửi hình ảnh
│   ├── Gửi file
│   └── Xem lịch sử tin nhắn
├── Nhóm Chat
│   ├── Tạo nhóm
│   ├── Thêm/Xóa thành viên
│   └── Gửi tin nhắn nhóm
└── Tính Năng Tin Nhắn
    ├── Xóa tin nhắn
    ├── Chỉnh sửa tin nhắn
    ├── Emoji và sticker
    └── Thông báo đã đọc/chưa đọc
```

## 5. Gọi Điện (Voice/Video Call)
```
├── Gọi Thoại
│   ├── Khởi tạo cuộc gọi
│   ├── Nhận cuộc gọi
│   ├── Từ chối cuộc gọi
│   └── Kết thúc cuộc gọi
├── Gọi Video
│   ├── Bật/Tắt camera
│   ├── Bật/Tắt microphone
│   └── Chia sẻ màn hình
└── Quản Lý Cuộc Gọi
    ├── Lịch sử cuộc gọi
    ├── Cuộc gọi nhỡ
    └── Gọi lại
```

## 6. Màn Hình Chính (Main Screen)
```
├── Danh Sách Cuộc Trò Chuyện
│   ├── Hiển thị các cuộc trò chuyện gần đây
│   ├── Tìm kiếm cuộc trò chuyện
│   └── Ghim cuộc trò chuyện
├── Thông Báo
│   ├── Tin nhắn mới
│   ├── Cuộc gọi nhỡ
│   └── Lời mời kết bạn
└── Điều Hướng
    ├── Chuyển đến tin nhắn
    ├── Chuyển đến danh sách bạn bè
    └── Chuyển đến hồ sơ cá nhân
```

## 7. Cơ Sở Dữ Liệu (Database)
```
├── Quản Lý Người Dùng
│   ├── Lưu trữ thông tin người dùng
│   └── Quản lý phiên đăng nhập
├── Quản Lý Tin Nhắn
│   ├── Lưu trữ tin nhắn
│   └── Lưu trữ file đính kèm
├── Quản Lý Quan Hệ
│   ├── Lưu trữ danh sách bạn bè
│   └── Lưu trữ lời mời kết bạn
└── Quản Lý Cuộc Gọi
    ├── Lưu lịch sử cuộc gọi
    └── Lưu trữ thông tin cuộc gọi
```

## 8. Giao Diện Người Dùng (UI)
```
├── Màu Sắc Chủ Đạo
│   ├── Xanh Dương (Primary): #1E3A8A, #2563EB
│   ├── Đen (Secondary): #000000, #1F2937
│   └── Màu Phụ: #3B82F6, #60A5FA, #93C5FD
├── Font Chữ
│   ├── Tiêu đề: Segoe UI Bold
│   └── Nội dung: Segoe UI Regular
└── Bố Cục
    ├── Responsive design
    ├── Navigation sidebar
    └── Content area
```
