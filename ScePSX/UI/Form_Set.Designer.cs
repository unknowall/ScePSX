namespace ScePSX.UI
{
    partial class Form_Set
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
            btnsave = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            groupBox1 = new System.Windows.Forms.GroupBox();
            chkTTY = new System.Windows.Forms.CheckBox();
            chkcpu = new System.Windows.Forms.CheckBox();
            chkbios = new System.Windows.Forms.CheckBox();
            tbcyles = new System.Windows.Forms.TextBox();
            label9 = new System.Windows.Forms.Label();
            tbframeidle = new System.Windows.Forms.TextBox();
            label8 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            tbaudiobuffer = new System.Windows.Forms.TextBox();
            label6 = new System.Windows.Forms.Label();
            tbframeskip = new System.Windows.Forms.TextBox();
            label5 = new System.Windows.Forms.Label();
            cbmsaa = new System.Windows.Forms.ComboBox();
            label4 = new System.Windows.Forms.Label();
            tbmipslock = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            tbcpusync = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            cbconsole = new System.Windows.Forms.CheckBox();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // btnsave
            // 
            btnsave.Location = new System.Drawing.Point(348, 189);
            btnsave.Name = "btnsave";
            btnsave.Size = new System.Drawing.Size(75, 23);
            btnsave.TabIndex = 0;
            btnsave.Text = "保存";
            btnsave.UseVisualStyleBackColor = true;
            btnsave.Click += btnsave_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
            label1.Location = new System.Drawing.Point(12, 192);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(261, 19);
            label1.TabIndex = 1;
            label1.Text = "如果不清楚在做什么，不要修改任何设置";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(chkTTY);
            groupBox1.Controls.Add(chkcpu);
            groupBox1.Controls.Add(chkbios);
            groupBox1.Controls.Add(tbcyles);
            groupBox1.Controls.Add(label9);
            groupBox1.Controls.Add(tbframeidle);
            groupBox1.Controls.Add(label8);
            groupBox1.Controls.Add(label7);
            groupBox1.Controls.Add(tbaudiobuffer);
            groupBox1.Controls.Add(label6);
            groupBox1.Controls.Add(tbframeskip);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(cbmsaa);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(tbmipslock);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(tbcpusync);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(cbconsole);
            groupBox1.Location = new System.Drawing.Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(411, 171);
            groupBox1.TabIndex = 2;
            groupBox1.TabStop = false;
            groupBox1.Text = "所有改动重启后生效";
            // 
            // chkTTY
            // 
            chkTTY.AutoSize = true;
            chkTTY.Location = new System.Drawing.Point(334, 25);
            chkTTY.Name = "chkTTY";
            chkTTY.Size = new System.Drawing.Size(72, 21);
            chkTTY.TabIndex = 18;
            chkTTY.Text = "TTY输出";
            chkTTY.UseVisualStyleBackColor = true;
            // 
            // chkcpu
            // 
            chkcpu.AutoSize = true;
            chkcpu.Location = new System.Drawing.Point(250, 26);
            chkcpu.Name = "chkcpu";
            chkcpu.Size = new System.Drawing.Size(75, 21);
            chkcpu.TabIndex = 17;
            chkcpu.Text = "CPU调试";
            chkcpu.UseVisualStyleBackColor = true;
            // 
            // chkbios
            // 
            chkbios.AutoSize = true;
            chkbios.Location = new System.Drawing.Point(156, 26);
            chkbios.Name = "chkbios";
            chkbios.Size = new System.Drawing.Size(76, 21);
            chkbios.TabIndex = 16;
            chkbios.Text = "Bios调试";
            chkbios.UseVisualStyleBackColor = true;
            // 
            // tbcyles
            // 
            tbcyles.Location = new System.Drawing.Point(367, 59);
            tbcyles.Name = "tbcyles";
            tbcyles.Size = new System.Drawing.Size(30, 23);
            tbcyles.TabIndex = 15;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(294, 61);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(66, 17);
            label9.TabIndex = 14;
            label9.Text = "cycles修正";
            // 
            // tbframeidle
            // 
            tbframeidle.Location = new System.Drawing.Point(331, 103);
            tbframeidle.Name = "tbframeidle";
            tbframeidle.Size = new System.Drawing.Size(66, 23);
            tbframeidle.TabIndex = 13;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(229, 110);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(69, 17);
            label8.TabIndex = 12;
            label8.Text = "帧限制(ms)";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.ForeColor = System.Drawing.Color.Blue;
            label7.Location = new System.Drawing.Point(16, 84);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(330, 17);
            label7.TabIndex = 11;
            label7.Text = "如果碰到游戏只能进BIOS，首先更换BIOS，再尝试修改时序";
            // 
            // tbaudiobuffer
            // 
            tbaudiobuffer.Location = new System.Drawing.Point(116, 104);
            tbaudiobuffer.Name = "tbaudiobuffer";
            tbaudiobuffer.Size = new System.Drawing.Size(96, 23);
            tbaudiobuffer.TabIndex = 10;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(16, 109);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(81, 17);
            label6.TabIndex = 9;
            label6.Text = "音频缓冲(ms)";
            // 
            // tbframeskip
            // 
            tbframeskip.Location = new System.Drawing.Point(331, 134);
            tbframeskip.Name = "tbframeskip";
            tbframeskip.Size = new System.Drawing.Size(66, 23);
            tbframeskip.TabIndex = 8;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(229, 137);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(96, 17);
            label5.TabIndex = 7;
            label5.Text = "D2D1,D3D 跳帧";
            // 
            // cbmsaa
            // 
            cbmsaa.FormattingEnabled = true;
            cbmsaa.Items.AddRange(new object[] { "None MSAA", "4xMSAA", "6xMSAA", "8xMSAA", "16xMSAA" });
            cbmsaa.Location = new System.Drawing.Point(116, 133);
            cbmsaa.Name = "cbmsaa";
            cbmsaa.Size = new System.Drawing.Size(96, 25);
            cbmsaa.TabIndex = 6;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(16, 136);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(94, 17);
            label4.TabIndex = 5;
            label4.Text = "OpenGL MSAA";
            // 
            // tbmipslock
            // 
            tbmipslock.Location = new System.Drawing.Point(218, 58);
            tbmipslock.Name = "tbmipslock";
            tbmipslock.Size = new System.Drawing.Size(61, 23);
            tbmipslock.TabIndex = 4;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(156, 61);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(56, 17);
            label3.TabIndex = 3;
            label3.Text = "CPU步进";
            // 
            // tbcpusync
            // 
            tbcpusync.Location = new System.Drawing.Point(78, 58);
            tbcpusync.Name = "tbcpusync";
            tbcpusync.Size = new System.Drawing.Size(61, 23);
            tbcpusync.TabIndex = 2;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(16, 61);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(56, 17);
            label2.TabIndex = 1;
            label2.Text = "CPU时序";
            // 
            // cbconsole
            // 
            cbconsole.AutoSize = true;
            cbconsole.Location = new System.Drawing.Point(16, 26);
            cbconsole.Name = "cbconsole";
            cbconsole.Size = new System.Drawing.Size(123, 21);
            cbconsole.TabIndex = 0;
            cbconsole.Text = "启动时开启控制台";
            cbconsole.UseVisualStyleBackColor = true;
            // 
            // Form_Set
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(435, 224);
            Controls.Add(groupBox1);
            Controls.Add(label1);
            Controls.Add(btnsave);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Name = "Form_Set";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "设置";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btnsave;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbmipslock;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbcpusync;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox cbconsole;
        private System.Windows.Forms.ComboBox cbmsaa;
        private System.Windows.Forms.TextBox tbframeskip;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbaudiobuffer;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox tbframeidle;
        private System.Windows.Forms.TextBox tbcyles;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.CheckBox chkcpu;
        private System.Windows.Forms.CheckBox chkbios;
        private System.Windows.Forms.CheckBox chkTTY;
    }
}
