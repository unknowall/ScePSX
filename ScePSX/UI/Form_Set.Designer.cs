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
            groupBox1 = new System.Windows.Forms.GroupBox();
            cbscalemode = new System.Windows.Forms.ComboBox();
            label7 = new System.Windows.Forms.Label();
            chkTTY = new System.Windows.Forms.CheckBox();
            chkcpu = new System.Windows.Forms.CheckBox();
            chkbios = new System.Windows.Forms.CheckBox();
            tbcylesfix = new System.Windows.Forms.TextBox();
            label9 = new System.Windows.Forms.Label();
            tbframeidle = new System.Windows.Forms.TextBox();
            label8 = new System.Windows.Forms.Label();
            tbaudiobuffer = new System.Windows.Forms.TextBox();
            label6 = new System.Windows.Forms.Label();
            tbframeskip = new System.Windows.Forms.TextBox();
            label5 = new System.Windows.Forms.Label();
            cbmsaa = new System.Windows.Forms.ComboBox();
            label4 = new System.Windows.Forms.Label();
            tbcputicks = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            tbbuscycles = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            cbconsole = new System.Windows.Forms.CheckBox();
            label1 = new System.Windows.Forms.Label();
            cbbios = new System.Windows.Forms.ComboBox();
            btndel = new System.Windows.Forms.Button();
            label10 = new System.Windows.Forms.Label();
            cbcpumode = new System.Windows.Forms.ComboBox();
            label11 = new System.Windows.Forms.Label();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // btnsave
            // 
            btnsave.Location = new System.Drawing.Point(343, 227);
            btnsave.Name = "btnsave";
            btnsave.Size = new System.Drawing.Size(75, 27);
            btnsave.TabIndex = 0;
            btnsave.Text = Properties.Resources.Form_Set_InitializeComponent_save;
            btnsave.UseVisualStyleBackColor = true;
            btnsave.Click += btnsave_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(cbscalemode);
            groupBox1.Controls.Add(label7);
            groupBox1.Controls.Add(chkTTY);
            groupBox1.Controls.Add(chkcpu);
            groupBox1.Controls.Add(chkbios);
            groupBox1.Controls.Add(tbcylesfix);
            groupBox1.Controls.Add(label9);
            groupBox1.Controls.Add(tbframeidle);
            groupBox1.Controls.Add(label8);
            groupBox1.Controls.Add(tbaudiobuffer);
            groupBox1.Controls.Add(label6);
            groupBox1.Controls.Add(tbframeskip);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(cbmsaa);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(tbcputicks);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(tbbuscycles);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(cbconsole);
            groupBox1.Location = new System.Drawing.Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(411, 171);
            groupBox1.TabIndex = 2;
            groupBox1.TabStop = false;
            // 
            // cbscalemode
            // 
            cbscalemode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbscalemode.FormattingEnabled = true;
            cbscalemode.Items.AddRange(new object[] { "xBR", "Fast" });
            cbscalemode.Location = new System.Drawing.Point(331, 44);
            cbscalemode.Name = "cbscalemode";
            cbscalemode.Size = new System.Drawing.Size(66, 25);
            cbscalemode.TabIndex = 20;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(229, 49);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(54, 17);
            label7.TabIndex = 19;
            label7.Text = "IR Scale";
            // 
            // chkTTY
            // 
            chkTTY.AutoSize = true;
            chkTTY.Location = new System.Drawing.Point(334, 17);
            chkTTY.Name = "chkTTY";
            chkTTY.Size = new System.Drawing.Size(71, 21);
            chkTTY.TabIndex = 18;
            chkTTY.Text = "TTY log";
            chkTTY.UseVisualStyleBackColor = true;
            // 
            // chkcpu
            // 
            chkcpu.AutoSize = true;
            chkcpu.Location = new System.Drawing.Point(250, 18);
            chkcpu.Name = "chkcpu";
            chkcpu.Size = new System.Drawing.Size(74, 21);
            chkcpu.TabIndex = 17;
            chkcpu.Text = "CPU log";
            chkcpu.UseVisualStyleBackColor = true;
            // 
            // chkbios
            // 
            chkbios.AutoSize = true;
            chkbios.Location = new System.Drawing.Point(156, 18);
            chkbios.Name = "chkbios";
            chkbios.Size = new System.Drawing.Size(75, 21);
            chkbios.TabIndex = 16;
            chkbios.Text = "Bios log";
            chkbios.UseVisualStyleBackColor = true;
            // 
            // tbcylesfix
            // 
            tbcylesfix.Location = new System.Drawing.Point(331, 74);
            tbcylesfix.Name = "tbcylesfix";
            tbcylesfix.Size = new System.Drawing.Size(66, 23);
            tbcylesfix.TabIndex = 15;
            tbcylesfix.KeyPress += edtxt_KeyPress;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(229, 78);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(55, 17);
            label9.TabIndex = 14;
            label9.Text = "Cyles fix";
            // 
            // tbframeidle
            // 
            tbframeidle.Location = new System.Drawing.Point(331, 103);
            tbframeidle.Name = "tbframeidle";
            tbframeidle.Size = new System.Drawing.Size(66, 23);
            tbframeidle.TabIndex = 13;
            tbframeidle.KeyPress += edtxt_KeyPress;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(229, 107);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(72, 17);
            label8.TabIndex = 12;
            label8.Text = "Frame limit";
            // 
            // tbaudiobuffer
            // 
            tbaudiobuffer.Location = new System.Drawing.Point(116, 104);
            tbaudiobuffer.Name = "tbaudiobuffer";
            tbaudiobuffer.Size = new System.Drawing.Size(96, 23);
            tbaudiobuffer.TabIndex = 10;
            tbaudiobuffer.KeyPress += edtxt_KeyPress;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(16, 109);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(102, 17);
            label6.TabIndex = 9;
            label6.Text = "Audiobuffer(ms)";
            // 
            // tbframeskip
            // 
            tbframeskip.Location = new System.Drawing.Point(331, 134);
            tbframeskip.Name = "tbframeskip";
            tbframeskip.Size = new System.Drawing.Size(66, 23);
            tbframeskip.TabIndex = 8;
            tbframeskip.KeyPress += edtxt_KeyPress;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(229, 137);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(72, 17);
            label5.TabIndex = 7;
            label5.Text = "Frame skip";
            // 
            // cbmsaa
            // 
            cbmsaa.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
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
            // tbcputicks
            // 
            tbcputicks.Location = new System.Drawing.Point(116, 75);
            tbcputicks.Name = "tbcputicks";
            tbcputicks.Size = new System.Drawing.Size(96, 23);
            tbcputicks.TabIndex = 4;
            tbcputicks.KeyPress += edtxt_KeyPress;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(16, 78);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(65, 17);
            label3.TabIndex = 3;
            label3.Text = "CPU Ticks";
            // 
            // tbbuscycles
            // 
            tbbuscycles.Location = new System.Drawing.Point(116, 46);
            tbbuscycles.Name = "tbbuscycles";
            tbbuscycles.Size = new System.Drawing.Size(96, 23);
            tbbuscycles.TabIndex = 2;
            tbbuscycles.KeyPress += edtxt_KeyPress;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(16, 49);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(62, 17);
            label2.TabIndex = 1;
            label2.Text = "Bus Ticks";
            // 
            // cbconsole
            // 
            cbconsole.AutoSize = true;
            cbconsole.Location = new System.Drawing.Point(16, 18);
            cbconsole.Name = "cbconsole";
            cbconsole.Size = new System.Drawing.Size(115, 21);
            cbconsole.TabIndex = 0;
            cbconsole.Text = Properties.Resources.Form_Set_InitializeComponent_con;
            cbconsole.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(13, 193);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(37, 17);
            label1.TabIndex = 3;
            label1.Text = "BIOS";
            // 
            // cbbios
            // 
            cbbios.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbbios.FormattingEnabled = true;
            cbbios.Location = new System.Drawing.Point(61, 190);
            cbbios.Name = "cbbios";
            cbbios.Size = new System.Drawing.Size(141, 25);
            cbbios.TabIndex = 4;
            // 
            // btndel
            // 
            btndel.Location = new System.Drawing.Point(236, 228);
            btndel.Name = "btndel";
            btndel.Size = new System.Drawing.Size(101, 27);
            btndel.TabIndex = 5;
            btndel.Text = Properties.Resources.Form_Set_InitializeComponent_gbs;
            btndel.UseVisualStyleBackColor = true;
            btndel.Click += btndel_Click;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(230, 193);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(32, 17);
            label10.TabIndex = 6;
            label10.Text = "CPU";
            // 
            // cbcpumode
            // 
            cbcpumode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbcpumode.FormattingEnabled = true;
            cbcpumode.Items.AddRange(new object[] { Properties.Resources.Form_Set_InitializeComponent_性能优化模式, Properties.Resources.Form_Set_InitializeComponent_完整指令模式 });
            cbcpumode.Location = new System.Drawing.Point(277, 190);
            cbcpumode.Name = "cbcpumode";
            cbcpumode.Size = new System.Drawing.Size(141, 25);
            cbcpumode.TabIndex = 7;
            // 
            // label11
            // 
            label11.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
            label11.Location = new System.Drawing.Point(12, 231);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(211, 23);
            label11.TabIndex = 8;
            label11.Text = "Modify only if known";
            // 
            // Form_Set
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(435, 266);
            Controls.Add(label11);
            Controls.Add(cbcpumode);
            Controls.Add(label10);
            Controls.Add(btndel);
            Controls.Add(cbbios);
            Controls.Add(label1);
            Controls.Add(groupBox1);
            Controls.Add(btnsave);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Form_Set";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Setting";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btnsave;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbcputicks;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbbuscycles;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox cbconsole;
        private System.Windows.Forms.ComboBox cbmsaa;
        private System.Windows.Forms.TextBox tbframeskip;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbaudiobuffer;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox tbframeidle;
        private System.Windows.Forms.TextBox tbcylesfix;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.CheckBox chkcpu;
        private System.Windows.Forms.CheckBox chkbios;
        private System.Windows.Forms.CheckBox chkTTY;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbbios;
        private System.Windows.Forms.Button btndel;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox cbcpumode;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox cbscalemode;
        private System.Windows.Forms.Label label7;
    }
}
