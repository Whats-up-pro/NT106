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
        /// Get or create conversation between two users
        /// </summary>
        public async Task<string> GetOrCreateConversation(string user1Id, string user2Id)
        {
            try
            {
                // Check if conversation exists
                var query = _db.Collection("conversations")
                    .WhereArrayContains("participants", user1Id);

                var snapshot = await query.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    var participants = doc.GetValue<List<object>>("participants");
                    if (participants.Contains(user2Id))
                    {
                        return doc.Id;
                    }
                }

                // Create new conversation
                var conversationData = new Dictionary<string, object>
                {
                    { "participants", new List<string> { user1Id, user2Id } },
                    { "createdAt", FieldValue.ServerTimestamp },
                    { "lastMessageAt", FieldValue.ServerTimestamp },
                    { "lastMessage", "" }
                };

                var newDoc = await _db.Collection("conversations").AddAsync(conversationData);
                return newDoc.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting/creating conversation: {ex.Message}");
                throw;
            }
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
                    { "timestamp", FieldValue.ServerTimestamp },
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
        public async Task<List<Dictionary<string, object>>> GetMessages(string conversationId, int limit = 50)
        {
            try
            {
                var messages = new List<Dictionary<string, object>>();

                var query = _db.Collection("messages")
                    .WhereEqualTo("conversationId", conversationId)
                    .OrderByDescending("timestamp")
                    .Limit(limit);

                var snapshot = await query.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    var msgData = doc.ToDictionary();
                    msgData["messageId"] = doc.Id;
                    messages.Add(msgData);
                }

                // Reverse to show oldest first
                messages.Reverse();
                return messages;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting messages: {ex.Message}");
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
                .OrderByDescending("timestamp")
                .Limit(50);

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
    }
}
