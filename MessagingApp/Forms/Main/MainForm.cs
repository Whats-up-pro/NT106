using MessagingApp.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MessagingApp.Forms.Main
{
    public partial class MainForm : Form
    {
        private readonly ThemeService _theme = ThemeService.Instance;
        private readonly FirebaseAuthService _authService = FirebaseAuthService.Instance;

        private Label lblWelcome = null!;
        private Button btnFriends = null!;
        private Button btnMessages = null!;
        private Button btnLogout = null!;
        private Button btnToggleTheme = null!;
    private Button btnProfile = null!;

        public MainForm()
        {
            InitializeComponent();
            InitializeCustomUI();
            ApplyTheme();
            LoadUserData();

            _theme.OnThemeChanged += OnThemeChanged;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Name = "MainForm";
            this.Text = "Messaging App - Màn Hình Chính";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            this.ResumeLayout(false);
        }

        private void InitializeCustomUI()
        {
            // Welcome label
            lblWelcome = new Label
            {
                Text = "Đang tải...",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(50, 50)
            };
            this.Controls.Add(lblWelcome);

            // Friends button
            btnFriends = new Button
            {
                Text = "👥 Bạn Bè",
                Width = 180,
                Height = 60,
                Location = new Point(50, 120),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnFriends.Click += BtnFriends_Click;
            this.Controls.Add(btnFriends);

            // Messages button
            btnMessages = new Button
            {
                Text = "💬 Tin Nhắn",
                Width = 180,
                Height = 60,
                Location = new Point(250, 120),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnMessages.Click += BtnMessages_Click;
            this.Controls.Add(btnMessages);

            // Logout button
            btnLogout = new Button
            {
                Text = "Đăng Xuất",
                Width = 150,
                Height = 45,
                Location = new Point(50, 200),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogout.Click += BtnLogout_Click;
            this.Controls.Add(btnLogout);

            // Profile button
            btnProfile = new Button
            {
                Text = "🧑 Hồ Sơ",
                Width = 150,
                Height = 45,
                Location = new Point(220, 200),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnProfile.Click += BtnProfile_Click;
            this.Controls.Add(btnProfile);

            // Toggle theme button
            btnToggleTheme = new Button
            {
                Text = "🌙 Chế Độ Tối",
                Width = 150,
                Height = 45,
                Location = new Point(390, 200),
                Font = new Font("Segoe UI", 11F),
                Cursor = Cursors.Hand
            };
            btnToggleTheme.Click += BtnToggleTheme_Click;
            this.Controls.Add(btnToggleTheme);
        }

        private void ApplyTheme()
        {
            this.BackColor = _theme.Background;
            lblWelcome.ForeColor = _theme.TextPrimary;

            _theme.StyleButton(btnFriends, isPrimary: true);
            _theme.StyleButton(btnMessages, isPrimary: true);
            _theme.StyleButton(btnLogout, isPrimary: false);
            _theme.StyleButton(btnProfile, isPrimary: false);
            _theme.StyleButton(btnToggleTheme, isPrimary: false);

            // Update theme button text
            btnToggleTheme.Text = _theme.CurrentTheme == ThemeService.ThemeMode.Light 
                ? "🌙 Chế Độ Tối" 
                : "☀️ Chế Độ Sáng";
        }

        private void OnThemeChanged(ThemeService.ThemeMode newTheme)
        {
            ApplyTheme();
        }

        private async void LoadUserData()
        {
            try
            {
                var userData = await _authService.GetCurrentUserData();

                if (userData != null && userData.ContainsKey("fullName"))
                {
                    string fullName = userData["fullName"]?.ToString() ?? "Người dùng";
                    lblWelcome.Text = $"Xin chào, {fullName}!";
                }
                else
                {
                    lblWelcome.Text = "Xin chào!";
                }
            }
            catch (Exception ex)
            {
                lblWelcome.Text = "Xin chào!";
                Console.WriteLine($"Error loading user data: {ex.Message}");
            }
        }

        private async void BtnLogout_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn đăng xuất?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                await _authService.SignOut();
                
                // Close MainForm, which will show LoginForm again (handled in LoginForm.cs)
                this.Close();
            }
        }

        private void BtnFriends_Click(object? sender, EventArgs e)
        {
            var friendsForm = new Social.FriendsForm();
            friendsForm.ShowDialog();
        }

        private void BtnMessages_Click(object? sender, EventArgs e)
        {
            var conversationsForm = new Messaging.ConversationsForm();
            conversationsForm.ShowDialog();
        }

        private void BtnProfile_Click(object? sender, EventArgs e)
        {
            var profileForm = new Social.ProfileForm();
            profileForm.ShowDialog();
        }

        private void BtnToggleTheme_Click(object? sender, EventArgs e)
        {
            _theme.ToggleTheme();
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
