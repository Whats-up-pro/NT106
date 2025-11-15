using MessagingApp.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MessagingApp.Forms.Social
{
    public partial class AddFriendForm : Form
    {
        private readonly ThemeService _theme = ThemeService.Instance;
        private readonly FirestoreFriendsService _friendsService = FirestoreFriendsService.Instance;
        private readonly FirebaseAuthService _authService = FirebaseAuthService.Instance;

        private Panel pnlMain = null!;
        private Label lblTitle = null!;
        private Label lblInstruction = null!;
        private Label lblSearch = null!;
        private TextBox txtSearch = null!;
        private Button btnSearch = null!;
        private ListView listViewResults = null!;
        private Label lblLoading = null!;
        private Label lblNoResults = null!;

        public AddFriendForm()
        {
            InitializeComponent();
            InitializeCustomUI();
            ApplyTheme();

            _theme.OnThemeChanged += OnThemeChanged;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 550);
            this.Name = "AddFriendForm";
            this.Text = "Thêm Bạn - Messaging App";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

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

            int yPos = 20;

            // Title
            lblTitle = new Label
            {
                Text = "Thêm Bạn Mới",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, yPos)
            };
            pnlMain.Controls.Add(lblTitle);
            yPos += 50;

            // Instruction
            lblInstruction = new Label
            {
                Text = "Nhập email hoặc username để tìm kiếm người dùng",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(0, yPos)
            };
            pnlMain.Controls.Add(lblInstruction);
            yPos += 40;

            // Search label
            lblSearch = new Label
            {
                Text = "Email hoặc Username:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, yPos)
            };
            pnlMain.Controls.Add(lblSearch);
            yPos += 30;

            // Search textbox
            txtSearch = new TextBox
            {
                Width = 450,
                Height = 35,
                Location = new Point(0, yPos),
                Font = new Font("Segoe UI", 11F),
                PlaceholderText = "email@example.com hoặc username"
            };
            txtSearch.KeyPress += TxtSearch_KeyPress;
            pnlMain.Controls.Add(txtSearch);

            // Search button
            btnSearch = new Button
            {
                Text = "🔍 Tìm Kiếm",
                Width = 130,
                Height = 35,
                Location = new Point(460, yPos),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSearch.Click += BtnSearch_Click;
            pnlMain.Controls.Add(btnSearch);
            yPos += 60;

            // ListView for results
            listViewResults = new ListView
            {
                Width = 590,
                Height = 250,
                Location = new Point(0, yPos),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 10F)
            };
            listViewResults.Columns.Add("Tên", 200);
            listViewResults.Columns.Add("Username", 150);
            listViewResults.Columns.Add("Email", 240);
            listViewResults.DoubleClick += ListViewResults_DoubleClick;
            pnlMain.Controls.Add(listViewResults);

            // Loading label
            lblLoading = new Label
            {
                Text = "⏳ Đang tìm kiếm...",
                Font = new Font("Segoe UI", 11F),
                AutoSize = true,
                Location = new Point(200, yPos + 100),
                Visible = false
            };
            pnlMain.Controls.Add(lblLoading);
            lblLoading.BringToFront();

            // No results label
            lblNoResults = new Label
            {
                Text = "Không tìm thấy người dùng nào.\nHãy thử lại với email hoặc username khác.",
                Font = new Font("Segoe UI", 10F),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Width = 400,
                Height = 80,
                Location = new Point(95, yPos + 85),
                Visible = false
            };
            pnlMain.Controls.Add(lblNoResults);
            lblNoResults.BringToFront();
        }

        private void ApplyTheme()
        {
            this.BackColor = _theme.Background;
            pnlMain.BackColor = _theme.Background;

            lblTitle.ForeColor = _theme.TextPrimary;
            lblInstruction.ForeColor = _theme.TextSecondary;
            lblSearch.ForeColor = _theme.TextPrimary;

            _theme.StyleTextBox(txtSearch);
            _theme.StyleButton(btnSearch, isPrimary: true);

            listViewResults.BackColor = _theme.Surface;
            listViewResults.ForeColor = _theme.TextPrimary;

            lblLoading.ForeColor = _theme.Primary;
            lblNoResults.ForeColor = _theme.TextSecondary;
        }

        private void OnThemeChanged(ThemeService.ThemeMode newTheme)
        {
            ApplyTheme();
        }

        private void TxtSearch_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                BtnSearch_Click(sender, EventArgs.Empty);
            }
        }

        private async void BtnSearch_Click(object? sender, EventArgs e)
        {
            string searchText = txtSearch.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                MessageBox.Show("Vui lòng nhập email hoặc username!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                lblLoading.Visible = true;
                lblNoResults.Visible = false;
                listViewResults.Items.Clear();
                btnSearch.Enabled = false;

                string? currentUserId = _authService.CurrentUserId;
                if (currentUserId == null)
                {
                    MessageBox.Show("Vui lòng đăng nhập lại!", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                var results = await _friendsService.SearchUsers(searchText, currentUserId);

                lblLoading.Visible = false;
                btnSearch.Enabled = true;

                if (results.Count == 0)
                {
                    lblNoResults.Visible = true;
                    return;
                }

                foreach (var user in results)
                {
                    string fullName = user.ContainsKey("fullName") ? user["fullName"].ToString()! : "";
                    string username = user.ContainsKey("username") ? user["username"].ToString()! : "";
                    string email = user.ContainsKey("email") ? user["email"].ToString()! : "";

                    string displayName = string.IsNullOrEmpty(fullName) ? username : fullName;

                    var item = new ListViewItem(displayName);
                    item.SubItems.Add(username);
                    item.SubItems.Add(email);
                    item.Tag = user;

                    listViewResults.Items.Add(item);
                }

                MessageBox.Show($"Tìm thấy {results.Count} người dùng.\nDouble-click để gửi lời mời kết bạn.",
                    "Kết quả", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblLoading.Visible = false;
                btnSearch.Enabled = true;
                MessageBox.Show($"Lỗi khi tìm kiếm: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ListViewResults_DoubleClick(object? sender, EventArgs e)
        {
            if (listViewResults.SelectedItems.Count == 0) return;

            var selectedItem = listViewResults.SelectedItems[0];
            var userData = (Dictionary<string, object>)selectedItem.Tag;
            string targetUserId = userData["userId"].ToString()!;
            string displayName = userData.ContainsKey("fullName") && !string.IsNullOrEmpty(userData["fullName"].ToString())
                ? userData["fullName"].ToString()!
                : userData["username"].ToString()!;

            var result = MessageBox.Show(
                $"Gửi lời mời kết bạn đến {displayName}?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result != DialogResult.Yes) return;

            try
            {
                string? currentUserId = _authService.CurrentUserId;
                if (currentUserId == null) return;

                var (success, message) = await _friendsService.SendFriendRequest(currentUserId, targetUserId);

                MessageBox.Show(message, success ? "Thành công" : "Thông báo",
                    MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

                if (success)
                {
                    // Remove from list after sending request
                    listViewResults.Items.Remove(selectedItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
