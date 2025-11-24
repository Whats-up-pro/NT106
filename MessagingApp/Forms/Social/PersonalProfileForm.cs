using MessagingApp.Services;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;

namespace MessagingApp.Forms.Social
{
    public partial class PersonalProfileForm : Form
    {
        private readonly ThemeService _theme = ThemeService.Instance;
        private readonly FirebaseAuthService _auth = FirebaseAuthService.Instance;
        private readonly FirestoreFriendsService _friendsService = FirestoreFriendsService.Instance;

        private PictureBox pictureBox1 = null!;
        private Label label1 = null!;
        private Label label2 = null!;
        private TextBox textBox1 = null!;
        private Button button1 = null!;
        private ListView listView1 = null!;

        private string? selectedAvatarPath;

        public PersonalProfileForm()
        {
            InitializeComponent();
            InitializeUserProfile();
            ApplyTheme();
            _theme.OnThemeChanged += OnThemeChanged;

            // Load data async after shown
            this.Shown += async (_, __) => await LoadUserData();
        }

        private void InitializeComponent()
        {
            this.pictureBox1 = new PictureBox();
            this.label1 = new Label();
            this.label2 = new Label();
            this.textBox1 = new TextBox();
            this.button1 = new Button();
            this.listView1 = new ListView();

            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();

            // pictureBox1
            this.pictureBox1.Location = new Point(135, 20);
            this.pictureBox1.Size = new Size(100, 100);
            this.pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBox1.BorderStyle = BorderStyle.FixedSingle;
            this.pictureBox1.BackColor = Color.White;

            // label1 - tên tài khoản
            this.label1.Location = new Point(20, 130);
            this.label1.Size = new Size(330, 30);
            this.label1.TextAlign = ContentAlignment.MiddleCenter;
            this.label1.Font = new Font("Segoe UI", 14F, FontStyle.Bold);

            // label2 - trạng thái
            this.label2.Location = new Point(20, 165);
            this.label2.Size = new Size(330, 20);
            this.label2.TextAlign = ContentAlignment.MiddleCenter;
            this.label2.Font = new Font("Segoe UI", 10F);

            // textBox1 - tiểu sử
            this.textBox1.Location = new Point(20, 200);
            this.textBox1.Size = new Size(330, 60);
            this.textBox1.Multiline = true;
            this.textBox1.ReadOnly = true;
            this.textBox1.BorderStyle = BorderStyle.FixedSingle;
            this.textBox1.Font = new Font("Segoe UI", 10F);
            this.textBox1.KeyDown += new KeyEventHandler(this.textBox1_KeyDown);

            // button1 - chỉnh sửa
            this.button1.Location = new Point(110, 270);
            this.button1.Size = new Size(150, 35);
            this.button1.Text = "Chỉnh sửa hồ sơ";
            this.button1.FlatStyle = FlatStyle.Flat;
            this.button1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.button1.Cursor = Cursors.Hand;
            this.button1.Click += new EventHandler(this.button1_Click);

            // listView1 - bạn bè
            this.listView1.Location = new Point(20, 320);
            this.listView1.Size = new Size(330, 100);
            this.listView1.View = View.List;
            this.listView1.Font = new Font("Segoe UI", 10F);
            this.listView1.BorderStyle = BorderStyle.FixedSingle;

            // Form
            this.ClientSize = new Size(370, 450);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.listView1);
            this.Text = "Hồ sơ người dùng";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void InitializeUserProfile()
        {
            label1.Text = "Đang tải...";
            label2.Text = "○ Đang tải...";
            textBox1.Text = "Đang tải thông tin...";
            listView1.View = View.List;
            listView1.Items.Clear();
        }

        private void ApplyTheme()
        {
            this.BackColor = _theme.Background;
            label1.ForeColor = _theme.TextPrimary;
            label2.ForeColor = _theme.TextSecondary;
            textBox1.BackColor = _theme.Surface;
            textBox1.ForeColor = _theme.TextPrimary;
            listView1.BackColor = _theme.Surface;
            listView1.ForeColor = _theme.TextPrimary;
            
            _theme.StyleButton(button1, isPrimary: true);
        }

        private void OnThemeChanged(ThemeService.ThemeMode _)
        {
            ApplyTheme();
        }

        private async Task LoadUserData()
        {
            try
            {
                var userData = await _auth.GetCurrentUserData();

                if (userData != null)
                {
                    // Load fullName
                    string fullName = userData.ContainsKey("fullName") 
                        ? userData["fullName"]?.ToString() ?? "Người dùng" 
                        : "Người dùng";
                    label1.Text = fullName;

                    // Load status
                    string status = userData.ContainsKey("status") 
                        ? userData["status"]?.ToString() ?? "offline" 
                        : "offline";
                    label2.Text = status == "online" ? "● Đang hoạt động" : "○ Ngoại tuyến";
                    label2.ForeColor = status == "online" ? Color.Green : _theme.TextSecondary;

                    // Load bio
                    string bio = userData.ContainsKey("bio") 
                        ? userData["bio"]?.ToString() ?? "Xin chào! Tôi là người yêu công nghệ và thích lập trình." 
                        : "Xin chào! Tôi là người yêu công nghệ và thích lập trình.";
                    textBox1.Text = bio;

                    // Load avatar
                    string avatarUrl = userData.ContainsKey("avatarUrl") 
                        ? userData["avatarUrl"]?.ToString() ?? string.Empty 
                        : string.Empty;
                    
                    if (!string.IsNullOrWhiteSpace(avatarUrl) && System.IO.File.Exists(avatarUrl))
                    {
                        try
                        {
                            pictureBox1.Image = Image.FromFile(avatarUrl);
                        }
                        catch
                        {
                            pictureBox1.Image = CreateInitialsAvatar(fullName);
                        }
                    }
                    else
                    {
                        pictureBox1.Image = CreateInitialsAvatar(fullName);
                    }
                }
                else
                {
                    label1.Text = "Người dùng";
                    label2.Text = "○ Ngoại tuyến";
                    textBox1.Text = "Xin chào! Tôi là người yêu công nghệ và thích lập trình.";
                }

                // Load friends list
                await LoadFriendsList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadFriendsList()
        {
            listView1.Items.Clear();

            if (_auth.CurrentUserId != null)
            {
                var friends = await _friendsService.GetFriends(_auth.CurrentUserId);

                if (friends.Count == 0)
                {
                    listView1.Items.Add("Chưa có bạn bè");
                }
                else
                {
                    foreach (var friend in friends)
                    {
                        string name = friend.ContainsKey("fullName") 
                            ? friend["fullName"]?.ToString() ?? "Bạn bè" 
                            : (friend.ContainsKey("username") ? friend["username"]?.ToString() ?? "Bạn bè" : "Bạn bè");
                        listView1.Items.Add(name);
                    }
                }
            }
            else
            {
                listView1.Items.Add("Chưa có bạn bè");
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            using OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Chọn ảnh đại diện";
            openFileDialog.Filter = "Ảnh (*.jpg; *.png)|*.jpg;*.png";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                selectedAvatarPath = openFileDialog.FileName;
                try
                {
                    pictureBox1.Image = Image.FromFile(selectedAvatarPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi tải ảnh: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            textBox1.ReadOnly = false;
            textBox1.Focus();
            MessageBox.Show("Bạn có thể chỉnh sửa tiểu sử. Nhấn Enter để lưu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !textBox1.ReadOnly)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await SaveProfile();
            }
        }

        private async Task SaveProfile()
        {
            if (_auth.CurrentUserId == null)
            {
                MessageBox.Show("Bạn cần đăng nhập trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string bio = textBox1.Text.Trim();
            string? avatarPath = selectedAvatarPath;

            var (success, message) = await _auth.UpdateUserProfile(bio, avatarPath);

            if (success)
            {
                textBox1.ReadOnly = true;
                selectedAvatarPath = null;
                MessageBox.Show("Tiểu sử đã được lưu.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Lỗi lưu hồ sơ: {message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Image CreateInitialsAvatar(string fullName)
        {
            string initials = "";
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                var parts = fullName.Trim().Split(' ');
                if (parts.Length >= 2)
                    initials = (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
                else if (parts.Length == 1 && parts[0].Length > 0)
                    initials = parts[0][0].ToString().ToUpper();
            }

            if (string.IsNullOrEmpty(initials))
                initials = "U";

            Bitmap bmp = new Bitmap(100, 100);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, 100, 100),
                    Color.LightSkyBlue, 
                    Color.SteelBlue, 
                    45f);
                g.FillEllipse(brush, 0, 0, 100, 100);

                using var font = new Font("Segoe UI", 28, FontStyle.Bold);
                var size = g.MeasureString(initials, font);
                g.DrawString(initials, font, Brushes.White, (100 - size.Width) / 2, (100 - size.Height) / 2);
            }
            return bmp;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _theme.OnThemeChanged -= OnThemeChanged;
                pictureBox1?.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
