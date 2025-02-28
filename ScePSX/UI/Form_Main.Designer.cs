namespace ScePSX {
    partial class FrmMain {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
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
            MainMenu = new System.Windows.Forms.MenuStrip();
            MnuFile = new System.Windows.Forms.ToolStripMenuItem();
            LoadDIsk = new System.Windows.Forms.ToolStripMenuItem();
            SwapDisk = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            MnuBios = new System.Windows.Forms.ToolStripMenuItem();
            KeyTool = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            SaveStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            LoadStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            UnLoadStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            ChatCode = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
            FreeSpeed = new System.Windows.Forms.ToolStripMenuItem();
            MnuDebug = new System.Windows.Forms.ToolStripMenuItem();
            cPUToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            内存编辑ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            RenderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            directx3DToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            openGLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
            xBRScaleAdd = new System.Windows.Forms.ToolStripMenuItem();
            xBRScaleDec = new System.Windows.Forms.ToolStripMenuItem();
            MnuPause = new System.Windows.Forms.ToolStripMenuItem();
            MainMenu.SuspendLayout();
            SuspendLayout();
            // 
            // MainMenu
            // 
            MainMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            MainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { MnuFile, MnuDebug, RenderToolStripMenuItem, MnuPause });
            MainMenu.Location = new System.Drawing.Point(0, 0);
            MainMenu.Name = "MainMenu";
            MainMenu.Padding = new System.Windows.Forms.Padding(5, 2, 0, 2);
            MainMenu.Size = new System.Drawing.Size(831, 25);
            MainMenu.TabIndex = 0;
            MainMenu.Text = "menuStrip1";
            // 
            // MnuFile
            // 
            MnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { LoadDIsk, SwapDisk, toolStripMenuItem1, MnuBios, KeyTool, toolStripMenuItem2, SaveStripMenuItem, LoadStripMenuItem, UnLoadStripMenuItem, toolStripMenuItem3, ChatCode, toolStripMenuItem5, FreeSpeed });
            MnuFile.Name = "MnuFile";
            MnuFile.Size = new System.Drawing.Size(62, 21);
            MnuFile.Text = "文件(&F))";
            // 
            // LoadDIsk
            // 
            LoadDIsk.Name = "LoadDIsk";
            LoadDIsk.Size = new System.Drawing.Size(180, 22);
            LoadDIsk.Text = "加载光盘";
            LoadDIsk.Click += LoadDisk_Click;
            // 
            // SwapDisk
            // 
            SwapDisk.Name = "SwapDisk";
            SwapDisk.Size = new System.Drawing.Size(180, 22);
            SwapDisk.Text = "切换光盘";
            SwapDisk.Click += SwapDisk_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new System.Drawing.Size(177, 6);
            // 
            // MnuBios
            // 
            MnuBios.Name = "MnuBios";
            MnuBios.Size = new System.Drawing.Size(180, 22);
            MnuBios.Text = "BIOS设置";
            // 
            // KeyTool
            // 
            KeyTool.Name = "KeyTool";
            KeyTool.Size = new System.Drawing.Size(180, 22);
            KeyTool.Text = "按键设置";
            KeyTool.Click += KeyToolStripMenuItem_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new System.Drawing.Size(177, 6);
            // 
            // SaveStripMenuItem
            // 
            SaveStripMenuItem.Enabled = false;
            SaveStripMenuItem.Name = "SaveStripMenuItem";
            SaveStripMenuItem.Size = new System.Drawing.Size(180, 22);
            SaveStripMenuItem.Text = "即时存档 (F5)";
            // 
            // LoadStripMenuItem
            // 
            LoadStripMenuItem.Enabled = false;
            LoadStripMenuItem.Name = "LoadStripMenuItem";
            LoadStripMenuItem.Size = new System.Drawing.Size(180, 22);
            LoadStripMenuItem.Text = "即时读取 (F6)";
            // 
            // UnLoadStripMenuItem
            // 
            UnLoadStripMenuItem.Enabled = false;
            UnLoadStripMenuItem.Name = "UnLoadStripMenuItem";
            UnLoadStripMenuItem.Size = new System.Drawing.Size(180, 22);
            UnLoadStripMenuItem.Text = "撤销读取 (F7)";
            // 
            // toolStripMenuItem3
            // 
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            toolStripMenuItem3.Size = new System.Drawing.Size(177, 6);
            // 
            // ChatCode
            // 
            ChatCode.Name = "ChatCode";
            ChatCode.Size = new System.Drawing.Size(180, 22);
            ChatCode.Text = "金手指";
            ChatCode.Click += CheatCode_Click;
            // 
            // toolStripMenuItem5
            // 
            toolStripMenuItem5.Name = "toolStripMenuItem5";
            toolStripMenuItem5.Size = new System.Drawing.Size(177, 6);
            // 
            // FreeSpeed
            // 
            FreeSpeed.Name = "FreeSpeed";
            FreeSpeed.Size = new System.Drawing.Size(180, 22);
            FreeSpeed.Text = "加速快进 (TAB)";
            // 
            // MnuDebug
            // 
            MnuDebug.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { cPUToolStripMenuItem, 内存编辑ToolStripMenuItem });
            MnuDebug.Name = "MnuDebug";
            MnuDebug.Size = new System.Drawing.Size(44, 21);
            MnuDebug.Text = "调试";
            // 
            // cPUToolStripMenuItem
            // 
            cPUToolStripMenuItem.Enabled = false;
            cPUToolStripMenuItem.Name = "cPUToolStripMenuItem";
            cPUToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            cPUToolStripMenuItem.Text = "CPU";
            // 
            // 内存编辑ToolStripMenuItem
            // 
            内存编辑ToolStripMenuItem.Name = "内存编辑ToolStripMenuItem";
            内存编辑ToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            内存编辑ToolStripMenuItem.Text = "内存编辑";
            内存编辑ToolStripMenuItem.Click += MnuDebug_Click;
            // 
            // RenderToolStripMenuItem
            // 
            RenderToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { directx3DToolStripMenuItem, openGLToolStripMenuItem, toolStripMenuItem4, xBRScaleAdd, xBRScaleDec });
            RenderToolStripMenuItem.Name = "RenderToolStripMenuItem";
            RenderToolStripMenuItem.Size = new System.Drawing.Size(56, 21);
            RenderToolStripMenuItem.Text = "渲染器";
            // 
            // directx3DToolStripMenuItem
            // 
            directx3DToolStripMenuItem.CheckOnClick = true;
            directx3DToolStripMenuItem.Name = "directx3DToolStripMenuItem";
            directx3DToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            directx3DToolStripMenuItem.Text = "Directx3D";
            directx3DToolStripMenuItem.Click += directx3DToolStripMenuItem_Click;
            // 
            // openGLToolStripMenuItem
            // 
            openGLToolStripMenuItem.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            openGLToolStripMenuItem.CheckOnClick = true;
            openGLToolStripMenuItem.Name = "openGLToolStripMenuItem";
            openGLToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            openGLToolStripMenuItem.Text = "OpenGL";
            openGLToolStripMenuItem.Click += openGLToolStripMenuItem_Click;
            // 
            // toolStripMenuItem4
            // 
            toolStripMenuItem4.Name = "toolStripMenuItem4";
            toolStripMenuItem4.Size = new System.Drawing.Size(179, 6);
            // 
            // xBRScaleAdd
            // 
            xBRScaleAdd.Name = "xBRScaleAdd";
            xBRScaleAdd.Size = new System.Drawing.Size(182, 22);
            xBRScaleAdd.Text = "xBR Scale++ (F11)";
            xBRScaleAdd.Click += xBRScaleAdd_Click;
            // 
            // xBRScaleDec
            // 
            xBRScaleDec.Name = "xBRScaleDec";
            xBRScaleDec.Size = new System.Drawing.Size(182, 22);
            xBRScaleDec.Text = "xBR Scale --  (F12)";
            xBRScaleDec.Click += xBRScaleDec_Click;
            // 
            // MnuPause
            // 
            MnuPause.Name = "MnuPause";
            MnuPause.Size = new System.Drawing.Size(109, 21);
            MnuPause.Text = "暂停/继续 (空格)";
            MnuPause.Click += MnuPause_Click;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(831, 623);
            Controls.Add(MainMenu);
            MainMenuStrip = MainMenu;
            Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            Name = "FrmMain";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "sPSX";
            MainMenu.ResumeLayout(false);
            MainMenu.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip MainMenu;
        private System.Windows.Forms.ToolStripMenuItem MnuFile;
        private System.Windows.Forms.ToolStripMenuItem MnuDebug;
        private System.Windows.Forms.ToolStripMenuItem MnuPause;
        private System.Windows.Forms.ToolStripMenuItem cPUToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 内存编辑ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem RenderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem directx3DToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openGLToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem LoadDIsk;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem MnuBios;
        private System.Windows.Forms.ToolStripMenuItem KeyTool;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem SaveStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem LoadStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UnLoadStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem SwapDisk;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem ChatCode;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem5;
        private System.Windows.Forms.ToolStripMenuItem FreeSpeed;
        private System.Windows.Forms.ToolStripMenuItem xBRScaleAdd;
        private System.Windows.Forms.ToolStripMenuItem xBRScaleDec;
    }
}
