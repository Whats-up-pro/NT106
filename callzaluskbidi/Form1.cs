using System;
using System.Drawing;
using System.Windows.Forms;

namespace callzaluskbidi
{
    public partial class Form1 : Form
    {
        public enum CallState
        {
            Idle,
            Outgoing,
            Incoming,
            InCall
        }

        private CallState currentState = CallState.Idle;

        private System.Windows.Forms.Timer callTimer = new System.Windows.Forms.Timer();
        private int callSeconds = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Image avatar;
            try
            {
                // Thử tải ảnh từ file
                avatar = Image.FromFile("avatar.jpg");
            }
            catch
            {
                // Tạo avatar mặc định nếu không tìm thấy file
                avatar = CreateDefaultAvatar("TB");
            }
            SwitchToIncoming("Trần Thị B", avatar);
        }

        private Image CreateDefaultAvatar(string initials)
        {
            // Tạo ảnh avatar mặc định với chữ cái đầu
            Bitmap bmp = new Bitmap(200, 200);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                // Nền gradient
                g.FillEllipse(new SolidBrush(Color.FromArgb(100, 149, 237)), 0, 0, 200, 200);
                
                // Vẽ chữ
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Font font = new Font("Segoe UI", 72, FontStyle.Bold);
                SizeF textSize = g.MeasureString(initials, font);
                g.DrawString(initials, font, Brushes.White,
                    (200 - textSize.Width) / 2,
                    (200 - textSize.Height) / 2);
            }
            return bmp;
        }

        //private void Form1_Load(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        Image avatar = Image.FromFile("avatar.jpg");
        //        SwitchToOutgoing("Nguyễn Văn A", avatar);
        //    }
        //    catch
        //    {
        //        MessageBox.Show("Không tìm thấy ảnh avatar.");
        //    }
        //}
        


        private void SwitchToOutgoing(string name, Image avatar)
        {
            currentState = CallState.Outgoing;
            pnlOutgoing.Visible = true;
            pnlIncoming.Visible = false;
            pnlInCall.Visible = false;

            lblNameOut.Text = name;
            picAvatarOut.Image = avatar;
            lblStatusOut.Text = "Đang gọi...";
        }

        private void SwitchToIncoming(string name, Image avatar)
        {
            currentState = CallState.Incoming;
            pnlOutgoing.Visible = false;
            pnlIncoming.Visible = true;
            pnlInCall.Visible = false;

            lblNameIn.Text = name;
            picAvatarIn.Image = avatar;
            lblStatusIn.Text = "Cuộc gọi đến...";
        }

        private void SwitchToInCall(string name, Image avatar)
        {
            currentState = CallState.InCall;
            pnlOutgoing.Visible = false;
            pnlIncoming.Visible = false;
            pnlInCall.Visible = true;

            lblNameTalk.Text = name;
            picAvatarTalk.Image = avatar;
            StartCallTimer();
        }

        private void StartCallTimer()
        {
            callSeconds = 0;
            callTimer.Interval = 1000;
            callTimer.Tick += CallTimer_Tick;
            callTimer.Start();
        }

        private void CallTimer_Tick(object sender, EventArgs e)
        {
            callSeconds++;
            lblDuration.Text = TimeSpan.FromSeconds(callSeconds).ToString(@"mm\:ss");
        }

        private void StopCallTimer()
        {
            callTimer.Stop();
            lblDuration.Text = "00:00";
        }

        private void btnCancelCall_Click(object sender, EventArgs e)
        {
            currentState = CallState.Idle;
            pnlOutgoing.Visible = false;
        }

        private void btnAcceptCall_Click(object sender, EventArgs e)
        {
            SwitchToInCall(lblNameIn.Text, picAvatarIn.Image);
        }

        private void btnDeclineCall_Click(object sender, EventArgs e)
        {
            currentState = CallState.Idle;
            pnlIncoming.Visible = false;
        }

        private void btnEndCall_Click(object sender, EventArgs e)
        {
            StopCallTimer();
            currentState = CallState.Idle;
            pnlInCall.Visible = false;
        }

        private void btnToggleMic_Click(object sender, EventArgs e)
        {
            btnToggleMic.Text = btnToggleMic.Text == "Mic On" ? "Mic Off" : "Mic On";
        }

        private void pnlInCall_Paint(object sender, PaintEventArgs e)
        {
            // Vẽ custom nếu cần
        }

        private void pnlInCall_Paint_1(object sender, PaintEventArgs e)
        {
            // Vẽ custom nếu cần
        }
    }
}