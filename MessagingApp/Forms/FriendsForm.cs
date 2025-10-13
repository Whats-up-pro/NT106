using MessagingApp.Utils;
using System.Data.SqlClient;

namespace MessagingApp.Forms
{
    public partial class FriendsForm : Form
    {
        public FriendsForm()
        {
            InitializeComponent();
            ApplyTheme();
            LoadFriends();
        }

        private void ApplyTheme()
        {
            // Apply theme to form
            ThemeColors.ApplyTheme(this);
            ThemeColors.StylePanel(panelMain, false);

            // Style labels
            ThemeColors.StyleLabel(lblTitle, true);
            lblTitle.ForeColor = ThemeColors.PrimaryLightBlue;

            // Style text boxes
            ThemeColors.StyleTextBox(txtSearch);

            // Style buttons
            ThemeColors.StylePrimaryButton(btnSearch);
            ThemeColors.StylePrimaryButton(btnAddFriend);
            ThemeColors.StyleSecondaryButton(btnClose);

            // Style ListView
            listViewFriends.BackColor = ThemeColors.BackgroundMedium;
            listViewFriends.ForeColor = ThemeColors.White;
            listViewFriends.Font = new Font("Segoe UI", 10F);
        }

        private void LoadFriends()
        {
            try
            {
                listViewFriends.Items.Clear();

                string query = @"SELECT u.UserID, u.Username, u.FullName, u.Email, u.Status
                               FROM Users u
                               INNER JOIN Friendships f ON (f.UserID1 = @UserID AND f.UserID2 = u.UserID) 
                                                        OR (f.UserID2 = @UserID AND f.UserID1 = u.UserID)
                               WHERE f.Status = 'Accepted' AND u.UserID != @UserID
                               ORDER BY u.FullName, u.Username";

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@UserID", CurrentUser.UserID)
                };

                var result = DatabaseConnection.ExecuteQuery(query, parameters);

                foreach (System.Data.DataRow row in result.Rows)
                {
                    string name = !string.IsNullOrEmpty(row["FullName"].ToString()) 
                        ? row["FullName"].ToString()! 
                        : row["Username"].ToString()!;
                    string status = row["Status"].ToString()!;
                    string email = row["Email"].ToString()!;

                    var item = new ListViewItem(name);
                    item.SubItems.Add(status);
                    item.SubItems.Add(email);
                    item.Tag = row["UserID"];

                    listViewFriends.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải danh sách bạn bè: " + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text.Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                LoadFriends();
                return;
            }

            try
            {
                listViewFriends.Items.Clear();

                string query = @"SELECT u.UserID, u.Username, u.FullName, u.Email, u.Status
                               FROM Users u
                               INNER JOIN Friendships f ON (f.UserID1 = @UserID AND f.UserID2 = u.UserID) 
                                                        OR (f.UserID2 = @UserID AND f.UserID1 = u.UserID)
                               WHERE f.Status = 'Accepted' AND u.UserID != @UserID
                               AND (u.Username LIKE @Search OR u.FullName LIKE @Search OR u.Email LIKE @Search)
                               ORDER BY u.FullName, u.Username";

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@UserID", CurrentUser.UserID),
                    new SqlParameter("@Search", "%" + searchText + "%")
                };

                var result = DatabaseConnection.ExecuteQuery(query, parameters);

                foreach (System.Data.DataRow row in result.Rows)
                {
                    string name = !string.IsNullOrEmpty(row["FullName"].ToString()) 
                        ? row["FullName"].ToString()! 
                        : row["Username"].ToString()!;
                    string status = row["Status"].ToString()!;
                    string email = row["Email"].ToString()!;

                    var item = new ListViewItem(name);
                    item.SubItems.Add(status);
                    item.SubItems.Add(email);
                    item.Tag = row["UserID"];

                    listViewFriends.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tìm kiếm: " + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddFriend_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Tính năng thêm bạn đang được phát triển!", "Thông Báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
