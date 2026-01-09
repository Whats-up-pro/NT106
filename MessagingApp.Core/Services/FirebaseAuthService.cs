using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using MessagingApp.Config;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MessagingApp.Services
{
    /// <summary>
    /// Service for Firebase Authentication operations
    /// </summary>
    public class FirebaseAuthService
    {
        private static FirebaseAuthService? _instance;
        private static readonly object _lock = new object();
        private static readonly HttpClient _http = new HttpClient();
        private readonly FirebaseAuth _auth;
        private readonly FirestoreDb _db;

        /// <summary>
        /// Current authenticated user ID
        /// </summary>
        public string? CurrentUserId { get; private set; }

        /// <summary>
        /// Current authenticated user data
        /// </summary>
        public Dictionary<string, object>? CurrentUserData { get; private set; }

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static FirebaseAuthService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new FirebaseAuthService();
                    }
                }
                return _instance;
            }
        }

        private FirebaseAuthService()
        {
            FirebaseConfig.Initialize();
            _auth = FirebaseAuth.GetAuth(FirebaseConfig.GetApp());
            _db = FirebaseConfig.GetFirestoreDb();
        }

        /// <summary>
        /// Đăng nhập bằng email + mật khẩu.
        /// Lưu ý: Firebase Admin SDK không hỗ trợ xác thực mật khẩu. Vì vậy phải dùng Identity Toolkit REST API
        /// (accounts:signInWithPassword) với Firebase Web API Key.
        /// </summary>
        public async Task<(bool success, string message, string? userId)> SignInWithEmailPassword(string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    return (false, "Vui lòng nhập email và mật khẩu.", null);
                }

                // If the project has "Email Enumeration Protection" enabled, Firebase REST may return
                // INVALID_LOGIN_CREDENTIALS for both wrong email and wrong password.
                // To satisfy UX requirements, we first check whether the email exists using Admin SDK.
                // - If email does NOT exist: show generic message.
                // - If email exists but sign-in fails: show "Mật khẩu không đúng".
                try
                {
                    await _auth.GetUserByEmailAsync(email.Trim());
                }
                catch (FirebaseAuthException ex) when (
                    ex.AuthErrorCode == AuthErrorCode.UserNotFound ||
                    ex.AuthErrorCode == AuthErrorCode.EmailNotFound)
                {
                    return (false, "Email hoặc mật khẩu không đúng.", null);
                }

                string? apiKey = FirebaseConfig.WebApiKey;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return (false,
                        "Thiếu Firebase Web API Key. Hãy đặt biến môi trường FIREBASE_WEB_API_KEY (hoặc FIREBASE_API_KEY) để đăng nhập bằng mật khẩu.",
                        null);
                }

                // Verify email/password via Firebase Auth REST API
                var signIn = await SignInWithPasswordRestAsync(apiKey, email.Trim(), password);
                if (signIn == null || string.IsNullOrWhiteSpace(signIn.LocalId))
                {
                    return (false, "Đăng nhập thất bại.", null);
                }

                string uid = signIn.LocalId;

                // Lấy document user trong Firestore
                var userDoc = await _db.Collection("users").Document(uid).GetSnapshotAsync();
                if (!userDoc.Exists)
                {
                    return (false, "Dữ liệu người dùng không tồn tại.", null);
                }

                var userData = userDoc.ToDictionary();
                CurrentUserId = uid;
                CurrentUserData = userData;

                // Cập nhật thời gian đăng nhập + trạng thái
                await UpdateLastLogin(uid);

                return (true, "Đăng nhập thành công!", uid);
            }
            catch (FirebaseAuthRestException ex)
            {
                // At this point we already know the email exists => treat generic credential errors as wrong password.
                if (ex.Code == "INVALID_LOGIN_CREDENTIALS" || ex.Code == "INVALID_CREDENTIAL")
                {
                    return (false, "Mật khẩu không đúng.", null);
                }

                return (false, MapFirebaseAuthRestErrorToVietnamese(ex.Code), null);
            }
            catch (FirebaseAuthException ex)
            {
                return (false, $"Lỗi xác thực: {ex.Message}", null);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Không thể kết nối dịch vụ đăng nhập: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}", null);
            }
        }

        private sealed class FirebaseAuthRestException : Exception
        {
            public string Code { get; }

            public FirebaseAuthRestException(string code)
                : base(code)
            {
                Code = code;
            }
        }

        private sealed class SignInWithPasswordResponse
        {
            [JsonPropertyName("localId")]
            public string? LocalId { get; set; }

            [JsonPropertyName("idToken")]
            public string? IdToken { get; set; }

            [JsonPropertyName("refreshToken")]
            public string? RefreshToken { get; set; }

            [JsonPropertyName("expiresIn")]
            public string? ExpiresIn { get; set; }
        }

        private static async Task<SignInWithPasswordResponse?> SignInWithPasswordRestAsync(string apiKey, string email, string password)
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={Uri.EscapeDataString(apiKey)}";

            var payload = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var json = JsonSerializer.Serialize(payload);
            using var response = await _http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                string code = TryExtractFirebaseAuthRestError(body) ?? response.ReasonPhrase ?? "UNKNOWN";
                throw new FirebaseAuthRestException(code);
            }

            return JsonSerializer.Deserialize<SignInWithPasswordResponse>(body);
        }

        private static string? TryExtractFirebaseAuthRestError(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("error", out var err) &&
                    err.TryGetProperty("message", out var msg))
                {
                    return msg.GetString();
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        private static string MapFirebaseAuthRestErrorToVietnamese(string error)
        {
            // Common Identity Toolkit error codes
            return error switch
            {
                "EMAIL_NOT_FOUND" => "Email không tồn tại.",
                "INVALID_PASSWORD" => "Mật khẩu không đúng.",
                "INVALID_LOGIN_CREDENTIALS" => "Email hoặc mật khẩu không đúng.",
                "INVALID_CREDENTIAL" => "Email hoặc mật khẩu không đúng.",
                "USER_DISABLED" => "Tài khoản đã bị vô hiệu hóa.",
                "TOO_MANY_ATTEMPTS_TRY_LATER" => "Thử đăng nhập quá nhiều lần. Vui lòng thử lại sau.",
                "INVALID_EMAIL" => "Email không hợp lệ.",
                _ => $"Đăng nhập thất bại: {error}"
            };
        }

        /// <summary>
        /// Gửi email khôi phục mật khẩu (Firebase sẽ gửi email thật theo template trong Firebase Console).
        /// Dùng Identity Toolkit REST API accounts:sendOobCode với Web API Key.
        /// </summary>
        public async Task<(bool success, string message)> SendPasswordResetEmail(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return (false, "Vui lòng nhập email.");
                }

                string? apiKey = FirebaseConfig.WebApiKey;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return (false, "Thiếu Firebase Web API Key. Vui lòng cấu hình Web API Key để gửi email khôi phục.");
                }

                var url = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={Uri.EscapeDataString(apiKey)}";
                var payload = new
                {
                    requestType = "PASSWORD_RESET",
                    email = email.Trim(),
                };

                var json = JsonSerializer.Serialize(payload);
                using var response = await _http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var code = TryExtractFirebaseAuthRestError(body) ?? response.ReasonPhrase ?? "UNKNOWN";

                    // Keep generic to avoid leaking whether the email exists.
                    if (code == "EMAIL_NOT_FOUND")
                    {
                        return (true, "Nếu email tồn tại, hệ thống đã gửi email khôi phục. Vui lòng kiểm tra hộp thư (và Spam)." );
                    }

                    if (code == "INVALID_EMAIL")
                    {
                        return (false, "Email không hợp lệ.");
                    }

                    return (false, $"Không thể gửi email khôi phục: {code}");
                }

                return (true, "Đã gửi email khôi phục mật khẩu. Vui lòng kiểm tra hộp thư (và Spam)." );
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Không thể kết nối dịch vụ gửi email: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Sign up new user with email and password
        /// </summary>
        public async Task<(bool success, string message, string? userId)> SignUpWithEmailPassword(
            string email, 
            string password, 
            string username, 
            string fullName)
        {
            try
            {
                // Check if username already exists
                var usernameQuery = await _db.Collection("users")
                    .WhereEqualTo("username", username)
                    .Limit(1)
                    .GetSnapshotAsync();

                if (usernameQuery.Count > 0)
                {
                    return (false, "Tên đăng nhập đã tồn tại.", null);
                }

                // Create user in Firebase Authentication
                var userArgs = new UserRecordArgs
                {
                    Email = email,
                    Password = password,
                    DisplayName = fullName,
                    EmailVerified = false
                };

                var userRecord = await _auth.CreateUserAsync(userArgs);

                // Create user document in Firestore
                var userData = new Dictionary<string, object>
                {
                    { "userId", userRecord.Uid },
                    { "username", username },
                    { "email", email },
                    { "fullName", fullName },
                    { "phoneNumber", "" },
                    { "avatarUrl", "" },
                    { "bio", "" },
                    { "status", "offline" },
                    { "createdAt", Timestamp.GetCurrentTimestamp() },
                    { "lastLogin", Timestamp.GetCurrentTimestamp() },
                    { "isActive", true },
                    { "theme", "light" }
                };

                await _db.Collection("users").Document(userRecord.Uid).SetAsync(userData);

                CurrentUserId = userRecord.Uid;
                CurrentUserData = userData;

                return (true, "Đăng ký thành công!", userRecord.Uid);
            }
            catch (FirebaseAuthException ex)
            {
                string errorMessage = ex.AuthErrorCode switch
                {
                    AuthErrorCode.EmailAlreadyExists => "Email đã được sử dụng.",
                    _ => $"Lỗi xác thực: {ex.Message}"
                };
                return (false, errorMessage, null);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Update user password
        /// </summary>
        public async Task<(bool success, string message)> UpdatePassword(string userId, string newPassword)
        {
            try
            {
                var userArgs = new UserRecordArgs
                {
                    Uid = userId,
                    Password = newPassword
                };

                await _auth.UpdateUserAsync(userArgs);

                return (true, "Mật khẩu đã được cập nhật thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi cập nhật mật khẩu: {ex.Message}");
            }
        }

        /// <summary>
        /// Sign out current user
        /// </summary>
        public async Task SignOut()
        {
            if (CurrentUserId != null)
            {
                // Update status to offline
                await UpdateUserStatus(CurrentUserId, "offline");
                CurrentUserId = null;
                CurrentUserData = null;
            }
        }

        /// <summary>
        /// Get current user data from Firestore
        /// </summary>
        public async Task<Dictionary<string, object>?> GetCurrentUserData()
        {
            if (CurrentUserId == null)
                return null;

            try
            {
                var userDoc = await _db.Collection("users").Document(CurrentUserId).GetSnapshotAsync();

                if (userDoc.Exists)
                {
                    CurrentUserData = userDoc.ToDictionary();
                    return CurrentUserData;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Update user status
        /// </summary>
        public async Task UpdateUserStatus(string userId, string status)
        {
            try
            {
                await _db.Collection("users").Document(userId).UpdateAsync(new Dictionary<string, object>
                {
                    { "status", status }
                });

                if (CurrentUserData != null && userId == CurrentUserId)
                {
                    CurrentUserData["status"] = status;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating status: {ex.Message}");
            }
        }

        /// <summary>
        /// Update last login timestamp
        /// </summary>
        private async Task UpdateLastLogin(string userId)
        {
            try
            {
                await _db.Collection("users").Document(userId).UpdateAsync(new Dictionary<string, object>
                {
                    { "lastLogin", Timestamp.GetCurrentTimestamp() },
                    { "status", "online" }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating last login: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if user is signed in
        /// </summary>
        public bool IsSignedIn()
        {
            return CurrentUserId != null;
        }
    }
}
