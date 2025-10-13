using MessagingApp.Utils;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace MessagingApp.Forms
{
    public partial class RegisterForm : Form
    {
        public RegisterForm()
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
            ThemeColors.StyleLabel(lblUsername);
            ThemeColors.StyleLabel(lblEmail);
            ThemeColors.StyleLabel(lblPassword);
            ThemeColors.StyleLabel(lblConfirmPassword);
            ThemeColors.StyleLabel(lblFullName);
            ThemeColors.StyleLabel(lblPhoneNumber);

            // Style text boxes
            ThemeColors.StyleTextBox(txtUsername);
            ThemeColors.StyleTextBox(txtEmail);
            ThemeColors.StyleTextBox(txtPassword);
            ThemeColors.StyleTextBox(txtConfirmPassword);
            ThemeColors.StyleTextBox(txtFullName);
            ThemeColors.StyleTextBox(txtPhoneNumber);

            // Style buttons
            ThemeColors.StylePrimaryButton(btnRegister);
            ThemeColors.StyleSecondaryButton(btnCancel);
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;
            string fullName = txtFullName.Text.Trim();
            string phoneNumber = txtPhoneNumber.Text.Trim();

            // Validation
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || 
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                lblMessage.Text = "Vui lòng nhập đầy đủ thông tin bắt buộc!";
                lblMessage.ForeColor = ThemeColors.ErrorRed;
                return;
            }

            if (username.Length < 3)
            {
                lblMessage.Text = "Tên đăng nhập phải có ít nhất 3 ký tự!";
                lblMessage.ForeColor = ThemeColors.ErrorRed;
                return;
            }

            if (!IsValidEmail(email))
            {
                lblMessage.Text = "Email không hợp lệ!";
                lblMessage.ForeColor = ThemeColors.ErrorRed;
                return;
            }

            if (password.Length < 6)
            {
                lblMessage.Text = "Mật khẩu phải có ít nhất 6 ký tự!";
                lblMessage.ForeColor = ThemeColors.ErrorRed;
                return;
            }

            if (password != confirmPassword)
            {
                lblMessage.Text = "Mật khẩu xác nhận không khớp!";
                lblMessage.ForeColor = ThemeColors.ErrorRed;
                return;
            }

            try
            {
                // Check if username or email already exists
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username OR Email = @Email";
                var checkParams = new SqlParameter[]
                {
                    new SqlParameter("@Username", username),
                    new SqlParameter("@Email", email)
                };

                int count = Convert.ToInt32(DatabaseConnection.ExecuteScalar(checkQuery, checkParams));

                if (count > 0)
                {
                    lblMessage.Text = "Tên đăng nhập hoặc email đã tồn tại!";
                    lblMessage.ForeColor = ThemeColors.ErrorRed;
                    return;
                }

                // Hash password
                string passwordHash = PasswordHelper.HashPassword(password);

                // Insert new user
                string insertQuery = @"INSERT INTO Users (Username, Email, PasswordHash, FullName, PhoneNumber, Status, CreatedAt, IsActive)
                                     VALUES (@Username, @Email, @PasswordHash, @FullName, @PhoneNumber, 'Offline', GETDATE(), 1)";

                var insertParams = new SqlParameter[]
                {
                    new SqlParameter("@Username", username),
                    new SqlParameter("@Email", email),
                    new SqlParameter("@PasswordHash", passwordHash),
                    new SqlParameter("@FullName", string.IsNullOrEmpty(fullName) ? (object)DBNull.Value : fullName),
                    new SqlParameter("@PhoneNumber", string.IsNullOrEmpty(phoneNumber) ? (object)DBNull.Value : phoneNumber)
                };

                int result = DatabaseConnection.ExecuteNonQuery(insertQuery, insertParams);

                if (result > 0)
                {
                    MessageBox.Show("Đăng ký thành công! Vui lòng đăng nhập.", "Thành Công", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
                else
                {
                    lblMessage.Text = "Đăng ký thất bại. Vui lòng thử lại!";
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
