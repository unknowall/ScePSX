namespace ScePSX.UI
{
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
            MnuPause = new System.Windows.Forms.ToolStripMenuItem();
            MnuDebug = new System.Windows.Forms.ToolStripMenuItem();
            cPUToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            内存编辑ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            RenderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            CutBlackLineMnu = new System.Windows.Forms.ToolStripMenuItem();
            frameskipmnu = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem6 = new System.Windows.Forms.ToolStripSeparator();
            directx2DRender = new System.Windows.Forms.ToolStripMenuItem();
            directx3DRender = new System.Windows.Forms.ToolStripMenuItem();
            openGLRender = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
            xBRScaleAdd = new System.Windows.Forms.ToolStripMenuItem();
            xBRScaleDec = new System.Windows.Forms.ToolStripMenuItem();
            NetPlayMnu = new System.Windows.Forms.ToolStripMenuItem();
            AboutMnu = new System.Windows.Forms.ToolStripMenuItem();
            NetPlaySetMnu = new System.Windows.Forms.ToolStripMenuItem();
            SysSetMnu = new System.Windows.Forms.ToolStripMenuItem();
            MainMenu.SuspendLayout();
            SuspendLayout();
            // 
            // MainMenu
            // 
            MainMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            MainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { MnuFile, MnuDebug, RenderToolStripMenuItem, NetPlayMnu, AboutMnu });
            MainMenu.Location = new System.Drawing.Point(0, 0);
            MainMenu.Name = "MainMenu";
            MainMenu.Padding = new System.Windows.Forms.Padding(5, 2, 0, 2);
            MainMenu.Size = new System.Drawing.Size(831, 25);
            MainMenu.TabIndex = 0;
            MainMenu.Text = "menuStrip1";
            // 
            // MnuFile
            // 
            MnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { LoadDIsk, SwapDisk, toolStripMenuItem1, MnuBios, SysSetMnu, KeyTool, toolStripMenuItem2, SaveStripMenuItem, LoadStripMenuItem, UnLoadStripMenuItem, toolStripMenuItem3, ChatCode, toolStripMenuItem5, FreeSpeed, MnuPause });
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
            // MnuPause
            // 
            MnuPause.Name = "MnuPause";
            MnuPause.Size = new System.Drawing.Size(180, 22);
            MnuPause.Text = "暂停/继续 (空格)";
            MnuPause.Click += MnuPause_Click;
            // 
            // MnuDebug
            // 
            MnuDebug.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { cPUToolStripMenuItem, 内存编辑ToolStripMenuItem });
            MnuDebug.Name = "MnuDebug";
            MnuDebug.Size = new System.Drawing.Size(61, 21);
            MnuDebug.Text = "调试(&D)";
            // 
            // cPUToolStripMenuItem
            // 
            cPUToolStripMenuItem.Enabled = false;
            cPUToolStripMenuItem.Name = "cPUToolStripMenuItem";
            cPUToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            cPUToolStripMenuItem.Text = "CPU";
            // 
            // 内存编辑ToolStripMenuItem
            // 
            内存编辑ToolStripMenuItem.Name = "内存编辑ToolStripMenuItem";
            内存编辑ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            内存编辑ToolStripMenuItem.Text = "内存编辑";
            内存编辑ToolStripMenuItem.Click += MnuDebug_Click;
            // 
            // RenderToolStripMenuItem
            // 
            RenderToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { CutBlackLineMnu, frameskipmnu, toolStripMenuItem6, directx2DRender, directx3DRender, openGLRender, toolStripMenuItem4, xBRScaleAdd, xBRScaleDec });
            RenderToolStripMenuItem.Name = "RenderToolStripMenuItem";
            RenderToolStripMenuItem.Size = new System.Drawing.Size(72, 21);
            RenderToolStripMenuItem.Text = "渲染器(&R)";
            // 
            // CutBlackLineMnu
            // 
            CutBlackLineMnu.CheckOnClick = true;
            CutBlackLineMnu.Name = "CutBlackLineMnu";
            CutBlackLineMnu.Size = new System.Drawing.Size(228, 22);
            CutBlackLineMnu.Text = "裁剪上下黑边(可能造成失真)";
            CutBlackLineMnu.CheckedChanged += CutBlackLineMnu_CheckedChanged;
            // 
            // frameskipmnu
            // 
            frameskipmnu.Checked = true;
            frameskipmnu.CheckOnClick = true;
            frameskipmnu.CheckState = System.Windows.Forms.CheckState.Checked;
            frameskipmnu.Name = "frameskipmnu";
            frameskipmnu.Size = new System.Drawing.Size(228, 22);
            frameskipmnu.Text = "跳帧 (只对 D2D,D3D)";
            frameskipmnu.CheckedChanged += frameskipmnu_CheckedChanged;
            // 
            // toolStripMenuItem6
            // 
            toolStripMenuItem6.Name = "toolStripMenuItem6";
            toolStripMenuItem6.Size = new System.Drawing.Size(225, 6);
            // 
            // directx2DRender
            // 
            directx2DRender.CheckOnClick = true;
            directx2DRender.Name = "directx2DRender";
            directx2DRender.Size = new System.Drawing.Size(228, 22);
            directx2DRender.Text = "DirectxD2D";
            directx2DRender.Click += directx2DRender_Click;
            // 
            // directx3DRender
            // 
            directx3DRender.CheckOnClick = true;
            directx3DRender.Name = "directx3DRender";
            directx3DRender.Size = new System.Drawing.Size(228, 22);
            directx3DRender.Text = "DirectxD3D";
            directx3DRender.Click += directx3DToolStripMenuItem_Click;
            // 
            // openGLRender
            // 
            openGLRender.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            openGLRender.CheckOnClick = true;
            openGLRender.Name = "openGLRender";
            openGLRender.Size = new System.Drawing.Size(228, 22);
            openGLRender.Text = "OpenGL";
            openGLRender.Click += openGLToolStripMenuItem_Click;
            // 
            // toolStripMenuItem4
            // 
            toolStripMenuItem4.Name = "toolStripMenuItem4";
            toolStripMenuItem4.Size = new System.Drawing.Size(225, 6);
            // 
            // xBRScaleAdd
            // 
            xBRScaleAdd.Name = "xBRScaleAdd";
            xBRScaleAdd.Size = new System.Drawing.Size(228, 22);
            xBRScaleAdd.Text = "xBR Scale++ (F11)";
            xBRScaleAdd.Click += xBRScaleAdd_Click;
            // 
            // xBRScaleDec
            // 
            xBRScaleDec.Name = "xBRScaleDec";
            xBRScaleDec.Size = new System.Drawing.Size(228, 22);
            xBRScaleDec.Text = "xBR Scale --  (F12)";
            xBRScaleDec.Click += xBRScaleDec_Click;
            // 
            // NetPlayMnu
            // 
            NetPlayMnu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { NetPlaySetMnu });
            NetPlayMnu.Name = "NetPlayMnu";
            NetPlayMnu.Size = new System.Drawing.Size(86, 21);
            NetPlayMnu.Text = "网络对战(&N)";
            // 
            // AboutMnu
            // 
            AboutMnu.Name = "AboutMnu";
            AboutMnu.Size = new System.Drawing.Size(60, 21);
            AboutMnu.Text = "关于(&A)";
            AboutMnu.Click += AboutMnu_Click;
            // 
            // NetPlaySetMnu
            // 
            NetPlaySetMnu.Name = "NetPlaySetMnu";
            NetPlaySetMnu.Size = new System.Drawing.Size(180, 22);
            NetPlaySetMnu.Text = "设置";
            NetPlaySetMnu.Click += NetPlaySetMnu_Click;
            // 
            // SysSetMnu
            // 
            SysSetMnu.Name = "SysSetMnu";
            SysSetMnu.Size = new System.Drawing.Size(180, 22);
            SysSetMnu.Text = "系统设置";
            SysSetMnu.Click += SysSetMnu_Click;
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
            Text = "ScePSX";
            MainMenu.ResumeLayout(false);
            MainMenu.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip MainMenu;
        private System.Windows.Forms.ToolStripMenuItem MnuFile;
        private System.Windows.Forms.ToolStripMenuItem MnuDebug;
        private System.Windows.Forms.ToolStripMenuItem cPUToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 内存编辑ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem RenderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem directx3DRender;
        private System.Windows.Forms.ToolStripMenuItem openGLRender;
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
        private System.Windows.Forms.ToolStripMenuItem directx2DRender;
        private System.Windows.Forms.ToolStripMenuItem CutBlackLineMnu;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem6;
        private System.Windows.Forms.ToolStripMenuItem frameskipmnu;
        private System.Windows.Forms.ToolStripMenuItem MnuPause;
        private System.Windows.Forms.ToolStripMenuItem SysSetMnu;
        private System.Windows.Forms.ToolStripMenuItem NetPlayMnu;
        private System.Windows.Forms.ToolStripMenuItem NetPlaySetMnu;
        private System.Windows.Forms.ToolStripMenuItem AboutMnu;
    }
}
