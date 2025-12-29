using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessagingApp.Config;

namespace MessagingApp.Services
{
    /// <summary>
    /// Service for managing friendships and friend requests in Firestore
    /// </summary>
    public class FirestoreFriendsService
    {
        private static FirestoreFriendsService? _instance;
        public static FirestoreFriendsService Instance => _instance ??= new FirestoreFriendsService();

        private readonly FirestoreDb _db;

        private FirestoreFriendsService()
        {
            _db = FirebaseConfig.GetFirestoreDb();
        }

        /// <summary>
        /// Search users by email or username
        /// </summary>
        public async Task<List<Dictionary<string, object>>> SearchUsers(string searchText, string currentUserId)
        {
            try
            {
                var users = new List<Dictionary<string, object>>();
                var seen = new HashSet<string>(StringComparer.Ordinal);

                // Search by email (exact match preferred)
                var emailQuery = _db.Collection("users")
                    .WhereGreaterThanOrEqualTo("email", searchText.ToLower())
                    .WhereLessThanOrEqualTo("email", searchText.ToLower() + "\uf8ff")
                    .Limit(10);

                var emailSnapshot = await emailQuery.GetSnapshotAsync();
                
                foreach (var doc in emailSnapshot.Documents)
                {
                    if (doc.Id != currentUserId)
                    {
                        var userData = doc.ToDictionary();
                        userData["userId"] = doc.Id;
                        if (seen.Add(doc.Id)) users.Add(userData);
                    }
                }

                // Search by username (add-on)
                var usernameQuery = _db.Collection("users")
                    .WhereGreaterThanOrEqualTo("username", searchText.ToLower())
                    .WhereLessThanOrEqualTo("username", searchText.ToLower() + "\uf8ff")
                    .Limit(10);

                var usernameSnapshot = await usernameQuery.GetSnapshotAsync();
                foreach (var doc in usernameSnapshot.Documents)
                {
                    if (doc.Id == currentUserId) continue;
                    var userData = doc.ToDictionary();
                    userData["userId"] = doc.Id;
                    if (seen.Add(doc.Id)) users.Add(userData);
                }

                // Search by fullName (add-on)
                var fullNameQuery = _db.Collection("users")
                    .WhereGreaterThanOrEqualTo("fullName", searchText)
                    .WhereLessThanOrEqualTo("fullName", searchText + "\uf8ff")
                    .Limit(10);

                var fullNameSnapshot = await fullNameQuery.GetSnapshotAsync();
                foreach (var doc in fullNameSnapshot.Documents)
                {
                    if (doc.Id == currentUserId) continue;
                    var userData = doc.ToDictionary();
                    userData["userId"] = doc.Id;
                    if (seen.Add(doc.Id)) users.Add(userData);
                }

                return users;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching users: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        /// <summary>
        /// Send friend request
        /// </summary>
        public async Task<(bool success, string message)> SendFriendRequest(string fromUserId, string toUserId)
        {
            try
            {
                // Block sending to self
                if (fromUserId == toUserId)
                {
                    return (false, "Bạn không thể gửi lời mời kết bạn cho chính mình!");
                }

                // Check if already friends (any existing friendship doc for this pair)
                var existingFriendship = await GetFriendship(fromUserId, toUserId);
                if (existingFriendship != null)
                {
                    return (false, "Hai bạn đã là bạn bè!");
                }

                // Check if there's already a pending request in either direction
                var existingRequest = await GetFriendRequest(fromUserId, toUserId);
                if (existingRequest != null)
                {
                    return (false, "Lời mời kết bạn đã tồn tại, vui lòng chờ phản hồi.");
                }

                // Idempotent creation: use deterministic document ID for the pending request
                string canonicalId = GetCanonicalPairId(fromUserId, toUserId);
                var requestRef = _db.Collection("friendRequests").Document(canonicalId);
                var requestSnap = await requestRef.GetSnapshotAsync();

                if (requestSnap.Exists)
                {
                    // Nếu doc đã tồn tại nhưng status không còn pending thì cho phép gửi lại
                    string status = requestSnap.ContainsField("status") ? requestSnap.GetValue<string>("status") : "";
                    if (string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, "Lời mời kết bạn đã tồn tại, vui lòng chờ phản hồi.");
                    }
                }

                var requestData = new Dictionary<string, object>
                {
                    { "fromUserId", fromUserId },
                    { "toUserId", toUserId },
                    { "status", "pending" },
                    { "createdAt", FieldValue.ServerTimestamp }
                };

                await requestRef.SetAsync(requestData, SetOptions.Overwrite);

                return (true, "Đã gửi lời mời kết bạn!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Get friend request between two users
        /// </summary>
        private async Task<DocumentSnapshot?> GetFriendRequest(string userId1, string userId2)
        {
            try
            {
                // Check both directions
                var query1 = await _db.Collection("friendRequests")
                    .WhereEqualTo("fromUserId", userId1)
                    .WhereEqualTo("toUserId", userId2)
                    .WhereEqualTo("status", "pending")
                    .GetSnapshotAsync();

                if (query1.Count > 0)
                    return query1.Documents[0];

                var query2 = await _db.Collection("friendRequests")
                    .WhereEqualTo("fromUserId", userId2)
                    .WhereEqualTo("toUserId", userId1)
                    .WhereEqualTo("status", "pending")
                    .GetSnapshotAsync();

                if (query2.Count > 0)
                    return query2.Documents[0];

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get friendship between two users
        /// </summary>
        private async Task<DocumentSnapshot?> GetFriendship(string userId1, string userId2)
        {
            try
            {
                var query = await _db.Collection("friendships")
                    .WhereArrayContains("users", userId1)
                    .GetSnapshotAsync();

                foreach (var doc in query.Documents)
                {
                    List<string>? users = null;
                    try
                    {
                        users = doc.GetValue<List<string>>("users");
                    }
                    catch
                    {
                        // Fallback for older documents stored with mixed types
                        var raw = doc.GetValue<IList<object>>("users");
                        users = raw?.Select(u => u?.ToString() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
                    }

                    if (users.Contains(userId2))
                        return doc;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get pending friend requests for current user
        /// </summary>
        public async Task<List<Dictionary<string, object>>> GetPendingRequests(string userId)
        {
            try
            {
                var requests = new List<Dictionary<string, object>>();
                Query query;
                QuerySnapshot snapshot;

                try
                {
                    // Preferred: filter + order by (may require composite index)
                    query = _db.Collection("friendRequests")
                        .WhereEqualTo("toUserId", userId)
                        .WhereEqualTo("status", "pending")
                        .OrderByDescending("createdAt");

                    snapshot = await query.GetSnapshotAsync();
                }
                catch (Exception ex)
                {
                    // Fallback: if index is missing, try without OrderBy and sort in-memory
                    Console.WriteLine($"GetPendingRequests primary query failed (likely missing index): {ex.Message}");

                    query = _db.Collection("friendRequests")
                        .WhereEqualTo("toUserId", userId)
                        .WhereEqualTo("status", "pending");

                    snapshot = await query.GetSnapshotAsync();
                }

                foreach (var doc in snapshot.Documents)
                {
                    var requestData = doc.ToDictionary();
                    requestData["requestId"] = doc.Id;

                    // Get sender info
                    string fromUserId = requestData.ContainsKey("fromUserId") && requestData["fromUserId"] != null
                        ? requestData["fromUserId"].ToString()!
                        : string.Empty;
                    if (string.IsNullOrEmpty(fromUserId))
                    {
                        // Skip malformed document
                        continue;
                    }
                    var userDoc = await _db.Collection("users").Document(fromUserId).GetSnapshotAsync();
                    
                    if (userDoc.Exists)
                    {
                        requestData["senderUsername"] = userDoc.GetValue<string>("username");
                        requestData["senderFullName"] = userDoc.GetValue<string>("fullName");
                        requestData["senderEmail"] = userDoc.GetValue<string>("email");
                    }

                    requests.Add(requestData);
                }

                // Ensure newest first if we fell back without OrderBy
                try
                {
                    requests = requests
                        .OrderByDescending(r => r.ContainsKey("createdAt") && r["createdAt"] is Timestamp
                            ? ((Timestamp)r["createdAt"]).ToDateTime()
                            : DateTime.MinValue)
                        .ToList();
                }
                catch { }

                return requests;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting pending requests: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        /// <summary>
        /// Accept friend request
        /// </summary>
        public async Task<(bool success, string message)> AcceptFriendRequest(string requestId, string fromUserId, string toUserId)
        {
            try
            {
                // Use transaction to avoid duplicates and ensure atomicity
                await _db.RunTransactionAsync(async transaction =>
                {
                    // 1) Load the request document
                    var requestRef = _db.Collection("friendRequests").Document(requestId);
                    var requestSnap = await transaction.GetSnapshotAsync(requestRef);

                    if (!requestSnap.Exists)
                    {
                        throw new InvalidOperationException("Lời mời không tồn tại hoặc đã bị xóa.");
                    }

                    string status = requestSnap.ContainsField("status") ? requestSnap.GetValue<string>("status") : "";
                    string reqFrom = requestSnap.ContainsField("fromUserId") ? requestSnap.GetValue<string>("fromUserId") : "";
                    string reqTo = requestSnap.ContainsField("toUserId") ? requestSnap.GetValue<string>("toUserId") : "";

                    if (!string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase))
                    {
                        // Idempotent: if already accepted, just return success
                        if (string.Equals(status, "accepted", StringComparison.OrdinalIgnoreCase))
                        {
                            return; // no-op
                        }
                        throw new InvalidOperationException("Lời mời không còn ở trạng thái chờ.");
                    }

                    // Validate participants match parameters
                    if (reqFrom != fromUserId || reqTo != toUserId)
                    {
                        throw new InvalidOperationException("Thông tin lời mời không khớp.");
                    }

                    // 2) Create or ensure friendship using deterministic document ID
                    string friendshipId = GetCanonicalPairId(fromUserId, toUserId);
                    var friendshipRef = _db.Collection("friendships").Document(friendshipId);

                    var friendshipSnap = await transaction.GetSnapshotAsync(friendshipRef);
                    if (!friendshipSnap.Exists)
                    {
                        var friendshipDataTx = new Dictionary<string, object>
                        {
                            { "users", new List<string> { fromUserId, toUserId } },
                            { "createdAt", FieldValue.ServerTimestamp }
                        };
                        transaction.Set(friendshipRef, friendshipDataTx);
                    }

                    // 3) Update request status -> accepted
                    transaction.Update(requestRef, new Dictionary<string, object>
                    {
                        { "status", "accepted" },
                        { "acceptedAt", FieldValue.ServerTimestamp }
                    });
                });

                // Best-effort: clean up any duplicate friendship docs that may exist from older logic
                try
                {
                    string canonicalId = GetCanonicalPairId(fromUserId, toUserId);
                    await CleanupDuplicateFriendships(fromUserId, toUserId, canonicalId);
                }
                catch (Exception cleanupEx)
                {
                    Console.WriteLine($"Cleanup duplicates failed: {cleanupEx.Message}");
                }

                return (true, "Đã chấp nhận lời mời kết bạn!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Listen to pending friend requests for a user in real-time
        /// </summary>
        public FirestoreChangeListener ListenToPendingRequests(string userId, Action onChanged)
        {
            // Prefer index; if missing, Firestore SDK will give an index error. In that case
            // consider removing OrderBy and triggering a manual refresh in the UI callback.
            var query = _db.Collection("friendRequests")
                .WhereEqualTo("toUserId", userId)
                .WhereEqualTo("status", "pending")
                .OrderByDescending("createdAt");

            return query.Listen(_ =>
            {
                try
                {
                    onChanged();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in pending requests listener callback: {ex.Message}");
                }
            });
        }

        private static string GetCanonicalPairId(string userId1, string userId2)
        {
            if (string.CompareOrdinal(userId1, userId2) < 0)
                return $"{userId1}_{userId2}";
            return $"{userId2}_{userId1}";
        }

        /// <summary>
        /// Listen to friendships for a user (real-time)
        /// </summary>
        public FirestoreChangeListener ListenToFriendships(string userId, Action onChanged)
        {
            var query = _db.Collection("friendships")
                .WhereArrayContains("users", userId);

            return query.Listen(_ =>
            {
                try { onChanged(); }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in friendships listener callback: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Listen to a user's presence/status changes in real-time.
        /// </summary>
        public FirestoreChangeListener ListenToUserStatus(string userId, Action onChanged)
        {
            var docRef = _db.Collection("users").Document(userId);
            return docRef.Listen(_ =>
            {
                try { onChanged(); }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in user status listener callback: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Fetch a user's document data.
        /// </summary>
        public async Task<Dictionary<string, object>?> GetUserAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId)) return null;
                var snap = await _db.Collection("users").Document(userId).GetSnapshotAsync();
                if (!snap.Exists) return null;

                var data = snap.ToDictionary();
                data["userId"] = snap.Id;
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Update a user's presence state.
        /// Writes: isOnline (bool), status ("online"/"offline"), lastSeen (server timestamp)
        /// </summary>
        public async Task UpdatePresenceAsync(string userId, bool isOnline)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId)) return;

                var docRef = _db.Collection("users").Document(userId);
                var updates = new Dictionary<string, object>
                {
                    { "isOnline", isOnline },
                    { "status", isOnline ? "online" : "offline" },
                    { "lastSeen", FieldValue.ServerTimestamp }
                };

                try
                {
                    await docRef.UpdateAsync(updates);
                }
                catch
                {
                    // In case the user doc doesn't exist yet.
                    await docRef.SetAsync(updates, SetOptions.MergeAll);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating presence: {ex.Message}");
            }
        }

        /// <summary>
        /// Count number of friends for a user.
        /// </summary>
        public async Task<int> GetFriendsCountAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId)) return 0;
                var snapshot = await _db.Collection("friendships")
                    .WhereArrayContains("users", userId)
                    .GetSnapshotAsync();
                return snapshot.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting friends count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get set of toUserIds for pending outgoing friend requests.
        /// </summary>
        public async Task<HashSet<string>> GetOutgoingPendingRequestToUserIdsAsync(string fromUserId)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                if (string.IsNullOrWhiteSpace(fromUserId)) return result;

                var snapshot = await _db.Collection("friendRequests")
                    .WhereEqualTo("fromUserId", fromUserId)
                    .WhereEqualTo("status", "pending")
                    .GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    try
                    {
                        if (doc.ContainsField("toUserId"))
                        {
                            var toId = doc.GetValue<string>("toUserId");
                            if (!string.IsNullOrWhiteSpace(toId)) result.Add(toId);
                        }
                    }
                    catch { }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting outgoing pending requests: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Get mapping fromUserId -> requestId for pending incoming friend requests.
        /// </summary>
        public async Task<Dictionary<string, string>> GetIncomingPendingRequestFromUserIdToRequestIdAsync(string toUserId)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                if (string.IsNullOrWhiteSpace(toUserId)) return result;

                var snapshot = await _db.Collection("friendRequests")
                    .WhereEqualTo("toUserId", toUserId)
                    .WhereEqualTo("status", "pending")
                    .GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    try
                    {
                        if (!doc.ContainsField("fromUserId")) continue;
                        var fromId = doc.GetValue<string>("fromUserId");
                        if (string.IsNullOrWhiteSpace(fromId)) continue;
                        result[fromId] = doc.Id;
                    }
                    catch { }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting incoming pending requests: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Remove friendship between two users if it exists.
        /// </summary>
        public async Task<(bool success, string message)> UnfriendAsync(string userId1, string userId2)
        {
            if (string.IsNullOrWhiteSpace(userId1) || string.IsNullOrWhiteSpace(userId2))
                return (false, "Thiếu thông tin người dùng.");
            if (userId1 == userId2)
                return (false, "Không hợp lệ.");

            try
            {
                var snapshot = await _db.Collection("friendships")
                    .WhereArrayContains("users", userId1)
                    .GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    List<string>? users = null;
                    try
                    {
                        users = doc.GetValue<List<string>>("users");
                    }
                    catch
                    {
                        try
                        {
                            var raw = doc.GetValue<IList<object>>("users");
                            users = raw?.Select(u => u?.ToString() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList();
                        }
                        catch { }
                    }

                    if (users != null && users.Contains(userId2))
                    {
                        await _db.Collection("friendships").Document(doc.Id).DeleteAsync();
                        return (true, "Đã hủy kết bạn.");
                    }
                }

                return (false, "Không tìm thấy quan hệ bạn bè.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Decline friend request
        /// </summary>
        public async Task<(bool success, string message)> DeclineFriendRequest(string requestId)
        {
            try
            {
                await _db.Collection("friendRequests").Document(requestId).UpdateAsync(new Dictionary<string, object>
                {
                    { "status", "declined" },
                    { "declinedAt", FieldValue.ServerTimestamp }
                });

                return (true, "Đã từ chối lời mời kết bạn!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Cancel an outgoing pending friend request (so the recipient no longer sees it).
        /// </summary>
        public async Task<(bool success, string message)> CancelFriendRequest(string fromUserId, string toUserId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fromUserId) || string.IsNullOrWhiteSpace(toUserId))
                    return (false, "Thiếu thông tin người dùng.");
                if (fromUserId == toUserId)
                    return (false, "Không hợp lệ.");

                // Prefer canonical id (newer logic)
                try
                {
                    var canonicalId = GetCanonicalPairId(fromUserId, toUserId);
                    var docRef = _db.Collection("friendRequests").Document(canonicalId);
                    var snap = await docRef.GetSnapshotAsync();
                    if (snap.Exists)
                    {
                        var status = snap.ContainsField("status") ? snap.GetValue<string>("status") : string.Empty;
                        var from = snap.ContainsField("fromUserId") ? snap.GetValue<string>("fromUserId") : string.Empty;
                        var to = snap.ContainsField("toUserId") ? snap.GetValue<string>("toUserId") : string.Empty;

                        if (string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase)
                            && string.Equals(from, fromUserId, StringComparison.Ordinal)
                            && string.Equals(to, toUserId, StringComparison.Ordinal))
                        {
                            await docRef.UpdateAsync(new Dictionary<string, object>
                            {
                                { "status", "cancelled" },
                                { "cancelledAt", FieldValue.ServerTimestamp }
                            });
                            return (true, "Đã hủy gửi lời mời kết bạn.");
                        }
                    }
                }
                catch { }

                // Fallback: query by fields
                var query = await _db.Collection("friendRequests")
                    .WhereEqualTo("fromUserId", fromUserId)
                    .WhereEqualTo("toUserId", toUserId)
                    .WhereEqualTo("status", "pending")
                    .Limit(1)
                    .GetSnapshotAsync();

                if (query.Count == 0)
                    return (false, "Không còn lời mời để hủy.");

                var doc = query.Documents[0];
                await _db.Collection("friendRequests").Document(doc.Id).UpdateAsync(new Dictionary<string, object>
                {
                    { "status", "cancelled" },
                    { "cancelledAt", FieldValue.ServerTimestamp }
                });

                return (true, "Đã hủy gửi lời mời kết bạn.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Get list of friends
        /// </summary>
        public async Task<List<Dictionary<string, object>>> GetFriends(string userId)
        {
            try
            {
                var friends = new List<Dictionary<string, object>>();
                var seen = new HashSet<string>();

                var query = _db.Collection("friendships")
                    .WhereArrayContains("users", userId);

                var snapshot = await query.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    List<string>? users = null;
                    try
                    {
                        users = doc.GetValue<List<string>>("users");
                    }
                    catch
                    {
                        // Fallback for older documents stored with mixed types
                        var raw = doc.GetValue<IList<object>>("users");
                        users = raw?.Select(u => u?.ToString() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
                    }

                    if (users == null || users.Count != 2)
                        continue;

                    // Defensive: find the other user id safely
                    var otherId = users.FirstOrDefault(u => u != userId);
                    if (string.IsNullOrEmpty(otherId))
                        continue;

                    // Skip duplicates if multiple friendship documents exist for the same pair
                    if (seen.Contains(otherId))
                        continue;

                    // Get friend's info
                    var friendDoc = await _db.Collection("users").Document(otherId).GetSnapshotAsync();
                    
                    if (friendDoc.Exists)
                    {
                        var friendData = friendDoc.ToDictionary();
                        friendData["userId"] = otherId;
                        friendData["friendshipId"] = doc.Id;
                        if (!friendData.ContainsKey("status") || friendData["status"] == null)
                        {
                            friendData["status"] = "offline";
                        }
                        friends.Add(friendData);
                        seen.Add(otherId);
                    }
                }

                return friends;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting friends: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        /// <summary>
        /// Remove duplicate friendship documents for a user pair, keeping only the canonical document id
        /// </summary>
        private async Task CleanupDuplicateFriendships(string userId1, string userId2, string keepDocumentId)
        {
            try
            {
                var query = await _db.Collection("friendships")
                    .WhereArrayContains("users", userId1)
                    .GetSnapshotAsync();

                foreach (var doc in query.Documents)
                {
                    if (doc.Id == keepDocumentId) continue;

                    List<string>? users = null;
                    try { users = doc.GetValue<List<string>>("users"); }
                    catch
                    {
                        var raw = doc.GetValue<IList<object>>("users");
                        users = raw?.Select(u => u?.ToString() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
                    }

                    if (users != null && users.Contains(userId2))
                    {
                        await doc.Reference.DeleteAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up duplicate friendships: {ex.Message}");
            }
        }

        /// <summary>
        /// Unfriend a user
        /// </summary>
        public async Task<(bool success, string message)> Unfriend(string friendshipId)
        {
            try
            {
                await _db.Collection("friendships").Document(friendshipId).DeleteAsync();
                return (true, "Đã hủy kết bạn!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }
    }
}
