namespace callzaluskbidi
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        // Các control
        private Panel pnlOutgoing;
        private Panel pnlIncoming;
        private Panel pnlInCall;

        private PictureBox picAvatarOut;
        private PictureBox picAvatarIn;
        private PictureBox picAvatarTalk;

        private Label lblNameOut;
        private Label lblNameIn;
        private Label lblNameTalk;

        private Label lblStatusOut;
        private Label lblStatusIn;
        private Label lblDuration;

        private Button btnCancelCall;
        private Button btnAcceptCall;
        private Button btnDeclineCall;
        private Button btnEndCall;
        private Button btnToggleMic;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlOutgoing = new Panel();
            this.pnlIncoming = new Panel();
            this.pnlInCall = new Panel();

            this.picAvatarOut = new PictureBox();
            this.picAvatarIn = new PictureBox();
            this.picAvatarTalk = new PictureBox();

            this.lblNameOut = new Label();
            this.lblNameIn = new Label();
            this.lblNameTalk = new Label();

            this.lblStatusOut = new Label();
            this.lblStatusIn = new Label();
            this.lblDuration = new Label();

            this.btnCancelCall = new Button();
            this.btnAcceptCall = new Button();
            this.btnDeclineCall = new Button();
            this.btnEndCall = new Button();
            this.btnToggleMic = new Button();

            this.SuspendLayout();

            // Form
            this.ClientSize = new System.Drawing.Size(400, 600);
            this.Name = "Form1";
            this.Text = "Giao diện gọi điện";
            this.Load += new System.EventHandler(this.Form1_Load);

            // pnlOutgoing
            this.pnlOutgoing.Controls.Add(this.picAvatarOut);
            this.pnlOutgoing.Controls.Add(this.lblNameOut);
            this.pnlOutgoing.Controls.Add(this.lblStatusOut);
            this.pnlOutgoing.Controls.Add(this.btnCancelCall);
            this.pnlOutgoing.Dock = DockStyle.Fill;
            this.pnlOutgoing.Visible = false;

            // pnlIncoming
            this.pnlIncoming.Controls.Add(this.picAvatarIn);
            this.pnlIncoming.Controls.Add(this.lblNameIn);
            this.pnlIncoming.Controls.Add(this.lblStatusIn);
            this.pnlIncoming.Controls.Add(this.btnAcceptCall);
            this.pnlIncoming.Controls.Add(this.btnDeclineCall);
            this.pnlIncoming.Dock = DockStyle.Fill;
            this.pnlIncoming.Visible = false;

            // pnlInCall
            this.pnlInCall.Controls.Add(this.picAvatarTalk);
            this.pnlInCall.Controls.Add(this.lblNameTalk);
            this.pnlInCall.Controls.Add(this.lblDuration);
            this.pnlInCall.Controls.Add(this.btnEndCall);
            this.pnlInCall.Controls.Add(this.btnToggleMic);
            this.pnlInCall.Dock = DockStyle.Fill;
            this.pnlInCall.Visible = false;
            this.pnlInCall.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlInCall_Paint);

            // PictureBoxes
            this.picAvatarOut.Size = this.picAvatarIn.Size = this.picAvatarTalk.Size = new System.Drawing.Size(100, 100);
            this.picAvatarOut.Location = new System.Drawing.Point(150, 50);
            this.picAvatarIn.Location = new System.Drawing.Point(150, 50);
            this.picAvatarTalk.Location = new System.Drawing.Point(150, 50);
            this.picAvatarOut.SizeMode = this.picAvatarIn.SizeMode = this.picAvatarTalk.SizeMode = PictureBoxSizeMode.Zoom;

            // Labels
            this.lblNameOut.Location = new System.Drawing.Point(150, 160);
            this.lblNameOut.Size = new System.Drawing.Size(100, 30);
            this.lblNameOut.TextAlign = ContentAlignment.MiddleCenter;

            this.lblNameIn.Location = new System.Drawing.Point(150, 160);
            this.lblNameIn.Size = new System.Drawing.Size(100, 30);
            this.lblNameIn.TextAlign = ContentAlignment.MiddleCenter;

            this.lblNameTalk.Location = new System.Drawing.Point(150, 160);
            this.lblNameTalk.Size = new System.Drawing.Size(100, 30);
            this.lblNameTalk.TextAlign = ContentAlignment.MiddleCenter;

            this.lblStatusOut.Location = new System.Drawing.Point(150, 200);
            this.lblStatusOut.Size = new System.Drawing.Size(100, 30);
            this.lblStatusOut.TextAlign = ContentAlignment.MiddleCenter;

            this.lblStatusIn.Location = new System.Drawing.Point(150, 200);
            this.lblStatusIn.Size = new System.Drawing.Size(100, 30);
            this.lblStatusIn.TextAlign = ContentAlignment.MiddleCenter;

            this.lblDuration.Location = new System.Drawing.Point(150, 200);
            this.lblDuration.Size = new System.Drawing.Size(100, 30);
            this.lblDuration.Text = "00:00";
            this.lblDuration.TextAlign = ContentAlignment.MiddleCenter;

            // Buttons
            this.btnCancelCall.Location = new System.Drawing.Point(150, 250);
            this.btnCancelCall.Size = new System.Drawing.Size(100, 40);
            this.btnCancelCall.Text = "Hủy";
            this.btnCancelCall.Click += new System.EventHandler(this.btnCancelCall_Click);

            this.btnAcceptCall.Location = new System.Drawing.Point(100, 250);
            this.btnAcceptCall.Size = new System.Drawing.Size(80, 40);
            this.btnAcceptCall.Text = "Chấp nhận";
            this.btnAcceptCall.Click += new System.EventHandler(this.btnAcceptCall_Click);

            this.btnDeclineCall.Location = new System.Drawing.Point(220, 250);
            this.btnDeclineCall.Size = new System.Drawing.Size(80, 40);
            this.btnDeclineCall.Text = "Từ chối";
            this.btnDeclineCall.Click += new System.EventHandler(this.btnDeclineCall_Click);

            this.btnEndCall.Location = new System.Drawing.Point(100, 250);
            this.btnEndCall.Size = new System.Drawing.Size(80, 40);
            this.btnEndCall.Text = "Kết thúc";
            this.btnEndCall.Click += new System.EventHandler(this.btnEndCall_Click);

            this.btnToggleMic.Location = new System.Drawing.Point(220, 250);
            this.btnToggleMic.Size = new System.Drawing.Size(80, 40);
            this.btnToggleMic.Text = "Mic On";
            this.btnToggleMic.Click += new System.EventHandler(this.btnToggleMic_Click);

            // Thêm panel vào form
            this.Controls.Add(this.pnlOutgoing);
            this.Controls.Add(this.pnlIncoming);
            this.Controls.Add(this.pnlInCall);

            this.ResumeLayout(false);
        }
    }
}