using MessagingApp.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MessagingApp.Forms.Social
{
    public partial class FriendRequestsForm : Form
    {
        private readonly ThemeService _theme = ThemeService.Instance;
        private readonly FirestoreFriendsService _friendsService = FirestoreFriendsService.Instance;
        private readonly FirebaseAuthService _authService = FirebaseAuthService.Instance;

        private Panel pnlMain = null!;
        private Label lblTitle = null!;
        private ListView listViewRequests = null!;
        private Label lblLoading = null!;
        private Label lblNoRequests = null!;

        public FriendRequestsForm()
        {
            InitializeComponent();
            InitializeCustomUI();
            ApplyTheme();
            LoadRequests();

            _theme.OnThemeChanged += OnThemeChanged;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.Name = "FriendRequestsForm";
            this.Text = "Lời Mời Kết Bạn - Messaging App";
            this.StartPosition = FormStartPosition.CenterParent;

            this.ResumeLayout(false);
        }

        private void InitializeCustomUI()
        {
            // Main panel
            pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30)
            };
            this.Controls.Add(pnlMain);

            // Title
            lblTitle = new Label
            {
                Text = "Lời Mời Kết Bạn",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 10)
            };
            pnlMain.Controls.Add(lblTitle);

            // ListView for requests
            listViewRequests = new ListView
            {
                Width = 740,
                Height = 350,
                Location = new Point(0, 70),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 10F)
            };
            listViewRequests.Columns.Add("Tên", 200);
            listViewRequests.Columns.Add("Username", 150);
            listViewRequests.Columns.Add("Email", 200);
            listViewRequests.Columns.Add("Thời gian", 190);
            listViewRequests.MouseClick += ListViewRequests_MouseClick;
            pnlMain.Controls.Add(listViewRequests);

            // Loading label
            lblLoading = new Label
            {
                Text = "⏳ Đang tải lời mời...",
                Font = new Font("Segoe UI", 12F),
                AutoSize = true,
                Location = new Point(250, 200),
                Visible = false
            };
            pnlMain.Controls.Add(lblLoading);
            lblLoading.BringToFront();

            // No requests label
            lblNoRequests = new Label
            {
                Text = "Không có lời mời kết bạn nào.",
                Font = new Font("Segoe UI", 12F),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Width = 300,
                Height = 50,
                Location = new Point(220, 200),
                Visible = false
            };
            pnlMain.Controls.Add(lblNoRequests);
            lblNoRequests.BringToFront();
        }

        private void ApplyTheme()
        {
            this.BackColor = _theme.Background;
            pnlMain.BackColor = _theme.Background;

            lblTitle.ForeColor = _theme.TextPrimary;

            listViewRequests.BackColor = _theme.Surface;
            listViewRequests.ForeColor = _theme.TextPrimary;

            lblLoading.ForeColor = _theme.Primary;
            lblNoRequests.ForeColor = _theme.TextSecondary;
        }

        private void OnThemeChanged(ThemeService.ThemeMode newTheme)
        {
            ApplyTheme();
        }

        private async void LoadRequests()
        {
            try
            {
                lblLoading.Visible = true;
                lblNoRequests.Visible = false;
                listViewRequests.Items.Clear();

                string? currentUserId = _authService.CurrentUserId;
                if (currentUserId == null)
                {
                    MessageBox.Show("Vui lòng đăng nhập lại!", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                var requests = await _friendsService.GetPendingRequests(currentUserId);

                lblLoading.Visible = false;

                if (requests.Count == 0)
                {
                    lblNoRequests.Visible = true;
                    return;
                }

                foreach (var request in requests)
                {
                    string fullName = request.ContainsKey("senderFullName") ? request["senderFullName"].ToString()! : "";
                    string username = request.ContainsKey("senderUsername") ? request["senderUsername"].ToString()! : "";
                    string email = request.ContainsKey("senderEmail") ? request["senderEmail"].ToString()! : "";

                    string displayName = string.IsNullOrEmpty(fullName) ? username : fullName;
                    
                    // Format timestamp
                    string timeText = "Vừa xong";
                    if (request.ContainsKey("createdAt") && request["createdAt"] != null)
                    {
                        try
                        {
                            var timestamp = (Google.Cloud.Firestore.Timestamp)request["createdAt"];
                            var dateTime = timestamp.ToDateTime().ToLocalTime();
                            var timeAgo = DateTime.Now - dateTime;

                            if (timeAgo.TotalMinutes < 1)
                                timeText = "Vừa xong";
                            else if (timeAgo.TotalHours < 1)
                                timeText = $"{(int)timeAgo.TotalMinutes} phút trước";
                            else if (timeAgo.TotalDays < 1)
                                timeText = $"{(int)timeAgo.TotalHours} giờ trước";
                            else
                                timeText = dateTime.ToString("dd/MM/yyyy HH:mm");
                        }
                        catch { }
                    }

                    var item = new ListViewItem(displayName);
                    item.SubItems.Add(username);
                    item.SubItems.Add(email);
                    item.SubItems.Add(timeText);
                    item.Tag = request;

                    listViewRequests.Items.Add(item);
                }

                MessageBox.Show($"Bạn có {requests.Count} lời mời kết bạn.\nRight-click để chấp nhận hoặc từ chối.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblLoading.Visible = false;
                MessageBox.Show($"Lỗi khi tải lời mời: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ListViewRequests_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && listViewRequests.SelectedItems.Count > 0)
            {
                var selectedItem = listViewRequests.SelectedItems[0];
                var requestData = (Dictionary<string, object>)selectedItem.Tag;

                var contextMenu = new ContextMenuStrip();

                var acceptItem = new ToolStripMenuItem("✅ Chấp nhận");
                acceptItem.Click += async (s, args) =>
                {
                    string requestId = requestData["requestId"].ToString()!;
                    string fromUserId = requestData["fromUserId"].ToString()!;
                    string? currentUserId = _authService.CurrentUserId;

                    if (currentUserId == null) return;

                    var (success, message) = await _friendsService.AcceptFriendRequest(requestId, fromUserId, currentUserId);

                    MessageBox.Show(message, success ? "Thành công" : "Lỗi",
                        MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);

                    if (success)
                    {
                        LoadRequests();
                    }
                };
                contextMenu.Items.Add(acceptItem);

                var declineItem = new ToolStripMenuItem("❌ Từ chối");
                declineItem.Click += async (s, args) =>
                {
                    string requestId = requestData["requestId"].ToString()!;

                    var result = MessageBox.Show(
                        "Bạn có chắc chắn muốn từ chối lời mời này?",
                        "Xác nhận",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    if (result == DialogResult.Yes)
                    {
                        var (success, message) = await _friendsService.DeclineFriendRequest(requestId);

                        MessageBox.Show(message, success ? "Thành công" : "Lỗi",
                            MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);

                        if (success)
                        {
                            LoadRequests();
                        }
                    }
                };
                contextMenu.Items.Add(declineItem);

                contextMenu.Show(listViewRequests, e.Location);
            }
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
