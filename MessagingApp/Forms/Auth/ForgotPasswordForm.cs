using MessagingApp.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MessagingApp.Forms.Auth
{
    public partial class ForgotPasswordForm : Form
    {
        private readonly ThemeService _theme = ThemeService.Instance;
        private readonly FirebaseAuthService _authService = FirebaseAuthService.Instance;

        // UI Controls
        private Panel pnlMain = null!;
        private Panel pnlResetCard = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Label lblEmail = null!;
        private TextBox txtEmail = null!;
        private Button btnSendReset = null!;
        private LinkLabel lnkBackToLogin = null!;
        private Label lblLoading = null!;
        private Label lblMessage = null!;

        public ForgotPasswordForm()
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
            this.ClientSize = new System.Drawing.Size(900, 500);
            this.Name = "ForgotPasswordForm";
            this.Text = "Quên Mật Khẩu - Messaging App";
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

            // Reset card panel
            pnlResetCard = new Panel
            {
                Width = 450,
                Height = 400,
                Location = new Point((this.ClientSize.Width - 450) / 2, (this.ClientSize.Height - 400) / 2)
            };
            pnlMain.Controls.Add(pnlResetCard);

            int yPos = 30;

            // Title
            lblTitle = new Label
            {
                Text = "Quên Mật Khẩu?",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30, yPos)
            };
            pnlResetCard.Controls.Add(lblTitle);
            yPos += 50;

            // Subtitle
            lblSubtitle = new Label
            {
                Text = "Nhập email của bạn để nhận liên kết đặt lại mật khẩu",
                Font = new Font("Segoe UI", 10F),
                AutoSize = false,
                Width = 390,
                Height = 40,
                Location = new Point(30, yPos)
            };
            pnlResetCard.Controls.Add(lblSubtitle);
            yPos += 60;

            // Email label
            lblEmail = new Label
            {
                Text = "Email",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30, yPos)
            };
            pnlResetCard.Controls.Add(lblEmail);
            yPos += 30;

            // Email textbox
            txtEmail = new TextBox
            {
                Width = 390,
                Height = 35,
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 11F),
                PlaceholderText = "email@example.com"
            };
            txtEmail.KeyPress += TxtEmail_KeyPress;
            pnlResetCard.Controls.Add(txtEmail);
            yPos += 60;

            // Send reset button
            btnSendReset = new Button
            {
                Text = "Gửi Email Khôi Phục",
                Width = 390,
                Height = 45,
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSendReset.Click += BtnSendReset_Click;
            pnlResetCard.Controls.Add(btnSendReset);
            yPos += 60;

            // Loading label
            lblLoading = new Label
            {
                Text = "⏳ Đang gửi email...",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(30, yPos),
                Visible = false
            };
            pnlResetCard.Controls.Add(lblLoading);

            // Message label (for both success and error)
            lblMessage = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9.5F),
                AutoSize = false,
                Width = 390,
                Height = 60,
                Location = new Point(30, yPos),
                Visible = false
            };
            pnlResetCard.Controls.Add(lblMessage);
            yPos += 70;

            // Back to login link
            lnkBackToLogin = new LinkLabel
            {
                Text = "← Quay lại Đăng nhập",
                AutoSize = true,
                Location = new Point(30, yPos),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            lnkBackToLogin.LinkClicked += LnkBackToLogin_LinkClicked;
            pnlResetCard.Controls.Add(lnkBackToLogin);
        }

        private void ApplyTheme()
        {
            this.BackColor = _theme.Background;
            pnlMain.BackColor = _theme.Background;
            pnlResetCard.BackColor = _theme.Surface;

            lblTitle.ForeColor = _theme.TextPrimary;
            lblSubtitle.ForeColor = _theme.TextSecondary;
            lblEmail.ForeColor = _theme.TextPrimary;

            txtEmail.BackColor = _theme.Surface;
            txtEmail.ForeColor = _theme.TextPrimary;

            _theme.StyleButton(btnSendReset, isPrimary: true);

            lnkBackToLogin.LinkColor = _theme.Primary;
            lnkBackToLogin.ActiveLinkColor = _theme.PrimaryHover;
            lnkBackToLogin.VisitedLinkColor = _theme.Primary;

            lblLoading.ForeColor = _theme.Primary;
        }

        private void OnThemeChanged(ThemeService.ThemeMode newTheme)
        {
            ApplyTheme();
        }

        private void TxtEmail_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                BtnSendReset_Click(sender, EventArgs.Empty);
            }
        }

        private async void BtnSendReset_Click(object? sender, EventArgs e)
        {
            lblMessage.Visible = false;
            lblLoading.Visible = true;
            btnSendReset.Enabled = false;

            string email = txtEmail.Text.Trim();

            // Validation
            if (string.IsNullOrWhiteSpace(email))
            {
                ShowMessage("Vui lòng nhập email.", isError: true);
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowMessage("Email không hợp lệ.", isError: true);
                return;
            }

            try
            {
                // Send password reset email
                var (success, message) = await _authService.SendPasswordResetEmail(email);

                lblLoading.Visible = false;
                btnSendReset.Enabled = true;

                if (success)
                {
                    ShowMessage(
                        $"✅ {message}\n\nLưu ý: Đây là demo, link reset password sẽ được in ra console.\nTrong production, email sẽ được gửi thật.",
                        isError: false
                    );

                    // Optionally navigate back after delay
                    await System.Threading.Tasks.Task.Delay(3000);
                    this.Close();
                }
                else
                {
                    ShowMessage(message, isError: true);
                }
            }
            catch (Exception ex)
            {
                lblLoading.Visible = false;
                btnSendReset.Enabled = true;
                ShowMessage($"Lỗi: {ex.Message}", isError: true);
            }
        }

        private void ShowMessage(string message, bool isError)
        {
            lblLoading.Visible = false;
            lblMessage.Text = isError ? $"❌ {message}" : message;
            lblMessage.ForeColor = isError ? _theme.Error : _theme.Success;
            lblMessage.Visible = true;
            btnSendReset.Enabled = true;
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

        private void LnkBackToLogin_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
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
