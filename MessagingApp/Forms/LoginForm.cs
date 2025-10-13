using MessagingApp.Utils;
using System.Data.SqlClient;

namespace MessagingApp.Forms
{
    public partial class LoginForm : Form
    {
        public LoginForm()
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
            ThemeColors.StyleLabel(lblPassword);

            // Style text boxes
            ThemeColors.StyleTextBox(txtUsername);
            ThemeColors.StyleTextBox(txtPassword);

            // Style buttons
            ThemeColors.StylePrimaryButton(btnLogin);
            ThemeColors.StyleSecondaryButton(btnRegister);
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblMessage.Text = "Vui lòng nhập đầy đủ thông tin!";
                lblMessage.ForeColor = ThemeColors.ErrorRed;
                return;
            }

            try
            {
                // Hash password
                string passwordHash = PasswordHelper.HashPassword(password);

                // Query to check user credentials
                string query = @"SELECT UserID, Username, FullName 
                               FROM Users 
                               WHERE (Username = @Username OR Email = @Username) 
                               AND PasswordHash = @PasswordHash 
                               AND IsActive = 1";

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Username", username),
                    new SqlParameter("@PasswordHash", passwordHash)
                };

                var result = DatabaseConnection.ExecuteQuery(query, parameters);

                if (result.Rows.Count > 0)
                {
                    // Update last login
                    int userId = Convert.ToInt32(result.Rows[0]["UserID"]);
                    string updateQuery = "UPDATE Users SET LastLogin = GETDATE(), Status = 'Online' WHERE UserID = @UserID";
                    DatabaseConnection.ExecuteNonQuery(updateQuery, new SqlParameter[] { new SqlParameter("@UserID", userId) });

                    // Store current user info
                    CurrentUser.UserID = userId;
                    CurrentUser.Username = result.Rows[0]["Username"].ToString() ?? "";
                    CurrentUser.FullName = result.Rows[0]["FullName"].ToString() ?? "";

                    lblMessage.Text = "Đăng nhập thành công!";
                    lblMessage.ForeColor = ThemeColors.SuccessGreen;

                    // Open main form
                    this.Hide();
                    var mainForm = new MainForm();
                    mainForm.FormClosed += (s, args) => this.Close();
                    mainForm.Show();
                }
                else
                {
                    lblMessage.Text = "Tên đăng nhập hoặc mật khẩu không đúng!";
                    lblMessage.ForeColor = ThemeColors.ErrorRed;
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Lỗi: " + ex.Message;
                lblMessage.ForeColor = ThemeColors.ErrorRed;
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            var registerForm = new RegisterForm();
            registerForm.ShowDialog();
        }

        private void lnkForgotPassword_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var forgotPasswordForm = new ForgotPasswordForm();
            forgotPasswordForm.ShowDialog();
        }
    }

    // Static class to store current user information
    public static class CurrentUser
    {
        public static int UserID { get; set; }
        public static string Username { get; set; } = string.Empty;
        public static string FullName { get; set; } = string.Empty;
    }
}
