using MessagingApp.Utils;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace MessagingApp.Forms
{
    public partial class ForgotPasswordForm : Form
    {
        public ForgotPasswordForm()
        {
            InitializeComponent();
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            // Apply theme to form
            ThemeColors.ApplyTheme(this);
            ThemeColors.StylePanel(panelMain, false);

            // Style labels
            ThemeColors.StyleLabel(lblTitle, true);
            lblTitle.ForeColor = ThemeColors.PrimaryLightBlue;
            ThemeColors.StyleLabel(lblInstruction);
            ThemeColors.StyleLabel(lblEmail);
            ThemeColors.StyleLabel(lblNewPassword);
            ThemeColors.StyleLabel(lblConfirmPassword);

            // Style text boxes
            ThemeColors.StyleTextBox(txtEmail);
            ThemeColors.StyleTextBox(txtNewPassword);
            ThemeColors.StyleTextBox(txtConfirmPassword);

            // Style buttons
            ThemeColors.StylePrimaryButton(btnReset);
            ThemeColors.StyleSecondaryButton(btnCancel);
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string newPassword = txtNewPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;

            // Validation
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                lblMessage.Text = "Vui lòng nhập đầy đủ thông tin!";
                lblMessage.ForeColor = ThemeColors.ErrorRed;
                return;
            }

            if (!IsValidEmail(email))
            {
                lblMessage.Text = "Email không hợp lệ!";
                lblMessage.ForeColor = ThemeColors.ErrorRed;
                return;
            }

            if (newPassword.Length < 6)
            {
                lblMessage.Text = "Mật khẩu phải có ít nhất 6 ký tự!";
                lblMessage.ForeColor = ThemeColors.ErrorRed;
                return;
            }

            if (newPassword != confirmPassword)
            {
                lblMessage.Text = "Mật khẩu xác nhận không khớp!";
                lblMessage.ForeColor = ThemeColors.ErrorRed;
                return;
            }

            try
            {
                // Check if email exists
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
                var checkParams = new SqlParameter[]
                {
                    new SqlParameter("@Email", email)
                };

                int count = Convert.ToInt32(DatabaseConnection.ExecuteScalar(checkQuery, checkParams));

                if (count == 0)
                {
                    lblMessage.Text = "Email không tồn tại trong hệ thống!";
                    lblMessage.ForeColor = ThemeColors.ErrorRed;
                    return;
                }

                // Hash new password
                string passwordHash = PasswordHelper.HashPassword(newPassword);

                // Update password
                string updateQuery = "UPDATE Users SET PasswordHash = @PasswordHash WHERE Email = @Email";
                var updateParams = new SqlParameter[]
                {
                    new SqlParameter("@PasswordHash", passwordHash),
                    new SqlParameter("@Email", email)
                };

                int result = DatabaseConnection.ExecuteNonQuery(updateQuery, updateParams);

                if (result > 0)
                {
                    MessageBox.Show("Đặt lại mật khẩu thành công! Vui lòng đăng nhập.", "Thành Công",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
                else
                {
                    lblMessage.Text = "Đặt lại mật khẩu thất bại. Vui lòng thử lại!";
                    lblMessage.ForeColor = ThemeColors.ErrorRed;
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Lỗi: " + ex.Message;
                lblMessage.ForeColor = ThemeColors.ErrorRed;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private bool IsValidEmail(string email)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }
    }
}
