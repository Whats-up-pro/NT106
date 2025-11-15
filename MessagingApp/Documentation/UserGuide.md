# Hướng Dẫn Sử Dụng - Messaging App

## Mục Lục
1. [Đăng Nhập](#đăng-nhập)
2. [Đăng Ký](#đăng-ký)
3. [Quên Mật Khẩu](#quên-mật-khẩu)
4. [Màn Hình Chính](#màn-hình-chính)
5. [Hồ Sơ Cá Nhân](#hồ-sơ-cá-nhân)
6. [Bạn Bè](#bạn-bè)
7. [Tin Nhắn](#tin-nhắn)
8. [Cuộc Gọi](#cuộc-gọi)

---

## Đăng Nhập

### Cách Đăng Nhập
1. Khởi động ứng dụng
2. Nhập tên đăng nhập hoặc email
3. Nhập mật khẩu
4. Nhấn nút **"Đăng Nhập"**

### Lưu Ý
- Tên đăng nhập và email đều có thể được sử dụng để đăng nhập
- Mật khẩu được mã hóa để bảo mật
- Tài khoản phải được kích hoạt (IsActive = true)

### Tài Khoản Mẫu
```
Username: admin
Password: password123
```

---

## Đăng Ký

### Các Bước Đăng Ký
1. Từ màn hình đăng nhập, nhấn **"Đăng Ký Tài Khoản Mới"**
2. Điền các thông tin bắt buộc:
   - Tên đăng nhập (tối thiểu 3 ký tự)
   - Email (định dạng hợp lệ)
   - Mật khẩu (tối thiểu 6 ký tự)
   - Xác nhận mật khẩu
3. Điền thông tin tùy chọn:
   - Họ và tên
   - Số điện thoại
4. Nhấn **"Đăng Ký"**

### Quy Tắc Xác Thực
- **Tên đăng nhập**: 
  - Tối thiểu 3 ký tự
  - Phải là duy nhất
- **Email**: 
  - Định dạng hợp lệ (example@domain.com)
  - Phải là duy nhất
- **Mật khẩu**: 
  - Tối thiểu 6 ký tự
  - Mật khẩu xác nhận phải khớp

---

## Quên Mật Khẩu

### Khôi Phục Mật Khẩu
1. Từ màn hình đăng nhập, nhấn **"Quên mật khẩu?"**
2. Nhập email đã đăng ký
3. Nhập mật khẩu mới (tối thiểu 6 ký tự)
4. Xác nhận mật khẩu mới
5. Nhấn **"Đặt Lại Mật Khẩu"**

### Lưu Ý
- Email phải tồn tại trong hệ thống
- Mật khẩu mới sẽ được mã hóa trước khi lưu

---

## Màn Hình Chính

### Giao Diện
Màn hình chính gồm hai phần:
- **Sidebar (Thanh Bên)**: Menu điều hướng
- **Content Area (Khu Vực Nội Dung)**: Hiển thị thông tin

### Menu Điều Hướng
1. **💬 Tin Nhắn**: Quản lý tin nhắn
2. **👥 Bạn Bè**: Danh sách bạn bè
3. **📞 Cuộc Gọi**: Lịch sử cuộc gọi
4. **👤 Hồ Sơ**: Thông tin cá nhân
5. **🚪 Đăng Xuất**: Thoát khỏi ứng dụng

### Tính Năng
- Hiển thị tên người dùng
- Danh sách cuộc trò chuyện gần đây
- Thông báo tin nhắn mới

---

## Hồ Sơ Cá Nhân

### Thông Tin Có Thể Chỉnh Sửa
1. **Email**: Địa chỉ email liên hệ
2. **Họ và Tên**: Tên đầy đủ
3. **Số Điện Thoại**: Số liên lạc
4. **Tiểu Sử**: Mô tả ngắn về bản thân
5. **Trạng Thái**: 
   - Online (Trực tuyến)
   - Away (Vắng mặt)
   - Busy (Bận)
   - Offline (Ngoại tuyến)

### Cách Cập Nhật
1. Nhấn **"👤 Hồ Sơ"** từ menu
2. Chỉnh sửa thông tin mong muốn
3. Nhấn **"Lưu Thay Đổi"**

### Lưu Ý
- Tên đăng nhập không thể thay đổi
- Thông tin được cập nhật ngay lập tức

---

## Bạn Bè

### Danh Sách Bạn Bè
Hiển thị:
- Tên bạn bè
- Trạng thái (Online/Offline/Away/Busy)
- Email

### Tìm Kiếm Bạn Bè
1. Nhập tên, email hoặc username vào ô tìm kiếm
2. Nhấn **"🔍 Tìm Kiếm"**
3. Kết quả sẽ được lọc theo từ khóa

### Thêm Bạn Bè
1. Nhấn **"➕ Thêm Bạn"**
2. Tìm kiếm người dùng
3. Gửi lời mời kết bạn
   *(Tính năng đang phát triển)*

### Trạng Thái Quan Hệ
- **Pending**: Chờ chấp nhận
- **Accepted**: Đã chấp nhận
- **Blocked**: Đã chặn

---

## Tin Nhắn

### Giao Diện Tin Nhắn
- **Danh sách tin nhắn**: Hiển thị lịch sử
- **Ô nhập tin nhắn**: Soạn tin nhắn mới
- **Nút gửi**: Gửi tin nhắn

### Gửi Tin Nhắn
1. Chọn người nhận từ danh sách bạn bè
2. Nhập tin nhắn vào ô chat
3. Nhấn **"📤 Gửi"** hoặc Enter

### Tính Năng
- Lịch sử tin nhắn được lưu trữ
- Hiển thị thời gian gửi
- Hỗ trợ tin nhắn văn bản
- *Tin nhắn hình ảnh, file (đang phát triển)*

### Loại Tin Nhắn
- **Text**: Tin nhắn văn bản
- **Image**: Hình ảnh *(đang phát triển)*
- **File**: Tệp đính kèm *(đang phát triển)*
- **Audio**: Tin nhắn âm thanh *(đang phát triển)*
- **Video**: Video *(đang phát triển)*

---

## Cuộc Gọi

### Lịch Sử Cuộc Gọi
Hiển thị:
- Tên người gọi/nhận
- Loại cuộc gọi (📞 Thoại / 📹 Video)
- Trạng thái:
  - ✅ Hoàn thành
  - ❌ Nhỡ
  - 🚫 Từ chối
  - ⚠️ Thất bại
- Thời lượng
- Thời gian

### Thực Hiện Cuộc Gọi
1. **Gọi Thoại**:
   - Nhấn **"📞 Gọi Thoại"**
   - Chọn người nhận
   - *(Tính năng đang phát triển)*

2. **Gọi Video**:
   - Nhấn **"📹 Gọi Video"**
   - Chọn người nhận
   - *(Tính năng đang phát triển)*

### Trong Cuộc Gọi
- Bật/Tắt microphone
- Bật/Tắt camera (video call)
- Kết thúc cuộc gọi
- *(Tính năng đang phát triển)*

---

## Màu Sắc Giao Diện

### Bảng Màu
- **Primary Blue**: #2563EB - Màu chính
- **Dark Blue**: #1E3A8A - Nền tối
- **Light Blue**: #3B82F6 - Điểm nhấn
- **Background Dark**: #111827 - Nền đen
- **Background Medium**: #1F2937 - Nền xám đen
- **White**: #FFFFFF - Văn bản
- **Success Green**: #22C55E - Thành công
- **Error Red**: #EF4444 - Lỗi
- **Warning Yellow**: #EAB308 - Cảnh báo

### Phối Màu
- Nút chính: Xanh dương sáng trên nền tối
- Nút phụ: Viền xanh dương, nền đen
- Text: Trắng trên nền tối
- Links: Xanh dương nhạt

---

## Xử Lý Lỗi

### Lỗi Đăng Nhập
- **"Vui lòng nhập đầy đủ thông tin"**: Điền username và password
- **"Tên đăng nhập hoặc mật khẩu không đúng"**: Kiểm tra lại thông tin
- **"Lỗi kết nối database"**: Kiểm tra SQL Server và connection string

### Lỗi Đăng Ký
- **"Tên đăng nhập phải có ít nhất 3 ký tự"**: Username quá ngắn
- **"Email không hợp lệ"**: Kiểm tra định dạng email
- **"Mật khẩu phải có ít nhất 6 ký tự"**: Password quá ngắn
- **"Mật khẩu xác nhận không khớp"**: Password và confirm password khác nhau
- **"Tên đăng nhập hoặc email đã tồn tại"**: Chọn username/email khác

### Lỗi Kết Nối Database
```
Kiểm tra:
1. SQL Server đang chạy
2. Database MessagingAppDB đã được tạo
3. Connection string đúng
4. Quyền truy cập database
```

---

## Bảo Mật

### Mã Hóa Mật Khẩu
- Sử dụng SHA256 hash
- Mật khẩu không được lưu dạng plaintext
- Mỗi lần đăng nhập so sánh hash

### Bảo Vệ Dữ Liệu
- Validation đầu vào
- SQL injection prevention
- Secure connection string

---

## Tips và Tricks

### Hiệu Suất
- Đóng các form không sử dụng
- Đăng xuất khi không sử dụng
- Xóa tin nhắn cũ định kỳ

### Tùy Chỉnh
- Cập nhật trạng thái phù hợp với tình trạng
- Thêm thông tin hồ sơ đầy đủ
- Sử dụng ảnh đại diện *(đang phát triển)*

---

## Câu Hỏi Thường Gặp

**Q: Làm thế nào để thay đổi mật khẩu?**  
A: Sử dụng tính năng "Quên mật khẩu" từ màn hình đăng nhập.

**Q: Tôi có thể xóa tài khoản không?**  
A: Liên hệ admin hoặc sử dụng tính năng vô hiệu hóa tài khoản.

**Q: Ứng dụng có hoạt động offline không?**  
A: Không, cần kết nối database để hoạt động.

**Q: Làm thế nào để thêm bạn bè?**  
A: Tính năng đang được phát triển, sẽ có trong phiên bản tiếp theo.

---

## Liên Hệ Hỗ Trợ

Nếu gặp vấn đề, vui lòng:
1. Kiểm tra tài liệu
2. Xem phần xử lý lỗi
3. Tạo issue trên GitHub: https://github.com/Whats-up-pro/NT106/issues

---

**Phiên bản**: 1.0.0  
**Cập nhật lần cuối**: 2025-10-13  
**Tác giả**: 614_2U0C Team
