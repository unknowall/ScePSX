using System.Reflection;

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
            BtnSave = new System.Windows.Forms.Button();
            btndel = new System.Windows.Forms.Button();
            labSetHint = new System.Windows.Forms.Label();
            SetTab = new System.Windows.Forms.TabControl();
            SetPage1 = new System.Windows.Forms.TabPage();
            ChkFMV = new System.Windows.Forms.CheckBox();
            cbcdrom = new System.Windows.Forms.ComboBox();
            label14 = new System.Windows.Forms.Label();
            chkkeepar = new System.Windows.Forms.CheckBox();
            chkpgxpt = new System.Windows.Forms.CheckBox();
            chkrealcolor = new System.Windows.Forms.CheckBox();
            cbgpures = new System.Windows.Forms.ComboBox();
            labIRScale = new System.Windows.Forms.Label();
            cbgpu = new System.Windows.Forms.ComboBox();
            label12 = new System.Windows.Forms.Label();
            cbcpumode = new System.Windows.Forms.ComboBox();
            label10 = new System.Windows.Forms.Label();
            cbbios = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            groupBox1 = new System.Windows.Forms.GroupBox();
            cbscalemode = new System.Windows.Forms.ComboBox();
            labSoftScale = new System.Windows.Forms.Label();
            chkTTY = new System.Windows.Forms.CheckBox();
            chkcpu = new System.Windows.Forms.CheckBox();
            chkbios = new System.Windows.Forms.CheckBox();
            tbcylesfix = new System.Windows.Forms.TextBox();
            labCycleFix = new System.Windows.Forms.Label();
            tbframeidle = new System.Windows.Forms.TextBox();
            labFrameLimit = new System.Windows.Forms.Label();
            tbaudiobuffer = new System.Windows.Forms.TextBox();
            labAudioBuff = new System.Windows.Forms.Label();
            tbframeskip = new System.Windows.Forms.TextBox();
            labFrameSkip = new System.Windows.Forms.Label();
            cbmsaa = new System.Windows.Forms.ComboBox();
            label4 = new System.Windows.Forms.Label();
            tbcputicks = new System.Windows.Forms.TextBox();
            labCpuTick = new System.Windows.Forms.Label();
            tbbuscycles = new System.Windows.Forms.TextBox();
            labBusTick = new System.Windows.Forms.Label();
            chkconsole = new System.Windows.Forms.CheckBox();
            SetPage2 = new System.Windows.Forms.TabPage();
            chkpgxp_avs = new System.Windows.Forms.CheckBox();
            chkpgxp_clip = new System.Windows.Forms.CheckBox();
            chkpgxp_memcap = new System.Windows.Forms.CheckBox();
            chkpgxp_nc = new System.Windows.Forms.CheckBox();
            chkpgxp_ppc = new System.Windows.Forms.CheckBox();
            chkpgxp_aff = new System.Windows.Forms.CheckBox();
            chkpgxp_highpos = new System.Windows.Forms.CheckBox();
            labPGXP = new System.Windows.Forms.Label();
            chkpgxp = new System.Windows.Forms.CheckBox();
            panelBottom = new System.Windows.Forms.Panel();
            SetTab.SuspendLayout();
            SetPage1.SuspendLayout();
            groupBox1.SuspendLayout();
            SetPage2.SuspendLayout();
            panelBottom.SuspendLayout();
            SuspendLayout();
            // 
            // BtnSave
            // 
            BtnSave.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
            BtnSave.Location = new System.Drawing.Point(478, 11);
            BtnSave.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            BtnSave.Name = "BtnSave";
            BtnSave.Size = new System.Drawing.Size(90, 32);
            BtnSave.TabIndex = 0;
            BtnSave.Text = "保存";
            BtnSave.UseVisualStyleBackColor = true;
            BtnSave.Click += btnsave_Click;
            // 
            // btndel
            // 
            btndel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
            btndel.Location = new System.Drawing.Point(368, 11);
            btndel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            btndel.Name = "btndel";
            btndel.Size = new System.Drawing.Size(90, 32);
            btndel.TabIndex = 5;
            btndel.Text = "删除";
            btndel.UseVisualStyleBackColor = true;
            btndel.Click += btndel_Click;
            // 
            // labSetHint
            // 
            labSetHint.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
            labSetHint.Location = new System.Drawing.Point(20, 15);
            labSetHint.Name = "labSetHint";
            labSetHint.Size = new System.Drawing.Size(180, 23);
            labSetHint.TabIndex = 8;
            labSetHint.Text = "只修改清楚作用的设置";
            labSetHint.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // SetTab
            // 
            SetTab.Controls.Add(SetPage1);
            SetTab.Controls.Add(SetPage2);
            SetTab.Dock = System.Windows.Forms.DockStyle.Fill;
            SetTab.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
            SetTab.Location = new System.Drawing.Point(0, 0);
            SetTab.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            SetTab.Name = "SetTab";
            SetTab.SelectedIndex = 0;
            SetTab.Size = new System.Drawing.Size(580, 520);
            SetTab.TabIndex = 18;
            // 
            // SetPage1
            // 
            SetPage1.Controls.Add(ChkFMV);
            SetPage1.Controls.Add(cbcdrom);
            SetPage1.Controls.Add(label14);
            SetPage1.Controls.Add(chkkeepar);
            SetPage1.Controls.Add(chkpgxpt);
            SetPage1.Controls.Add(chkrealcolor);
            SetPage1.Controls.Add(cbgpures);
            SetPage1.Controls.Add(labIRScale);
            SetPage1.Controls.Add(cbgpu);
            SetPage1.Controls.Add(label12);
            SetPage1.Controls.Add(cbcpumode);
            SetPage1.Controls.Add(label10);
            SetPage1.Controls.Add(cbbios);
            SetPage1.Controls.Add(label1);
            SetPage1.Controls.Add(groupBox1);
            SetPage1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
            SetPage1.Location = new System.Drawing.Point(4, 26);
            SetPage1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            SetPage1.Name = "SetPage1";
            SetPage1.Padding = new System.Windows.Forms.Padding(10);
            SetPage1.Size = new System.Drawing.Size(572, 490);
            SetPage1.TabIndex = 0;
            SetPage1.Text = "Base";
            SetPage1.UseVisualStyleBackColor = true;
            // 
            // ChkFMV
            // 
            ChkFMV.AutoSize = true;
            ChkFMV.Location = new System.Drawing.Point(290, 461);
            ChkFMV.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            ChkFMV.Name = "ChkFMV";
            ChkFMV.Size = new System.Drawing.Size(77, 21);
            ChkFMV.TabIndex = 33;
            ChkFMV.Text = "启用FMV";
            ChkFMV.UseVisualStyleBackColor = true;
            // 
            // cbcdrom
            // 
            cbcdrom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbcdrom.FormattingEnabled = true;
            cbcdrom.Items.AddRange(new object[] { "自适应", "1x", "2x", "3x", "4x", "5x", "6x", "7x", "8x", "9x", "10x" });
            cbcdrom.Location = new System.Drawing.Point(120, 394);
            cbcdrom.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cbcdrom.Name = "cbcdrom";
            cbcdrom.Size = new System.Drawing.Size(150, 25);
            cbcdrom.TabIndex = 32;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new System.Drawing.Point(20, 399);
            label14.Name = "label14";
            label14.Size = new System.Drawing.Size(55, 17);
            label14.TabIndex = 31;
            label14.Text = "CDROM";
            // 
            // chkkeepar
            // 
            chkkeepar.AutoSize = true;
            chkkeepar.Location = new System.Drawing.Point(20, 461);
            chkkeepar.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkkeepar.Name = "chkkeepar";
            chkkeepar.Size = new System.Drawing.Size(75, 21);
            chkkeepar.TabIndex = 29;
            chkkeepar.Text = "保持比例";
            chkkeepar.UseVisualStyleBackColor = true;
            // 
            // chkpgxpt
            // 
            chkpgxpt.AutoSize = true;
            chkpgxpt.Location = new System.Drawing.Point(290, 431);
            chkpgxpt.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkpgxpt.Name = "chkpgxpt";
            chkpgxpt.Size = new System.Drawing.Size(82, 21);
            chkpgxpt.TabIndex = 30;
            chkpgxpt.Text = "PGXP纹理";
            chkpgxpt.UseVisualStyleBackColor = true;
            // 
            // chkrealcolor
            // 
            chkrealcolor.AutoSize = true;
            chkrealcolor.Location = new System.Drawing.Point(20, 431);
            chkrealcolor.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkrealcolor.Name = "chkrealcolor";
            chkrealcolor.Size = new System.Drawing.Size(75, 21);
            chkrealcolor.TabIndex = 27;
            chkrealcolor.Text = "真实色彩";
            chkrealcolor.UseVisualStyleBackColor = true;
            // 
            // cbgpures
            // 
            cbgpures.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbgpures.Enabled = false;
            cbgpures.FormattingEnabled = true;
            cbgpures.Items.AddRange(new object[] { "自适应", "1x", "2x", "3x(720P)", "4x", "5x(1080P)", "6x(1440P)", "7x", "8x", "9x(4K)", "10x", "11x", "12x" });
            cbgpures.Location = new System.Drawing.Point(444, 354);
            cbgpures.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cbgpures.Name = "cbgpures";
            cbgpures.Size = new System.Drawing.Size(115, 25);
            cbgpures.TabIndex = 26;
            // 
            // labIRScale
            // 
            labIRScale.AutoSize = true;
            labIRScale.Location = new System.Drawing.Point(290, 359);
            labIRScale.Name = "labIRScale";
            labIRScale.Size = new System.Drawing.Size(68, 17);
            labIRScale.TabIndex = 25;
            labIRScale.Text = "内部分辨率";
            // 
            // cbgpu
            // 
            cbgpu.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbgpu.FormattingEnabled = true;
            cbgpu.Items.AddRange(new object[] { "自适应", "Software", "OpenGL", "VulKan" });
            cbgpu.Location = new System.Drawing.Point(120, 354);
            cbgpu.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cbgpu.Name = "cbgpu";
            cbgpu.Size = new System.Drawing.Size(150, 25);
            cbgpu.TabIndex = 24;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new System.Drawing.Point(20, 359);
            label12.Name = "label12";
            label12.Size = new System.Drawing.Size(33, 17);
            label12.TabIndex = 23;
            label12.Text = "GPU";
            // 
            // cbcpumode
            // 
            cbcpumode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbcpumode.FormattingEnabled = true;
            cbcpumode.Location = new System.Drawing.Point(444, 314);
            cbcpumode.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cbcpumode.Name = "cbcpumode";
            cbcpumode.Size = new System.Drawing.Size(115, 25);
            cbcpumode.TabIndex = 22;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(290, 319);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(32, 17);
            label10.TabIndex = 21;
            label10.Text = "CPU";
            // 
            // cbbios
            // 
            cbbios.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbbios.FormattingEnabled = true;
            cbbios.Location = new System.Drawing.Point(120, 314);
            cbbios.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cbbios.Name = "cbbios";
            cbbios.Size = new System.Drawing.Size(150, 25);
            cbbios.TabIndex = 20;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(20, 319);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(37, 17);
            label1.TabIndex = 19;
            label1.Text = "BIOS";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(cbscalemode);
            groupBox1.Controls.Add(labSoftScale);
            groupBox1.Controls.Add(chkTTY);
            groupBox1.Controls.Add(chkcpu);
            groupBox1.Controls.Add(chkbios);
            groupBox1.Controls.Add(tbcylesfix);
            groupBox1.Controls.Add(labCycleFix);
            groupBox1.Controls.Add(tbframeidle);
            groupBox1.Controls.Add(labFrameLimit);
            groupBox1.Controls.Add(tbaudiobuffer);
            groupBox1.Controls.Add(labAudioBuff);
            groupBox1.Controls.Add(tbframeskip);
            groupBox1.Controls.Add(labFrameSkip);
            groupBox1.Controls.Add(cbmsaa);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(tbcputicks);
            groupBox1.Controls.Add(labCpuTick);
            groupBox1.Controls.Add(tbbuscycles);
            groupBox1.Controls.Add(labBusTick);
            groupBox1.Controls.Add(chkconsole);
            groupBox1.Location = new System.Drawing.Point(10, 10);
            groupBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(10);
            groupBox1.Size = new System.Drawing.Size(550, 296);
            groupBox1.TabIndex = 18;
            groupBox1.TabStop = false;
            // 
            // cbscalemode
            // 
            cbscalemode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbscalemode.FormattingEnabled = true;
            cbscalemode.Items.AddRange(new object[] { "Neighbor", "JINC", "xBR" });
            cbscalemode.Location = new System.Drawing.Point(400, 172);
            cbscalemode.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cbscalemode.Name = "cbscalemode";
            cbscalemode.Size = new System.Drawing.Size(130, 25);
            cbscalemode.TabIndex = 20;
            // 
            // labSoftScale
            // 
            labSoftScale.AutoSize = true;
            labSoftScale.Location = new System.Drawing.Point(20, 177);
            labSoftScale.Name = "labSoftScale";
            labSoftScale.Size = new System.Drawing.Size(80, 17);
            labSoftScale.TabIndex = 19;
            labSoftScale.Text = "软件后端缩放";
            // 
            // chkTTY
            // 
            chkTTY.AutoSize = true;
            chkTTY.Location = new System.Drawing.Point(450, 20);
            chkTTY.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkTTY.Name = "chkTTY";
            chkTTY.Size = new System.Drawing.Size(71, 21);
            chkTTY.TabIndex = 18;
            chkTTY.Text = "TTY log";
            chkTTY.UseVisualStyleBackColor = true;
            // 
            // chkcpu
            // 
            chkcpu.AutoSize = true;
            chkcpu.Location = new System.Drawing.Point(350, 20);
            chkcpu.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkcpu.Name = "chkcpu";
            chkcpu.Size = new System.Drawing.Size(74, 21);
            chkcpu.TabIndex = 17;
            chkcpu.Text = "CPU log";
            chkcpu.UseVisualStyleBackColor = true;
            // 
            // chkbios
            // 
            chkbios.AutoSize = true;
            chkbios.Location = new System.Drawing.Point(250, 20);
            chkbios.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkbios.Name = "chkbios";
            chkbios.Size = new System.Drawing.Size(75, 21);
            chkbios.TabIndex = 16;
            chkbios.Text = "Bios log";
            chkbios.UseVisualStyleBackColor = true;
            // 
            // tbcylesfix
            // 
            tbcylesfix.Location = new System.Drawing.Point(400, 202);
            tbcylesfix.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tbcylesfix.Name = "tbcylesfix";
            tbcylesfix.Size = new System.Drawing.Size(130, 23);
            tbcylesfix.TabIndex = 15;
            // 
            // labCycleFix
            // 
            labCycleFix.AutoSize = true;
            labCycleFix.Location = new System.Drawing.Point(20, 207);
            labCycleFix.Name = "labCycleFix";
            labCycleFix.Size = new System.Drawing.Size(56, 17);
            labCycleFix.TabIndex = 14;
            labCycleFix.Text = "时序修正";
            // 
            // tbframeidle
            // 
            tbframeidle.Location = new System.Drawing.Point(400, 232);
            tbframeidle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tbframeidle.Name = "tbframeidle";
            tbframeidle.Size = new System.Drawing.Size(130, 23);
            tbframeidle.TabIndex = 13;
            // 
            // labFrameLimit
            // 
            labFrameLimit.AutoSize = true;
            labFrameLimit.Location = new System.Drawing.Point(20, 237);
            labFrameLimit.Name = "labFrameLimit";
            labFrameLimit.Size = new System.Drawing.Size(56, 17);
            labFrameLimit.TabIndex = 12;
            labFrameLimit.Text = "帧率限制";
            // 
            // tbaudiobuffer
            // 
            tbaudiobuffer.Location = new System.Drawing.Point(400, 112);
            tbaudiobuffer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tbaudiobuffer.Name = "tbaudiobuffer";
            tbaudiobuffer.Size = new System.Drawing.Size(130, 23);
            tbaudiobuffer.TabIndex = 10;
            // 
            // labAudioBuff
            // 
            labAudioBuff.AutoSize = true;
            labAudioBuff.Location = new System.Drawing.Point(20, 117);
            labAudioBuff.Name = "labAudioBuff";
            labAudioBuff.Size = new System.Drawing.Size(81, 17);
            labAudioBuff.TabIndex = 9;
            labAudioBuff.Text = "音频缓冲(ms)";
            // 
            // tbframeskip
            // 
            tbframeskip.Location = new System.Drawing.Point(400, 262);
            tbframeskip.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tbframeskip.Name = "tbframeskip";
            tbframeskip.Size = new System.Drawing.Size(130, 23);
            tbframeskip.TabIndex = 8;
            // 
            // labFrameSkip
            // 
            labFrameSkip.AutoSize = true;
            labFrameSkip.Location = new System.Drawing.Point(20, 267);
            labFrameSkip.Name = "labFrameSkip";
            labFrameSkip.Size = new System.Drawing.Size(89, 17);
            labFrameSkip.TabIndex = 7;
            labFrameSkip.Text = "D2D,D3D 跳帧";
            // 
            // cbmsaa
            // 
            cbmsaa.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbmsaa.FormattingEnabled = true;
            cbmsaa.Items.AddRange(new object[] { "None MSAA", "4xMSAA", "6xMSAA", "8xMSAA", "16xMSAA" });
            cbmsaa.Location = new System.Drawing.Point(400, 142);
            cbmsaa.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cbmsaa.Name = "cbmsaa";
            cbmsaa.Size = new System.Drawing.Size(130, 25);
            cbmsaa.TabIndex = 6;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(20, 147);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(43, 17);
            label4.TabIndex = 5;
            label4.Text = "MSAA";
            // 
            // tbcputicks
            // 
            tbcputicks.Location = new System.Drawing.Point(400, 82);
            tbcputicks.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tbcputicks.Name = "tbcputicks";
            tbcputicks.Size = new System.Drawing.Size(130, 23);
            tbcputicks.TabIndex = 4;
            // 
            // labCpuTick
            // 
            labCpuTick.AutoSize = true;
            labCpuTick.Location = new System.Drawing.Point(20, 87);
            labCpuTick.Name = "labCpuTick";
            labCpuTick.Size = new System.Drawing.Size(56, 17);
            labCpuTick.TabIndex = 3;
            labCpuTick.Text = "CPU执行";
            // 
            // tbbuscycles
            // 
            tbbuscycles.Location = new System.Drawing.Point(400, 52);
            tbbuscycles.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tbbuscycles.Name = "tbbuscycles";
            tbbuscycles.Size = new System.Drawing.Size(130, 23);
            tbbuscycles.TabIndex = 2;
            // 
            // labBusTick
            // 
            labBusTick.AutoSize = true;
            labBusTick.Location = new System.Drawing.Point(20, 57);
            labBusTick.Name = "labBusTick";
            labBusTick.Size = new System.Drawing.Size(56, 17);
            labBusTick.TabIndex = 1;
            labBusTick.Text = "总线执行";
            // 
            // chkconsole
            // 
            chkconsole.AutoSize = true;
            chkconsole.Location = new System.Drawing.Point(20, 24);
            chkconsole.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkconsole.Name = "chkconsole";
            chkconsole.Size = new System.Drawing.Size(87, 21);
            chkconsole.TabIndex = 0;
            chkconsole.Text = "控制台日志";
            chkconsole.UseVisualStyleBackColor = true;
            // 
            // SetPage2
            // 
            SetPage2.Controls.Add(chkpgxp_avs);
            SetPage2.Controls.Add(chkpgxp_clip);
            SetPage2.Controls.Add(chkpgxp_memcap);
            SetPage2.Controls.Add(chkpgxp_nc);
            SetPage2.Controls.Add(chkpgxp_ppc);
            SetPage2.Controls.Add(chkpgxp_aff);
            SetPage2.Controls.Add(chkpgxp_highpos);
            SetPage2.Controls.Add(labPGXP);
            SetPage2.Controls.Add(chkpgxp);
            SetPage2.Location = new System.Drawing.Point(4, 26);
            SetPage2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            SetPage2.Name = "SetPage2";
            SetPage2.Padding = new System.Windows.Forms.Padding(10);
            SetPage2.Size = new System.Drawing.Size(572, 490);
            SetPage2.TabIndex = 1;
            SetPage2.Text = "PGXP";
            SetPage2.UseVisualStyleBackColor = true;
            // 
            // chkpgxp_avs
            // 
            chkpgxp_avs.AutoSize = true;
            chkpgxp_avs.Location = new System.Drawing.Point(12, 113);
            chkpgxp_avs.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkpgxp_avs.Name = "chkpgxp_avs";
            chkpgxp_avs.Size = new System.Drawing.Size(78, 21);
            chkpgxp_avs.TabIndex = 37;
            chkpgxp_avs.Text = "AVS 修正";
            chkpgxp_avs.UseVisualStyleBackColor = true;
            // 
            // chkpgxp_clip
            // 
            chkpgxp_clip.AutoSize = true;
            chkpgxp_clip.Location = new System.Drawing.Point(12, 286);
            chkpgxp_clip.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkpgxp_clip.Name = "chkpgxp_clip";
            chkpgxp_clip.Size = new System.Drawing.Size(75, 21);
            chkpgxp_clip.TabIndex = 36;
            chkpgxp_clip.Text = "裁剪修正";
            chkpgxp_clip.UseVisualStyleBackColor = true;
            // 
            // chkpgxp_memcap
            // 
            chkpgxp_memcap.AutoSize = true;
            chkpgxp_memcap.Location = new System.Drawing.Point(12, 250);
            chkpgxp_memcap.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkpgxp_memcap.Name = "chkpgxp_memcap";
            chkpgxp_memcap.Size = new System.Drawing.Size(75, 21);
            chkpgxp_memcap.TabIndex = 35;
            chkpgxp_memcap.Text = "内存限制";
            chkpgxp_memcap.UseVisualStyleBackColor = true;
            // 
            // chkpgxp_nc
            // 
            chkpgxp_nc.AutoSize = true;
            chkpgxp_nc.Location = new System.Drawing.Point(12, 215);
            chkpgxp_nc.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkpgxp_nc.Name = "chkpgxp_nc";
            chkpgxp_nc.Size = new System.Drawing.Size(75, 21);
            chkpgxp_nc.TabIndex = 34;
            chkpgxp_nc.Text = "近裁剪面";
            chkpgxp_nc.UseVisualStyleBackColor = true;
            // 
            // chkpgxp_ppc
            // 
            chkpgxp_ppc.AutoSize = true;
            chkpgxp_ppc.Location = new System.Drawing.Point(12, 181);
            chkpgxp_ppc.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkpgxp_ppc.Name = "chkpgxp_ppc";
            chkpgxp_ppc.Size = new System.Drawing.Size(75, 21);
            chkpgxp_ppc.TabIndex = 33;
            chkpgxp_ppc.Text = "透视修正";
            chkpgxp_ppc.UseVisualStyleBackColor = true;
            // 
            // chkpgxp_aff
            // 
            chkpgxp_aff.AutoSize = true;
            chkpgxp_aff.Location = new System.Drawing.Point(12, 146);
            chkpgxp_aff.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkpgxp_aff.Name = "chkpgxp_aff";
            chkpgxp_aff.Size = new System.Drawing.Size(75, 21);
            chkpgxp_aff.TabIndex = 32;
            chkpgxp_aff.Text = "仿射修正";
            chkpgxp_aff.UseVisualStyleBackColor = true;
            // 
            // chkpgxp_highpos
            // 
            chkpgxp_highpos.AutoSize = true;
            chkpgxp_highpos.Location = new System.Drawing.Point(12, 82);
            chkpgxp_highpos.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkpgxp_highpos.Name = "chkpgxp_highpos";
            chkpgxp_highpos.Size = new System.Drawing.Size(87, 21);
            chkpgxp_highpos.TabIndex = 31;
            chkpgxp_highpos.Text = "高精度位置";
            chkpgxp_highpos.UseVisualStyleBackColor = true;
            // 
            // labPGXP
            // 
            labPGXP.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
            labPGXP.Location = new System.Drawing.Point(20, 10);
            labPGXP.Name = "labPGXP";
            labPGXP.Size = new System.Drawing.Size(544, 30);
            labPGXP.TabIndex = 30;
            labPGXP.Text = "PGXP 设置";
            labPGXP.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // chkpgxp
            // 
            chkpgxp.AutoSize = true;
            chkpgxp.Location = new System.Drawing.Point(12, 50);
            chkpgxp.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkpgxp.Name = "chkpgxp";
            chkpgxp.Size = new System.Drawing.Size(82, 21);
            chkpgxp.TabIndex = 29;
            chkpgxp.Text = "启用PGXP";
            chkpgxp.UseVisualStyleBackColor = true;
            // 
            // panelBottom
            // 
            panelBottom.Controls.Add(labSetHint);
            panelBottom.Controls.Add(btndel);
            panelBottom.Controls.Add(BtnSave);
            panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            panelBottom.Location = new System.Drawing.Point(0, 520);
            panelBottom.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            panelBottom.Name = "panelBottom";
            panelBottom.Size = new System.Drawing.Size(580, 55);
            panelBottom.TabIndex = 19;
            // 
            // Form_Set
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(580, 575);
            Controls.Add(SetTab);
            Controls.Add(panelBottom);
            Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            MaximizeBox = false;
            Name = "Form_Set";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Settings";
            Shown += Form_Set_Shown;
            SetTab.ResumeLayout(false);
            SetPage1.ResumeLayout(false);
            SetPage1.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            SetPage2.ResumeLayout(false);
            SetPage2.PerformLayout();
            panelBottom.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button BtnSave;
        private System.Windows.Forms.Button btndel;
        private System.Windows.Forms.Label labSetHint;
        private System.Windows.Forms.TabControl SetTab;
        private System.Windows.Forms.TabPage SetPage1;
        private System.Windows.Forms.TabPage SetPage2;
        private System.Windows.Forms.ComboBox cbcdrom;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.CheckBox chkkeepar;
        private System.Windows.Forms.CheckBox chkpgxpt;
        private System.Windows.Forms.CheckBox chkrealcolor;
        private System.Windows.Forms.ComboBox cbgpures;
        private System.Windows.Forms.Label labIRScale;
        private System.Windows.Forms.ComboBox cbgpu;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ComboBox cbcpumode;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox cbbios;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox cbscalemode;
        private System.Windows.Forms.Label labSoftScale;
        private System.Windows.Forms.CheckBox chkTTY;
        private System.Windows.Forms.CheckBox chkcpu;
        private System.Windows.Forms.CheckBox chkbios;
        private System.Windows.Forms.TextBox tbcylesfix;
        private System.Windows.Forms.Label labCycleFix;
        private System.Windows.Forms.TextBox tbframeidle;
        private System.Windows.Forms.Label labFrameLimit;
        private System.Windows.Forms.TextBox tbaudiobuffer;
        private System.Windows.Forms.Label labAudioBuff;
        private System.Windows.Forms.TextBox tbframeskip;
        private System.Windows.Forms.Label labFrameSkip;
        private System.Windows.Forms.ComboBox cbmsaa;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbcputicks;
        private System.Windows.Forms.Label labCpuTick;
        private System.Windows.Forms.TextBox tbbuscycles;
        private System.Windows.Forms.Label labBusTick;
        private System.Windows.Forms.CheckBox chkconsole;
        private System.Windows.Forms.CheckBox chkpgxp;
        private System.Windows.Forms.CheckBox ChkFMV;
        private System.Windows.Forms.Label labPGXP;
        private System.Windows.Forms.CheckBox chkpgxp_highpos;
        private System.Windows.Forms.CheckBox chkpgxp_nc;
        private System.Windows.Forms.CheckBox chkpgxp_ppc;
        private System.Windows.Forms.CheckBox chkpgxp_aff;
        private System.Windows.Forms.CheckBox chkpgxp_memcap;
        private System.Windows.Forms.CheckBox chkpgxp_clip;
        private System.Windows.Forms.CheckBox chkpgxp_avs;
        private System.Windows.Forms.Panel panelBottom;
    }
}