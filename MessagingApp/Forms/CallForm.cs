using MessagingApp.Utils;
using System.Data.SqlClient;

namespace MessagingApp.Forms
{
    public partial class CallForm : Form
    {
        public CallForm()
        {
            InitializeComponent();
            ApplyTheme();
            LoadCallHistory();
        }

        private void ApplyTheme()
        {
            // Apply theme to form
            ThemeColors.ApplyTheme(this);
            ThemeColors.StylePanel(panelMain, false);

            // Style labels
            ThemeColors.StyleLabel(lblTitle, true);
            lblTitle.ForeColor = ThemeColors.PrimaryLightBlue;

            // Style buttons
            ThemeColors.StylePrimaryButton(btnVoiceCall);
            ThemeColors.StylePrimaryButton(btnVideoCall);
            ThemeColors.StyleSecondaryButton(btnClose);

            // Style ListView
            listViewCallHistory.BackColor = ThemeColors.BackgroundMedium;
            listViewCallHistory.ForeColor = ThemeColors.White;
            listViewCallHistory.Font = new Font("Segoe UI", 10F);
        }

        private void LoadCallHistory()
        {
            try
            {
                listViewCallHistory.Items.Clear();

                string query = @"SELECT 
                                    CASE 
                                        WHEN ch.CallerID = @UserID THEN u2.FullName
                                        ELSE u1.FullName
                                    END AS ContactName,
                                    CASE 
                                        WHEN ch.CallerID = @UserID THEN u2.Username
                                        ELSE u1.Username
                                    END AS ContactUsername,
                                    ch.CallType,
                                    ch.Status,
                                    ch.Duration,
                                    ch.StartTime
                               FROM CallHistory ch
                               LEFT JOIN Users u1 ON ch.CallerID = u1.UserID
                               LEFT JOIN Users u2 ON ch.ReceiverID = u2.UserID
                               WHERE ch.CallerID = @UserID OR ch.ReceiverID = @UserID
                               ORDER BY ch.StartTime DESC";

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@UserID", CurrentUser.UserID)
                };

                var result = DatabaseConnection.ExecuteQuery(query, parameters);

                foreach (System.Data.DataRow row in result.Rows)
                {
                    string contact = !string.IsNullOrEmpty(row["ContactName"].ToString()) 
                        ? row["ContactName"].ToString()! 
                        : row["ContactUsername"].ToString()!;
                    string callType = row["CallType"].ToString()!;
                    string status = row["Status"].ToString()!;
                    int duration = Convert.ToInt32(row["Duration"]);
                    DateTime startTime = Convert.ToDateTime(row["StartTime"]);

                    string durationStr = duration > 0 ? $"{duration / 60}:{duration % 60:D2}" : "0:00";

                    var item = new ListViewItem(contact);
                    item.SubItems.Add(callType == "Voice" ? "📞 Thoại" : "📹 Video");
                    item.SubItems.Add(GetStatusText(status));
                    item.SubItems.Add(durationStr);
                    item.SubItems.Add(startTime.ToString("dd/MM/yyyy HH:mm"));

                    listViewCallHistory.Items.Add(item);
                }

                if (listViewCallHistory.Items.Count == 0)
                {
                    var item = new ListViewItem("Chưa có cuộc gọi nào");
                    item.SubItems.Add("");
                    item.SubItems.Add("");
                    item.SubItems.Add("");
                    item.SubItems.Add("");
                    listViewCallHistory.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải lịch sử cuộc gọi: " + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetStatusText(string status)
        {
            return status switch
            {
                "Completed" => "✅ Hoàn thành",
                "Missed" => "❌ Nhỡ",
                "Rejected" => "🚫 Từ chối",
                "Failed" => "⚠️ Thất bại",
                _ => status
            };
        }

        private void btnVoiceCall_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Tính năng gọi thoại đang được phát triển!", "Thông Báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnVideoCall_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Tính năng gọi video đang được phát triển!", "Thông Báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
