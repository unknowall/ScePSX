using ScePSX.Properties;

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
            btndel = new System.Windows.Forms.Button();
            label11 = new System.Windows.Forms.Label();
            SetTab = new System.Windows.Forms.TabControl();
            SetPage1 = new System.Windows.Forms.TabPage();
            ChkFMV = new System.Windows.Forms.CheckBox();
            cbcdrom = new System.Windows.Forms.ComboBox();
            label14 = new System.Windows.Forms.Label();
            chkkeepar = new System.Windows.Forms.CheckBox();
            chkpgxpt = new System.Windows.Forms.CheckBox();
            chkrealcolor = new System.Windows.Forms.CheckBox();
            cbgpures = new System.Windows.Forms.ComboBox();
            label13 = new System.Windows.Forms.Label();
            cbgpu = new System.Windows.Forms.ComboBox();
            label12 = new System.Windows.Forms.Label();
            cbcpumode = new System.Windows.Forms.ComboBox();
            label10 = new System.Windows.Forms.Label();
            cbbios = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
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
            SetTab.SuspendLayout();
            SetPage1.SuspendLayout();
            groupBox1.SuspendLayout();
            SetPage2.SuspendLayout();
            SuspendLayout();
            // 
            // btnsave
            // 
            btnsave.Location = new System.Drawing.Point(347, 361);
            btnsave.Name = "btnsave";
            btnsave.Size = new System.Drawing.Size(75, 27);
            btnsave.TabIndex = 0;
            btnsave.Text = Resources.Form_Set_InitializeComponent_save;
            btnsave.UseVisualStyleBackColor = true;
            btnsave.Click += btnsave_Click;
            // 
            // btndel
            // 
            btndel.Location = new System.Drawing.Point(240, 360);
            btndel.Name = "btndel";
            btndel.Size = new System.Drawing.Size(101, 27);
            btndel.TabIndex = 5;
            btndel.Text = Resources.Form_Set_InitializeComponent_gbs;
            btndel.UseVisualStyleBackColor = true;
            btndel.Click += btndel_Click;
            // 
            // label11
            // 
            label11.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
            label11.Location = new System.Drawing.Point(16, 362);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(211, 23);
            label11.TabIndex = 8;
            label11.Text = Resources.Form_Set_InitializeComponent_ModifyOnlyIfexp;
            // 
            // SetTab
            // 
            SetTab.Controls.Add(SetPage1);
            SetTab.Controls.Add(SetPage2);
            SetTab.Location = new System.Drawing.Point(12, 2);
            SetTab.Name = "SetTab";
            SetTab.SelectedIndex = 0;
            SetTab.Size = new System.Drawing.Size(432, 352);
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
            SetPage1.Controls.Add(label13);
            SetPage1.Controls.Add(cbgpu);
            SetPage1.Controls.Add(label12);
            SetPage1.Controls.Add(cbcpumode);
            SetPage1.Controls.Add(label10);
            SetPage1.Controls.Add(cbbios);
            SetPage1.Controls.Add(label1);
            SetPage1.Controls.Add(groupBox1);
            SetPage1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
            SetPage1.Location = new System.Drawing.Point(4, 26);
            SetPage1.Name = "SetPage1";
            SetPage1.Padding = new System.Windows.Forms.Padding(3);
            SetPage1.Size = new System.Drawing.Size(424, 322);
            SetPage1.TabIndex = 0;
            SetPage1.Text = "Base";
            SetPage1.UseVisualStyleBackColor = true;
            // 
            // ChkFMV
            // 
            ChkFMV.AutoSize = true;
            ChkFMV.Location = new System.Drawing.Point(305, 294);
            ChkFMV.Name = "ChkFMV";
            ChkFMV.Size = new System.Drawing.Size(86, 21);
            ChkFMV.TabIndex = 33;
            ChkFMV.Text = Resources.Form_Set_InitializeComponent_24bitfmv;
            ChkFMV.UseVisualStyleBackColor = true;
            // 
            // cbcdrom
            // 
            cbcdrom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbcdrom.FormattingEnabled = true;
            cbcdrom.Items.AddRange(new object[] { Resources.Form_Set_InitializeComponent_自适应, "1x", "2x", "3x", "4x", "5x", "6x", "7x", "8x", "9x", "10x" });
            cbcdrom.Location = new System.Drawing.Point(68, 255);
            cbcdrom.Name = "cbcdrom";
            cbcdrom.Size = new System.Drawing.Size(141, 25);
            cbcdrom.TabIndex = 32;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new System.Drawing.Point(9, 258);
            label14.Name = "label14";
            label14.Size = new System.Drawing.Size(55, 17);
            label14.TabIndex = 31;
            label14.Text = "CDROM";
            // 
            // chkkeepar
            // 
            chkkeepar.AutoSize = true;
            chkkeepar.Location = new System.Drawing.Point(210, 294);
            chkkeepar.Name = "chkkeepar";
            chkkeepar.Size = new System.Drawing.Size(87, 21);
            chkkeepar.TabIndex = 29;
            chkkeepar.Text = Resources.Form_Set_InitializeComponent_Keep43;
            chkkeepar.UseVisualStyleBackColor = true;
            // 
            // chkpgxpt
            // 
            chkpgxpt.AutoSize = true;
            chkpgxpt.Location = new System.Drawing.Point(108, 293);
            chkpgxpt.Name = "chkpgxpt";
            chkpgxpt.Size = new System.Drawing.Size(93, 21);
            chkpgxpt.TabIndex = 30;
            chkpgxpt.Text = Resources.Form_Set_InitializeComponent_Trianglefix;
            chkpgxpt.UseVisualStyleBackColor = true;
            // 
            // chkrealcolor
            // 
            chkrealcolor.AutoSize = true;
            chkrealcolor.Location = new System.Drawing.Point(9, 294);
            chkrealcolor.Name = "chkrealcolor";
            chkrealcolor.Size = new System.Drawing.Size(89, 21);
            chkrealcolor.TabIndex = 27;
            chkrealcolor.Text = Resources.Form_Set_InitializeComponent_truecolor;
            chkrealcolor.UseVisualStyleBackColor = true;
            // 
            // cbgpures
            // 
            cbgpures.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbgpures.Enabled = false;
            cbgpures.FormattingEnabled = true;
            cbgpures.Items.AddRange(new object[] { Resources.Form_Set_InitializeComponent_自适应, "1x", "2x", "3x(720P)", "4x", "5x(1080P)", "6x(1440P)", "7x", "8x", "9x(4K)", "10x", "11x", "12x" });
            cbgpures.Location = new System.Drawing.Point(303, 215);
            cbgpures.Name = "cbgpures";
            cbgpures.Size = new System.Drawing.Size(109, 25);
            cbgpures.TabIndex = 26;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new System.Drawing.Point(224, 219);
            label13.Name = "label13";
            label13.Size = new System.Drawing.Size(98, 17);
            label13.TabIndex = 25;
            label13.Text = Resources.Form_Set_InitializeComponent_GPUResolution;
            // 
            // cbgpu
            // 
            cbgpu.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbgpu.FormattingEnabled = true;
            cbgpu.Items.AddRange(new object[] { Resources.Form_Set_InitializeComponent_自适应, "Software", "OpenGL", "VulKan" });
            cbgpu.Location = new System.Drawing.Point(68, 215);
            cbgpu.Name = "cbgpu";
            cbgpu.Size = new System.Drawing.Size(141, 25);
            cbgpu.TabIndex = 24;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new System.Drawing.Point(7, 219);
            label12.Name = "label12";
            label12.Size = new System.Drawing.Size(33, 17);
            label12.TabIndex = 23;
            label12.Text = "GPU";
            // 
            // cbcpumode
            // 
            cbcpumode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbcpumode.FormattingEnabled = true;
            cbcpumode.Items.AddRange(new object[] { Resources.Form_Set_InitializeComponent_性能优化模式, Resources.Form_Set_InitializeComponent_完整指令模式 });
            cbcpumode.Location = new System.Drawing.Point(303, 178);
            cbcpumode.Name = "cbcpumode";
            cbcpumode.Size = new System.Drawing.Size(109, 25);
            cbcpumode.TabIndex = 22;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(224, 181);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(32, 17);
            label10.TabIndex = 21;
            label10.Text = "CPU";
            // 
            // cbbios
            // 
            cbbios.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbbios.FormattingEnabled = true;
            cbbios.Location = new System.Drawing.Point(68, 178);
            cbbios.Name = "cbbios";
            cbbios.Size = new System.Drawing.Size(141, 25);
            cbbios.TabIndex = 20;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(7, 181);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(37, 17);
            label1.TabIndex = 19;
            label1.Text = "BIOS";
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
            groupBox1.Location = new System.Drawing.Point(6, 0);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(411, 168);
            groupBox1.TabIndex = 18;
            groupBox1.TabStop = false;
            // 
            // cbscalemode
            // 
            cbscalemode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbscalemode.FormattingEnabled = true;
            cbscalemode.Items.AddRange(new object[] { "Neighbor", "JINC", "xBR" });
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
            label7.Size = new System.Drawing.Size(90, 17);
            label7.TabIndex = 19;
            label7.Text = Resources.Form_Set_InitializeComponent_softscale;
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
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(229, 78);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(55, 17);
            label9.TabIndex = 14;
            label9.Text = Resources.Form_Set_InitializeComponent_cyclefix;
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
            label8.Location = new System.Drawing.Point(229, 107);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(72, 17);
            label8.TabIndex = 12;
            label8.Text = Resources.Form_Set_InitializeComponent_framelimit;
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
            label6.Location = new System.Drawing.Point(14, 109);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(90, 17);
            label6.TabIndex = 9;
            label6.Text = Resources.Form_Set_InitializeComponent_audiobuff;
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
            label5.Size = new System.Drawing.Size(101, 17);
            label5.TabIndex = 7;
            label5.Text = Resources.Form_Set_InitializeComponent_frameskip;
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
            label4.Size = new System.Drawing.Size(43, 17);
            label4.TabIndex = 5;
            label4.Text = "MSAA";
            // 
            // tbcputicks
            // 
            tbcputicks.Location = new System.Drawing.Point(116, 75);
            tbcputicks.Name = "tbcputicks";
            tbcputicks.Size = new System.Drawing.Size(96, 23);
            tbcputicks.TabIndex = 4;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(14, 78);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(59, 17);
            label3.TabIndex = 3;
            label3.Text = Resources.Form_Set_InitializeComponent_CPUtick;
            // 
            // tbbuscycles
            // 
            tbbuscycles.Location = new System.Drawing.Point(116, 46);
            tbbuscycles.Name = "tbbuscycles";
            tbbuscycles.Size = new System.Drawing.Size(96, 23);
            tbbuscycles.TabIndex = 2;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(15, 49);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(59, 17);
            label2.TabIndex = 1;
            label2.Text = Resources.Form_Set_InitializeComponent_bustick;
            // 
            // cbconsole
            // 
            cbconsole.AutoSize = true;
            cbconsole.Location = new System.Drawing.Point(16, 18);
            cbconsole.Name = "cbconsole";
            cbconsole.Size = new System.Drawing.Size(117, 21);
            cbconsole.TabIndex = 0;
            cbconsole.Text = Resources.Form_Set_InitializeComponent_debugcon;
            cbconsole.UseVisualStyleBackColor = true;
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
            SetPage2.Name = "SetPage2";
            SetPage2.Padding = new System.Windows.Forms.Padding(3);
            SetPage2.Size = new System.Drawing.Size(424, 322);
            SetPage2.TabIndex = 1;
            SetPage2.Text = "PGXP";
            SetPage2.UseVisualStyleBackColor = true;
            // 
            // chkpgxp_avs
            // 
            chkpgxp_avs.AutoSize = true;
            chkpgxp_avs.Location = new System.Drawing.Point(13, 103);
            chkpgxp_avs.Name = "chkpgxp_avs";
            chkpgxp_avs.Size = new System.Drawing.Size(278, 21);
            chkpgxp_avs.TabIndex = 37;
            chkpgxp_avs.Text = Resources.Form_Set_InitializeComponent_PGXPavsz;
            chkpgxp_avs.UseVisualStyleBackColor = true;
            // 
            // chkpgxp_clip
            // 
            chkpgxp_clip.AutoSize = true;
            chkpgxp_clip.Location = new System.Drawing.Point(13, 273);
            chkpgxp_clip.Name = "chkpgxp_clip";
            chkpgxp_clip.Size = new System.Drawing.Size(242, 21);
            chkpgxp_clip.TabIndex = 36;
            chkpgxp_clip.Text = Resources.Form_Set_InitializeComponent_PGXPclip;
            chkpgxp_clip.UseVisualStyleBackColor = true;
            // 
            // chkpgxp_memcap
            // 
            chkpgxp_memcap.AutoSize = true;
            chkpgxp_memcap.Location = new System.Drawing.Point(13, 238);
            chkpgxp_memcap.Name = "chkpgxp_memcap";
            chkpgxp_memcap.Size = new System.Drawing.Size(242, 21);
            chkpgxp_memcap.TabIndex = 35;
            chkpgxp_memcap.Text = Resources.Form_Set_InitializeComponent_PGXPmemcap;
            chkpgxp_memcap.UseVisualStyleBackColor = true;
            // 
            // chkpgxp_nc
            // 
            chkpgxp_nc.AutoSize = true;
            chkpgxp_nc.Location = new System.Drawing.Point(13, 203);
            chkpgxp_nc.Name = "chkpgxp_nc";
            chkpgxp_nc.Size = new System.Drawing.Size(258, 21);
            chkpgxp_nc.TabIndex = 34;
            chkpgxp_nc.Text = Resources.Form_Set_InitializeComponent_PGXPnc;
            chkpgxp_nc.UseVisualStyleBackColor = true;
            // 
            // chkpgxp_ppc
            // 
            chkpgxp_ppc.AutoSize = true;
            chkpgxp_ppc.Location = new System.Drawing.Point(13, 170);
            chkpgxp_ppc.Name = "chkpgxp_ppc";
            chkpgxp_ppc.Size = new System.Drawing.Size(294, 21);
            chkpgxp_ppc.TabIndex = 33;
            chkpgxp_ppc.Text = Resources.Form_Set_InitializeComponent_PGXPppc;
            chkpgxp_ppc.UseVisualStyleBackColor = true;
            // 
            // chkpgxp_aff
            // 
            chkpgxp_aff.AutoSize = true;
            chkpgxp_aff.Location = new System.Drawing.Point(13, 136);
            chkpgxp_aff.Name = "chkpgxp_aff";
            chkpgxp_aff.Size = new System.Drawing.Size(258, 21);
            chkpgxp_aff.TabIndex = 32;
            chkpgxp_aff.Text = Resources.Form_Set_InitializeComponent_PGXPaff;
            chkpgxp_aff.UseVisualStyleBackColor = true;
            // 
            // chkpgxp_highpos
            // 
            chkpgxp_highpos.AutoSize = true;
            chkpgxp_highpos.Location = new System.Drawing.Point(13, 71);
            chkpgxp_highpos.Name = "chkpgxp_highpos";
            chkpgxp_highpos.Size = new System.Drawing.Size(294, 21);
            chkpgxp_highpos.TabIndex = 31;
            chkpgxp_highpos.Text = Resources.Form_Set_InitializeComponent_PGXPhighpos;
            chkpgxp_highpos.UseVisualStyleBackColor = true;
            // 
            // labPGXP
            // 
            labPGXP.AutoSize = true;
            labPGXP.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
            labPGXP.Location = new System.Drawing.Point(18, 10);
            labPGXP.Name = "labPGXP";
            labPGXP.Size = new System.Drawing.Size(286, 17);
            labPGXP.TabIndex = 30;
            labPGXP.Text = Resources.Form_Set_InitializeComponent_pgxphint;
            // 
            // chkpgxp
            // 
            chkpgxp.AutoSize = true;
            chkpgxp.Location = new System.Drawing.Point(13, 41);
            chkpgxp.Name = "chkpgxp";
            chkpgxp.Size = new System.Drawing.Size(194, 21);
            chkpgxp.TabIndex = 29;
            chkpgxp.Text = Resources.Form_Set_InitializeComponent_PGXPbase;
            chkpgxp.UseVisualStyleBackColor = true;
            // 
            // Form_Set
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(451, 399);
            Controls.Add(SetTab);
            Controls.Add(label11);
            Controls.Add(btndel);
            Controls.Add(btnsave);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Form_Set";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Settings";
            SetTab.ResumeLayout(false);
            SetPage1.ResumeLayout(false);
            SetPage1.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            SetPage2.ResumeLayout(false);
            SetPage2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button btnsave;
        private System.Windows.Forms.Button btndel;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TabControl SetTab;
        private System.Windows.Forms.TabPage SetPage1;
        private System.Windows.Forms.TabPage SetPage2;
        private System.Windows.Forms.ComboBox cbcdrom;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.CheckBox chkkeepar;
        private System.Windows.Forms.CheckBox chkpgxpt;
        private System.Windows.Forms.CheckBox chkrealcolor;
        private System.Windows.Forms.ComboBox cbgpures;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.ComboBox cbgpu;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ComboBox cbcpumode;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox cbbios;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox cbscalemode;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox chkTTY;
        private System.Windows.Forms.CheckBox chkcpu;
        private System.Windows.Forms.CheckBox chkbios;
        private System.Windows.Forms.TextBox tbcylesfix;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox tbframeidle;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox tbaudiobuffer;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox tbframeskip;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cbmsaa;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbcputicks;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbbuscycles;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox cbconsole;
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
    }
}
