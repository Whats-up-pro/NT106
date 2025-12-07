using MessagingApp.Services;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MessagingApp.Forms.Messaging
{
    public partial class MessageForm : Form
    {
        private readonly ThemeService _theme = ThemeService.Instance;
        private readonly FirestoreMessagingService _messagingService = FirestoreMessagingService.Instance;
        private readonly FirebaseAuthService _authService = FirebaseAuthService.Instance;

        private readonly string _conversationId;
        private readonly Dictionary<string, object> _friendData;
        private FirestoreChangeListener? _messageListener;
        private readonly List<Dictionary<string, object>> _cachedMessages = new();
        private readonly HashSet<string> _renderedMessageKeys = new();
        private DateTime _lastRenderedLatestTs = DateTime.MinValue;
        private DateTime _oldestTimestamp = DateTime.MaxValue;
        private bool _isLoadingMore = false;
        private bool _hasMore = true;
        private const int PageSize = 100;

        private Panel pnlHeader = null!;
        private Panel pnlContent = null!;
        private Label lblFriendName = null!;
        private Label lblFriendStatus = null!;
        private RichTextBox rtbMessages = null!;
        private TextBox txtMessage = null!;
        private Button btnSend = null!;
        private System.Windows.Forms.Timer _typingTimer = null!;
        private System.Windows.Forms.Timer _refreshTimer = null!;
        private FirestoreChangeListener? _typingListener;
        private readonly string _otherUserId;
        private readonly string _displayName;
        private string _baseStatusText = "";
        private bool _isLoadingMessages = false;

        public MessageForm(string conversationId, Dictionary<string, object> friendData)
        {
            _conversationId = conversationId;
            _friendData = friendData;
            _otherUserId = friendData.ContainsKey("userId") ? friendData["userId"].ToString()! : string.Empty;
            _displayName = BuildDisplayName();

            InitializeComponent();
            InitializeCustomUI();
            ApplyTheme();
            LoadMessages();
            StartMessageListener();
            InitTyping();
            StartTypingListener();
            StartRefreshTimer();

            _theme.OnThemeChanged += OnThemeChanged;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Name = "MessageForm";
            this.Text = "Tin Nhắn - Messaging App";
            this.StartPosition = FormStartPosition.CenterScreen;

            this.ResumeLayout(false);
        }

        private void InitializeCustomUI()
        {
            // Header panel
            pnlHeader = new Panel
            {
                Height = 80,
                Dock = DockStyle.Top,
                Padding = new Padding(20, 15, 20, 15)
            };
            this.Controls.Add(pnlHeader);

            // Friend name
            string friendName = _displayName;

            lblFriendName = new Label
            {
                Text = friendName,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 5)
            };
            pnlHeader.Controls.Add(lblFriendName);

            // Friend status
            string status = _friendData.ContainsKey("status") ? _friendData["status"].ToString()! : "offline";
            string statusText = status == "online" ? "🟢 Đang hoạt động" : "⚫ Không hoạt động";
            _baseStatusText = statusText;

            lblFriendStatus = new Label
            {
                Text = statusText,
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(0, 40)
            };
            pnlHeader.Controls.Add(lblFriendStatus);

            // Content panel between header and input to avoid overlap
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 10, 20, 10)
            };
            this.Controls.Add(pnlContent);

            // Messages display
            rtbMessages = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Segoe UI", 12F), // chữ to hơn
                BorderStyle = BorderStyle.None,
                Padding = new Padding(10),
                Margin = new Padding(0),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            rtbMessages.VScroll += RtbMessages_VScroll;
            pnlContent.Controls.Add(rtbMessages);

            // Input panel
            Panel pnlInput = new Panel
            {
                Height = 70,
                Dock = DockStyle.Bottom,
                Padding = new Padding(20, 15, 20, 15)
            };
            this.Controls.Add(pnlInput);

            // Message textbox
            txtMessage = new TextBox
            {
                Width = 630,
                Height = 40,
                Location = new Point(0, 5),
                Font = new Font("Segoe UI", 11F),
                PlaceholderText = "Nhập tin nhắn...",
                Multiline = false
            };
            txtMessage.KeyPress += TxtMessage_KeyPress;
            pnlInput.Controls.Add(txtMessage);

            // Send button
            btnSend = new Button
            {
                Text = "📤 Gửi",
                Width = 100,
                Height = 40,
                Location = new Point(640, 5),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSend.Click += BtnSend_Click;
            pnlInput.Controls.Add(btnSend);
        }

        private string BuildDisplayName()
        {
            string fullName = GetValueOrEmpty("fullName");
            string username = GetValueOrEmpty("username");
            string email = GetValueOrEmpty("email");

            if (!string.IsNullOrWhiteSpace(fullName)) return fullName;
            if (!string.IsNullOrWhiteSpace(username) && !IsLikelyUid(username, _otherUserId)) return username;
            if (!string.IsNullOrWhiteSpace(email)) return email;

            if (!string.IsNullOrWhiteSpace(_otherUserId))
            {
                int take = Math.Min(6, _otherUserId.Length);
                return $"Người dùng {_otherUserId.Substring(0, take)}";
            }

            return "Người dùng";
        }

        private string GetValueOrEmpty(string key)
        {
            return _friendData.ContainsKey(key) && _friendData[key] != null
                ? _friendData[key].ToString()!
                : string.Empty;
        }

        private static bool IsLikelyUid(string candidate, string userId)
        {
            if (string.IsNullOrWhiteSpace(candidate)) return false;
            return string.Equals(candidate, userId, StringComparison.OrdinalIgnoreCase) || candidate.Length >= 24;
        }

        private static DateTime ExtractTimestamp(Dictionary<string, object> msg)
        {
            try
            {
                if (msg.ContainsKey("timestamp") && msg["timestamp"] is Google.Cloud.Firestore.Timestamp ts)
                {
                    return ts.ToDateTime().ToLocalTime();
                }
            }
            catch { }
            return DateTime.MinValue;
        }

        private static string GetMessageKey(Dictionary<string, object> msg)
        {
            if (msg.ContainsKey("messageId") && msg["messageId"] != null)
            {
                return msg["messageId"].ToString()!;
            }

            // Fallback key for optimistic local messages without id yet
            string senderId = msg.ContainsKey("senderId") && msg["senderId"] != null ? msg["senderId"].ToString()! : "";
            string content = msg.ContainsKey("content") && msg["content"] != null ? msg["content"].ToString()! : "";
            var ts = ExtractTimestamp(msg);
            string tsKey = ts == DateTime.MinValue ? "notime" : ts.Ticks.ToString();
            return $"{senderId}|{tsKey}|{content}";
        }

        private void AppendMessageBubble(bool isCurrentUser, string content, string timeText)
        {
            var prefix = isCurrentUser ? "Bạn: " : "Đối phương: ";

            if (isCurrentUser)
            {
                rtbMessages.SelectionAlignment = HorizontalAlignment.Right;
                rtbMessages.SelectionColor = _theme.TextPrimary;
                rtbMessages.SelectionBackColor = _theme.Primary;
                rtbMessages.AppendText($"{prefix}{content}");
                rtbMessages.SelectionBackColor = _theme.Background;
                rtbMessages.SelectionColor = _theme.TextSecondary;
                rtbMessages.AppendText($" {timeText}\n\n");
            }
            else
            {
                rtbMessages.SelectionAlignment = HorizontalAlignment.Left;
                rtbMessages.SelectionColor = _theme.TextPrimary;
                rtbMessages.SelectionBackColor = _theme.Surface;
                rtbMessages.AppendText($"{prefix}{content}");
                rtbMessages.SelectionBackColor = _theme.Background;
                rtbMessages.SelectionColor = _theme.TextSecondary;
                rtbMessages.AppendText($" {timeText}\n\n");
            }
        }

        private void UpdateOldestTimestamp(List<Dictionary<string, object>> messages)
        {
            if (messages.Count == 0) return;
            var first = messages.Min(m => ExtractTimestamp(m));
            if (first > DateTime.MinValue)
            {
                _oldestTimestamp = first;
            }
        }

        private List<Dictionary<string, object>> DeduplicateMessages(List<Dictionary<string, object>> source)
        {
            var order = new List<string>();
            var map = new Dictionary<string, Dictionary<string, object>>();

            foreach (var msg in source)
            {
                if (!msg.ContainsKey("senderId") || !msg.ContainsKey("content")) continue;
                string senderId = msg["senderId"].ToString()!;
                string content = msg["content"].ToString()!;
                var ts = ExtractTimestamp(msg);

                // Prefer a stable key: messageId if present; else sender + rounded timestamp + content
                string key = msg.ContainsKey("messageId") && msg["messageId"] != null
                    ? msg["messageId"].ToString()!
                    : BuildMergeKey(senderId, content, ts);

                bool hasId = msg.ContainsKey("messageId") && msg["messageId"] != null;
                bool isLocal = msg.ContainsKey("isLocal") && msg["isLocal"] is bool b && b;

                if (!map.ContainsKey(key))
                {
                    map[key] = msg;
                    order.Add(key);
                }
                else
                {
                    var existing = map[key];
                    bool existingHasId = existing.ContainsKey("messageId") && existing["messageId"] != null;
                    bool existingIsLocal = existing.ContainsKey("isLocal") && existing["isLocal"] is bool b2 && b2;

                    // Prefer message with real id; if both have id, keep the first
                    if (!existingHasId && hasId)
                    {
                        map[key] = msg;
                    }
                    else if (existingIsLocal && !isLocal)
                    {
                        map[key] = msg;
                    }
                }
            }

            var result = new List<Dictionary<string, object>>();
            foreach (var k in order)
            {
                result.Add(map[k]);
            }
            return result;
        }

        private static string BuildMergeKey(string senderId, string content, DateTime ts)
        {
            long bucket = ts == DateTime.MinValue ? 0 : (ts.Ticks / TimeSpan.FromSeconds(5).Ticks); // 5s buckets to merge optimistic + server
            return $"{senderId}|{bucket}|{content}";
        }

        private void InitTyping()
        {
            _typingTimer = new System.Windows.Forms.Timer();
            _typingTimer.Interval = 1500; // 1.5s idle to stop typing
            _typingTimer.Tick += async (s, e) =>
            {
                _typingTimer.Stop();
                try
                {
                    string? currentUserId = _authService.CurrentUserId;
                    if (currentUserId != null)
                        await _messagingService.SetTypingAsync(_conversationId, currentUserId, false);
                }
                catch { }
            };

            txtMessage.TextChanged += async (s, e) =>
            {
                try
                {
                    string? currentUserId = _authService.CurrentUserId;
                    if (currentUserId != null)
                        await _messagingService.SetTypingAsync(_conversationId, currentUserId, true);
                }
                catch { }
                _typingTimer.Stop();
                _typingTimer.Start();
            };
        }

        private void StartTypingListener()
        {
            if (string.IsNullOrEmpty(_otherUserId)) return;
            _typingListener = _messagingService.ListenToTyping(_conversationId, _otherUserId, isTyping =>
            {
                if (this.IsHandleCreated)
                {
                    try
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            lblFriendStatus.Text = isTyping ? "✍️ Đang nhập..." : _baseStatusText;
                        }));
                    }
                    catch { }
                }
            });
        }

        private void RtbMessages_VScroll(object? sender, EventArgs e)
        {
            if (_isLoadingMore || !_hasMore || _cachedMessages.Count == 0) return;

            // If the top visible char is near the start, load older messages
            int topIndex = rtbMessages.GetCharIndexFromPosition(new Point(1, 1));
            if (topIndex <= 3)
            {
                _ = LoadOlderMessages();
            }
        }

        private void ApplyTheme()
        {
            this.BackColor = _theme.Background;

            pnlHeader.BackColor = _theme.Surface;
            pnlContent.BackColor = _theme.Background;
            lblFriendName.ForeColor = _theme.TextPrimary;
            
            string status = _friendData.ContainsKey("status") ? _friendData["status"].ToString()! : "offline";
            lblFriendStatus.ForeColor = status == "online" ? _theme.Success : _theme.TextSecondary;

            rtbMessages.BackColor = _theme.Background;
            rtbMessages.ForeColor = _theme.TextPrimary;
            rtbMessages.BorderStyle = BorderStyle.None;
            rtbMessages.Padding = new Padding(10, 5, 10, 5);

            _theme.StyleTextBox(txtMessage);
            _theme.StyleButton(btnSend, isPrimary: true);
        }

        private void OnThemeChanged(ThemeService.ThemeMode newTheme)
        {
            ApplyTheme();
        }

        private async void LoadMessages()
        {
            try
            {
                if (_isLoadingMessages) return;
                _isLoadingMessages = true;

                var messages = await _messagingService.GetMessages(_conversationId);
                if (messages.Count > 0)
                {
                    _cachedMessages.Clear();
                    _cachedMessages.AddRange(messages);
                    UpdateOldestTimestamp(_cachedMessages);
                    DisplayMessages(_cachedMessages, fullRedraw: true);
                }

                string? currentUserId = _authService.CurrentUserId;
                if (currentUserId != null)
                {
                    await _messagingService.MarkMessagesAsRead(_conversationId, currentUserId);
                }
            }
            catch
            {
                // Đơn giản hóa: không popup lỗi, chỉ bỏ qua
            }
            finally
            {
                _isLoadingMessages = false;
            }
        }

        private void StartMessageListener()
        {
            _messageListener = _messagingService.ListenToMessages(_conversationId, messages =>
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        if (messages.Count == 0) return; // tránh xóa trắng nếu listener trả về rỗng
                        _cachedMessages.Clear();
                        _cachedMessages.AddRange(messages);
                        UpdateOldestTimestamp(_cachedMessages);
                        DisplayMessages(_cachedMessages, fullRedraw: false);
                    }));
                }
                else
                {
                    if (messages.Count == 0) return;
                    _cachedMessages.Clear();
                    _cachedMessages.AddRange(messages);
                    UpdateOldestTimestamp(_cachedMessages);
                    DisplayMessages(_cachedMessages, fullRedraw: false);
                }
            });
        }

        private void StartRefreshTimer()
        {
            _refreshTimer = new System.Windows.Forms.Timer();
            _refreshTimer.Interval = 4000; // 4s để bắt lại tin nếu listener lỡ
            _refreshTimer.Tick += async (s, e) =>
            {
                try
                {
                    await LoadMessagesSafe();
                }
                catch { }
            };
            _refreshTimer.Start();
        }

        private async Task LoadMessagesSafe()
        {
            if (_isLoadingMessages) return;
            try
            {
                _isLoadingMessages = true;
                var messages = await _messagingService.GetMessages(_conversationId);
                if (messages.Count == 0) return;

                _cachedMessages.Clear();
                _cachedMessages.AddRange(messages);
                UpdateOldestTimestamp(_cachedMessages);
                DisplayMessages(_cachedMessages, fullRedraw: false);
            }
            catch { }
            finally
            {
                _isLoadingMessages = false;
            }
        }

        private async Task LoadOlderMessages()
        {
            if (_isLoadingMore || !_hasMore) return;
            if (_oldestTimestamp == DateTime.MaxValue || _oldestTimestamp == DateTime.MinValue) return;

            try
            {
                _isLoadingMore = true;
                var ts = Google.Cloud.Firestore.Timestamp.FromDateTime(_oldestTimestamp.ToUniversalTime());
                var older = await _messagingService.GetMessagesBefore(_conversationId, ts, PageSize);
                if (older.Count == 0)
                {
                    _hasMore = false;
                    return;
                }

                // Keep cached list ordered by time
                _cachedMessages.InsertRange(0, older);
                _cachedMessages.Sort((a, b) => DateTime.Compare(ExtractTimestamp(a), ExtractTimestamp(b)));
                UpdateOldestTimestamp(_cachedMessages);
                DisplayMessages(_cachedMessages, fullRedraw: true);
            }
            catch
            {
                // If index is missing, Firestore may throw; we simply stop trying to load more
                _hasMore = false;
            }
            finally
            {
                _isLoadingMore = false;
            }
        }

        private void DisplayMessages(List<Dictionary<string, object>> messages, bool fullRedraw)
        {
            string? currentUserId = _authService.CurrentUserId;

            // Sort by timestamp ascending to avoid out-of-order display (backend now returns unsorted to avoid needing an index)
            messages.Sort((a, b) =>
            {
                var ta = ExtractTimestamp(a);
                var tb = ExtractTimestamp(b);
                return DateTime.Compare(ta, tb);
            });

            // Deduplicate optimistic + server messages (prefer messages with messageId)
            var cleaned = DeduplicateMessages(messages);
            messages = cleaned;
            _cachedMessages.Clear();
            _cachedMessages.AddRange(cleaned);
            bool firstRender = _renderedMessageKeys.Count == 0;

            if (fullRedraw)
            {
                _renderedMessageKeys.Clear();
                rtbMessages.Clear();
                firstRender = true;
            }

            var toAppend = new List<Dictionary<string, object>>();
            foreach (var msg in messages)
            {
                var key = GetMessageKey(msg);
                if (string.IsNullOrEmpty(key)) continue;
                if (_renderedMessageKeys.Contains(key) && !firstRender)
                {
                    continue; // already drawn
                }
                toAppend.Add(msg);
            }

            if (toAppend.Count == 0) return;

            foreach (var msg in toAppend)
            {
                if (!msg.ContainsKey("senderId") || !msg.ContainsKey("content"))
                {
                    continue;
                }

                string senderId = msg["senderId"].ToString()!;
                string content = msg["content"].ToString()!;
                bool isCurrentUser = senderId == currentUserId;

                string timeText = "";
                try
                {
                    var dt = ExtractTimestamp(msg);
                    if (dt > DateTime.MinValue)
                    {
                        timeText = dt.ToString("HH:mm");
                        if (dt > _lastRenderedLatestTs) _lastRenderedLatestTs = dt;
                    }
                }
                catch { }

                AppendMessageBubble(isCurrentUser, content, timeText);

                var key = GetMessageKey(msg);
                if (!string.IsNullOrEmpty(key))
                {
                    _renderedMessageKeys.Add(key);
                }
            }

            rtbMessages.SelectionStart = rtbMessages.Text.Length;
            rtbMessages.ScrollToCaret();
        }

        private void TxtMessage_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                BtnSend_Click(sender, EventArgs.Empty);
            }
        }

        private async void BtnSend_Click(object? sender, EventArgs e)
        {
            string content = txtMessage.Text.Trim();

            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            try
            {
                string? currentUserId = _authService.CurrentUserId;
                if (currentUserId == null)
                {
                    MessageBox.Show("Vui lòng đăng nhập lại!", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                btnSend.Enabled = false;
                txtMessage.Enabled = false;

                var timeText = DateTime.Now.ToString("HH:mm");
                var (success, message) = await _messagingService.SendMessage(_conversationId, currentUserId, content);

                if (success)
                {
                    var localMsg = new Dictionary<string, object>
                    {
                        { "senderId", currentUserId },
                        { "content", content },
                        { "timestamp", Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow) },
                        { "isLocal", true }
                    };
                    _cachedMessages.Add(localMsg);
                    DisplayMessages(_cachedMessages, fullRedraw: false);
                    try
                    {
                        _typingTimer.Stop();
                        await _messagingService.SetTypingAsync(_conversationId, currentUserId, false);
                    }
                    catch { }
                }
                else
                {
                    MessageBox.Show(message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                txtMessage.Clear();
                btnSend.Enabled = true;
                txtMessage.Enabled = true;
                txtMessage.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi gửi tin nhắn: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                btnSend.Enabled = true;
                txtMessage.Enabled = true;
            }
        }

        private void AppendOwnMessage(string content, string timeText)
        {
            if (string.IsNullOrWhiteSpace(content)) return;
            rtbMessages.SelectionAlignment = HorizontalAlignment.Right;
            rtbMessages.SelectionColor = _theme.TextPrimary;
            rtbMessages.SelectionBackColor = _theme.Primary;
            rtbMessages.AppendText(content);
            rtbMessages.SelectionBackColor = _theme.Background;
            rtbMessages.SelectionColor = _theme.TextSecondary;
            rtbMessages.AppendText($" {timeText}\n\n");
            rtbMessages.SelectionStart = rtbMessages.Text.Length;
            rtbMessages.ScrollToCaret();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _messageListener?.StopAsync();
                try { _typingListener?.StopAsync(); } catch { }
                try { _refreshTimer?.Stop(); } catch { }
                try
                {
                    string? currentUserId = _authService.CurrentUserId;
                    if (currentUserId != null)
                    {
                        // Fire and forget to best-effort clear typing
                        _ = _messagingService.SetTypingAsync(_conversationId, currentUserId, false);
                    }
                }
                catch { }
                _theme.OnThemeChanged -= OnThemeChanged;
            }
            base.Dispose(disposing);
        }
    }
}
