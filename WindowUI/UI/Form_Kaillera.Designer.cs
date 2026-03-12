namespace ScePSX.Win.UI
{
    partial class Form_Kaillera
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnRefrsh = new System.Windows.Forms.Button();
            edAddress = new System.Windows.Forms.TextBox();
            lvSrv = new System.Windows.Forms.ListView();
            columnHeader0 = new System.Windows.Forms.ColumnHeader();
            columnHeader1 = new System.Windows.Forms.ColumnHeader();
            columnHeader2 = new System.Windows.Forms.ColumnHeader();
            columnHeader3 = new System.Windows.Forms.ColumnHeader();
            btnConnect = new System.Windows.Forms.Button();
            lvGames = new System.Windows.Forms.ListView();
            columnHeader4 = new System.Windows.Forms.ColumnHeader();
            columnHeader5 = new System.Windows.Forms.ColumnHeader();
            columnHeader6 = new System.Windows.Forms.ColumnHeader();
            tbChat = new System.Windows.Forms.TextBox();
            btnSend = new System.Windows.Forms.Button();
            labUser = new System.Windows.Forms.Label();
            edUserName = new System.Windows.Forms.TextBox();
            edChat = new System.Windows.Forms.TextBox();
            btnJoin = new System.Windows.Forms.Button();
            btnCreate = new System.Windows.Forms.Button();
            Status = new System.Windows.Forms.StatusStrip();
            Label1 = new System.Windows.Forms.ToolStripStatusLabel();
            Status.SuspendLayout();
            SuspendLayout();
            // 
            // btnRefrsh
            // 
            btnRefrsh.Location = new System.Drawing.Point(12, 12);
            btnRefrsh.Name = "btnRefrsh";
            btnRefrsh.Size = new System.Drawing.Size(75, 23);
            btnRefrsh.TabIndex = 0;
            btnRefrsh.Text = "Refrsh";
            btnRefrsh.UseVisualStyleBackColor = true;
            btnRefrsh.Click += btnRefrsh_Click;
            // 
            // edAddress
            // 
            edAddress.Location = new System.Drawing.Point(12, 361);
            edAddress.Name = "edAddress";
            edAddress.Size = new System.Drawing.Size(195, 23);
            edAddress.TabIndex = 1;
            edAddress.Text = "127.0.0.1:27888";
            // 
            // lvSrv
            // 
            lvSrv.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { columnHeader0, columnHeader1, columnHeader2, columnHeader3 });
            lvSrv.FullRowSelect = true;
            lvSrv.Location = new System.Drawing.Point(12, 41);
            lvSrv.MultiSelect = false;
            lvSrv.Name = "lvSrv";
            lvSrv.Size = new System.Drawing.Size(300, 309);
            lvSrv.TabIndex = 3;
            lvSrv.UseCompatibleStateImageBehavior = false;
            lvSrv.View = System.Windows.Forms.View.Details;
            lvSrv.DoubleClick += lvSrv_DoubleClick;
            // 
            // columnHeader0
            // 
            columnHeader0.Text = "Ping";
            columnHeader0.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            columnHeader0.Width = 40;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Name";
            columnHeader1.Width = 100;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "Location";
            columnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            columnHeader2.Width = 65;
            // 
            // columnHeader3
            // 
            columnHeader3.Text = "Users";
            columnHeader3.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            columnHeader3.Width = 68;
            // 
            // btnConnect
            // 
            btnConnect.Location = new System.Drawing.Point(213, 356);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new System.Drawing.Size(99, 33);
            btnConnect.TabIndex = 4;
            btnConnect.Text = "Connect";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // lvGames
            // 
            lvGames.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { columnHeader4, columnHeader5, columnHeader6 });
            lvGames.Location = new System.Drawing.Point(318, 12);
            lvGames.MultiSelect = false;
            lvGames.Name = "lvGames";
            lvGames.Size = new System.Drawing.Size(305, 183);
            lvGames.TabIndex = 5;
            lvGames.UseCompatibleStateImageBehavior = false;
            lvGames.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader4
            // 
            columnHeader4.Text = "Game";
            columnHeader4.Width = 150;
            // 
            // columnHeader5
            // 
            columnHeader5.Text = "Owner";
            columnHeader5.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader6
            // 
            columnHeader6.Text = "Players";
            columnHeader6.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // tbChat
            // 
            tbChat.Location = new System.Drawing.Point(318, 236);
            tbChat.Multiline = true;
            tbChat.Name = "tbChat";
            tbChat.ReadOnly = true;
            tbChat.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            tbChat.Size = new System.Drawing.Size(305, 114);
            tbChat.TabIndex = 6;
            // 
            // btnSend
            // 
            btnSend.Location = new System.Drawing.Point(548, 356);
            btnSend.Name = "btnSend";
            btnSend.Size = new System.Drawing.Size(75, 33);
            btnSend.TabIndex = 7;
            btnSend.Text = "Send";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // labUser
            // 
            labUser.AutoSize = true;
            labUser.Location = new System.Drawing.Point(97, 15);
            labUser.Name = "labUser";
            labUser.Size = new System.Drawing.Size(69, 17);
            labUser.TabIndex = 8;
            labUser.Text = "NickName";
            // 
            // edUserName
            // 
            edUserName.Location = new System.Drawing.Point(173, 12);
            edUserName.Name = "edUserName";
            edUserName.Size = new System.Drawing.Size(139, 23);
            edUserName.TabIndex = 9;
            edUserName.Text = "ScePSX#User";
            // 
            // edChat
            // 
            edChat.Location = new System.Drawing.Point(318, 361);
            edChat.Name = "edChat";
            edChat.Size = new System.Drawing.Size(224, 23);
            edChat.TabIndex = 10;
            // 
            // btnJoin
            // 
            btnJoin.Location = new System.Drawing.Point(548, 201);
            btnJoin.Name = "btnJoin";
            btnJoin.Size = new System.Drawing.Size(75, 29);
            btnJoin.TabIndex = 11;
            btnJoin.Text = "Join";
            btnJoin.UseVisualStyleBackColor = true;
            btnJoin.Click += btnJoin_Click;
            // 
            // btnCreate
            // 
            btnCreate.Location = new System.Drawing.Point(467, 201);
            btnCreate.Name = "btnCreate";
            btnCreate.Size = new System.Drawing.Size(75, 29);
            btnCreate.TabIndex = 12;
            btnCreate.Text = "Create";
            btnCreate.UseVisualStyleBackColor = true;
            btnCreate.Click += btnCreate_Click;
            // 
            // Status
            // 
            Status.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { Label1 });
            Status.Location = new System.Drawing.Point(0, 396);
            Status.Name = "Status";
            Status.Size = new System.Drawing.Size(635, 22);
            Status.TabIndex = 13;
            // 
            // Label1
            // 
            Label1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
            Label1.Name = "Label1";
            Label1.Size = new System.Drawing.Size(54, 17);
            Label1.Text = "Ready...";
            // 
            // Form_Kaillera
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(635, 418);
            Controls.Add(Status);
            Controls.Add(btnCreate);
            Controls.Add(btnJoin);
            Controls.Add(edChat);
            Controls.Add(edUserName);
            Controls.Add(labUser);
            Controls.Add(btnSend);
            Controls.Add(tbChat);
            Controls.Add(lvGames);
            Controls.Add(btnConnect);
            Controls.Add(lvSrv);
            Controls.Add(edAddress);
            Controls.Add(btnRefrsh);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Name = "Form_Kaillera";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Form_Kaillera";
            Load += Form_Kaillera_Load;
            Shown += Form_Kaillera_Shown;
            Status.ResumeLayout(false);
            Status.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btnRefrsh;
        private System.Windows.Forms.TextBox edAddress;
        private System.Windows.Forms.ListView lvSrv;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.ListView lvGames;
        private System.Windows.Forms.TextBox tbChat;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Label labUser;
        private System.Windows.Forms.TextBox edUserName;
        private System.Windows.Forms.TextBox edChat;
        private System.Windows.Forms.Button btnJoin;
        private System.Windows.Forms.Button btnCreate;
        private System.Windows.Forms.StatusStrip Status;
        private System.Windows.Forms.ToolStripStatusLabel Label1;
        private System.Windows.Forms.ColumnHeader columnHeader0;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
    }
}