# BÁO CÁO ĐỒ ÁN NT106 – 3Mess (Chat App WPF + Firebase)

## Mục lục
Chương I: Tổng quan đề tài
  1.1 Giới thiệu đề tài
  1.2 Mục tiêu đề tài
  1.3 Phạm vi đề tài
  1.4 Công nghệ sử dụng
Chương II: Phân tích và thiết kế hệ thống
  2.1 Sơ đồ phân rã chức năng (DFD - Decomposition Diagram)
  2.2 Sơ đồ use-case
    2.2.1 Chức năng đăng nhập
    2.2.2 Chức năng đăng ký
    2.2.3 Chức năng quên mật khẩu
    2.2.4 Chức năng tìm bạn (Friend Finder)
    2.2.5 Chức năng gửi lời mời kết bạn
    2.2.6 Chức năng chấp nhận/từ chối kết bạn
    2.2.7 Chức năng chat (mở hội thoại, gửi tin nhắn)
    2.2.8 Chức năng gửi hình ảnh và tập tin
    2.2.9 Chức năng thông báo trong ứng dụng
    2.2.10 Chức năng cập nhật hồ sơ cá nhân
    2.2.11 Chức năng cài đặt (theme, quyền riêng tư)
  2.3 Thiết kế giao diện (UI)
  2.4 Kiến trúc hệ thống
Chương III: Hiện thực đề tài
  3.1 Công nghệ và môi trường
  3.2 Hiện thực các module
  3.3 Thiết kế cơ sở dữ liệu
  3.4 Hướng dẫn cấu hình và chạy
Chương IV: Kiểm thử đề tài
Chương V: Kết luận
Tài liệu tham khảo

---

# Chương I. TỔNG QUAN ĐỀ TÀI.

## 1.1 Giới thiệu đề tài.
Trong thời đại số hóa hiện nay, giao tiếp trực tuyến đã trở thành một phần không thể thiếu trong học tập và công việc. Đề tài đồ án **“3Mess – Ứng dụng nhắn tin trên desktop Windows”** hướng đến việc xây dựng một nền tảng giao tiếp nhanh chóng, thuận tiện, có đồng bộ dữ liệu theo thời gian thực.

Ứng dụng hỗ trợ các chức năng chính như: đăng ký/đăng nhập, tìm kiếm và kết bạn, chat 1-1 và chat nhóm, gửi/nhận tin nhắn thời gian thực, chia sẻ hình ảnh/tập tin đính kèm, thông báo trong ứng dụng, và cài đặt giao diện (sáng/tối) kèm tùy chọn quyền riêng tư (ẩn/hiện trạng thái hoạt động).

## 1.2 Mục tiêu đề tài.
Mục tiêu đề tài bao gồm các nội dung chính:

• Xây dựng nền tảng giao tiếp thời gian thực
  - Phát triển ứng dụng chat có khả năng gửi/nhận tin nhắn nhanh, đồng bộ và cập nhật theo thời gian thực thông qua realtime listener.

• Đảm bảo tính riêng tư cơ bản
  - Cung cấp tùy chọn ẩn/hiện trạng thái hoạt động (privacy) và lưu cấu hình theo tài khoản.

• Tối ưu hóa trải nghiệm người dùng
  - Thiết kế UI WPF theo MVVM, thao tác nhanh; có thông báo trong ứng dụng (chuông + badge), hỗ trợ dark mode, và quản lý hồ sơ cá nhân (avatar/cover/bio).

## 1.3 Phạm vi đề tài
Đề tài tập trung phát triển ứng dụng chat desktop trên nền tảng **WPF (.NET 8)** kết hợp **Firebase** để xử lý xác thực và lưu trữ dữ liệu.

Phạm vi chức năng:
• Client-side (WPF)
  - Đăng ký/đăng nhập/quên mật khẩu.
  - Giao diện chính hiển thị danh sách hội thoại/bạn bè.
  - Cho phép gửi và nhận:
    - Tin nhắn văn bản thời gian thực.
    - Hình ảnh (được nén và gửi theo dạng dữ liệu trong tin nhắn).
    - Tập tin đính kèm (upload lên Firebase Storage, lưu tham chiếu trong tin nhắn).
  - Hiển thị trạng thái hoạt động (online/offline) có xét thời gian `lastSeen`.
  - Thông báo trong ứng dụng khi có tin nhắn/lời mời kết bạn.
  - Theme sáng/tối (light/dark) và tùy chọn quyền riêng tư.

• Server-side (Firebase)
  - Firebase Authentication: quản lý tài khoản người dùng.
  - Cloud Firestore: lưu dữ liệu người dùng, bạn bè, hội thoại và tin nhắn.
  - Firebase Storage: lưu trữ file/tập tin đính kèm.

Giới hạn đề tài:
• Chỉ triển khai trên desktop Windows.
• Sử dụng Firebase có giới hạn quota/dung lượng tùy gói.

## 1.4 Công nghệ sử dụng.
1) Ngôn ngữ lập trình C#
- C# là ngôn ngữ chính để phát triển ứng dụng .NET, phù hợp xây dựng ứng dụng desktop Windows.

2) Windows Presentation Foundation (WPF)
- WPF là framework UI trên Windows sử dụng XAML + C#.
- 3Mess áp dụng MVVM để tách biệt UI và xử lý nghiệp vụ, dùng binding/command để giảm code-behind.

3) Firebase
- Firebase Authentication: đăng ký/đăng nhập tài khoản.
- Cloud Firestore: lưu dữ liệu người dùng, bạn bè, hội thoại và tin nhắn; hỗ trợ listener realtime.
- Firebase Storage: upload/download file đính kèm.

4) Quản lý dự án bằng GitHub
- Sử dụng Git để quản lý phiên bản và GitHub để lưu trữ source code, thuận tiện làm việc nhóm.

---

# Chương II. PHÂN TÍCH THIẾT KẾ HỆ THỐNG.

## 2.1 Sơ đồ phân rã chức năng (DFD - Decomposition Diagram)
Hệ thống được phân rã thành 5 phân hệ chính:

1) **Xác thực (Auth)**
- Đăng ký
- Đăng nhập
- Quên mật khẩu

2) **Người dùng (User/Profile)**
- Xem hồ sơ
- Sửa hồ sơ (bio, avatar, cover)
- Sinh/lưu khóa RSA (nền tảng bảo mật)

3) **Bạn bè (Friends)**
- Friend Finder: tìm user theo email/username/fullName
- Gửi lời mời kết bạn
- Nhận & xử lý lời mời (accept/decline)
- Danh sách bạn bè + trạng thái hoạt động

4) **Nhắn tin (Messaging)**
- Tạo/mở hội thoại
- Gửi/nhận tin nhắn realtime
- Typing indicator
- Đánh dấu đã đọc
- Thu hồi/xóa tin nhắn

5) **Thông báo & Cài đặt (Notifications/Settings)**
- Popup chuông + badge
- Điều hướng từ thông báo
- Bật/tắt thông báo
- Dark mode
- Quyền riêng tư trạng thái hoạt động

Gợi ý minh họa DFD:
- Level 0: User ↔ 3Mess ↔ Firebase.
- Level 1: tách luồng Auth/Friends/Messaging/Profile/Settings.

## 2.2 Sơ đồ use-case
Gợi ý cách làm sơ đồ use-case cho “đúng app chat” và dễ nhìn:
- Vẽ **2 sơ đồ** thay vì 1 sơ đồ quá nhiều đường nối:
  - **Hình 2.2.1**: Use-case nhóm **Auth** (Đăng nhập/Đăng ký/Quên mật khẩu).
  - **Hình 2.2.2**: Use-case nhóm **Chat & Friends** (tìm bạn, kết bạn, chat, gửi ảnh/file, thông báo, profile, cài đặt).
- Tác nhân chính: **Người dùng**.
- Tác nhân phụ (nếu bạn muốn vẽ rõ backend): **Firebase Authentication**, **Cloud Firestore**, **Firebase Storage**.

**Hình 2.x.** Sơ đồ use-case (nhóm tự chèn hình vào đây).

### 2.2.1 Chức năng đăng nhập
Mô tả (ngắn gọn):
- Người dùng nhập **email** và **mật khẩu**.
- Hệ thống xác thực tài khoản bằng **Firebase Authentication**.
- Đăng nhập thành công: tải thông tin `users/{uid}` và chuyển sang giao diện chính.

Ghi chú: ứng dụng xác thực mật khẩu qua Firebase Auth REST API (`signInWithPassword`) và cần Web API Key.

### 2.2.2 Chức năng đăng ký
Mô tả (ngắn gọn):
- Người dùng nhập thông tin đăng ký (email, mật khẩu, tên hiển thị).
- Hệ thống tạo tài khoản trên Firebase Authentication và tạo hồ sơ `users/{uid}` trên Firestore.
- Ứng dụng sinh RSA keypair và lưu public key lên Firestore (phục vụ nền tảng bảo mật).

### 2.2.3 Chức năng quên mật khẩu
Mô tả (ngắn gọn):
- Người dùng nhập email.
- Hệ thống yêu cầu Firebase gửi email khôi phục mật khẩu.
- Người dùng mở email và đặt lại mật khẩu theo link.

Ghi chú: để đảm bảo bảo mật, ứng dụng có thể hiển thị thông báo dạng chung.

### 2.2.4 Chức năng tìm bạn (Friend Finder)
Mô tả (ngắn gọn):
- Người dùng nhập từ khóa (email/username/tên).
- Hệ thống tìm kiếm người dùng và hiển thị danh sách kết quả.
- Người dùng chọn kết quả để xem hồ sơ và trạng thái kết bạn.

### 2.2.5 Chức năng gửi lời mời kết bạn
Mô tả (ngắn gọn):
- Người dùng chọn người khác và bấm gửi lời mời kết bạn.
- Hệ thống kiểm tra hợp lệ (không tự gửi cho mình, chưa là bạn, chưa có lời mời đang chờ).
- Tạo lời mời kết bạn ở trạng thái **pending**.

### 2.2.6 Chức năng chấp nhận/từ chối kết bạn
Mô tả (ngắn gọn):
- Người dùng xem danh sách lời mời đang chờ.
- Chấp nhận: cập nhật trạng thái và thiết lập quan hệ bạn bè.
- Từ chối: cập nhật trạng thái lời mời.

### 2.2.7 Chức năng chat (mở hội thoại, gửi tin nhắn)
Mô tả (ngắn gọn):
- Người dùng mở hội thoại với bạn bè/nhóm.
- Hệ thống tải lịch sử tin nhắn và cập nhật theo thời gian thực.
- Người dùng gửi tin nhắn, hệ thống lưu và hiển thị ngay cho các bên.

### 2.2.8 Chức năng gửi hình ảnh và tập tin
Mô tả (ngắn gọn):
- Người dùng chọn ảnh/tập tin để gửi.
- Hệ thống xử lý dữ liệu và lưu trữ file (nếu cần), sau đó gửi tin nhắn kèm nội dung/đính kèm.
- Người nhận có thể xem ảnh hoặc tải tập tin.

### 2.2.9 Chức năng thông báo trong ứng dụng
Mô tả (ngắn gọn):
- Ứng dụng hiển thị thông báo cho các sự kiện chính (tin nhắn mới, lời mời kết bạn, cập nhật quan hệ bạn bè).
- Có badge/popup và cho phép nhấn để điều hướng nhanh đến nội dung liên quan.

### 2.2.10 Chức năng cập nhật hồ sơ cá nhân
Mô tả (ngắn gọn):
- Người dùng xem và chỉnh sửa hồ sơ (avatar/cover/bio).
- Hệ thống lưu thay đổi lên Firestore.

### 2.2.11 Chức năng cài đặt (theme, quyền riêng tư)
Mô tả (ngắn gọn):
- Theme: chuyển sáng/tối.
- Quyền riêng tư: bật/tắt hiển thị trạng thái hoạt động.
- Thông báo: bật/tắt nhận thông báo trong ứng dụng.

## 2.3 Thiết kế giao diện (UI)
### 2.3.1 Tổng quan bố cục
3Mess thiết kế theo layout 3 cột:
- Cột trái: danh sách hội thoại/bạn bè + ô tìm kiếm + chuyển chế độ Friend Finder.
- Cột giữa: vùng nội dung chính (chat hoặc profile).
- Cột phải: sidebar nhanh (avatar user, chuông thông báo, cài đặt, media/file panel).

### 2.3.2 Các màn hình chính
- LandingWindow: giới thiệu/điều hướng đăng nhập.
- LoginWindow: đăng nhập.
- SignupWindow: đăng ký 2 bước.
- ForgotPasswordWindow: khôi phục mật khẩu.
- MainWindow:
  - Chat view
  - Friend Finder mode (list kết quả bên trái, profile ở giữa)
  - My Profile edit (avatar/cover/bio)
  - Popup notifications (chuông)
  - Popup settings (gear)

### 2.3.3 Quy chuẩn UI quan trọng
- Avatar hiển thị **hình tròn** bằng mask Ellipse + ImageBrush.
- Edit avatar/cover: cho phép kéo (drag) để chọn vùng hiển thị; chỉ commit khi bấm Save.
- Tránh MessageBox, dùng confirm/toast in-app để UX đồng nhất.

## 2.4 Kiến trúc hệ thống
### 2.4.1 Kiến trúc tổng thể
- Client WPF (3Mess)
- Firebase Authentication
- Cloud Firestore (data + realtime listener)
- Cloud Storage (upload/download file)

### 2.4.2 Áp dụng MVVM
- View (XAML): MainWindow.xaml, LoginWindow.xaml…
- ViewModel: MainViewModel, LoginViewModel, SignupViewModel…
- Infrastructure: ObservableObject, RelayCommand
- Services:
  - MessagingApp.Core/Services: FirestoreFriendsService, FirestoreMessagingService, FirebaseStorageService
  - 3Mess/Services: ThemeManager, RsaKeyService

Luồng dữ liệu:
- UI thao tác → ICommand → ViewModel → gọi Service → Firestore/Storage → listener callback cập nhật ObservableCollection → UI tự render.

---

# Chương III. HIỆN THỰC ĐỀ TÀI.

## 3.1 Công nghệ và môi trường
- .NET 8 (WPF), C#
- FirebaseAdmin, Google.Cloud.Firestore, Google.Cloud.Storage.V1
- Visual Studio

## 3.2 Hiện thực các module

### 3.2.1 Module Auth
- Đăng ký theo 2 bước, validate email/mật khẩu.
- Sau khi Firebase Auth tạo user:
  - sinh khóa RSA 2048,
  - lưu private key bằng DPAPI tại `%APPDATA%/3Mess/keys/{uid}.private.dpapi`,
  - lưu public key PEM vào `users/{uid}.rsaPublicKeyPem`.

- Đăng nhập:
  - xác thực email/mật khẩu qua Firebase Auth REST API (`signInWithPassword`), sau đó load dữ liệu user từ Firestore.
- Quên mật khẩu:
  - gọi REST API `sendOobCode` để Firebase gửi email reset thật; người dùng đặt lại mật khẩu qua link trong email.



### 3.2.2 Module Friends (Friend Finder + Friend Requests)
- Tìm user: query prefix trên `users.email`, `users.username`, `users.fullName` với kỹ thuật `\uf8ff`.
- Gửi lời mời:
  - kiểm tra đã là bạn,
  - kiểm tra đã có request pending,
  - tạo doc `friendRequests/{canonicalPairId}`.
- Chấp nhận:
  - transaction update request + tạo `friendships/{canonicalPairId}`.
- Realtime:
  - listener pending requests,
  - listener friendships,
  - listener status từng user (presence).

### 3.2.3 Module Presence + Privacy
- Presence được publish vào `users/{uid}` theo timer (khoảng 25s):
  - `isOnline`, `status`, `lastSeen`.
- Quy tắc hiển thị online:
  - chỉ coi là online nếu `isOnline=true` và `lastSeen` gần đây (<= 90s) để tránh tình trạng “kẹt online”.
- Privacy:
  - nếu user tắt “trạng thái hoạt động”, client sẽ publish `isOnline=false` (gating), dù app vẫn chạy.

### 3.2.4 Module Messaging
- 1-1 conversation:
  - `conversationId = canonicalPairId(user1,user2)`.
  - `conversations/{conversationId}` lưu `participants`, `lastMessage`, `lastMessageAt`…
- Group conversation:
  - tạo conversationId GUID,
  - lưu `isGroup=true`, `groupName`, `participants`, `groupAvatarDataUrl`.
- Message:
  - lưu vào collection `messages` với `conversationId`.
  - update `conversations.lastMessage` để list chat có preview.
- Typing:
  - `conversations/{conversationId}.typing.{userId} = server timestamp` khi đang gõ; xóa key khi dừng.
- Read:
  - messages có `read=false/true` và hàm mark as read.
- Thu hồi:
  - sender có thể xóa message bằng messageId.

### 3.2.5 Module Notifications
- App tổng hợp 3 nhóm:
  - tin nhắn mới (dựa trên lastMessageAt),
  - lời mời kết bạn pending,
  - friendship mới (accepted).
- UI:
  - icon chuông đổi trạng thái khi disable,
  - badge hiển thị theo tổng unread (giới hạn 20+).
- Click notification:
  - điều hướng tới conversation hoặc mở profile người gửi kèm nút accept/decline.
- Có cơ chế “startup populate” để khi mở lại app vẫn thấy pending requests.

### 3.2.6 Profile & UI Image
- Profile xem/sửa:
  - `avatarDataUrl`, `coverDataUrl`, `bio`.
- Crop/drag:
  - dùng Viewbox (Rect 0..1) để chọn vùng hiển thị trong Ellipse/Rectangle.
  - chỉ ghi Firestore khi user bấm Save.

### 3.2.7 Theme (Light/Dark)
- ThemeManager thay ResourceDictionary (Light/Dark).
- Cờ `theme` được lưu trong `users/{uid}.theme`.

### 3.2.8 Storage (hạ tầng upload/download)
- Có service upload/download lên Firebase Storage.
- ObjectName mặc định: `uploads/{guid}/{filename}`.
- Bucket có thể tự phát hiện nếu cấu hình sai.

## 3.3 Thiết kế cơ sở dữ liệu
3Mess sử dụng Firebase làm backend chính, bao gồm:
- **Firebase Authentication** để xác thực và quản lý tài khoản người dùng.
- **Cloud Firestore** để lưu dữ liệu nghiệp vụ (hồ sơ người dùng, quan hệ bạn bè, hội thoại, tin nhắn) và hỗ trợ realtime listener.
- **Firebase Storage** để lưu file đính kèm (ảnh/tài liệu), còn Firestore chỉ lưu metadata/tham chiếu.

Ngoài ra, sau khi đăng ký, ứng dụng sinh **RSA keypair 2048** để phục vụ nền tảng bảo mật: public key lưu trong hồ sơ `users/{uid}`, private key được mã hóa bằng DPAPI và lưu local trên máy người dùng.

**Hình 3.3.1.** Sơ đồ quan hệ dữ liệu (ERD) (nhóm tự chèn hình; có thể tham khảo file Documentation/DATABASE_ER_DIAGRAM.md).

**Bảng 3.3.1.** Các collection/thành phần chính và vai trò

| Collection/Thành phần | Vai trò chính |
|---|---|
| Firebase Authentication | Xác thực tài khoản, cấp `uid` định danh người dùng |
| Firestore `users` | Lưu thông tin hồ sơ, presence, cài đặt, và `rsaPublicKeyPem` |
| Firestore `friendRequests` | Lưu lời mời kết bạn (pending/accepted/declined) |
| Firestore `friendships` | Lưu quan hệ bạn bè đã thiết lập giữa 2 người dùng |
| Firestore `conversations` | Lưu metadata hội thoại (1-1/nhóm), danh sách thành viên, preview tin nhắn |
| Firestore `messages` | Lưu tin nhắn theo `conversationId`, có thể kèm metadata file/ảnh |
| Firebase Storage `uploads/...` | Lưu dữ liệu file/ảnh thực tế; message chỉ tham chiếu đường dẫn/object |

**Bảng 3.3.2.** Quan hệ giữa các collection/thành phần chính

| Mối quan hệ | Mô tả |
|---|---|
| Authentication ↔ `users` | 1-1: `uid` từ Auth là document id của `users/{uid}` |
| `users` ↔ `friendRequests` | 1-n: một user có thể gửi/nhận nhiều lời mời kết bạn |
| `friendRequests` ↔ `friendships` | 0-1: khi lời mời được chấp nhận sẽ hình thành quan hệ bạn bè |
| `users` ↔ `conversations` | n-n: một user tham gia nhiều hội thoại; mỗi hội thoại có nhiều thành viên |
| `conversations` ↔ `messages` | 1-n: một hội thoại chứa nhiều tin nhắn (liên kết bằng `conversationId`) |
| `messages` ↔ Storage objects | 0-1: một tin nhắn có thể tham chiếu 1 file/ảnh lưu trong Storage |

Cơ sở dữ liệu của 3Mess được thiết kế nhằm:
- Hỗ trợ đầy đủ chức năng: đăng nhập/đăng ký, kết bạn, chat 1-1/nhóm, gửi file/ảnh.
- Tối ưu realtime: dữ liệu phù hợp để lắng nghe cập nhật (listener) cho hội thoại/tin nhắn.
- Tối ưu truy vấn: ưu tiên cấu trúc đơn giản, và dùng id “deterministic” cho dữ liệu quan hệ 1-1 để tránh trùng.
- Tách biệt file nhị phân: file/ảnh lưu ở Storage, Firestore chỉ giữ metadata để giảm kích thước document.
- Dễ mở rộng: có thể bổ sung read-receipt, thu hồi/xóa tin nhắn, phân quyền nhóm… mà không phá vỡ cấu trúc hiện có.

## 3.4 Hướng dẫn cấu hình và chạy
Tham khảo chi tiết ở:
- Documentation/FIREBASE_SETUP.md

Tóm tắt:
1) Tạo Firebase project, bật Auth (Email/Password), Firestore, Storage.
2) Tạo service account JSON, đặt đường dẫn theo hướng dẫn.
3) (Khuyến nghị) set `FIREBASE_STORAGE_BUCKET` đúng bucket.
4) Cấu hình Firebase Web API Key cho client (để đăng nhập và gửi email reset):
   - điền `webApiKey` trong `3Mess/Config/firebase-client-config.json`.
5) Build và chạy solution.

---

# Chương IV. KIỂM THỬ ĐỀ TÀI.

## 4.1 Phương pháp kiểm thử
- Kiểm thử thủ công theo kịch bản (scenario-based testing).
- Kiểm thử tích hợp với Firebase (Auth/Firestore/Storage).

## 4.2 Test case tiêu biểu
### TC01 – Đăng ký
- Input: email hợp lệ, password >= 6, displayName.
- Expected:
  - tạo user Auth,
  - `users/{uid}` có `rsaPublicKeyPem`, `rsaCreatedAt`,
  - private key file tồn tại trong `%APPDATA%/3Mess/keys/`.

### TC02 – Friend Finder
- Input: search theo email/username/fullName.
- Expected: kết quả không chứa chính mình, hiển thị đúng avatar (nếu có).

### TC03 – Gửi lời mời kết bạn
- Expected: tạo `friendRequests/{canonicalPairId}` status=pending.
- Negative: gửi lại khi pending → báo “đã tồn tại”.

### TC04 – Accept lời mời
- Expected:
  - `friendRequests.status=accepted`,
  - tạo `friendships/{canonicalPairId}` với `users=[A,B]`.

### TC05 – Chat realtime
- Step: A gửi tin nhắn cho B.
- Expected:
  - B thấy message tự cập nhật qua listener,
  - `conversations.lastMessage` cập nhật.

### TC06 – Typing indicator
- Step: A gõ.
- Expected: B thấy trạng thái “đang nhập…”, dữ liệu phản ánh ở `conversations.typing.A`.

### TC07 – Notifications
- Step: có tin nhắn mới / có lời mời mới.
- Expected:
  - badge tăng,
  - click item điều hướng đúng.

### TC08 – Privacy (ẩn trạng thái hoạt động)
- Step: tắt “trạng thái hoạt động”.
- Expected:
  - publish presence = offline,
  - người khác thấy “Không hoạt động”.

---

# Chương V. KẾT LUẬN.

## 5.1 Kết luận
3Mess đã hiện thực một ứng dụng chat desktop theo MVVM, tích hợp Firebase/Firestore để đồng bộ dữ liệu realtime. Hệ thống đáp ứng các chức năng cốt lõi: xác thực, kết bạn, nhắn tin, thông báo, hồ sơ cá nhân, theme và quyền riêng tư.

## 5.2 Hướng phát triển
Tiếp tục tối ưu và bảo trì hệ thống.

---

# Tài liệu tham khảo
1. Microsoft Docs – WPF overview.
2. Microsoft Docs – MVVM pattern.
3. Firebase Docs – Authentication.
4. Firebase Docs – Cloud Firestore.
5. Google Cloud – Firestore .NET client library.
6. Tài liệu dự án: Documentation/FIREBASE_SETUP.md
