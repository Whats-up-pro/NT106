namespace MessagingApp.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

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
            this.panelSidebar = new System.Windows.Forms.Panel();
            this.btnLogout = new System.Windows.Forms.Button();
            this.btnProfile = new System.Windows.Forms.Button();
            this.btnCalls = new System.Windows.Forms.Button();
            this.btnFriends = new System.Windows.Forms.Button();
            this.btnMessages = new System.Windows.Forms.Button();
            this.lblUserName = new System.Windows.Forms.Label();
            this.panelContent = new System.Windows.Forms.Panel();
            this.lblWelcome = new System.Windows.Forms.Label();
            this.listViewConversations = new System.Windows.Forms.ListView();
            this.columnHeaderName = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderLastMessage = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderTime = new System.Windows.Forms.ColumnHeader();
            this.panelSidebar.SuspendLayout();
            this.panelContent.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelSidebar
            // 
            this.panelSidebar.Controls.Add(this.btnLogout);
            this.panelSidebar.Controls.Add(this.btnProfile);
            this.panelSidebar.Controls.Add(this.btnCalls);
            this.panelSidebar.Controls.Add(this.btnFriends);
            this.panelSidebar.Controls.Add(this.btnMessages);
            this.panelSidebar.Controls.Add(this.lblUserName);
            this.panelSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelSidebar.Location = new System.Drawing.Point(0, 0);
            this.panelSidebar.Name = "panelSidebar";
            this.panelSidebar.Size = new System.Drawing.Size(250, 700);
            this.panelSidebar.TabIndex = 0;
            // 
            // lblUserName
            // 
            this.lblUserName.AutoSize = true;
            this.lblUserName.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblUserName.Location = new System.Drawing.Point(20, 30);
            this.lblUserName.Name = "lblUserName";
            this.lblUserName.Size = new System.Drawing.Size(150, 25);
            this.lblUserName.TabIndex = 0;
            this.lblUserName.Text = "User Name";
            // 
            // btnMessages
            // 
            this.btnMessages.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMessages.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.btnMessages.Location = new System.Drawing.Point(20, 100);
            this.btnMessages.Name = "btnMessages";
            this.btnMessages.Size = new System.Drawing.Size(210, 50);
            this.btnMessages.TabIndex = 1;
            this.btnMessages.Text = "💬 Tin Nhắn";
            this.btnMessages.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnMessages.UseVisualStyleBackColor = true;
            this.btnMessages.Click += new System.EventHandler(this.btnMessages_Click);
            // 
            // btnFriends
            // 
            this.btnFriends.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFriends.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.btnFriends.Location = new System.Drawing.Point(20, 160);
            this.btnFriends.Name = "btnFriends";
            this.btnFriends.Size = new System.Drawing.Size(210, 50);
            this.btnFriends.TabIndex = 2;
            this.btnFriends.Text = "👥 Bạn Bè";
            this.btnFriends.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnFriends.UseVisualStyleBackColor = true;
            this.btnFriends.Click += new System.EventHandler(this.btnFriends_Click);
            // 
            // btnCalls
            // 
            this.btnCalls.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCalls.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.btnCalls.Location = new System.Drawing.Point(20, 220);
            this.btnCalls.Name = "btnCalls";
            this.btnCalls.Size = new System.Drawing.Size(210, 50);
            this.btnCalls.TabIndex = 3;
            this.btnCalls.Text = "📞 Cuộc Gọi";
            this.btnCalls.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCalls.UseVisualStyleBackColor = true;
            this.btnCalls.Click += new System.EventHandler(this.btnCalls_Click);
            // 
            // btnProfile
            // 
            this.btnProfile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnProfile.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.btnProfile.Location = new System.Drawing.Point(20, 280);
            this.btnProfile.Name = "btnProfile";
            this.btnProfile.Size = new System.Drawing.Size(210, 50);
            this.btnProfile.TabIndex = 4;
            this.btnProfile.Text = "👤 Hồ Sơ";
            this.btnProfile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnProfile.UseVisualStyleBackColor = true;
            this.btnProfile.Click += new System.EventHandler(this.btnProfile_Click);
            // 
            // btnLogout
            // 
            this.btnLogout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogout.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.btnLogout.Location = new System.Drawing.Point(20, 630);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(210, 50);
            this.btnLogout.TabIndex = 5;
            this.btnLogout.Text = "🚪 Đăng Xuất";
            this.btnLogout.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnLogout.UseVisualStyleBackColor = true;
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);
            // 
            // panelContent
            // 
            this.panelContent.Controls.Add(this.listViewConversations);
            this.panelContent.Controls.Add(this.lblWelcome);
            this.panelContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelContent.Location = new System.Drawing.Point(250, 0);
            this.panelContent.Name = "panelContent";
            this.panelContent.Size = new System.Drawing.Size(950, 700);
            this.panelContent.TabIndex = 1;
            // 
            // lblWelcome
            // 
            this.lblWelcome.AutoSize = true;
            this.lblWelcome.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.lblWelcome.Location = new System.Drawing.Point(30, 30);
            this.lblWelcome.Name = "lblWelcome";
            this.lblWelcome.Size = new System.Drawing.Size(300, 32);
            this.lblWelcome.TabIndex = 0;
            this.lblWelcome.Text = "Chào mừng đến Messaging App";
            // 
            // listViewConversations
            // 
            this.listViewConversations.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName,
            this.columnHeaderLastMessage,
            this.columnHeaderTime});
            this.listViewConversations.FullRowSelect = true;
            this.listViewConversations.Location = new System.Drawing.Point(30, 90);
            this.listViewConversations.Name = "listViewConversations";
            this.listViewConversations.Size = new System.Drawing.Size(890, 580);
            this.listViewConversations.TabIndex = 1;
            this.listViewConversations.UseCompatibleStateImageBehavior = false;
            this.listViewConversations.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderName
            // 
            this.columnHeaderName.Text = "Tên";
            this.columnHeaderName.Width = 200;
            // 
            // columnHeaderLastMessage
            // 
            this.columnHeaderLastMessage.Text = "Tin nhắn cuối";
            this.columnHeaderLastMessage.Width = 500;
            // 
            // columnHeaderTime
            // 
            this.columnHeaderTime.Text = "Thời gian";
            this.columnHeaderTime.Width = 180;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Controls.Add(this.panelContent);
            this.Controls.Add(this.panelSidebar);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Messaging App";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.panelSidebar.ResumeLayout(false);
            this.panelSidebar.PerformLayout();
            this.panelContent.ResumeLayout(false);
            this.panelContent.PerformLayout();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel panelSidebar;
        private System.Windows.Forms.Panel panelContent;
        private System.Windows.Forms.Label lblUserName;
        private System.Windows.Forms.Button btnMessages;
        private System.Windows.Forms.Button btnFriends;
        private System.Windows.Forms.Button btnCalls;
        private System.Windows.Forms.Button btnProfile;
        private System.Windows.Forms.Button btnLogout;
        private System.Windows.Forms.Label lblWelcome;
        private System.Windows.Forms.ListView listViewConversations;
        private System.Windows.Forms.ColumnHeader columnHeaderName;
        private System.Windows.Forms.ColumnHeader columnHeaderLastMessage;
        private System.Windows.Forms.ColumnHeader columnHeaderTime;
    }
}
