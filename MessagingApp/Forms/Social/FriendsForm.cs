using MessagingApp.Services;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MessagingApp.Forms.Social
{
    public partial class FriendsForm : Form
    {
        private readonly ThemeService _theme = ThemeService.Instance;
        private readonly FirestoreFriendsService _friendsService = FirestoreFriendsService.Instance;
        private readonly FirebaseAuthService _authService = FirebaseAuthService.Instance;
        private readonly FirestoreMessagingService _messagingService = FirestoreMessagingService.Instance;

        private Panel pnlMain = null!;
        private Panel pnlHeader = null!;
        private Label lblTitle = null!;
        private TextBox txtSearch = null!;
        private Button btnSearch = null!;
        private Button btnAddFriend = null!;
        private Button btnRequests = null!;
        private ListView listViewFriends = null!;
        private Label lblLoading = null!;
        private Label lblNoFriends = null!;

        private List<Dictionary<string, object>> _currentFriends = new();
    private Google.Cloud.Firestore.FirestoreChangeListener? _friendsListener;

        public FriendsForm()
        {
            InitializeComponent();
            InitializeCustomUI();
            ApplyTheme();
            LoadFriends();

            _theme.OnThemeChanged += OnThemeChanged;
            
            // Delay listener to avoid exceeding quota
            _ = Task.Delay(2000).ContinueWith(_ =>
            {
                if (this.IsHandleCreated)
                {
                    try { this.BeginInvoke(new Action(StartFriendsRealtimeListener)); } catch { }
                }
            });
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 650);
            this.Name = "FriendsForm";
            this.Text = "Bạn Bè - Messaging App";
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
            pnlMain.SuspendLayout();

            // Header panel (create first, add later to ensure proper docking order)
            pnlHeader = new Panel
            {
                Height = 120,
                Dock = DockStyle.Top
            };

            // Title
            lblTitle = new Label
            {
                Text = "Danh Sách Bạn Bè",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 10)
            };
            pnlHeader.Controls.Add(lblTitle);

            // Search textbox
            txtSearch = new TextBox
            {
                Width = 300,
                Height = 35,
                Location = new Point(0, 60),
                Font = new Font("Segoe UI", 11F),
                PlaceholderText = "Tìm kiếm bạn bè..."
            };
            txtSearch.KeyPress += TxtSearch_KeyPress;
            pnlHeader.Controls.Add(txtSearch);

            // Search button
            btnSearch = new Button
            {
                Text = "🔍 Tìm",
                Width = 100,
                Height = 35,
                Location = new Point(310, 60),
                Font = new Font("Segoe UI", 11F),
                Cursor = Cursors.Hand
            };
            btnSearch.Click += BtnSearch_Click;
            pnlHeader.Controls.Add(btnSearch);

            // Add friend button
            btnAddFriend = new Button
            {
                Text = "➕ Thêm Bạn",
                Width = 130,
                Height = 35,
                Location = new Point(420, 60),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddFriend.Click += BtnAddFriend_Click;
            pnlHeader.Controls.Add(btnAddFriend);

            // Requests button
            btnRequests = new Button
            {
                Text = "📬 Lời Mời (0)",
                Width = 140,
                Height = 35,
                Location = new Point(560, 60),
                Font = new Font("Segoe UI", 11F),
                Cursor = Cursors.Hand
            };
            btnRequests.Click += BtnRequests_Click;
            pnlHeader.Controls.Add(btnRequests);

            // ListView for friends (add BEFORE header so header docks properly on top)
            listViewFriends = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 10F)
            };
            listViewFriends.Columns.Add("Tên", 250);
            listViewFriends.Columns.Add("Username", 150);
            listViewFriends.Columns.Add("Trạng thái", 100);
            listViewFriends.Columns.Add("Email", 250);
            listViewFriends.DoubleClick += ListViewFriends_DoubleClick;
            listViewFriends.MouseClick += ListViewFriends_MouseClick;
            pnlMain.Controls.Add(listViewFriends);

            // Now add header so Dock=Top is applied before Fill area is calculated
            pnlMain.Controls.Add(pnlHeader);

            // Loading label
            lblLoading = new Label
            {
                Text = "⏳ Đang tải danh sách bạn bè...",
                Font = new Font("Segoe UI", 12F),
                AutoSize = true,
                Location = new Point(250, 250),
                Visible = false
            };
            pnlMain.Controls.Add(lblLoading);
            lblLoading.BringToFront();

            // No friends label
            lblNoFriends = new Label
            {
                Text = "Chưa có bạn bè nào.\nHãy thêm bạn mới!",
                Font = new Font("Segoe UI", 12F),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Width = 300,
                Height = 80,
                Location = new Point(300, 250),
                Visible = false
            };
            pnlMain.Controls.Add(lblNoFriends);
            lblNoFriends.BringToFront();

            pnlMain.ResumeLayout(false);
        }

        private void ApplyTheme()
        {
            this.BackColor = _theme.Background;
            pnlMain.BackColor = _theme.Background;
            pnlHeader.BackColor = _theme.Background;

            lblTitle.ForeColor = _theme.TextPrimary;

            _theme.StyleTextBox(txtSearch);
            _theme.StyleButton(btnSearch, isPrimary: false);
            _theme.StyleButton(btnAddFriend, isPrimary: true);
            _theme.StyleButton(btnRequests, isPrimary: false);

            listViewFriends.BackColor = _theme.Surface;
            listViewFriends.ForeColor = _theme.TextPrimary;

            lblLoading.ForeColor = _theme.Primary;
            lblNoFriends.ForeColor = _theme.TextSecondary;
        }

        private void OnThemeChanged(ThemeService.ThemeMode newTheme)
        {
            ApplyTheme();
        }

        private async void LoadFriends()
        {
            try
            {
                lblLoading.Visible = true;
                lblNoFriends.Visible = false;
                listViewFriends.Items.Clear();

                string? currentUserId = _authService.CurrentUserId;
                if (currentUserId == null)
                {
                    MessageBox.Show("Vui lòng đăng nhập lại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                _currentFriends = await _friendsService.GetFriends(currentUserId);

                // Đảm bảo không trùng userId nếu Firestore còn sót document friendship cũ
                var seenIds = new HashSet<string>();
                var deduped = new List<Dictionary<string, object>>();
                foreach (var f in _currentFriends)
                {
                    var uid = f.ContainsKey("userId") ? f["userId"]?.ToString() ?? string.Empty : string.Empty;
                    if (string.IsNullOrEmpty(uid) || !seenIds.Add(uid)) continue;
                    deduped.Add(f);
                }
                _currentFriends = deduped;

                // Compute reliable online status with staleness window and sort online first
                const int OnlineStaleMinutes = 10; // consider online only if active within 10 minutes
                foreach (var f in _currentFriends)
                {
                    bool isOnline = false;
                    try
                    {
                        string status = f.ContainsKey("status") && f["status"] != null
                            ? f["status"].ToString()!.ToLower()
                            : string.Empty;

                        if (status == "online")
                        {
                            if (f.ContainsKey("lastLogin") && f["lastLogin"] is Timestamp ts)
                            {
                                var age = DateTime.UtcNow - ts.ToDateTime();
                                isOnline = age < TimeSpan.FromMinutes(OnlineStaleMinutes);
                            }
                            else
                            {
                                // If no timestamp, fall back to status flag
                                isOnline = true;
                            }
                        }
                    }
                    catch { }

                    f["isOnline"] = isOnline;
                }

                _currentFriends = _currentFriends
                    .OrderByDescending(f => f.ContainsKey("isOnline") && f["isOnline"] is bool b && b)
                    .ThenBy(f =>
                    {
                        string fullName = f.ContainsKey("fullName") && f["fullName"] != null ? f["fullName"].ToString()! : string.Empty;
                        string username = f.ContainsKey("username") && f["username"] != null ? f["username"].ToString()! : string.Empty;
                        return string.IsNullOrEmpty(fullName) ? username : fullName;
                    })
                    .ToList();

                lblLoading.Visible = false;

                if (_currentFriends.Count == 0)
                {
                    lblNoFriends.Visible = true;
                    return;
                }

                foreach (var friend in _currentFriends)
                {
                    string fullName = friend.ContainsKey("fullName") ? friend["fullName"].ToString()! : "";
                    string username = friend.ContainsKey("username") ? friend["username"].ToString()! : "";
                    bool isOnline = friend.ContainsKey("isOnline") && friend["isOnline"] is bool b && b;
                    string email = friend.ContainsKey("email") ? friend["email"].ToString()! : "";

                    string displayName = string.IsNullOrEmpty(fullName) ? username : fullName;
                    string statusText = isOnline ? "🟢 Online" : "⚫ Offline";

                    var item = new ListViewItem(displayName);
                    item.SubItems.Add(username);
                    item.SubItems.Add(statusText);
                    item.SubItems.Add(email);
                    item.Tag = friend;

                    // Color based on status
                    if (isOnline)
                    {
                        item.ForeColor = _theme.Success;
                    }

                    listViewFriends.Items.Add(item);
                }

                // Load pending requests count
                await LoadPendingRequestsCount();
            }
            catch (Exception ex)
            {
                lblLoading.Visible = false;
                MessageBox.Show($"Lỗi khi tải danh sách bạn bè: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadPendingRequestsCount()
        {
            try
            {
                string? currentUserId = _authService.CurrentUserId;
                if (currentUserId == null) return;

                var requests = await _friendsService.GetPendingRequests(currentUserId);
                btnRequests.Text = $"📬 Lời Mời ({requests.Count})";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading pending requests count: {ex.Message}");
            }
        }

        private void TxtSearch_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                BtnSearch_Click(sender, EventArgs.Empty);
            }
        }

        private void BtnSearch_Click(object? sender, EventArgs e)
        {
            string searchText = txtSearch.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                LoadFriends();
                return;
            }

            listViewFriends.Items.Clear();

            var filtered = _currentFriends.FindAll(f =>
            {
                string fullName = f.ContainsKey("fullName") ? f["fullName"].ToString()!.ToLower() : "";
                string username = f.ContainsKey("username") ? f["username"].ToString()!.ToLower() : "";
                string email = f.ContainsKey("email") ? f["email"].ToString()!.ToLower() : "";

                return fullName.Contains(searchText) || username.Contains(searchText) || email.Contains(searchText);
            });

            // Keep sorting: online first, then by display name
            var sortedFiltered = filtered
                .OrderByDescending(f => f.ContainsKey("isOnline") && f["isOnline"] is bool b && b)
                .ThenBy(f =>
                {
                    string fullName = f.ContainsKey("fullName") && f["fullName"] != null ? f["fullName"].ToString()! : string.Empty;
                    string username = f.ContainsKey("username") && f["username"] != null ? f["username"].ToString()! : string.Empty;
                    return string.IsNullOrEmpty(fullName) ? username : fullName;
                })
                .ToList();

            foreach (var friend in sortedFiltered)
            {
                string fullName = friend.ContainsKey("fullName") ? friend["fullName"].ToString()! : "";
                string username = friend.ContainsKey("username") ? friend["username"].ToString()! : "";
                bool isOnline = friend.ContainsKey("isOnline") && friend["isOnline"] is bool b && b;
                string email = friend.ContainsKey("email") ? friend["email"].ToString()! : "";

                string displayName = string.IsNullOrEmpty(fullName) ? username : fullName;
                string statusText = isOnline ? "🟢 Online" : "⚫ Offline";

                var item = new ListViewItem(displayName);
                item.SubItems.Add(username);
                item.SubItems.Add(statusText);
                item.SubItems.Add(email);
                item.Tag = friend;

                if (isOnline)
                {
                    item.ForeColor = _theme.Success;
                }

                listViewFriends.Items.Add(item);
            }
        }

        private void BtnAddFriend_Click(object? sender, EventArgs e)
        {
            var addFriendForm = new AddFriendForm();
            addFriendForm.ShowDialog();
            LoadFriends(); // Refresh after adding
        }

        private void BtnRequests_Click(object? sender, EventArgs e)
        {
            var requestsForm = new FriendRequestsForm();
            requestsForm.ShowDialog();
            LoadFriends(); // Refresh after handling requests
        }

        private async void ListViewFriends_DoubleClick(object? sender, EventArgs e)
        {
            if (listViewFriends.SelectedItems.Count == 0) return;

            var selectedItem = listViewFriends.SelectedItems[0];
            if (selectedItem.Tag is not Dictionary<string, object> friendData)
                return;
            string friendId = friendData["userId"].ToString()!;
            string? currentUserId = _authService.CurrentUserId;

            if (currentUserId == null) return;

            try
            {
                // Get or create conversation
                string conversationId = await _messagingService.GetOrCreateConversation(currentUserId, friendId);

                // Open message form
                var messageForm = new Messaging.MessageForm(conversationId, friendData);
                messageForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở tin nhắn: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ListViewFriends_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && listViewFriends.SelectedItems.Count > 0)
            {
                var selectedItem = listViewFriends.SelectedItems[0];
                if (selectedItem.Tag is not Dictionary<string, object> friendData)
                    return;

                var contextMenu = new ContextMenuStrip();

                var sendMessageItem = new ToolStripMenuItem("💬 Gửi tin nhắn");
                sendMessageItem.Click += (s, args) => ListViewFriends_DoubleClick(sender, EventArgs.Empty);
                contextMenu.Items.Add(sendMessageItem);

                var unfriendItem = new ToolStripMenuItem("❌ Hủy kết bạn");
                unfriendItem.Click += async (s, args) =>
                {
                    var result = MessageBox.Show(
                        "Bạn có chắc chắn muốn hủy kết bạn?",
                        "Xác nhận",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    if (result == DialogResult.Yes)
                    {
                        string friendshipId = friendData["friendshipId"].ToString()!;
                        var (success, message) = await _friendsService.Unfriend(friendshipId);
                        MessageBox.Show(message, success ? "Thành công" : "Lỗi",
                            MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                        
                        if (success)
                        {
                            LoadFriends();
                        }
                    }
                };
                contextMenu.Items.Add(unfriendItem);

                contextMenu.Show(listViewFriends, e.Location);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _theme.OnThemeChanged -= OnThemeChanged;
                try { _friendsListener?.StopAsync(); } catch { }
            }
            base.Dispose(disposing);
        }

        private void StartFriendsRealtimeListener()
        {
            string? currentUserId = _authService.CurrentUserId;
            if (currentUserId == null) return;

            _friendsListener = _friendsService.ListenToFriendships(currentUserId, () =>
            {
                if (this.IsHandleCreated && this.InvokeRequired)
                {
                    try { this.BeginInvoke(new Action(() => { LoadFriends(); })); } catch { }
                }
                else
                {
                    LoadFriends();
                }
            });
        }
    }
}
