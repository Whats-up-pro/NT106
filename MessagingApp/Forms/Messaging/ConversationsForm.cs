using MessagingApp.Services;
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
        private readonly FirebaseAuthService _authService = FirebaseAuthService.Instance;

        private Panel pnlMain = null!;
        private Label lblTitle = null!;
        private ListView listViewConversations = null!;
        private Label lblLoading = null!;
        private Label lblNoConversations = null!;
        private Label lblUnreadCount = null!;

        public ConversationsForm()
        {
            InitializeComponent();
            InitializeCustomUI();
            ApplyTheme();
            LoadConversations();

            _theme.OnThemeChanged += OnThemeChanged;
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
            listViewConversations.Columns.Add("Tin nhắn cuối", 400);
            listViewConversations.Columns.Add("Trạng thái", 100);
            listViewConversations.Columns.Add("Thời gian", 140);
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

        private async void LoadConversations()
        {
            try
            {
                lblLoading.Visible = true;
                lblNoConversations.Visible = false;
                listViewConversations.Items.Clear();

                string? currentUserId = _authService.CurrentUserId;
                if (currentUserId == null)
                {
                    MessageBox.Show("Vui lòng đăng nhập lại!", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                var conversations = await _messagingService.GetConversations(currentUserId);
                var unreadCount = await _messagingService.GetUnreadMessageCount(currentUserId);

                lblLoading.Visible = false;

                if (unreadCount > 0)
                {
                    lblUnreadCount.Text = $"({unreadCount} chưa đọc)";
                    lblUnreadCount.Visible = true;
                }

                if (conversations.Count == 0)
                {
                    lblNoConversations.Visible = true;
                    return;
                }

                foreach (var conv in conversations)
                {
                    string friendName = conv.ContainsKey("otherUserName") ? conv["otherUserName"].ToString()! : "";
                    string username = conv.ContainsKey("otherUsername") ? conv["otherUsername"].ToString()! : "";
                    string lastMessage = conv.ContainsKey("lastMessage") ? conv["lastMessage"].ToString()! : "Chưa có tin nhắn";
                    string status = conv.ContainsKey("otherUserStatus") ? conv["otherUserStatus"].ToString()! : "offline";

                    string displayName = string.IsNullOrEmpty(friendName) ? username : friendName;
                    string statusText = status == "online" ? "🟢 Online" : "⚫ Offline";

                    // Format timestamp
                    string timeText = "";
                    if (conv.ContainsKey("lastMessageAt") && conv["lastMessageAt"] != null)
                    {
                        try
                        {
                            var timestamp = (Google.Cloud.Firestore.Timestamp)conv["lastMessageAt"];
                            var dateTime = timestamp.ToDateTime().ToLocalTime();
                            var timeAgo = DateTime.Now - dateTime;

                            if (timeAgo.TotalMinutes < 1)
                                timeText = "Vừa xong";
                            else if (timeAgo.TotalHours < 1)
                                timeText = $"{(int)timeAgo.TotalMinutes} phút trước";
                            else if (timeAgo.TotalDays < 1)
                                timeText = $"{(int)timeAgo.TotalHours} giờ trước";
                            else
                                timeText = dateTime.ToString("dd/MM/yyyy");
                        }
                        catch { }
                    }

                    var item = new ListViewItem(displayName);
                    item.SubItems.Add(lastMessage);
                    item.SubItems.Add(statusText);
                    item.SubItems.Add(timeText);
                    item.Tag = conv;

                    if (status == "online")
                    {
                        item.ForeColor = _theme.Success;
                    }

                    listViewConversations.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                lblLoading.Visible = false;
                MessageBox.Show($"Lỗi khi tải tin nhắn: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ListViewConversations_DoubleClick(object? sender, EventArgs e)
        {
            if (listViewConversations.SelectedItems.Count == 0) return;

            var selectedItem = listViewConversations.SelectedItems[0];
            var convData = (Dictionary<string, object>)selectedItem.Tag;
            string conversationId = convData["conversationId"].ToString()!;
            string otherUserId = convData["otherUserId"].ToString()!;

            // Create friend data for MessageForm
            var friendData = new Dictionary<string, object>
            {
                { "userId", otherUserId },
                { "fullName", convData.ContainsKey("otherUserName") ? convData["otherUserName"] : "" },
                { "username", convData.ContainsKey("otherUsername") ? convData["otherUsername"] : "" },
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
            }
            base.Dispose(disposing);
        }
    }
}
