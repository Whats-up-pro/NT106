using MessagingApp.Services;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MessagingApp.Forms.Messaging
{
    public partial class ConversationsForm : Form
    {
        private readonly ThemeService _theme = ThemeService.Instance;
        private readonly FirestoreMessagingService _messagingService = FirestoreMessagingService.Instance;
        private readonly FirestoreFriendsService _friendsService = FirestoreFriendsService.Instance;
        private readonly FirebaseAuthService _authService = FirebaseAuthService.Instance;

        private Panel pnlMain = null!;
        private Label lblTitle = null!;
        private ListView listViewConversations = null!;
        private Label lblLoading = null!;
        private Label lblNoConversations = null!;
        private Label lblUnreadCount = null!;
        private readonly Dictionary<string, FirestoreChangeListener> _statusListeners = new();
        private readonly Dictionary<string, ListViewItem> _itemsByUserId = new();

        public ConversationsForm()
        {
            InitializeComponent();
            InitializeCustomUI();
            ApplyTheme();
            LoadFriendsWithConversations();

            _theme.OnThemeChanged += OnThemeChanged;
            // Delay listeners to avoid exceeding quota
            _ = Task.Delay(2000).ContinueWith(_ =>
            {
                if (this.IsHandleCreated)
                {
                    try { this.BeginInvoke(new Action(StartRealtimeConversationsListener)); } catch { }
                }
            });
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 650);
            this.Name = "ConversationsForm";
            this.Text = "Tin Nhắn - Messaging App";
            this.StartPosition = FormStartPosition.CenterParent;

            this.ResumeLayout(false);
        }

        private void InitializeCustomUI()
        {
            // Main panel
            pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };
            this.Controls.Add(pnlMain);

            // Header
            Panel pnlHeader = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top
            };
            pnlMain.Controls.Add(pnlHeader);

            // Title
            lblTitle = new Label
            {
                Text = "Tin Nhắn",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 10)
            };
            pnlHeader.Controls.Add(lblTitle);

            // Unread count
            lblUnreadCount = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 11F),
                AutoSize = true,
                Location = new Point(150, 18),
                Visible = false
            };
            pnlHeader.Controls.Add(lblUnreadCount);

            // ListView for conversations
            listViewConversations = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 10F)
            };
            listViewConversations.Columns.Add("Bạn bè", 200);
            listViewConversations.Columns.Add("Tin nhắn cuối", 360);
            listViewConversations.Columns.Add("Trạng thái", 100);
            listViewConversations.Columns.Add("Thời gian", 120);
            listViewConversations.Columns.Add("Chưa đọc", 100);
            listViewConversations.DoubleClick += ListViewConversations_DoubleClick;
            pnlMain.Controls.Add(listViewConversations);

            // Loading label
            lblLoading = new Label
            {
                Text = "⏳ Đang tải tin nhắn...",
                Font = new Font("Segoe UI", 12F),
                AutoSize = true,
                Location = new Point(300, 250),
                Visible = false
            };
            pnlMain.Controls.Add(lblLoading);
            lblLoading.BringToFront();

            // No conversations label
            lblNoConversations = new Label
            {
                Text = "Chưa có cuộc trò chuyện nào.\nHãy bắt đầu trò chuyện với bạn bè!",
                Font = new Font("Segoe UI", 12F),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Width = 400,
                Height = 80,
                Location = new Point(250, 250),
                Visible = false
            };
            pnlMain.Controls.Add(lblNoConversations);
            lblNoConversations.BringToFront();
        }

        private void ApplyTheme()
        {
            this.BackColor = _theme.Background;
            pnlMain.BackColor = _theme.Background;

            lblTitle.ForeColor = _theme.TextPrimary;
            lblUnreadCount.ForeColor = _theme.Error;

            listViewConversations.BackColor = _theme.Surface;
            listViewConversations.ForeColor = _theme.TextPrimary;

            lblLoading.ForeColor = _theme.Primary;
            lblNoConversations.ForeColor = _theme.TextSecondary;
        }

        private void OnThemeChanged(ThemeService.ThemeMode newTheme)
        {
            ApplyTheme();
        }

        private async void LoadFriendsWithConversations()
        {
            try
            {
                lblLoading.Visible = true;
                lblLoading.BringToFront();
                lblNoConversations.Visible = false;
                listViewConversations.BeginUpdate();
                listViewConversations.Items.Clear();
                _itemsByUserId.Clear();

                string? currentUserId = _authService.CurrentUserId;
                if (currentUserId == null)
                {
                    MessageBox.Show("Vui lòng đăng nhập lại!", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                // Chỉ cần danh sách bạn bè; conversationId sẽ được tạo/lấy khi mở chat
                var friends = await _friendsService.GetFriends(currentUserId);
                lblLoading.Visible = false;

                if (friends.Count == 0)
                {
                    lblNoConversations.Visible = true;
                    lblNoConversations.BringToFront();
                    return;
                }

                // Dừng các status listener cũ
                foreach (var kv in _statusListeners)
                {
                    try { kv.Value.StopAsync(); } catch { }
                }
                _statusListeners.Clear();

                var seenUserIds = new HashSet<string>();
                foreach (var friend in friends)
                {
                    string otherUserId = friend.GetValueOrDefault("userId", string.Empty).ToString()!;
                    if (string.IsNullOrEmpty(otherUserId) || !seenUserIds.Add(otherUserId))
                    {
                        continue;
                    }

                    string friendName = friend.GetValueOrDefault("fullName", string.Empty).ToString()!;
                    string username = friend.GetValueOrDefault("username", string.Empty).ToString()!;
                    string email = friend.GetValueOrDefault("email", string.Empty).ToString()!;
                    string status = friend.GetValueOrDefault("status", "offline").ToString()!;
                    status = string.IsNullOrEmpty(status) ? "offline" : status.Trim().ToLowerInvariant();

                    string displayName = GetDisplayName(friendName, username, email, otherUserId);
                    string statusText = status == "online" ? "🟢 Online" : "⚫ Offline";

                    var item = new ListViewItem(displayName);
                    item.SubItems.Add("Chưa có tin nhắn");
                    item.SubItems.Add(statusText);
                    item.SubItems.Add("");
                    item.SubItems.Add("");

                    var tagData = new Dictionary<string, object>
                    {
                        { "otherUserId", otherUserId },
                        { "otherUserName", friendName },
                        { "otherUsername", username },
                        { "otherEmail", email },
                        { "otherDisplayName", displayName },
                        { "otherUserStatus", status }
                    };

                    item.Tag = tagData;
                    item.ForeColor = status == "online" ? _theme.Success : _theme.TextPrimary;

                    listViewConversations.Items.Add(item);
                    _itemsByUserId[otherUserId] = item;

                    // Listener trạng thái online/offline
                    if (!_statusListeners.ContainsKey(otherUserId))
                    {
                        var listener = _friendsService.ListenToUserStatus(otherUserId, () =>
                        {
                            if (this.IsHandleCreated)
                            {
                                try { this.BeginInvoke(new Action(() => UpdateFriendStatus(otherUserId))); } catch { }
                            }
                        });
                        _statusListeners[otherUserId] = listener;
                    }
                }

                if (listViewConversations.Items.Count > 0)
                {
                    listViewConversations.BringToFront();
                }
                listViewConversations.EndUpdate();
            }
            catch (Exception ex)
            {
                lblLoading.Visible = false;
                listViewConversations.BringToFront();
                try { listViewConversations.EndUpdate(); } catch { }
                MessageBox.Show($"Lỗi khi tải tin nhắn: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ListViewConversations_DoubleClick(object? sender, EventArgs e)
        {
            if (listViewConversations.SelectedItems.Count == 0) return;

            var selectedItem = listViewConversations.SelectedItems[0];
            var convData = (Dictionary<string, object>)selectedItem.Tag;
            string otherUserId = convData["otherUserId"].ToString()!;

            // If conversationId missing, create/get it on demand
            string conversationId = convData.ContainsKey("conversationId") && convData["conversationId"] != null
                ? convData["conversationId"].ToString()!
                : await _messagingService.GetOrCreateConversation(_authService.CurrentUserId!, otherUserId);

            // Create friend data for MessageForm
            var friendData = new Dictionary<string, object>
            {
                { "userId", otherUserId },
                { "fullName", convData.ContainsKey("otherUserName") ? convData["otherUserName"] : "" },
                { "username", convData.ContainsKey("otherUsername") ? convData["otherUsername"] : "" },
                { "email", convData.ContainsKey("otherEmail") ? convData["otherEmail"] : "" },
                { "displayName", convData.ContainsKey("otherDisplayName") ? convData["otherDisplayName"] : "" },
                { "status", convData.ContainsKey("otherUserStatus") ? convData["otherUserStatus"] : "offline" }
            };

            var messageForm = new MessageForm(conversationId, friendData);
            messageForm.Show();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _theme.OnThemeChanged -= OnThemeChanged;
                try { _convListener?.StopAsync(); } catch { }
                try { _friendsListener?.StopAsync(); } catch { }
                foreach (var kv in _statusListeners)
                {
                    try { kv.Value.StopAsync(); } catch { }
                }
                _statusListeners.Clear();
            }
            base.Dispose(disposing);
        }

        private FirestoreChangeListener? _convListener;
        private FirestoreChangeListener? _friendsListener;

        private void StartRealtimeConversationsListener()
        {
            string? currentUserId = _authService.CurrentUserId;
            if (currentUserId == null) return;

            try
            {
                _convListener = _messagingService.ListenToConversations(currentUserId, () =>
                {
                    if (this.IsHandleCreated)
                    {
                        try { this.BeginInvoke(new Action(() => LoadFriendsWithConversations())); } catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting conversations listener: {ex.Message}");
            }
        }

        private void StartRealtimeFriendsListener()
        {
            string? currentUserId = _authService.CurrentUserId;
            if (currentUserId == null) return;

            try
            {
                _friendsListener = _friendsService.ListenToFriendships(currentUserId, () =>
                {
                    if (this.IsHandleCreated)
                    {
                        try { this.BeginInvoke(new Action(() => LoadFriendsWithConversations())); } catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting friends listener: {ex.Message}");
            }
        }

        private static string GetDisplayName(string fullName, string username, string email, string userId)
        {
            if (!string.IsNullOrWhiteSpace(fullName)) return fullName;
            if (!string.IsNullOrWhiteSpace(username) && !IsLikelyUid(username, userId)) return username;
            if (!string.IsNullOrWhiteSpace(email)) return email;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                int take = Math.Min(6, userId.Length);
                return $"Người dùng {userId.Substring(0, take)}";
            }
            return "Người dùng";
        }

        private static bool IsLikelyUid(string candidate, string userId)
        {
            if (string.IsNullOrWhiteSpace(candidate)) return false;
            return string.Equals(candidate, userId, StringComparison.OrdinalIgnoreCase) || candidate.Length >= 24;
        }

        private void UpdateFriendStatus(string userId)
        {
            if (!_itemsByUserId.TryGetValue(userId, out var item)) return;
            // Re-fetch minimal status for this user
            Task.Run(async () =>
            {
                try
                {
                    var currentUserId = _authService.CurrentUserId;
                    if (currentUserId == null) return;
                    var friends = await _friendsService.GetFriends(currentUserId);
                    var friend = friends.Find(f => f.GetValueOrDefault("userId", "").ToString() == userId);
                    if (friend == null) return;
                    string status = friend.GetValueOrDefault("status", "offline").ToString()!;
                    status = string.IsNullOrEmpty(status) ? "offline" : status.Trim().ToLowerInvariant();
                    string statusText = status == "online" ? "🟢 Online" : "⚫ Offline";

                    if (this.IsHandleCreated)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            listViewConversations.BeginUpdate();
                            item.SubItems[2].Text = statusText;
                            item.ForeColor = status == "online" ? _theme.Success : _theme.TextPrimary;
                            listViewConversations.EndUpdate();
                        }));
                    }
                }
                catch { }
            });
        }
    }
}
