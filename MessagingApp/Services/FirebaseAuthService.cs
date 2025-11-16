using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using MessagingApp.Config;
using System;
using System.Collections.Generic;
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
        /// Sign in user with email and password (Custom token approach for desktop)
        /// Note: This is simplified. For production, use Firebase REST API or implement custom auth backend
        /// </summary>
        public async Task<(bool success, string message, string? userId)> SignInWithEmailPassword(string email, string password)
        {
            try
            {
                // Get user by email
                var userRecord = await _auth.GetUserByEmailAsync(email);

                if (userRecord == null)
                {
                    return (false, "Không tìm thấy người dùng với email này.", null);
                }

                // Note: Firebase Admin SDK cannot verify password directly
                // For desktop apps, you need to:
                // 1. Use Firebase REST API for email/password sign-in
                // 2. Or implement custom verification in Firestore

                // For now, we'll use a simplified approach:
                // Check if user exists in Firestore with matching credentials
                var userDoc = await _db.Collection("users").Document(userRecord.Uid).GetSnapshotAsync();

                if (!userDoc.Exists)
                {
                    return (false, "Dữ liệu người dùng không tồn tại.", null);
                }

                var userData = userDoc.ToDictionary();

                // TODO: Implement proper password verification
                // For now, we assume the user exists and is valid

                CurrentUserId = userRecord.Uid;
                CurrentUserData = userData;

                // Update last login
                await UpdateLastLogin(userRecord.Uid);

                return (true, "Đăng nhập thành công!", userRecord.Uid);
            }
            catch (FirebaseAuthException ex)
            {
                return (false, $"Lỗi xác thực: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}", null);
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
        /// Send password reset email
        /// </summary>
        public async Task<(bool success, string message)> SendPasswordResetEmail(string email)
        {
            try
            {
                // Check if user exists
                var userRecord = await _auth.GetUserByEmailAsync(email);

                if (userRecord == null)
                {
                    return (false, "Không tìm thấy người dùng với email này.");
                }

                // Generate password reset link
                string resetLink = await _auth.GeneratePasswordResetLinkAsync(email);

                // In production, send email here using SendGrid, SMTP, etc.
                // For demo, we just confirm the link was generated
                Console.WriteLine($"Password reset link: {resetLink}");

                return (true, "Email khôi phục mật khẩu đã được gửi! Vui lòng kiểm tra hộp thư của bạn.");
            }
            catch (FirebaseAuthException ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
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

        /// <summary>
        /// Update basic profile fields for current user (bio and avatar local path)
        /// Note: avatarUrl here is stored as a local file path for demo purposes; uploading to Cloud Storage is out of scope.
        /// </summary>
        public async Task<(bool success, string message)> UpdateUserProfile(string? bio, string? avatarLocalPath)
        {
            if (CurrentUserId == null)
            {
                return (false, "Chưa đăng nhập.");
            }

            try
            {
                var updates = new Dictionary<string, object>();
                if (bio != null) updates["bio"] = bio;
                if (!string.IsNullOrWhiteSpace(avatarLocalPath)) updates["avatarUrl"] = avatarLocalPath!;

                if (updates.Count == 0)
                    return (true, "Không có thay đổi.");

                await _db.Collection("users").Document(CurrentUserId).UpdateAsync(updates);

                // Update cache
                if (CurrentUserData != null)
                {
                    foreach (var kv in updates)
                    {
                        CurrentUserData[kv.Key] = kv.Value;
                    }
                }

                return (true, "Đã lưu hồ sơ.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi cập nhật hồ sơ: {ex.Message}");
            }
        }
    }
}
