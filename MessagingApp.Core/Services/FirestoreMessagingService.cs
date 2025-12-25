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

                // Avoid composite-index requirements by not ordering server-side.
                // We'll sort client-side using lastMessageAt/createdAt when available.
                var query = _db.Collection("conversations")
                    .WhereArrayContains("participants", userId);

                var snapshot = await query.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    var convData = doc.ToDictionary();
                    convData["conversationId"] = doc.Id;

                    // Per-user settings (pinned / muted / hidden)
                    try
                    {
                        if (doc.ContainsField("userSettings"))
                        {
                            var settingsRoot = doc.GetValue<Dictionary<string, object>>("userSettings");
                            if (settingsRoot != null
                                && settingsRoot.TryGetValue(userId, out var settingsObj)
                                && settingsObj is Dictionary<string, object> settings)
                            {
                                bool pinned = settings.TryGetValue("pinned", out var p) && p is bool pb && pb;
                                bool muted = settings.TryGetValue("muted", out var m) && m is bool mb && mb;
                                bool hidden = settings.TryGetValue("hidden", out var h) && h is bool hb && hb;

                                Timestamp? clearedAt = null;
                                try
                                {
                                    if (settings.TryGetValue("clearedAt", out var c) && c is Timestamp ts)
                                    {
                                        clearedAt = ts;
                                    }
                                }
                                catch { }

                                convData["userPinned"] = pinned;
                                convData["userMuted"] = muted;
                                convData["userHidden"] = hidden;
                                if (clearedAt != null)
                                {
                                    convData["userClearedAt"] = clearedAt;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // ignore settings parsing errors
                    }

                    // Always include participants (for group rendering)
                    List<string> participantsStr;
                    try
                    {
                        var participants = doc.GetValue<List<object>>("participants");
                        participantsStr = participants.Select(p => p?.ToString() ?? string.Empty)
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Distinct(StringComparer.Ordinal)
                            .ToList();
                    }
                    catch
                    {
                        participantsStr = new List<string>();
                    }
                    convData["participants"] = participantsStr;

                    bool isGroup = false;
                    try
                    {
                        if (doc.ContainsField("isGroup"))
                        {
                            isGroup = doc.GetValue<bool>("isGroup");
                        }
                        else if (participantsStr.Count > 2)
                        {
                            isGroup = true;
                        }
                    }
                    catch { }

                    if (isGroup)
                    {
                        convData["isGroup"] = true;
                        convData["groupName"] = doc.ContainsField("groupName") ? doc.GetValue<string>("groupName") : "Nhóm chat";
                        if (doc.ContainsField("groupAvatarDataUrl"))
                        {
                            convData["groupAvatarDataUrl"] = doc.GetValue<string>("groupAvatarDataUrl");
                        }
                        convData["memberCount"] = participantsStr.Count;
                        conversations.Add(convData);
                        continue;
                    }
                    convData["isGroup"] = false;

                    // Get other participant's info
                    if (participantsStr.Count >= 2)
                    {
                        string otherUserId = participantsStr.FirstOrDefault(p => !string.Equals(p, userId, StringComparison.Ordinal)) ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(otherUserId))
                        {
                            var userDoc = await _db.Collection("users").Document(otherUserId).GetSnapshotAsync();
                            if (userDoc.Exists)
                            {
                                convData["otherUserName"] = userDoc.ContainsField("fullName") ? userDoc.GetValue<string>("fullName") : string.Empty;
                                convData["otherUsername"] = userDoc.ContainsField("username") ? userDoc.GetValue<string>("username") : string.Empty;
                                convData["otherUserStatus"] = userDoc.ContainsField("status") ? userDoc.GetValue<string>("status") : "offline";
                                convData["otherUserId"] = otherUserId;
                            }
                        }
                    }

                    conversations.Add(convData);
                }

                // Client-side sort (desc) by lastMessageAt, then createdAt.
                // Missing timestamps sort to the end.
                static Timestamp? TryGetTs(Dictionary<string, object> d, string key)
                {
                    if (!d.TryGetValue(key, out var v) || v == null) return null;
                    if (v is Timestamp ts) return ts;
                    return null;
                }

                return conversations
                    .OrderByDescending(d => TryGetTs(d, "lastMessageAt") ?? TryGetTs(d, "createdAt") ?? Timestamp.FromDateTime(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)))
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting conversations: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        /// <summary>
        /// Update per-user conversation settings. This is used for pin/mute/hide without affecting other participants.
        /// </summary>
        public async Task UpdateConversationUserSettings(
            string conversationId,
            string userId,
            bool? pinned = null,
            bool? muted = null,
            bool? hidden = null,
            bool clearHistory = false)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentException("conversationId must not be empty.");
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId must not be empty.");

            var updates = new Dictionary<string, object>();

            string prefix = $"userSettings.{userId}.";
            if (pinned.HasValue) updates[prefix + "pinned"] = pinned.Value;
            if (muted.HasValue) updates[prefix + "muted"] = muted.Value;
            if (hidden.HasValue) updates[prefix + "hidden"] = hidden.Value;
            if (clearHistory) updates[prefix + "clearedAt"] = FieldValue.ServerTimestamp;
            updates[prefix + "updatedAt"] = FieldValue.ServerTimestamp;

            await _db.Collection("conversations").Document(conversationId).UpdateAsync(updates);
        }

        /// <summary>
        /// Create a group conversation.
        /// The conversation document is stored in "conversations" and messages reference it by conversationId.
        /// </summary>
        public async Task<string> CreateGroupConversation(
            string creatorUserId,
            string groupName,
            List<string> participantUserIds,
            string? groupAvatarDataUrl = null)
        {
            if (string.IsNullOrWhiteSpace(creatorUserId))
                throw new ArgumentException("creatorUserId must not be empty.");

            groupName = (groupName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(groupName))
                throw new ArgumentException("groupName must not be empty.");

            participantUserIds ??= new List<string>();
            var participants = participantUserIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Append(creatorUserId)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (participants.Count < 3)
                throw new ArgumentException("A group must have at least 3 participants (including creator).");

            string conversationId = Guid.NewGuid().ToString("N");
            var convRef = _db.Collection("conversations").Document(conversationId);

            var conversationData = new Dictionary<string, object>
            {
                { "isGroup", true },
                { "groupName", groupName },
                { "participants", participants },
                { "createdBy", creatorUserId },
                { "createdAt", FieldValue.ServerTimestamp },
                { "lastMessageAt", FieldValue.ServerTimestamp },
                { "lastMessage", string.Empty }
            };

            if (!string.IsNullOrWhiteSpace(groupAvatarDataUrl))
            {
                conversationData["groupAvatarDataUrl"] = groupAvatarDataUrl;
            }

            await convRef.SetAsync(conversationData, SetOptions.MergeAll);
            return conversationId;
        }

        /// <summary>
        /// Send a message
        /// </summary>
        public async Task<(bool success, string message)> SendMessage(string conversationId, string senderId, string content)
        {
            return await SendMessage(conversationId, senderId, content, "text");
        }

        /// <summary>
        /// Send a message with type (text/link/image). Content is stored in the same "content" field.
        /// </summary>
        public async Task<(bool success, string message)> SendMessage(string conversationId, string senderId, string content, string type)
        {
            return await SendMessage(conversationId, senderId, content, type, extraFields: null);
        }

        /// <summary>
        /// Send a message with type and optional extra fields (e.g., fileName, size).
        /// </summary>
        public async Task<(bool success, string message)> SendMessage(
            string conversationId,
            string senderId,
            string content,
            string type,
            Dictionary<string, object>? extraFields)
        {
            try
            {
                type = string.IsNullOrWhiteSpace(type) ? "text" : type.Trim().ToLowerInvariant();

                var messageData = new Dictionary<string, object>
                {
                    { "conversationId", conversationId },
                    { "senderId", senderId },
                    { "content", content },
                    { "type", type },
                    { "timestamp", Timestamp.GetCurrentTimestamp() },
                    { "read", false }
                };

                if (extraFields != null)
                {
                    foreach (var kv in extraFields)
                    {
                        // Avoid overriding core fields
                        if (string.Equals(kv.Key, "conversationId", StringComparison.OrdinalIgnoreCase)) continue;
                        if (string.Equals(kv.Key, "senderId", StringComparison.OrdinalIgnoreCase)) continue;
                        if (string.Equals(kv.Key, "content", StringComparison.OrdinalIgnoreCase)) continue;
                        if (string.Equals(kv.Key, "type", StringComparison.OrdinalIgnoreCase)) continue;
                        if (string.Equals(kv.Key, "timestamp", StringComparison.OrdinalIgnoreCase)) continue;
                        if (string.Equals(kv.Key, "read", StringComparison.OrdinalIgnoreCase)) continue;

                        messageData[kv.Key] = kv.Value;
                    }
                }

                await _db.Collection("messages").AddAsync(messageData);

                string lastMessagePreview = type switch
                {
                    "image" => "[Ảnh]",
                    "file" => "[File]",
                    "link" => content,
                    _ => content
                };
                if (lastMessagePreview.Length > 50)
                {
                    lastMessagePreview = lastMessagePreview.Substring(0, 50) + "...";
                }

                await _db.Collection("conversations").Document(conversationId).UpdateAsync(new Dictionary<string, object>
                {
                    { "lastMessage", lastMessagePreview },
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

                var batch = _db.StartBatch();
                int updates = 0;

                foreach (var doc in snapshot.Documents)
                {
                    string senderId = doc.GetValue<string>("senderId");
                    if (senderId == userId) continue;

                    batch.Update(doc.Reference, new Dictionary<string, object>
                    {
                        { "read", true }
                    });
                    updates++;
                }

                if (updates > 0)
                {
                    await batch.CommitAsync();
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

        /// <summary>
        /// Delete (revoke) a message by its document id. Only the sender can delete their own message.
        /// </summary>
        public async Task<(bool success, string message)> DeleteMessageAsync(string messageId, string requesterUserId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return (false, "Thiếu messageId.");
            if (string.IsNullOrWhiteSpace(requesterUserId))
                return (false, "Thiếu người dùng hiện tại.");

            try
            {
                var docRef = _db.Collection("messages").Document(messageId);
                var snapshot = await docRef.GetSnapshotAsync();
                if (!snapshot.Exists)
                {
                    // Consider it already deleted.
                    return (true, "Tin nhắn đã được thu hồi.");
                }

                string senderId = string.Empty;
                try
                {
                    if (snapshot.ContainsField("senderId"))
                    {
                        senderId = snapshot.GetValue<string>("senderId");
                    }
                }
                catch { }

                if (!string.IsNullOrWhiteSpace(senderId) && senderId != requesterUserId)
                {
                    return (false, "Bạn chỉ có thể thu hồi tin nhắn của chính mình.");
                }

                await docRef.DeleteAsync();
                return (true, "Tin nhắn đã được thu hồi.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }
    }
}
