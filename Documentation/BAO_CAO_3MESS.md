# BÁO CÁO ĐỒ ÁN NT106 – 3Mess (Chat App WPF + Firebase)

## PHỤ LỤC (Mục lục)
- Chương 1: Lời mở đầu
  - 1.1 Lý do chọn đề tài
  - 1.2 Mục tiêu của đề tài
  - 1.3 Phạm vi đề tài
  - 1.4 Đối tượng sử dụng
  - 1.5 Phương pháp khảo sát và nghiên cứu
- Chương 2: Cơ sở lý thuyết
  - 2.1 Windows Presentation Foundation (WPF)
  - 2.2 Firebase (Authentication, Cloud Firestore, Cloud Storage)
- Chương 3: Phân tích, thiết kế hệ thống
  - 3.1 Sơ đồ phân rã chức năng (DFD – Decomposition Diagram)
  - 3.2 Sơ đồ Use Case
  - 3.3 Thiết kế cơ sở dữ liệu
  - 3.4 Thiết kế giao diện (UI)
  - 3.5 Kiến trúc hệ thống
- Chương 4: Hiện thực đề tài
- Chương 5: Kiểm thử đề tài
- Chương 6: Kết luận & hướng phát triển
- Tài liệu tham khảo

---

# Chương 1: Lời mở đầu
Chương này trình bày động cơ lựa chọn đề tài, mục tiêu, phạm vi triển khai và phương pháp thực hiện để xây dựng ứng dụng nhắn tin thời gian thực trên desktop Windows. Đồ án tập trung vào việc áp dụng WPF theo kiến trúc MVVM và tích hợp Firebase/Firestore nhằm đảm bảo đồng bộ dữ liệu realtime, dễ mở rộng và thuận tiện triển khai.

## 1.1 Lý do chọn đề tài
Trong bối cảnh nhu cầu giao tiếp tức thời tăng mạnh, các ứng dụng chat như Messenger/Zalo/Discord đã trở thành công cụ phổ biến cho học tập và làm việc. Song song đó, ứng dụng desktop vẫn có vai trò quan trọng nhờ tính ổn định, khả năng kiểm soát tài nguyên và trải nghiệm sử dụng tốt trên môi trường nội bộ.

WPF là nền tảng mạnh mẽ để xây dựng UI hiện đại cho Windows, đặc biệt khi kết hợp MVVM giúp tách biệt giao diện và logic, tăng khả năng bảo trì và phát triển theo nhóm. Vì vậy, nhóm lựa chọn đề tài xây dựng Chat App bằng WPF theo mô hình MVVM và sử dụng Firebase để giảm tải backend, tập trung vào chức năng và trải nghiệm người dùng.

## 1.2 Mục tiêu của đề tài
- Xây dựng ứng dụng chat desktop có UI trực quan, thao tác nhanh.
- Tích hợp xác thực tài khoản và lưu trữ dữ liệu cloud thông qua Firebase.
- Áp dụng MVVM để tổ chức mã nguồn rõ ràng, dễ bảo trì/mở rộng.
- Hiện thực các chức năng cốt lõi: đăng nhập/đăng ký, bạn bè, nhắn tin realtime, thông báo, hồ sơ cá nhân.

## 1.3 Phạm vi đề tài
**Phạm vi chức năng (đã hiện thực trong 3Mess):**
- Đăng ký/đăng nhập/quên mật khẩu (Firebase Authentication).
- Danh sách bạn bè, tìm bạn theo email/username, gửi lời mời kết bạn.
- Chấp nhận/từ chối lời mời kết bạn.
- Chat 1-1 và nhóm (Firestore) với cơ chế lắng nghe realtime.
- Thông báo trong ứng dụng (popup chuông), có badge và điều hướng.
- Trang cá nhân: chỉnh sửa avatar/cover/bio.
- Giao diện sáng/tối (theme) và tùy chọn quyền riêng tư (ẩn/hiện trạng thái hoạt động).

**Phạm vi kỹ thuật:**
- Ngôn ngữ: C#.
- Nền tảng: .NET 8, WPF (net8.0-windows).
- Kiến trúc: MVVM.
- Backend cloud: Firebase Admin SDK + Cloud Firestore + Cloud Storage.

**Giới hạn:**
- Ứng dụng chạy trên Windows (desktop), chưa hỗ trợ mobile/macOS/Linux.
- Dùng Firebase gói miễn phí nên có giới hạn quota.


## 1.4 Đối tượng sử dụng
- Sinh viên, giảng viên, nhóm học tập/làm việc nhỏ cần công cụ nhắn tin nội bộ.
- Người dùng phổ thông trên Windows muốn một công cụ chat gọn nhẹ.
- Sinh viên CNTT có thể tham khảo cách tổ chức dự án MVVM và tích hợp Firebase.

## 1.5 Phương pháp khảo sát và nghiên cứu
- Khảo sát UX từ các ứng dụng chat phổ biến để chọn các luồng chức năng cốt lõi.
- Nghiên cứu WPF + MVVM (binding, command, templates, resources).
- Nghiên cứu Firebase (Auth/Firestore/Storage), cấu hình service account và security rules.
- Phát triển theo quy trình: phân tích yêu cầu → thiết kế → hiện thực → kiểm thử thủ công → hoàn thiện.

---

# Chương 2: Cơ sở lý thuyết

## 2.1 Windows Presentation Foundation (WPF)
WPF là framework UI cho Windows, sử dụng XAML để mô tả giao diện và C# cho logic. WPF hỗ trợ Data Binding, Styles, Templates, ResourceDictionary và tích hợp tốt với kiến trúc MVVM.

Trong 3Mess, WPF được dùng để:
- Xây dựng UI đa vùng (danh sách bên trái, chat giữa, sidebar phải).
- Binding dữ liệu realtime từ ViewModel sang View.
- Theming bằng ResourceDictionary (Light/Dark) thông qua DynamicResource.

## 2.2 Firebase
Firebase là nền tảng cloud của Google, cung cấp nhiều dịch vụ. Trong đồ án:
- **Firebase Authentication**: quản lý tài khoản email/password.
- **Cloud Firestore**: lưu dữ liệu người dùng, bạn bè, lời mời kết bạn, hội thoại và tin nhắn.
- **Cloud Storage**: lưu ảnh/file đính kèm (hạ tầng đã tích hợp ở core service, có thể sử dụng cho ảnh/file gửi trong chat).

---

# Chương 3: Phân tích, thiết kế hệ thống

## 3.1 Sơ đồ phân rã chức năng (DFD – Decomposition Diagram)
Hệ thống 3Mess được phân rã thành các nhóm chức năng chính:
1. Xác thực người dùng
   - Đăng ký, đăng nhập, quên mật khẩu
2. Quản lý người dùng
   - Hồ sơ cá nhân (avatar/cover/bio)
   - Cài đặt (theme, quyền riêng tư)
3. Quản lý bạn bè
   - Tìm bạn
   - Gửi/nhận lời mời
   - Chấp nhận/từ chối
4. Nhắn tin
   - Chat 1-1/nhóm
   - Realtime listener, lịch sử tin nhắn
   - Thao tác tin nhắn cơ bản
5. Thông báo
   - Tổng hợp thông báo tin nhắn và kết bạn
   - Điều hướng từ thông báo

**Hình 1.** Sơ đồ phân rã chức năng (nhóm tự chèn hình vào đây).

## 3.2 Sơ đồ Use Case
Các use case chính:
- UC01: Đăng ký
- UC02: Đăng nhập
- UC03: Quên mật khẩu
- UC04: Tìm người dùng (Friend Finder)
- UC05: Gửi lời mời kết bạn
- UC06: Chấp nhận/Từ chối lời mời
- UC07: Mở cuộc trò chuyện
- UC08: Gửi tin nhắn
- UC09: Nhận tin nhắn realtime
- UC10: Xem thông báo và điều hướng
- UC11: Cập nhật hồ sơ cá nhân
- UC12: Bật/tắt dark mode và quyền riêng tư

**Hình 2.** Sơ đồ Use Case (nhóm tự chèn hình vào đây).

## 3.3 Thiết kế cơ sở dữ liệu
Dự án sử dụng **Cloud Firestore** (NoSQL). Các collection chính (đúng theo code service hiện tại):

### 3.3.1 Collection `users`
- Mục đích: lưu thông tin người dùng và cấu hình UI.
- Ví dụ field:
  - `userId`, `email`, `username`, `fullName`
  - `status` (online/offline…)
  - `avatarDataUrl` / `avatarUrl` / `photoUrl`
  - `coverDataUrl` / `coverUrl`
  - `bio`
  - `theme` (light/dark)
  - `showOnlineStatus` (privacy)
  - `notificationsEnabled`
  - `createdAt`, `lastLogin`

### 3.3.2 Collection `friendRequests`
- Mục đích: quản lý lời mời kết bạn.
- Trạng thái điển hình: pending/accepted/declined/cancelled (tùy triển khai).
- Thông tin: `fromUserId`, `toUserId`, `createdAt`, `status`.

### 3.3.3 Collection `friendships`
- Mục đích: lưu quan hệ bạn bè đã chấp nhận.
- Thông tin: `userId1`, `userId2`, `createdAt`.

### 3.3.4 Collection `conversations`
- Mục đích: lưu hội thoại 1-1/nhóm.
- Field tiêu biểu: `participants` (mảng userId), `isGroup`, `title`, `lastMessageAt`.

### 3.3.5 Collection `messages` (top-level)
- Mục đích: lưu tin nhắn của các hội thoại, liên kết bằng field `conversationId`.
- Field tiêu biểu: `conversationId`, `senderId`, `content`, `type` (text/image/file/link), `timestamp`, `read`.

**Hình 3.** Firestore schema/ERD quy ước (nhóm tự chèn hình vào đây).

## 3.4 Thiết kế giao diện (UI)
Thiết kế theo bố cục 3 vùng:
- **Trái**: danh sách bạn bè/nhóm, ô tìm kiếm.
- **Giữa**: khung chat hoặc profile/friend finder.
- **Phải**: khu vực nhanh (avatar cá nhân, thông báo, cài đặt).

Các màn hình chính:
- Landing / Login / Signup / Forgot Password.
- MainWindow (chat).
- Popup thông báo.
- Trang profile (xem/sửa).

Ghi chú UI quan trọng:
- Avatar hiển thị hình tròn (mask Ellipse).
- Dark mode bằng ResourceDictionary.

**Hình 4..12.** Ảnh giao diện (nhóm tự chèn hình vào đây).

## 3.5 Kiến trúc hệ thống
Dự án áp dụng MVVM:
- View: XAML (MainWindow, các Window khác).
- ViewModel: xử lý state + command (MainViewModel, LoginViewModel, SignupViewModel…).
- Services (tầng truy cập Firebase/Firestore/Storage): nằm trong project core.

Cấu trúc solution (tóm tắt):
- 3Mess: UI WPF + ViewModels.
- MessagingApp.Core: Config + Services dùng chung (Firestore/Firebase).

Tham khảo mô tả dự án và hướng dẫn chạy tại: [README.md](README.md) và [Documentation/FIREBASE_SETUP.md](Documentation/FIREBASE_SETUP.md).

---

# Chương 4: Hiện thực đề tài

## 4.1 Công nghệ và môi trường
- .NET 8, WPF
- FirebaseAdmin + Google.Cloud.Firestore + Google.Cloud.Storage.V1
- Visual Studio

## 4.2 Các module hiện thực
- **Auth**: đăng ký/đăng nhập/quên mật khẩu.
- **Friends**: friend finder, gửi/chấp nhận/từ chối lời mời.
- **Messaging**: chat realtime theo hội thoại, danh sách tin nhắn.
- **Notifications**: popup chuông, badge, click điều hướng.
- **Profile & Settings**: chỉnh sửa hồ sơ, theme, quyền riêng tư.

## 4.3 Hướng dẫn cấu hình và chạy
Tóm tắt (chi tiết xem [Documentation/FIREBASE_SETUP.md](Documentation/FIREBASE_SETUP.md)):
1. Tạo Firebase project, bật Auth (Email/Password) và Firestore.
2. Tạo service account JSON và cấu hình theo hướng dẫn.
3. Build/run solution.

---

# Chương 5: Kiểm thử đề tài

## 5.1 Phương pháp kiểm thử
- Kiểm thử thủ công theo checklist (UI + luồng nghiệp vụ).
- Kiểm thử tích hợp với Firebase sau khi cấu hình credentials.

## 5.2 Một số test case tiêu biểu
1. Đăng ký tài khoản mới → tạo user Auth + tạo document `users/{uid}`.
2. Đăng nhập → load dữ liệu người dùng và vào màn hình chính.
3. Tìm bạn theo email/username → hiển thị kết quả.
4. Gửi lời mời kết bạn → tạo `friendRequests`.
5. Người nhận chấp nhận/từ chối → cập nhật `friendRequests` và/hoặc tạo `friendships`.
6. Gửi tin nhắn → tin xuất hiện ngay và sync Firestore.
7. Thông báo: có badge, click mở chat/profile.
8. Dark mode: đổi theme và lưu setting.

---

# Chương 6: Kết luận & hướng phát triển

## 6.1 Kết luận
Đồ án đã xây dựng được ứng dụng chat desktop theo MVVM, tích hợp Firebase/Firestore để xử lý dữ liệu cloud và realtime. Hệ thống đáp ứng các chức năng cốt lõi: xác thực, kết bạn, nhắn tin, thông báo, hồ sơ cá nhân và tùy chọn giao diện.

## 6.2 Hướng phát triển
- Hoàn thiện gửi ảnh/file qua Firebase Storage (tối ưu upload + preview).
- Bổ sung test tự động (unit/integration) và logging.

---

# Tài liệu tham khảo
1. Microsoft Docs – WPF overview.
2. Microsoft Docs – MVVM pattern.
3. Firebase Docs – Authentication.
4. Firebase Docs – Cloud Firestore.
5. Google Cloud – Firestore .NET client library.
6. Tài liệu cấu hình Firebase của dự án: [Documentation/FIREBASE_SETUP.md](Documentation/FIREBASE_SETUP.md)
