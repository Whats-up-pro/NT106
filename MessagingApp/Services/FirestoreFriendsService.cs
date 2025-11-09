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
                        users.Add(userData);
                    }
                }

                // Search by username if no email results
                if (users.Count == 0)
                {
                    var usernameQuery = _db.Collection("users")
                        .WhereGreaterThanOrEqualTo("username", searchText.ToLower())
                        .WhereLessThanOrEqualTo("username", searchText.ToLower() + "\uf8ff")
                        .Limit(10);

                    var usernameSnapshot = await usernameQuery.GetSnapshotAsync();
                    
                    foreach (var doc in usernameSnapshot.Documents)
                    {
                        if (doc.Id != currentUserId)
                        {
                            var userData = doc.ToDictionary();
                            userData["userId"] = doc.Id;
                            users.Add(userData);
                        }
                    }
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
                // Check if already friends
                var existingFriendship = await GetFriendship(fromUserId, toUserId);
                if (existingFriendship != null)
                {
                    return (false, "Đã là bạn bè hoặc đã gửi lời mời!");
                }

                // Check if there's a pending request
                var existingRequest = await GetFriendRequest(fromUserId, toUserId);
                if (existingRequest != null)
                {
                    return (false, "Lời mời kết bạn đã được gửi trước đó!");
                }

                // Create friend request
                var requestData = new Dictionary<string, object>
                {
                    { "fromUserId", fromUserId },
                    { "toUserId", toUserId },
                    { "status", "pending" },
                    { "createdAt", FieldValue.ServerTimestamp }
                };

                await _db.Collection("friendRequests").AddAsync(requestData);

                return (true, "Lời mời kết bạn đã được gửi!");
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
                    var users = doc.GetValue<List<object>>("users");
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

                var query = _db.Collection("friendRequests")
                    .WhereEqualTo("toUserId", userId)
                    .WhereEqualTo("status", "pending")
                    .OrderByDescending("createdAt");

                var snapshot = await query.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    var requestData = doc.ToDictionary();
                    requestData["requestId"] = doc.Id;

                    // Get sender info
                    string fromUserId = requestData["fromUserId"].ToString()!;
                    var userDoc = await _db.Collection("users").Document(fromUserId).GetSnapshotAsync();
                    
                    if (userDoc.Exists)
                    {
                        requestData["senderUsername"] = userDoc.GetValue<string>("username");
                        requestData["senderFullName"] = userDoc.GetValue<string>("fullName");
                        requestData["senderEmail"] = userDoc.GetValue<string>("email");
                    }

                    requests.Add(requestData);
                }

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
                // Update request status
                await _db.Collection("friendRequests").Document(requestId).UpdateAsync(new Dictionary<string, object>
                {
                    { "status", "accepted" },
                    { "acceptedAt", FieldValue.ServerTimestamp }
                });

                // Create friendship
                var friendshipData = new Dictionary<string, object>
                {
                    { "users", new List<string> { fromUserId, toUserId } },
                    { "createdAt", FieldValue.ServerTimestamp }
                };

                await _db.Collection("friendships").AddAsync(friendshipData);

                return (true, "Đã chấp nhận lời mời kết bạn!");
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
        /// Get list of friends
        /// </summary>
        public async Task<List<Dictionary<string, object>>> GetFriends(string userId)
        {
            try
            {
                var friends = new List<Dictionary<string, object>>();

                var query = _db.Collection("friendships")
                    .WhereArrayContains("users", userId);

                var snapshot = await query.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    var users = doc.GetValue<List<object>>("users");
                    
                    // Get the other user's ID
                    string friendId = users.First(u => u.ToString() != userId).ToString()!;

                    // Get friend's info
                    var friendDoc = await _db.Collection("users").Document(friendId).GetSnapshotAsync();
                    
                    if (friendDoc.Exists)
                    {
                        var friendData = friendDoc.ToDictionary();
                        friendData["userId"] = friendId;
                        friendData["friendshipId"] = doc.Id;
                        friends.Add(friendData);
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
