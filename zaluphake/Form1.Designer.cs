namespace ChatProfileApp
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private PictureBox pictureBox1;
        private Label label1;
        private Label label2;
        private TextBox textBox1;
        private Button button1;
        private ListView listView1;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
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

            // Form background
            this.BackColor = Color.WhiteSmoke;

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
            this.textBox1.BackColor = Color.WhiteSmoke;
            this.textBox1.BorderStyle = BorderStyle.FixedSingle;
            this.textBox1.Font = new Font("Segoe UI", 10F);
            this.textBox1.KeyDown += new KeyEventHandler(this.textBox1_KeyDown);

            // button1 - chỉnh sửa
            this.button1.Location = new Point(110, 270);
            this.button1.Size = new Size(150, 35);
            this.button1.Text = "Chỉnh sửa hồ sơ";
            this.button1.BackColor = Color.LightSkyBlue;
            this.button1.FlatStyle = FlatStyle.Flat;
            this.button1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.button1.Click += new EventHandler(this.button1_Click);

            // listView1 - bạn bè
            this.listView1.Location = new Point(20, 320);
            this.listView1.Size = new Size(330, 100);
            this.listView1.View = View.List;
            this.listView1.Font = new Font("Segoe UI", 10F);

            // Form1
            this.ClientSize = new Size(370, 450);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.listView1);
            this.Text = "Hồ sơ người dùng";

            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}