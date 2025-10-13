using MessagingApp.Utils;
using System.Data.SqlClient;

namespace MessagingApp.Forms
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            ApplyTheme();
            LoadUserInfo();
        }

        private void ApplyTheme()
        {
            // Apply theme to form
            ThemeColors.ApplyTheme(this);
            ThemeColors.StylePanel(panelSidebar);
            ThemeColors.StylePanel(panelContent, false);

            // Style labels
            ThemeColors.StyleLabel(lblUserName, true);
            lblUserName.ForeColor = ThemeColors.PrimaryLightBlue;
            ThemeColors.StyleLabel(lblWelcome, true);
            lblWelcome.ForeColor = ThemeColors.White;

            // Style sidebar buttons
            StyleSidebarButton(btnMessages);
            StyleSidebarButton(btnFriends);
            StyleSidebarButton(btnCalls);
            StyleSidebarButton(btnProfile);
            StyleSidebarButton(btnLogout);

            // Style ListView
            listViewConversations.BackColor = ThemeColors.BackgroundMedium;
            listViewConversations.ForeColor = ThemeColors.White;
            listViewConversations.Font = new Font("Segoe UI", 10F);
        }

        private void StyleSidebarButton(Button btn)
        {
            btn.BackColor = ThemeColors.BackgroundDark;
            btn.ForeColor = ThemeColors.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ThemeColors.PrimaryDarkBlue;
            btn.Cursor = Cursors.Hand;
            btn.Font = new Font("Segoe UI", 11F);
        }

        private void LoadUserInfo()
        {
            lblUserName.Text = string.IsNullOrEmpty(CurrentUser.FullName) ? CurrentUser.Username : CurrentUser.FullName;
            lblWelcome.Text = $"Chào mừng, {lblUserName.Text}!";
        }

        private void btnMessages_Click(object sender, EventArgs e)
        {
            var messageForm = new MessageForm();
            messageForm.ShowDialog();
        }

        private void btnFriends_Click(object sender, EventArgs e)
        {
            var friendsForm = new FriendsForm();
            friendsForm.ShowDialog();
        }

        private void btnCalls_Click(object sender, EventArgs e)
        {
            var callForm = new CallForm();
            callForm.ShowDialog();
        }

        private void btnProfile_Click(object sender, EventArgs e)
        {
            var profileForm = new ProfileForm();
            profileForm.ShowDialog();
            LoadUserInfo(); // Refresh user info after profile update
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Xác Nhận Đăng Xuất",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Update user status to offline
                try
                {
                    string query = "UPDATE Users SET Status = 'Offline' WHERE UserID = @UserID";
                    DatabaseConnection.ExecuteNonQuery(query, new SqlParameter[] 
                    { 
                        new SqlParameter("@UserID", CurrentUser.UserID) 
                    });
                }
                catch { }

                // Clear current user info
                CurrentUser.UserID = 0;
                CurrentUser.Username = string.Empty;
                CurrentUser.FullName = string.Empty;

                // Show login form
                this.Hide();
                var loginForm = new LoginForm();
                loginForm.ShowDialog();
                this.Close();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Update user status to offline when closing
            try
            {
                if (CurrentUser.UserID > 0)
                {
                    string query = "UPDATE Users SET Status = 'Offline' WHERE UserID = @UserID";
                    DatabaseConnection.ExecuteNonQuery(query, new SqlParameter[] 
                    { 
                        new SqlParameter("@UserID", CurrentUser.UserID) 
                    });
                }
            }
            catch { }
        }
    }
}
