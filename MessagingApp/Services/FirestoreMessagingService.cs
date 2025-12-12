using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessagingApp.Config;

namespace MessagingApp.Services
{
    /// <summary>
    /// Service for managing conversations and messages in Firestore
    /// </summary>
    public class FirestoreMessagingService
    {
        private static FirestoreMessagingService? _instance;
        public static FirestoreMessagingService Instance => _instance ??= new FirestoreMessagingService();

        private readonly FirestoreDb _db;

        private FirestoreMessagingService()
        {
            _db = FirebaseConfig.GetFirestoreDb();
        }

        /// <summary>
        /// Get or create a unique 1-1 conversation between two users using a canonical ID.
        /// This ensures both sides always join the same conversation and avoids duplicates.
        /// </summary>
        public async Task<string> GetOrCreateConversation(string user1Id, string user2Id)
        {
            if (string.IsNullOrEmpty(user1Id) || string.IsNullOrEmpty(user2Id))
                throw new ArgumentException("User ids must not be empty.");

            try
            {
                // Canonical deterministic conversation id (same order for both sides)
                string conversationId = GetCanonicalPairId(user1Id, user2Id);
                var convRef = _db.Collection("conversations").Document(conversationId);
                var snapshot = await convRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    var participants = new List<string> { user1Id, user2Id };
                    var conversationData = new Dictionary<string, object>
                    {
                        { "participants", participants },
                        { "createdAt", FieldValue.ServerTimestamp },
                        { "lastMessageAt", FieldValue.ServerTimestamp },
                        { "lastMessage", string.Empty }
                    };

                    await convRef.SetAsync(conversationData, SetOptions.MergeAll);
                }

                return conversationId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting/creating conversation: {ex.Message}");
                throw;
            }
        }

        private static string GetCanonicalPairId(string userId1, string userId2)
        {
            if (string.CompareOrdinal(userId1, userId2) < 0)
                return $"{userId1}_{userId2}";
            return $"{userId2}_{userId1}";
        }

        /// <summary>
        /// Get all conversations for a user
        /// </summary>
        public async Task<List<Dictionary<string, object>>> GetConversations(string userId)
        {
            try
            {
                var conversations = new List<Dictionary<string, object>>();

                var query = _db.Collection("conversations")
                    .WhereArrayContains("participants", userId)
                    .OrderByDescending("lastMessageAt");

                var snapshot = await query.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    var convData = doc.ToDictionary();
                    convData["conversationId"] = doc.Id;

                    // Get other participant's info
                    var participants = doc.GetValue<List<object>>("participants");
                    string otherUserId = participants.First(p => p.ToString() != userId).ToString()!;

                    var userDoc = await _db.Collection("users").Document(otherUserId).GetSnapshotAsync();
                    if (userDoc.Exists)
                    {
                        convData["otherUserName"] = userDoc.GetValue<string>("fullName");
                        convData["otherUsername"] = userDoc.GetValue<string>("username");
                        convData["otherUserStatus"] = userDoc.GetValue<string>("status");
                        convData["otherUserId"] = otherUserId;
                    }

                    conversations.Add(convData);
                }

                return conversations;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting conversations: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        /// <summary>
        /// Send a message
        /// </summary>
        public async Task<(bool success, string message)> SendMessage(string conversationId, string senderId, string content)
        {
            try
            {
                var messageData = new Dictionary<string, object>
                {
                    { "conversationId", conversationId },
                    { "senderId", senderId },
                    { "content", content },
                    { "timestamp", Timestamp.GetCurrentTimestamp() },
                    { "read", false }
                };

                await _db.Collection("messages").AddAsync(messageData);

                // Update conversation's last message
                await _db.Collection("conversations").Document(conversationId).UpdateAsync(new Dictionary<string, object>
                {
                    { "lastMessage", content.Length > 50 ? content.Substring(0, 50) + "..." : content },
                    { "lastMessageAt", FieldValue.ServerTimestamp }
                });

                return (true, "Tin nhắn đã được gửi!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Send a file message (after file uploaded to Storage)
        /// </summary>
        public async Task<(bool success, string message)> SendFileMessage(string conversationId, string senderId, string fileName, string fileUrl, long fileSize, string contentType)
        {
            try
            {
                var displayText = $"[Tệp] {fileName}";
                var messageData = new Dictionary<string, object>
                {
                    { "conversationId", conversationId },
                    { "senderId", senderId },
                    { "content", displayText },
                    { "timestamp", FieldValue.ServerTimestamp },
                    { "read", false },
                    { "messageType", "file" },
                    { "fileName", fileName },
                    { "fileUrl", fileUrl },
                    { "fileSize", fileSize },
                    { "contentType", contentType }
                };

                await _db.Collection("messages").AddAsync(messageData);

                await _db.Collection("conversations").Document(conversationId).UpdateAsync(new Dictionary<string, object>
                {
                    { "lastMessage", displayText },
                    { "lastMessageAt", FieldValue.ServerTimestamp }
                });

                return (true, "Tệp đã được gửi!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi gửi tệp: {ex.Message}");
            }
        }

        /// <summary>
        /// Get messages in a conversation
        /// </summary>
        public async Task<List<Dictionary<string, object>>> GetMessages(string conversationId, int limit = 200)
        {
            try
            {
                var messages = new List<Dictionary<string, object>>();

                var query = _db.Collection("messages")
                    .WhereEqualTo("conversationId", conversationId)
                    .Limit(limit);

                var snapshot = await query.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    var msgData = doc.ToDictionary();
                    msgData["messageId"] = doc.Id;
                    messages.Add(msgData);
                }

                return messages;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting messages: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        /// <summary>
        /// Get older messages before a timestamp (for pagination / scroll-back). Returns ascending order.
        /// Note: This uses an orderBy on timestamp and may require a composite index with conversationId.
        /// </summary>
        public async Task<List<Dictionary<string, object>>> GetMessagesBefore(string conversationId, Timestamp before, int limit = 50)
        {
            try
            {
                var messages = new List<Dictionary<string, object>>();

                var query = _db.Collection("messages")
                    .WhereEqualTo("conversationId", conversationId)
                    .WhereLessThan("timestamp", before)
                    .OrderBy("timestamp")
                    .Limit(limit);

                var snapshot = await query.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    var msgData = doc.ToDictionary();
                    msgData["messageId"] = doc.Id;
                    messages.Add(msgData);
                }

                return messages;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting older messages: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        /// <summary>
        /// Mark messages as read
        /// </summary>
        public async Task MarkMessagesAsRead(string conversationId, string userId)
        {
            try
            {
                var query = _db.Collection("messages")
                    .WhereEqualTo("conversationId", conversationId)
                    .WhereEqualTo("read", false);

                var snapshot = await query.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    string senderId = doc.GetValue<string>("senderId");
                    if (senderId != userId)
                    {
                        await doc.Reference.UpdateAsync(new Dictionary<string, object>
                        {
                            { "read", true }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking messages as read: {ex.Message}");
            }
        }

        /// <summary>
        /// Listen to new messages in a conversation (real-time)
        /// </summary>
        public FirestoreChangeListener ListenToMessages(string conversationId, Action<List<Dictionary<string, object>>> onMessagesChanged)
        {
            var query = _db.Collection("messages")
                .WhereEqualTo("conversationId", conversationId)
                .Limit(200);

            return query.Listen(snapshot =>
            {
                var messages = new List<Dictionary<string, object>>();
                foreach (var doc in snapshot.Documents)
                {
                    var msgData = doc.ToDictionary();
                    msgData["messageId"] = doc.Id;
                    messages.Add(msgData);
                }
                messages.Reverse();
                onMessagesChanged(messages);
            });
        }

        /// <summary>
        /// Listen to conversation changes for a user (real-time)
        /// </summary>
        public FirestoreChangeListener ListenToConversations(string userId, Action onChanged)
        {
            var query = _db.Collection("conversations")
                .WhereArrayContains("participants", userId);

            return query.Listen(_ =>
            {
                try { onChanged(); } catch { }
            });
        }

        /// <summary>
        /// Get unread message count for user
        /// </summary>
        public async Task<int> GetUnreadMessageCount(string userId)
        {
            try
            {
                // Get all conversations for user
                var conversations = await GetConversations(userId);
                int unreadCount = 0;

                foreach (var conv in conversations)
                {
                    string conversationId = conv["conversationId"].ToString()!;

                    var query = _db.Collection("messages")
                        .WhereEqualTo("conversationId", conversationId)
                        .WhereEqualTo("read", false);

                    var snapshot = await query.GetSnapshotAsync();

                    foreach (var doc in snapshot.Documents)
                    {
                        string senderId = doc.GetValue<string>("senderId");
                        if (senderId != userId)
                        {
                            unreadCount++;
                        }
                    }
                }

                return unreadCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get unread message count for a specific conversation (excluding current user's own messages)
        /// </summary>
        public async Task<int> GetUnreadCountForConversation(string conversationId, string userId)
        {
            try
            {
                var query = _db.Collection("messages")
                    .WhereEqualTo("conversationId", conversationId)
                    .WhereEqualTo("read", false);

                var snapshot = await query.GetSnapshotAsync();
                int count = 0;
                foreach (var doc in snapshot.Documents)
                {
                    string senderId = doc.GetValue<string>("senderId");
                    if (senderId != userId)
                    {
                        count++;
                    }
                }
                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread count for conversation: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Set typing state for a user within a conversation.
        /// </summary>
        public async Task SetTypingAsync(string conversationId, string userId, bool isTyping)
        {
            try
            {
                var docRef = _db.Collection("conversations").Document(conversationId)
                    .Collection("typing").Document(userId);
                await docRef.SetAsync(new Dictionary<string, object>
                {
                    { "typing", isTyping },
                    { "updatedAt", FieldValue.ServerTimestamp }
                }, SetOptions.MergeAll);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting typing state: {ex.Message}");
            }
        }

        /// <summary>
        /// Listen to typing state of the other user in a conversation.
        /// </summary>
        public FirestoreChangeListener ListenToTyping(string conversationId, string otherUserId, Action<bool> onTypingChanged)
        {
            var docRef = _db.Collection("conversations").Document(conversationId)
                .Collection("typing").Document(otherUserId);
            return docRef.Listen(snapshot =>
            {
                bool isTyping = false;
                try
                {
                    if (snapshot.Exists && snapshot.ContainsField("typing"))
                    {
                        isTyping = snapshot.GetValue<bool>("typing");
                    }
                }
                catch { }
                onTypingChanged(isTyping);
            });
        }
    }
}
