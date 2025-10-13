namespace MessagingApp.Forms
{
    partial class CallForm
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
            this.panelMain = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.listViewCallHistory = new System.Windows.Forms.ListView();
            this.columnHeaderContact = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderType = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderStatus = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderDuration = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderTime = new System.Windows.Forms.ColumnHeader();
            this.btnVoiceCall = new System.Windows.Forms.Button();
            this.btnVideoCall = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.panelMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.btnClose);
            this.panelMain.Controls.Add(this.btnVideoCall);
            this.panelMain.Controls.Add(this.btnVoiceCall);
            this.panelMain.Controls.Add(this.listViewCallHistory);
            this.panelMain.Controls.Add(this.lblTitle);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(0, 0);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(900, 600);
            this.panelMain.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(30, 30);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(350, 45);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "LỊCH SỬ CUỘC GỌI";
            // 
            // listViewCallHistory
            // 
            this.listViewCallHistory.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderContact,
            this.columnHeaderType,
            this.columnHeaderStatus,
            this.columnHeaderDuration,
            this.columnHeaderTime});
            this.listViewCallHistory.FullRowSelect = true;
            this.listViewCallHistory.Location = new System.Drawing.Point(30, 100);
            this.listViewCallHistory.Name = "listViewCallHistory";
            this.listViewCallHistory.Size = new System.Drawing.Size(840, 420);
            this.listViewCallHistory.TabIndex = 1;
            this.listViewCallHistory.UseCompatibleStateImageBehavior = false;
            this.listViewCallHistory.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderContact
            // 
            this.columnHeaderContact.Text = "Liên hệ";
            this.columnHeaderContact.Width = 200;
            // 
            // columnHeaderType
            // 
            this.columnHeaderType.Text = "Loại";
            this.columnHeaderType.Width = 120;
            // 
            // columnHeaderStatus
            // 
            this.columnHeaderStatus.Text = "Trạng thái";
            this.columnHeaderStatus.Width = 150;
            // 
            // columnHeaderDuration
            // 
            this.columnHeaderDuration.Text = "Thời lượng";
            this.columnHeaderDuration.Width = 120;
            // 
            // columnHeaderTime
            // 
            this.columnHeaderTime.Text = "Thời gian";
            this.columnHeaderTime.Width = 240;
            // 
            // btnVoiceCall
            // 
            this.btnVoiceCall.Location = new System.Drawing.Point(30, 535);
            this.btnVoiceCall.Name = "btnVoiceCall";
            this.btnVoiceCall.Size = new System.Drawing.Size(200, 45);
            this.btnVoiceCall.TabIndex = 2;
            this.btnVoiceCall.Text = "📞 Gọi Thoại";
            this.btnVoiceCall.UseVisualStyleBackColor = true;
            this.btnVoiceCall.Click += new System.EventHandler(this.btnVoiceCall_Click);
            // 
            // btnVideoCall
            // 
            this.btnVideoCall.Location = new System.Drawing.Point(250, 535);
            this.btnVideoCall.Name = "btnVideoCall";
            this.btnVideoCall.Size = new System.Drawing.Size(200, 45);
            this.btnVideoCall.TabIndex = 3;
            this.btnVideoCall.Text = "📹 Gọi Video";
            this.btnVideoCall.UseVisualStyleBackColor = true;
            this.btnVideoCall.Click += new System.EventHandler(this.btnVideoCall_Click);
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(670, 535);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(200, 45);
            this.btnClose.TabIndex = 4;
            this.btnClose.Text = "Đóng";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // CallForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.panelMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "CallForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Cuộc Gọi - Messaging App";
            this.panelMain.ResumeLayout(false);
            this.panelMain.PerformLayout();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.ListView listViewCallHistory;
        private System.Windows.Forms.ColumnHeader columnHeaderContact;
        private System.Windows.Forms.ColumnHeader columnHeaderType;
        private System.Windows.Forms.ColumnHeader columnHeaderStatus;
        private System.Windows.Forms.ColumnHeader columnHeaderDuration;
        private System.Windows.Forms.ColumnHeader columnHeaderTime;
        private System.Windows.Forms.Button btnVoiceCall;
        private System.Windows.Forms.Button btnVideoCall;
        private System.Windows.Forms.Button btnClose;
    }
}
