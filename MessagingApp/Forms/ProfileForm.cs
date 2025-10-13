using MessagingApp.Utils;
using System.Data.SqlClient;

namespace MessagingApp.Forms
{
    public partial class ProfileForm : Form
    {
        public ProfileForm()
        {
            InitializeComponent();
            ApplyTheme();
            LoadUserProfile();
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
            ThemeColors.StyleLabel(lblFullName);
            ThemeColors.StyleLabel(lblPhoneNumber);
            ThemeColors.StyleLabel(lblBio);
            ThemeColors.StyleLabel(lblStatus);

            // Style text boxes
            ThemeColors.StyleTextBox(txtUsername);
            ThemeColors.StyleTextBox(txtEmail);
            ThemeColors.StyleTextBox(txtFullName);
            ThemeColors.StyleTextBox(txtPhoneNumber);
            ThemeColors.StyleTextBox(txtBio);

            // Style combo box
            cmbStatus.BackColor = ThemeColors.BackgroundMedium;
            cmbStatus.ForeColor = ThemeColors.White;
            cmbStatus.Font = new Font("Segoe UI", 10F);

            // Style buttons
            ThemeColors.StylePrimaryButton(btnSave);
            ThemeColors.StyleSecondaryButton(btnCancel);
        }

        private void LoadUserProfile()
        {
            try
            {
                string query = @"SELECT Username, Email, FullName, PhoneNumber, Bio, Status 
                               FROM Users WHERE UserID = @UserID";

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@UserID", CurrentUser.UserID)
                };

                var result = DatabaseConnection.ExecuteQuery(query, parameters);

                if (result.Rows.Count > 0)
                {
                    var row = result.Rows[0];
                    txtUsername.Text = row["Username"].ToString();
                    txtEmail.Text = row["Email"].ToString();
                    txtFullName.Text = row["FullName"].ToString();
                    txtPhoneNumber.Text = row["PhoneNumber"].ToString();
                    txtBio.Text = row["Bio"].ToString();
                    cmbStatus.SelectedItem = row["Status"].ToString();
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Lỗi khi tải thông tin: " + ex.Message;
                lblMessage.ForeColor = ThemeColors.ErrorRed;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string fullName = txtFullName.Text.Trim();
            string phoneNumber = txtPhoneNumber.Text.Trim();
            string bio = txtBio.Text.Trim();
            string status = cmbStatus.SelectedItem?.ToString() ?? "Offline";

            try
            {
                string updateQuery = @"UPDATE Users 
                                     SET Email = @Email, 
                                         FullName = @FullName, 
                                         PhoneNumber = @PhoneNumber, 
                                         Bio = @Bio, 
                                         Status = @Status 
                                     WHERE UserID = @UserID";

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Email", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email),
                    new SqlParameter("@FullName", string.IsNullOrEmpty(fullName) ? (object)DBNull.Value : fullName),
                    new SqlParameter("@PhoneNumber", string.IsNullOrEmpty(phoneNumber) ? (object)DBNull.Value : phoneNumber),
                    new SqlParameter("@Bio", string.IsNullOrEmpty(bio) ? (object)DBNull.Value : bio),
                    new SqlParameter("@Status", status),
                    new SqlParameter("@UserID", CurrentUser.UserID)
                };

                int result = DatabaseConnection.ExecuteNonQuery(updateQuery, parameters);

                if (result > 0)
                {
                    // Update CurrentUser info
                    CurrentUser.FullName = fullName;

                    MessageBox.Show("Cập nhật thông tin thành công!", "Thành Công",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
                else
                {
                    lblMessage.Text = "Cập nhật thất bại!";
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
    }
}
