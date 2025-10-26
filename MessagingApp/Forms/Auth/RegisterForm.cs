using MessagingApp.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MessagingApp.Forms.Auth
{
    public partial class RegisterForm : Form
    {
        private readonly ThemeService _theme = ThemeService.Instance;
        private readonly FirebaseAuthService _authService = FirebaseAuthService.Instance;

        // UI Controls
        private Panel pnlMain = null!;
        private Panel pnlRegisterCard = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        
        private Label lblFullName = null!;
        private TextBox txtFullName = null!;
        
        private Label lblUsername = null!;
        private TextBox txtUsername = null!;
        
        private Label lblEmail = null!;
        private TextBox txtEmail = null!;
        
        private Label lblPassword = null!;
        private TextBox txtPassword = null!;
        
        private Label lblConfirmPassword = null!;
        private TextBox txtConfirmPassword = null!;
        
        private CheckBox chkAgreeTerms = null!;
        private Button btnRegister = null!;
        private LinkLabel lnkLogin = null!;
        private Label lblLoading = null!;
        private Label lblError = null!;

        public RegisterForm()
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
            this.ClientSize = new System.Drawing.Size(1000, 700);
            this.Name = "RegisterForm";
            this.Text = "Đăng Ký - Messaging App";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            
            this.ResumeLayout(false);
        }

        private void InitializeCustomUI()
        {
            // Main panel
            pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0)
            };
            this.Controls.Add(pnlMain);

            // Register card panel
            pnlRegisterCard = new Panel
            {
                Width = 450,
                Height = 650,
                Location = new Point((this.ClientSize.Width - 450) / 2, (this.ClientSize.Height - 650) / 2)
            };
            pnlMain.Controls.Add(pnlRegisterCard);

            int yPos = 20;

            // Title
            lblTitle = new Label
            {
                Text = "Tạo Tài Khoản",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30, yPos)
            };
            pnlRegisterCard.Controls.Add(lblTitle);
            yPos += 50;

            // Subtitle
            lblSubtitle = new Label
            {
                Text = "Đăng ký để bắt đầu trò chuyện với bạn bè",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(30, yPos)
            };
            pnlRegisterCard.Controls.Add(lblSubtitle);
            yPos += 40;

            // Full Name
            lblFullName = new Label
            {
                Text = "Họ và tên",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30, yPos)
            };
            pnlRegisterCard.Controls.Add(lblFullName);
            yPos += 25;

            txtFullName = new TextBox
            {
                Width = 390,
                Height = 30,
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "Nguyễn Văn A"
            };
            pnlRegisterCard.Controls.Add(txtFullName);
            yPos += 45;

            // Username
            lblUsername = new Label
            {
                Text = "Tên đăng nhập",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30, yPos)
            };
            pnlRegisterCard.Controls.Add(lblUsername);
            yPos += 25;

            txtUsername = new TextBox
            {
                Width = 390,
                Height = 30,
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "username (tối thiểu 3 ký tự)"
            };
            pnlRegisterCard.Controls.Add(txtUsername);
            yPos += 45;

            // Email
            lblEmail = new Label
            {
                Text = "Email",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30, yPos)
            };
            pnlRegisterCard.Controls.Add(lblEmail);
            yPos += 25;

            txtEmail = new TextBox
            {
                Width = 390,
                Height = 30,
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "email@example.com"
            };
            pnlRegisterCard.Controls.Add(txtEmail);
            yPos += 45;

            // Password
            lblPassword = new Label
            {
                Text = "Mật khẩu",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30, yPos)
            };
            pnlRegisterCard.Controls.Add(lblPassword);
            yPos += 25;

            txtPassword = new TextBox
            {
                Width = 390,
                Height = 30,
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 10F),
                UseSystemPasswordChar = true,
                PlaceholderText = "Tối thiểu 6 ký tự"
            };
            pnlRegisterCard.Controls.Add(txtPassword);
            yPos += 45;

            // Confirm Password
            lblConfirmPassword = new Label
            {
                Text = "Xác nhận mật khẩu",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30, yPos)
            };
            pnlRegisterCard.Controls.Add(lblConfirmPassword);
            yPos += 25;

            txtConfirmPassword = new TextBox
            {
                Width = 390,
                Height = 30,
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 10F),
                UseSystemPasswordChar = true,
                PlaceholderText = "Nhập lại mật khẩu"
            };
            txtConfirmPassword.KeyPress += TxtConfirmPassword_KeyPress;
            pnlRegisterCard.Controls.Add(txtConfirmPassword);
            yPos += 45;

            // Agree to terms checkbox
            chkAgreeTerms = new CheckBox
            {
                Text = "Tôi đồng ý với Điều khoản Sử dụng và Chính sách Bảo mật",
                AutoSize = false,
                Width = 390,
                Height = 40,
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 9F)
            };
            pnlRegisterCard.Controls.Add(chkAgreeTerms);
            yPos += 45;

            // Register button
            btnRegister = new Button
            {
                Text = "Đăng Ký",
                Width = 390,
                Height = 45,
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRegister.Click += BtnRegister_Click;
            pnlRegisterCard.Controls.Add(btnRegister);
            yPos += 55;

            // Loading label
            lblLoading = new Label
            {
                Text = "⏳ Đang tạo tài khoản...",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(30, yPos),
                Visible = false
            };
            pnlRegisterCard.Controls.Add(lblLoading);

            // Error label
            lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9F),
                AutoSize = false,
                Width = 390,
                Height = 40,
                Location = new Point(30, yPos),
                Visible = false
            };
            pnlRegisterCard.Controls.Add(lblError);
            yPos += 50;

            // Login link
            Label lblHaveAccount = new Label
            {
                Text = "Đã có tài khoản?",
                AutoSize = true,
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 9.5F)
            };
            pnlRegisterCard.Controls.Add(lblHaveAccount);

            lnkLogin = new LinkLabel
            {
                Text = "Đăng nhập ngay",
                AutoSize = true,
                Location = new Point(30 + lblHaveAccount.Width + 5, yPos),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            lnkLogin.LinkClicked += LnkLogin_LinkClicked;
            pnlRegisterCard.Controls.Add(lnkLogin);
        }

        private void ApplyTheme()
        {
            this.BackColor = _theme.Background;
            pnlMain.BackColor = _theme.Background;
            pnlRegisterCard.BackColor = _theme.Surface;

            lblTitle.ForeColor = _theme.TextPrimary;
            lblSubtitle.ForeColor = _theme.TextSecondary;

            // Labels
            lblFullName.ForeColor = _theme.TextPrimary;
            lblUsername.ForeColor = _theme.TextPrimary;
            lblEmail.ForeColor = _theme.TextPrimary;
            lblPassword.ForeColor = _theme.TextPrimary;
            lblConfirmPassword.ForeColor = _theme.TextPrimary;

            // Textboxes
            txtFullName.BackColor = _theme.Surface;
            txtFullName.ForeColor = _theme.TextPrimary;
            txtUsername.BackColor = _theme.Surface;
            txtUsername.ForeColor = _theme.TextPrimary;
            txtEmail.BackColor = _theme.Surface;
            txtEmail.ForeColor = _theme.TextPrimary;
            txtPassword.BackColor = _theme.Surface;
            txtPassword.ForeColor = _theme.TextPrimary;
            txtConfirmPassword.BackColor = _theme.Surface;
            txtConfirmPassword.ForeColor = _theme.TextPrimary;

            // Checkbox
            chkAgreeTerms.ForeColor = _theme.TextSecondary;

            // Button
            _theme.StyleButton(btnRegister, isPrimary: true);

            // Link
            lnkLogin.LinkColor = _theme.Primary;
            lnkLogin.ActiveLinkColor = _theme.PrimaryHover;
            lnkLogin.VisitedLinkColor = _theme.Primary;

            // Loading & Error
            lblLoading.ForeColor = _theme.Primary;
            lblError.ForeColor = _theme.Error;
        }

        private void OnThemeChanged(ThemeService.ThemeMode newTheme)
        {
            ApplyTheme();
        }

        private void TxtConfirmPassword_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                BtnRegister_Click(sender, EventArgs.Empty);
            }
        }

        private async void BtnRegister_Click(object? sender, EventArgs e)
        {
            lblError.Visible = false;
            lblLoading.Visible = true;
            btnRegister.Enabled = false;

            string fullName = txtFullName.Text.Trim();
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;

            // Validation
            if (string.IsNullOrWhiteSpace(fullName))
            {
                ShowError("Vui lòng nhập họ và tên.");
                return;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Vui lòng nhập tên đăng nhập.");
                return;
            }

            if (username.Length < 3)
            {
                ShowError("Tên đăng nhập phải có ít nhất 3 ký tự.");
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("Vui lòng nhập email.");
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowError("Email không hợp lệ.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Vui lòng nhập mật khẩu.");
                return;
            }

            if (password.Length < 6)
            {
                ShowError("Mật khẩu phải có ít nhất 6 ký tự.");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("Mật khẩu xác nhận không khớp.");
                return;
            }

            if (!chkAgreeTerms.Checked)
            {
                ShowError("Vui lòng đồng ý với Điều khoản Sử dụng.");
                return;
            }

            try
            {
                // Attempt sign up with Firebase
                var (success, message, userId) = await _authService.SignUpWithEmailPassword(
                    email, password, username, fullName
                );

                lblLoading.Visible = false;
                btnRegister.Enabled = true;

                if (success && userId != null)
                {
                    // Registration successful
                    MessageBox.Show(
                        $"{message}\n\nBạn có thể đăng nhập ngay bây giờ.",
                        "Thành công",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    // Navigate back to login
                    this.Close();
                }
                else
                {
                    // Registration failed
                    ShowError(message);
                }
            }
            catch (Exception ex)
            {
                lblLoading.Visible = false;
                btnRegister.Enabled = true;
                ShowError($"Lỗi: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            lblLoading.Visible = false;
            lblError.Text = $"❌ {message}";
            lblError.Visible = true;
            btnRegister.Enabled = true;
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

        private void LnkLogin_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Close();
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
