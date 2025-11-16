using MessagingApp.Services;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MessagingApp.Forms.Social
{
    public class ProfileForm : Form
    {
        private readonly ThemeService _theme = ThemeService.Instance;
        private readonly FirebaseAuthService _auth = FirebaseAuthService.Instance;
        private readonly FirestoreFriendsService _friendsService = FirestoreFriendsService.Instance;

    // UI controls
    private RoundedPanel pnlCard = null!;
    private Panel pnlHeader = null!;
    private PictureBox picAvatar = null!;
    private LinkLabel lnkChangeAvatar = null!;
    private Label lblFullName = null!;
    private Label lblStatus = null!;
    private Label lblBio = null!;
    private TextBox txtBio = null!;
    private Button btnEditProfile = null!;
    private Button btnSave = null!;
    private GroupBox grpFriends = null!;
    private ListView lvFriends = null!;
        private OpenFileDialog avatarDialog = new OpenFileDialog();

        private string? selectedAvatarPath;

        public ProfileForm()
        {
            InitializeComponent();
            BuildUI();
            ApplyTheme();
            _theme.OnThemeChanged += OnThemeChanged;

            // Load data async after shown to avoid blocking UI
            this.Shown += async (_, __) => await LoadData();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(520, 640);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Hồ sơ cá nhân";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.ResumeLayout(false);
        }

        private void BuildUI()
        {
            // Card container
            pnlCard = new RoundedPanel { Width = 460, Height = 560, Radius = 14 };
            pnlCard.Location = new Point((this.ClientSize.Width - pnlCard.Width) / 2, (this.ClientSize.Height - pnlCard.Height) / 2);
            pnlCard.BackColor = _theme.Surface;
            this.Controls.Add(pnlCard);

            // Header with soft gradient
            pnlHeader = new Panel { Left = 0, Top = 0, Width = pnlCard.Width, Height = 140 };
            pnlHeader.Paint += (s, e) =>
            {
                var c1 = Color.FromArgb(40, _theme.Primary);
                var c2 = Color.FromArgb(10, _theme.Primary);
                using var br = new LinearGradientBrush(pnlHeader.ClientRectangle, c1, c2, LinearGradientMode.Vertical);
                e.Graphics.FillRectangle(br, pnlHeader.ClientRectangle);
            };
            pnlCard.Controls.Add(pnlHeader);

            // Avatar
            picAvatar = new PictureBox
            {
                Size = new Size(110, 110),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            picAvatar.Location = new Point((pnlCard.Width - picAvatar.Width) / 2, pnlHeader.Bottom - (picAvatar.Height / 2));
            pnlCard.Controls.Add(picAvatar);
            picAvatar.BringToFront();
            // Ensure circular mask initially and on resize
            picAvatar.SizeChanged += (_, __) => MakeAvatarCircle();
            MakeAvatarCircle();

            lnkChangeAvatar = new LinkLabel
            {
                Text = "Đổi ảnh",
                AutoSize = true,
                Location = new Point(picAvatar.Left + (picAvatar.Width / 2) - 30, picAvatar.Bottom + 4)
            };
            lnkChangeAvatar.Click += (_, __) => BtnEditProfile_Click(_, __);
            pnlCard.Controls.Add(lnkChangeAvatar);

            // Name & Status
            lblFullName = new Label
            {
                Location = new Point(20, lnkChangeAvatar.Bottom + 8),
                Size = new Size(pnlCard.Width - 40, 36),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Text = "Đang tải..."
            };
            pnlCard.Controls.Add(lblFullName);

            lblStatus = new Label
            {
                Location = new Point(20, lblFullName.Bottom - 4),
                Size = new Size(pnlCard.Width - 40, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10.5F),
                Text = ""
            };
            pnlCard.Controls.Add(lblStatus);

            // Bio section
            lblBio = new Label
            {
                Text = "Tiểu sử",
                Location = new Point(24, lblStatus.Bottom + 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            pnlCard.Controls.Add(lblBio);

            txtBio = new TextBox
            {
                Location = new Point(20, lblBio.Bottom + 6),
                Size = new Size(pnlCard.Width - 40, 70),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F)
            };
            txtBio.KeyDown += TxtBio_KeyDown;
            pnlCard.Controls.Add(txtBio);

            btnEditProfile = new Button
            {
                Location = new Point(20, txtBio.Bottom + 8),
                Size = new Size((pnlCard.Width - 50) / 2, 38),
                Text = "✏️ Chỉnh sửa",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEditProfile.Click += BtnEditProfile_Click;
            pnlCard.Controls.Add(btnEditProfile);

            btnSave = new Button
            {
                Location = new Point(btnEditProfile.Right + 10, txtBio.Bottom + 8),
                Size = new Size((pnlCard.Width - 50) / 2, 38),
                Text = "💾 Lưu",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnSave.Click += async (_, __) => await SaveProfile();
            pnlCard.Controls.Add(btnSave);

            // Friends group
            grpFriends = new GroupBox
            {
                Text = "Bạn bè",
                Location = new Point(20, btnSave.Bottom + 14),
                Size = new Size(pnlCard.Width - 40, pnlCard.Height - (btnSave.Bottom + 28))
            };
            pnlCard.Controls.Add(grpFriends);

            lvFriends = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.List,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10F)
            };
            grpFriends.Controls.Add(lvFriends);
        }

        private void ApplyTheme()
        {
            this.BackColor = _theme.Background;
            pnlCard.BackColor = _theme.Surface;
            lblFullName.ForeColor = _theme.TextPrimary;
            lblStatus.ForeColor = _theme.TextSecondary;
            lblBio.ForeColor = _theme.TextPrimary;
            txtBio.BackColor = _theme.Surface;
            txtBio.ForeColor = _theme.TextPrimary;
            grpFriends.ForeColor = _theme.TextPrimary;
            lnkChangeAvatar.LinkColor = _theme.Primary;
            lnkChangeAvatar.ActiveLinkColor = _theme.PrimaryHover;
            _theme.StyleButton(btnEditProfile, isPrimary: false);
            _theme.StyleButton(btnSave, isPrimary: true);
        }

        private void OnThemeChanged(ThemeService.ThemeMode _)
        {
            ApplyTheme();
        }

        private async Task LoadData()
        {
            var data = await _auth.GetCurrentUserData();
            string fullName = data != null && data.ContainsKey("fullName") ? data["fullName"]?.ToString() ?? "Người dùng" : "Người dùng";
            string status = data != null && data.ContainsKey("status") ? data["status"]?.ToString() ?? "offline" : "offline";
            string bio = data != null && data.ContainsKey("bio") ? data["bio"]?.ToString() ?? string.Empty : string.Empty;
            string avatarUrl = data != null && data.ContainsKey("avatarUrl") ? data["avatarUrl"]?.ToString() ?? string.Empty : string.Empty;

            lblFullName.Text = fullName;
            lblStatus.Text = (status == "online" ? "● " : "○ ") + (status == "online" ? "Đang hoạt động" : "Ngoại tuyến");
            lblStatus.ForeColor = status == "online" ? Color.SeaGreen : _theme.TextSecondary;
            txtBio.Text = string.IsNullOrWhiteSpace(bio) ? "(Chưa có tiểu sử)" : bio;

            // Load avatar if a local path exists, otherwise draw initials
            try
            {
                if (!string.IsNullOrWhiteSpace(avatarUrl) && File.Exists(avatarUrl))
                {
                    picAvatar.Image = Image.FromFile(avatarUrl);
                }
                else
                {
                    picAvatar.Image = CreateInitialsAvatar(fullName);
                }
            }
            catch
            {
                picAvatar.Image = CreateInitialsAvatar(fullName);
            }
            MakeAvatarCircle();

            // Load friends
            lvFriends.Items.Clear();
            if (_auth.CurrentUserId != null)
            {
                var friends = await _friendsService.GetFriends(_auth.CurrentUserId);
                if (friends.Count == 0)
                {
                    lvFriends.Items.Add("Chưa có bạn bè");
                }
                else
                {
                    foreach (var f in friends)
                    {
                        string name = f.ContainsKey("fullName") ? f["fullName"].ToString()! : (f.ContainsKey("username") ? f["username"].ToString()! : "Bạn bè");
                        lvFriends.Items.Add(name);
                    }
                }
            }
        }

        private void BtnEditProfile_Click(object? sender, EventArgs e)
        {
            avatarDialog.Title = "Chọn ảnh đại diện";
            avatarDialog.Filter = "Ảnh (*.jpg; *.png)|*.jpg;*.png";

            if (avatarDialog.ShowDialog() == DialogResult.OK)
            {
                selectedAvatarPath = avatarDialog.FileName;
                try { picAvatar.Image = Image.FromFile(selectedAvatarPath); } catch { }
                MakeAvatarCircle();
            }

            txtBio.ReadOnly = false;
            txtBio.Focus();
            btnSave.Enabled = true;
        }

        private async Task SaveProfile()
        {
            if (_auth.CurrentUserId == null)
            {
                MessageBox.Show("Bạn cần đăng nhập trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string bio = txtBio.Text.Trim();
            string? avatarPath = selectedAvatarPath; // Note: local path only; uploading to Storage is out of scope

            var (ok, msg) = await _auth.UpdateUserProfile(bio, avatarPath);
            if (ok)
            {
                btnSave.Enabled = false;
                txtBio.ReadOnly = true;
                MessageBox.Show("Đã lưu hồ sơ.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(msg, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtBio_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && btnSave.Enabled)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _ = SaveProfile();
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
                else
                    initials = parts[0][0].ToString().ToUpper();
            }

            Bitmap bmp = new Bitmap(100, 100);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, 100, 100),
                    Color.SteelBlue, Color.CornflowerBlue, 45f);
                g.FillEllipse(brush, 0, 0, 100, 100);

                using var font = new Font("Segoe UI", 28, FontStyle.Bold);
                var size = g.MeasureString(initials, font);
                g.DrawString(initials, font, Brushes.White, (100 - size.Width) / 2, (100 - size.Height) / 2);
            }
            return bmp;
        }

        private void MakeAvatarCircle()
        {
            using var path = new GraphicsPath();
            path.AddEllipse(0, 0, picAvatar.Width, picAvatar.Height);
            picAvatar.Region = new Region(path);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _theme.OnThemeChanged -= OnThemeChanged;
            }
            base.Dispose(disposing);
        }
    }

    internal class RoundedPanel : Panel
    {
        public int Radius { get; set; } = 12;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = GetRoundRectPath(new Rectangle(0, 0, this.Width - 1, this.Height - 1), Radius);
            using var br = new SolidBrush(this.BackColor);
            e.Graphics.FillPath(br, path);
            using var pen = new Pen(Color.FromArgb(40, 0, 0, 0));
            e.Graphics.DrawPath(pen, path);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            this.Invalidate();
        }

        private GraphicsPath GetRoundRectPath(Rectangle rect, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
