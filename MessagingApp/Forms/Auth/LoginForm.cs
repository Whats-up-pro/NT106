using MessagingApp.Services;
using System;
using System.Drawing;
using System.Windows.Forms;
using MessagingApp.Forms.Main;

namespace MessagingApp.Forms.Auth
{
    public partial class LoginForm : Form
    {
        private readonly ThemeService _theme = ThemeService.Instance;
        private readonly FirebaseAuthService _authService = FirebaseAuthService.Instance;

        // UI Controls
        private Panel pnlMain = null!;
        private Panel pnlLoginCard = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Label lblEmail = null!;
        private TextBox txtEmail = null!;
        private Label lblPassword = null!;
        private TextBox txtPassword = null!;
        private CheckBox chkRememberMe = null!;
        private Button btnLogin = null!;
        private LinkLabel lnkForgotPassword = null!;
        private LinkLabel lnkRegister = null!;
        private Label lblLoading = null!;
        private Label lblError = null!;

        public LoginForm()
        {
            InitializeComponent();
            InitializeCustomUI();
            ApplyTheme();
            
            // Subscribe to theme changes
            _theme.OnThemeChanged += OnThemeChanged;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 650);
            this.Name = "LoginForm";
            this.Text = "Đăng Nhập - Messaging App";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            
            this.ResumeLayout(false);
        }

        private void InitializeCustomUI()
        {
            // Main panel (full screen background)
            pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0)
            };
            this.Controls.Add(pnlMain);

            // Login card panel (centered)
            pnlLoginCard = new Panel
            {
                Width = 420,
                Height = 550,
                Location = new Point((this.ClientSize.Width - 420) / 2, (this.ClientSize.Height - 550) / 2)
            };
            pnlMain.Controls.Add(pnlLoginCard);

            int yPos = 30;

            // Title
            lblTitle = new Label
            {
                Text = "Chào mừng trở lại!",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30, yPos)
            };
            pnlLoginCard.Controls.Add(lblTitle);
            yPos += 50;

            // Subtitle
            lblSubtitle = new Label
            {
                Text = "Đăng nhập để tiếp tục trò chuyện",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(30, yPos)
            };
            pnlLoginCard.Controls.Add(lblSubtitle);
            yPos += 50;

            // Email label
            lblEmail = new Label
            {
                Text = "Email",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30, yPos)
            };
            pnlLoginCard.Controls.Add(lblEmail);
            yPos += 30;

            // Email textbox
            txtEmail = new TextBox
            {
                Width = 360,
                Height = 35,
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 11F),
                PlaceholderText = "email@example.com"
            };
            pnlLoginCard.Controls.Add(txtEmail);
            yPos += 50;

            // Password label
            lblPassword = new Label
            {
                Text = "Mật khẩu",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30, yPos)
            };
            pnlLoginCard.Controls.Add(lblPassword);
            yPos += 30;

            // Password textbox
            txtPassword = new TextBox
            {
                Width = 360,
                Height = 35,
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 11F),
                UseSystemPasswordChar = true,
                PlaceholderText = "Nhập mật khẩu"
            };
            txtPassword.KeyPress += TxtPassword_KeyPress;
            pnlLoginCard.Controls.Add(txtPassword);
            yPos += 50;

            // Remember me checkbox
            chkRememberMe = new CheckBox
            {
                Text = "Ghi nhớ đăng nhập",
                AutoSize = true,
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 9.5F)
            };
            pnlLoginCard.Controls.Add(chkRememberMe);
            yPos += 40;

            // Login button
            btnLogin = new Button
            {
                Text = "Đăng Nhập",
                Width = 360,
                Height = 45,
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.Click += BtnLogin_Click;
            pnlLoginCard.Controls.Add(btnLogin);
            yPos += 60;

            // Loading label (hidden by default)
            lblLoading = new Label
            {
                Text = "⏳ Đang đăng nhập...",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(30, yPos),
                Visible = false
            };
            pnlLoginCard.Controls.Add(lblLoading);

            // Error label (hidden by default)
            lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9F),
                AutoSize = false,
                Width = 360,
                Height = 40,
                Location = new Point(30, yPos),
                Visible = false
            };
            pnlLoginCard.Controls.Add(lblError);
            yPos += 50;

            // Forgot password link
            lnkForgotPassword = new LinkLabel
            {
                Text = "Quên mật khẩu?",
                AutoSize = true,
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 9.5F)
            };
            lnkForgotPassword.LinkClicked += LnkForgotPassword_LinkClicked;
            pnlLoginCard.Controls.Add(lnkForgotPassword);

            // Register link
            Label lblNoAccount = new Label
            {
                Text = "Chưa có tài khoản?",
                AutoSize = true,
                Location = new Point(220, yPos),
                Font = new Font("Segoe UI", 9.5F)
            };
            pnlLoginCard.Controls.Add(lblNoAccount);

            lnkRegister = new LinkLabel
            {
                Text = "Đăng ký ngay",
                AutoSize = true,
                Location = new Point(220 + lblNoAccount.Width + 5, yPos),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            lnkRegister.LinkClicked += LnkRegister_LinkClicked;
            pnlLoginCard.Controls.Add(lnkRegister);
        }

        private void ApplyTheme()
        {
            // Form background
            this.BackColor = _theme.Background;

            // Main panel
            pnlMain.BackColor = _theme.Background;

            // Login card
            pnlLoginCard.BackColor = _theme.Surface;

            // Title
            lblTitle.ForeColor = _theme.TextPrimary;

            // Subtitle
            lblSubtitle.ForeColor = _theme.TextSecondary;

            // Labels
            lblEmail.ForeColor = _theme.TextPrimary;
            lblPassword.ForeColor = _theme.TextPrimary;

            // Textboxes
            txtEmail.BackColor = _theme.Surface;
            txtEmail.ForeColor = _theme.TextPrimary;
            txtPassword.BackColor = _theme.Surface;
            txtPassword.ForeColor = _theme.TextPrimary;

            // Checkbox
            chkRememberMe.ForeColor = _theme.TextSecondary;

            // Login button
            _theme.StyleButton(btnLogin, isPrimary: true);

            // Links
            lnkForgotPassword.LinkColor = _theme.Primary;
            lnkForgotPassword.ActiveLinkColor = _theme.PrimaryHover;
            lnkForgotPassword.VisitedLinkColor = _theme.Primary;

            lnkRegister.LinkColor = _theme.Primary;
            lnkRegister.ActiveLinkColor = _theme.PrimaryHover;
            lnkRegister.VisitedLinkColor = _theme.Primary;

            // Loading label
            lblLoading.ForeColor = _theme.Primary;

            // Error label
            lblError.ForeColor = _theme.Error;
        }

        private void OnThemeChanged(ThemeService.ThemeMode newTheme)
        {
            ApplyTheme();
        }

        private void TxtPassword_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                BtnLogin_Click(sender, EventArgs.Empty);
            }
        }

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            // Hide previous error
            lblError.Visible = false;
            lblLoading.Visible = true;
            btnLogin.Enabled = false;

            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            // Validation
            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("Vui lòng nhập email.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Vui lòng nhập mật khẩu.");
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowError("Email không hợp lệ.");
                return;
            }

            try
            {
                // Attempt sign in with Firebase
                var (success, message, userId) = await _authService.SignInWithEmailPassword(email, password);

                lblLoading.Visible = false;
                btnLogin.Enabled = true;

                if (success && userId != null)
                {
                    // Login successful
                    MessageBox.Show(message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Navigate to main form
                    this.Hide();
                    MainForm mainForm = new MainForm();
                    mainForm.FormClosed += (s, args) => this.Close();
                    mainForm.Show();
                }
                else
                {
                    // Login failed
                    ShowError(message);
                }
            }
            catch (Exception ex)
            {
                lblLoading.Visible = false;
                btnLogin.Enabled = true;
                ShowError($"Lỗi: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            lblLoading.Visible = false;
            lblError.Text = $"❌ {message}";
            lblError.Visible = true;
            btnLogin.Enabled = true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void LnkForgotPassword_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            ForgotPasswordForm forgotForm = new ForgotPasswordForm();
            forgotForm.ShowDialog();
        }

        private void LnkRegister_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            RegisterForm registerForm = new RegisterForm();
            registerForm.ShowDialog();
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
