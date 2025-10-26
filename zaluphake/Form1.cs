using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChatProfileApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            InitializeUserProfile();
        }

        private void InitializeUserProfile()
        {
            label1.Text = "Lê Quang Minh";
            label2.Text = "● Đang hoạt động";
            label2.ForeColor = Color.Green;

            textBox1.Text = "Xin chào! Tôi là người yêu công nghệ và thích lập trình.";

            listView1.View = View.List;
            listView1.Items.Clear();
            listView1.Items.Add("Kiệt");
            listView1.Items.Add("Quốc");
            listView1.Items.Add("Huy");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Chọn ảnh đại diện";
            openFileDialog.Filter = "Ảnh (*.jpg; *.png)|*.jpg;*.png";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                pictureBox1.Image = Image.FromFile(openFileDialog.FileName);
            }

            textBox1.ReadOnly = false;
            MessageBox.Show("Bạn có thể chỉnh sửa tiểu sử. Nhấn Enter để lưu.");
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                textBox1.ReadOnly = true;
                MessageBox.Show("Tiểu sử đã được lưu.");
            }
        }
    }
}