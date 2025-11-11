using MessagingApp.Services;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MessagingApp.Forms.Messaging
{
    public partial class MessageForm : Form
    {
        private readonly ThemeService _theme = ThemeService.Instance;
        private readonly FirestoreMessagingService _messagingService = FirestoreMessagingService.Instance;
        private readonly FirebaseAuthService _authService = FirebaseAuthService.Instance;

        private readonly string _conversationId;
        private readonly Dictionary<string, object> _friendData;
        private FirestoreChangeListener? _messageListener;

        private Panel pnlHeader = null!;
        private Label lblFriendName = null!;
        private Label lblFriendStatus = null!;
        private RichTextBox rtbMessages = null!;
        private TextBox txtMessage = null!;
        private Button btnSend = null!;

        public MessageForm(string conversationId, Dictionary<string, object> friendData)
        {
            _conversationId = conversationId;
            _friendData = friendData;

            InitializeComponent();
            InitializeCustomUI();
            ApplyTheme();
            LoadMessages();
            StartMessageListener();

            _theme.OnThemeChanged += OnThemeChanged;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Name = "MessageForm";
            this.Text = "Tin Nhắn - Messaging App";
            this.StartPosition = FormStartPosition.CenterScreen;

            this.ResumeLayout(false);
        }

        private void InitializeCustomUI()
        {
            // Header panel
            pnlHeader = new Panel
            {
                Height = 80,
                Dock = DockStyle.Top,
                Padding = new Padding(20, 15, 20, 15)
            };
            this.Controls.Add(pnlHeader);

            // Friend name
            string friendName = _friendData.ContainsKey("fullName") && !string.IsNullOrEmpty(_friendData["fullName"].ToString())
                ? _friendData["fullName"].ToString()!
                : _friendData["username"].ToString()!;

            lblFriendName = new Label
            {
                Text = friendName,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 5)
            };
            pnlHeader.Controls.Add(lblFriendName);

            // Friend status
            string status = _friendData.ContainsKey("status") ? _friendData["status"].ToString()! : "offline";
            string statusText = status == "online" ? "🟢 Đang hoạt động" : "⚫ Không hoạt động";

            lblFriendStatus = new Label
            {
                Text = statusText,
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(0, 40)
            };
            pnlHeader.Controls.Add(lblFriendStatus);

            // Messages display
            rtbMessages = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                Padding = new Padding(10)
            };
            this.Controls.Add(rtbMessages);

            // Input panel
            Panel pnlInput = new Panel
            {
                Height = 70,
                Dock = DockStyle.Bottom,
                Padding = new Padding(20, 15, 20, 15)
            };
            this.Controls.Add(pnlInput);

            // Message textbox
            txtMessage = new TextBox
            {
                Width = 630,
                Height = 40,
                Location = new Point(0, 5),
                Font = new Font("Segoe UI", 11F),
                PlaceholderText = "Nhập tin nhắn...",
                Multiline = false
            };
            txtMessage.KeyPress += TxtMessage_KeyPress;
            pnlInput.Controls.Add(txtMessage);

            // Send button
            btnSend = new Button
            {
                Text = "📤 Gửi",
                Width = 100,
                Height = 40,
                Location = new Point(640, 5),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSend.Click += BtnSend_Click;
            pnlInput.Controls.Add(btnSend);
        }

        private void ApplyTheme()
        {
            this.BackColor = _theme.Background;

            pnlHeader.BackColor = _theme.Surface;
            lblFriendName.ForeColor = _theme.TextPrimary;
            
            string status = _friendData.ContainsKey("status") ? _friendData["status"].ToString()! : "offline";
            lblFriendStatus.ForeColor = status == "online" ? _theme.Success : _theme.TextSecondary;

            rtbMessages.BackColor = _theme.Background;
            rtbMessages.ForeColor = _theme.TextPrimary;

            _theme.StyleTextBox(txtMessage);
            _theme.StyleButton(btnSend, isPrimary: true);
        }

        private void OnThemeChanged(ThemeService.ThemeMode newTheme)
        {
            ApplyTheme();
        }

        private async void LoadMessages()
        {
            try
            {
                var messages = await _messagingService.GetMessages(_conversationId);
                DisplayMessages(messages);

                // Mark as read
                string? currentUserId = _authService.CurrentUserId;
                if (currentUserId != null)
                {
                    await _messagingService.MarkMessagesAsRead(_conversationId, currentUserId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải tin nhắn: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartMessageListener()
        {
            _messageListener = _messagingService.ListenToMessages(_conversationId, messages =>
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => DisplayMessages(messages)));
                }
                else
                {
                    DisplayMessages(messages);
                }
            });
        }

        private void DisplayMessages(List<Dictionary<string, object>> messages)
        {
            rtbMessages.Clear();
            string? currentUserId = _authService.CurrentUserId;

            foreach (var msg in messages)
            {
                string senderId = msg["senderId"].ToString()!;
                string content = msg["content"].ToString()!;
                bool isCurrentUser = senderId == currentUserId;

                // Format timestamp
                string timeText = "";
                if (msg.ContainsKey("timestamp") && msg["timestamp"] != null)
                {
                    try
                    {
                        var timestamp = (Google.Cloud.Firestore.Timestamp)msg["timestamp"];
                        var dateTime = timestamp.ToDateTime().ToLocalTime();
                        timeText = dateTime.ToString("HH:mm");
                    }
                    catch { }
                }

                // Display message
                if (isCurrentUser)
                {
                    // Current user's message (right-aligned)
                    rtbMessages.SelectionAlignment = HorizontalAlignment.Right;
                    rtbMessages.SelectionColor = _theme.TextPrimary;
                    rtbMessages.SelectionBackColor = _theme.Primary;
                    rtbMessages.AppendText($"{content}");
                    rtbMessages.SelectionBackColor = _theme.Background;
                    rtbMessages.SelectionColor = _theme.TextSecondary;
                    rtbMessages.AppendText($" {timeText}\n\n");
                }
                else
                {
                    // Friend's message (left-aligned)
                    rtbMessages.SelectionAlignment = HorizontalAlignment.Left;
                    rtbMessages.SelectionColor = _theme.TextPrimary;
                    rtbMessages.SelectionBackColor = _theme.Surface;
                    rtbMessages.AppendText($"{content}");
                    rtbMessages.SelectionBackColor = _theme.Background;
                    rtbMessages.SelectionColor = _theme.TextSecondary;
                    rtbMessages.AppendText($" {timeText}\n\n");
                }
            }

            // Scroll to bottom
            rtbMessages.SelectionStart = rtbMessages.Text.Length;
            rtbMessages.ScrollToCaret();
        }

        private void TxtMessage_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                BtnSend_Click(sender, EventArgs.Empty);
            }
        }

        private async void BtnSend_Click(object? sender, EventArgs e)
        {
            string content = txtMessage.Text.Trim();

            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            try
            {
                string? currentUserId = _authService.CurrentUserId;
                if (currentUserId == null)
                {
                    MessageBox.Show("Vui lòng đăng nhập lại!", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                btnSend.Enabled = false;
                txtMessage.Enabled = false;

                var (success, message) = await _messagingService.SendMessage(_conversationId, currentUserId, content);

                if (!success)
                {
                    MessageBox.Show(message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                txtMessage.Clear();
                btnSend.Enabled = true;
                txtMessage.Enabled = true;
                txtMessage.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi gửi tin nhắn: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                btnSend.Enabled = true;
                txtMessage.Enabled = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _messageListener?.StopAsync();
                _theme.OnThemeChanged -= OnThemeChanged;
            }
            base.Dispose(disposing);
        }
    }
}
