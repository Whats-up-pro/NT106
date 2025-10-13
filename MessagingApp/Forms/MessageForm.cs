using MessagingApp.Utils;

namespace MessagingApp.Forms
{
    public partial class MessageForm : Form
    {
        public MessageForm()
        {
            InitializeComponent();
            ApplyTheme();
            LoadMessages();
        }

        private void ApplyTheme()
        {
            // Apply theme to form
            ThemeColors.ApplyTheme(this);
            ThemeColors.StylePanel(panelMain, false);

            // Style labels
            ThemeColors.StyleLabel(lblTitle, true);
            lblTitle.ForeColor = ThemeColors.PrimaryLightBlue;
            ThemeColors.StyleLabel(lblRecipient);

            // Style text boxes
            ThemeColors.StyleTextBox(txtMessage);

            // Style buttons
            ThemeColors.StylePrimaryButton(btnSend);
            ThemeColors.StyleSecondaryButton(btnClose);

            // Style ListBox
            listBoxMessages.BackColor = ThemeColors.BackgroundMedium;
            listBoxMessages.ForeColor = ThemeColors.White;
            listBoxMessages.Font = new Font("Segoe UI", 10F);
        }

        private void LoadMessages()
        {
            // This is a simplified version. In a real app, you would load messages from database
            listBoxMessages.Items.Clear();
            listBoxMessages.Items.Add("💬 Chào mừng bạn đến với tính năng tin nhắn!");
            listBoxMessages.Items.Add("💡 Chọn bạn bè từ danh sách để bắt đầu trò chuyện.");
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string message = txtMessage.Text.Trim();

            if (string.IsNullOrEmpty(message))
            {
                MessageBox.Show("Vui lòng nhập tin nhắn!", "Thông Báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Add message to list (simplified - in real app, save to database)
            listBoxMessages.Items.Add($"Bạn: {message}");
            txtMessage.Clear();

            // Scroll to bottom
            listBoxMessages.TopIndex = listBoxMessages.Items.Count - 1;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
