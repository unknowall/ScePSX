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
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            MainMenu = new System.Windows.Forms.MenuStrip();
            MnuFile = new System.Windows.Forms.ToolStripMenuItem();
            LoadDIsk = new System.Windows.Forms.ToolStripMenuItem();
            SwapDisk = new System.Windows.Forms.ToolStripMenuItem();
            CloseRomMnu = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            SearchMnu = new System.Windows.Forms.ToolStripMenuItem();
            SysSetMnu = new System.Windows.Forms.ToolStripMenuItem();
            KeyTool = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            MnuSaveState = new System.Windows.Forms.ToolStripMenuItem();
            MnuLoadState = new System.Windows.Forms.ToolStripMenuItem();
            MnuUnloadState = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            CheatCode = new System.Windows.Forms.ToolStripMenuItem();
            MemEditMnu = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
            FreeSpeed = new System.Windows.Forms.ToolStripMenuItem();
            MnuPause = new System.Windows.Forms.ToolStripMenuItem();
            MnuRender = new System.Windows.Forms.ToolStripMenuItem();
            CutBlackLineMnu = new System.Windows.Forms.ToolStripMenuItem();
            frameskipmnu = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem6 = new System.Windows.Forms.ToolStripSeparator();
            directx2DRender = new System.Windows.Forms.ToolStripMenuItem();
            directx3DRender = new System.Windows.Forms.ToolStripMenuItem();
            openGLRender = new System.Windows.Forms.ToolStripMenuItem();
            VulkanRenderMnu = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
            xBRScaleAdd = new System.Windows.Forms.ToolStripMenuItem();
            xBRScaleDec = new System.Windows.Forms.ToolStripMenuItem();
            fullScreenF2 = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem7 = new System.Windows.Forms.ToolStripSeparator();
            NetPlayMnu = new System.Windows.Forms.ToolStripMenuItem();
            NetPlaySetMnu = new System.Windows.Forms.ToolStripMenuItem();
            HelpMnu = new System.Windows.Forms.ToolStripMenuItem();
            gitHubMnu = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem8 = new System.Windows.Forms.ToolStripSeparator();
            supportKoficomMnu = new System.Windows.Forms.ToolStripMenuItem();
            supportWeChatMnu = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem9 = new System.Windows.Forms.ToolStripSeparator();
            AboutMnu = new System.Windows.Forms.ToolStripMenuItem();
            StatusBar = new System.Windows.Forms.StatusStrip();
            toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            toolStripStatusLabel3 = new System.Windows.Forms.ToolStripStatusLabel();
            toolStripStatusLabel4 = new System.Windows.Forms.ToolStripStatusLabel();
            toolStripStatusLabel5 = new System.Windows.Forms.ToolStripStatusLabel();
            toolStripStatusLabel6 = new System.Windows.Forms.ToolStripStatusLabel();
            toolStripStatusLabel7 = new System.Windows.Forms.ToolStripStatusLabel();
            toolStripStatusLabel8 = new System.Windows.Forms.ToolStripStatusLabel();
            panel = new System.Windows.Forms.Panel();
            MainMenu.SuspendLayout();
            StatusBar.SuspendLayout();
            SuspendLayout();
            // 
            // MainMenu
            // 
            MainMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            MainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { MnuFile, MnuRender, NetPlayMnu, HelpMnu });
            MainMenu.Location = new System.Drawing.Point(0, 0);
            MainMenu.Name = "MainMenu";
            MainMenu.Padding = new System.Windows.Forms.Padding(5, 2, 0, 2);
            MainMenu.Size = new System.Drawing.Size(684, 25);
            MainMenu.TabIndex = 0;
            MainMenu.Text = "menuStrip1";
            // 
            // MnuFile
            // 
            MnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { LoadDIsk, SwapDisk, CloseRomMnu, toolStripMenuItem1, SearchMnu, SysSetMnu, KeyTool, toolStripMenuItem2, MnuSaveState, MnuLoadState, MnuUnloadState, toolStripMenuItem3, CheatCode, MemEditMnu, toolStripMenuItem5, FreeSpeed, MnuPause });
            MnuFile.Name = "MnuFile";
            MnuFile.Size = new System.Drawing.Size(57, 21);
            MnuFile.Text = "文件";
            // 
            // LoadDIsk
            // 
            LoadDIsk.Name = "LoadDIsk";
            LoadDIsk.Size = new System.Drawing.Size(191, 22);
            LoadDIsk.Text = "加载游戏";
            LoadDIsk.Click += LoadDisk_Click;
            // 
            // SwapDisk
            // 
            SwapDisk.Name = "SwapDisk";
            SwapDisk.Size = new System.Drawing.Size(191, 22);
            SwapDisk.Text = "更换光盘";
            SwapDisk.Click += SwapDisk_Click;
            // 
            // CloseRomMnu
            // 
            CloseRomMnu.Name = "CloseRomMnu";
            CloseRomMnu.Size = new System.Drawing.Size(191, 22);
            CloseRomMnu.Text = "返回列表";
            CloseRomMnu.Click += CloseRomMnu_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new System.Drawing.Size(188, 6);
            // 
            // SearchMnu
            // 
            SearchMnu.Name = "SearchMnu";
            SearchMnu.Size = new System.Drawing.Size(191, 22);
            SearchMnu.Text = "扫描目录";
            SearchMnu.Click += SearchMnu_Click;
            // 
            // SysSetMnu
            // 
            SysSetMnu.Name = "SysSetMnu";
            SysSetMnu.Size = new System.Drawing.Size(191, 22);
            SysSetMnu.Text = "系统设置";
            SysSetMnu.Click += SysSetMnu_Click;
            // 
            // KeyTool
            // 
            KeyTool.Name = "KeyTool";
            KeyTool.Size = new System.Drawing.Size(191, 22);
            KeyTool.Text = "按键设置";
            KeyTool.Click += KeyToolStripMenuItem_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new System.Drawing.Size(188, 6);
            // 
            // MnuSaveState
            // 
            MnuSaveState.Enabled = false;
            MnuSaveState.Name = "MnuSaveState";
            MnuSaveState.Size = new System.Drawing.Size(191, 22);
            MnuSaveState.Text = "即时存档 (F5)";
            // 
            // MnuLoadState
            // 
            MnuLoadState.Enabled = false;
            MnuLoadState.Name = "MnuLoadState";
            MnuLoadState.Size = new System.Drawing.Size(191, 22);
            MnuLoadState.Text = "即时读取 (F6)";
            // 
            // MnuUnloadState
            // 
            MnuUnloadState.Enabled = false;
            MnuUnloadState.Name = "MnuUnloadState";
            MnuUnloadState.Size = new System.Drawing.Size(191, 22);
            MnuUnloadState.Text = "撤销读取 (F7)";
            // 
            // toolStripMenuItem3
            // 
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            toolStripMenuItem3.Size = new System.Drawing.Size(188, 6);
            // 
            // CheatCode
            // 
            CheatCode.Enabled = false;
            CheatCode.Name = "CheatCode";
            CheatCode.Size = new System.Drawing.Size(191, 22);
            CheatCode.Text = "金手指";
            CheatCode.Click += CheatCode_Click;
            // 
            // MemEditMnu
            // 
            MemEditMnu.Name = "MemEditMnu";
            MemEditMnu.Size = new System.Drawing.Size(191, 22);
            MemEditMnu.Text = "内存编辑";
            MemEditMnu.Click += MnuDebug_Click;
            // 
            // toolStripMenuItem5
            // 
            toolStripMenuItem5.Name = "toolStripMenuItem5";
            toolStripMenuItem5.Size = new System.Drawing.Size(188, 6);
            // 
            // FreeSpeed
            // 
            FreeSpeed.Name = "FreeSpeed";
            FreeSpeed.Size = new System.Drawing.Size(191, 22);
            FreeSpeed.Text = "加速快进 (TAB)";
            // 
            // MnuPause
            // 
            MnuPause.Name = "MnuPause";
            MnuPause.Size = new System.Drawing.Size(191, 22);
            MnuPause.Text = "暂停/继续 (空格)";
            MnuPause.Click += MnuPause_Click;
            // 
            // MnuRender
            // 
            MnuRender.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { CutBlackLineMnu, frameskipmnu, toolStripMenuItem6, directx2DRender, directx3DRender, openGLRender, VulkanRenderMnu, toolStripMenuItem4, xBRScaleAdd, xBRScaleDec, fullScreenF2, toolStripMenuItem7 });
            MnuRender.Name = "MnuRender";
            MnuRender.Size = new System.Drawing.Size(78, 21);
            MnuRender.Text = "渲染器(&R)";
            // 
            // CutBlackLineMnu
            // 
            CutBlackLineMnu.CheckOnClick = true;
            CutBlackLineMnu.Name = "CutBlackLineMnu";
            CutBlackLineMnu.Size = new System.Drawing.Size(384, 22);
            CutBlackLineMnu.Text = "裁剪上下黑边(可能造成失真)";
            CutBlackLineMnu.CheckedChanged += CutBlackLineMnu_CheckedChanged;
            // 
            // frameskipmnu
            // 
            frameskipmnu.Checked = true;
            frameskipmnu.CheckOnClick = true;
            frameskipmnu.CheckState = System.Windows.Forms.CheckState.Checked;
            frameskipmnu.Name = "frameskipmnu";
            frameskipmnu.Size = new System.Drawing.Size(384, 22);
            frameskipmnu.Text = "跳帧 (只对 D2D/D3D)";
            frameskipmnu.CheckedChanged += frameskipmnu_CheckedChanged;
            // 
            // toolStripMenuItem6
            // 
            toolStripMenuItem6.Name = "toolStripMenuItem6";
            toolStripMenuItem6.Size = new System.Drawing.Size(381, 6);
            // 
            // directx2DRender
            // 
            directx2DRender.CheckOnClick = true;
            directx2DRender.Name = "directx2DRender";
            directx2DRender.Size = new System.Drawing.Size(384, 22);
            directx2DRender.Text = "DirectxD2D";
            directx2DRender.Click += directx2DRender_Click;
            // 
            // directx3DRender
            // 
            directx3DRender.CheckOnClick = true;
            directx3DRender.Name = "directx3DRender";
            directx3DRender.Size = new System.Drawing.Size(384, 22);
            directx3DRender.Text = "DirectxD3D";
            directx3DRender.Click += directx3DToolStripMenuItem_Click;
            // 
            // openGLRender
            // 
            openGLRender.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            openGLRender.CheckOnClick = true;
            openGLRender.Name = "openGLRender";
            openGLRender.Size = new System.Drawing.Size(384, 22);
            openGLRender.Text = "OpenGL";
            openGLRender.Click += openGLToolStripMenuItem_Click;
            // 
            // VulkanRenderMnu
            // 
            VulkanRenderMnu.CheckOnClick = true;
            VulkanRenderMnu.Name = "VulkanRenderMnu";
            VulkanRenderMnu.Size = new System.Drawing.Size(384, 22);
            VulkanRenderMnu.Text = "Vulkan";
            VulkanRenderMnu.Click += VulkanRenderMnu_Click;
            // 
            // toolStripMenuItem4
            // 
            toolStripMenuItem4.Name = "toolStripMenuItem4";
            toolStripMenuItem4.Size = new System.Drawing.Size(381, 6);
            // 
            // xBRScaleAdd
            // 
            xBRScaleAdd.Name = "xBRScaleAdd";
            xBRScaleAdd.Size = new System.Drawing.Size(384, 22);
            xBRScaleAdd.Text = "IR Scale++ (F11)";
            xBRScaleAdd.Click += UpScale_Click;
            // 
            // xBRScaleDec
            // 
            xBRScaleDec.Name = "xBRScaleDec";
            xBRScaleDec.Size = new System.Drawing.Size(384, 22);
            xBRScaleDec.Text = "IR Scale --  (F12)";
            xBRScaleDec.Click += DownScale_Click;
            // 
            // fullScreenF2
            // 
            fullScreenF2.Name = "fullScreenF2";
            fullScreenF2.Size = new System.Drawing.Size(384, 22);
            fullScreenF2.Text = "全屏模式 (F2)";
            fullScreenF2.Click += fullScreenF2_Click;
            // 
            // toolStripMenuItem7
            // 
            toolStripMenuItem7.Name = "toolStripMenuItem7";
            toolStripMenuItem7.Size = new System.Drawing.Size(381, 6);
            // 
            // NetPlayMnu
            // 
            NetPlayMnu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { NetPlaySetMnu });
            NetPlayMnu.Name = "NetPlayMnu";
            NetPlayMnu.Size = new System.Drawing.Size(82, 21);
            NetPlayMnu.Text = "网络对战(&N)";
            // 
            // NetPlaySetMnu
            // 
            NetPlaySetMnu.Name = "NetPlaySetMnu";
            NetPlaySetMnu.Size = new System.Drawing.Size(116, 22);
            NetPlaySetMnu.Text = "设置";
            NetPlaySetMnu.Click += NetPlaySetMnu_Click;
            // 
            // HelpMnu
            // 
            HelpMnu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { gitHubMnu, toolStripMenuItem8, supportKoficomMnu, supportWeChatMnu, toolStripMenuItem9, AboutMnu });
            HelpMnu.Name = "HelpMnu";
            HelpMnu.Size = new System.Drawing.Size(64, 21);
            HelpMnu.Text = "帮助(&H)";
            // 
            // gitHubMnu
            // 
            gitHubMnu.Name = "gitHubMnu";
            gitHubMnu.Size = new System.Drawing.Size(192, 22);
            gitHubMnu.Text = "GitHub(&G)";
            gitHubMnu.Click += gitHubMnu_Click;
            // 
            // toolStripMenuItem8
            // 
            toolStripMenuItem8.Name = "toolStripMenuItem8";
            toolStripMenuItem8.Size = new System.Drawing.Size(189, 6);
            // 
            // supportKoficomMnu
            // 
            supportKoficomMnu.Name = "supportKoficomMnu";
            supportKoficomMnu.Size = new System.Drawing.Size(192, 22);
            supportKoficomMnu.Text = "通过Ko-Fi支持本项目";
            supportKoficomMnu.Click += supportKoficomMnu_Click;
            // 
            // supportWeChatMnu
            // 
            supportWeChatMnu.Name = "supportWeChatMnu";
            supportWeChatMnu.Size = new System.Drawing.Size(192, 22);
            supportWeChatMnu.Text = "通过微信支持本项目";
            supportWeChatMnu.Click += supportWeChatMnu_Click;
            // 
            // toolStripMenuItem9
            // 
            toolStripMenuItem9.Name = "toolStripMenuItem9";
            toolStripMenuItem9.Size = new System.Drawing.Size(189, 6);
            // 
            // AboutMnu
            // 
            AboutMnu.Name = "AboutMnu";
            AboutMnu.Size = new System.Drawing.Size(192, 22);
            AboutMnu.Text = "关于(&A)";
            AboutMnu.Click += AboutMnu_Click;
            // 
            // StatusBar
            // 
            StatusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripStatusLabel1, toolStripStatusLabel2, toolStripStatusLabel3, toolStripStatusLabel4, toolStripStatusLabel5, toolStripStatusLabel6, toolStripStatusLabel7, toolStripStatusLabel8 });
            StatusBar.Location = new System.Drawing.Point(0, 476);
            StatusBar.Name = "StatusBar";
            StatusBar.Size = new System.Drawing.Size(684, 22);
            StatusBar.TabIndex = 1;
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Font = new System.Drawing.Font("Arial", 9F);
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripStatusLabel2
            // 
            toolStripStatusLabel2.Font = new System.Drawing.Font("Arial", 9F);
            toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            toolStripStatusLabel2.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripStatusLabel3
            // 
            toolStripStatusLabel3.Font = new System.Drawing.Font("Arial", 9F);
            toolStripStatusLabel3.Name = "toolStripStatusLabel3";
            toolStripStatusLabel3.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripStatusLabel4
            // 
            toolStripStatusLabel4.Font = new System.Drawing.Font("Arial", 9F);
            toolStripStatusLabel4.Name = "toolStripStatusLabel4";
            toolStripStatusLabel4.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripStatusLabel5
            // 
            toolStripStatusLabel5.Font = new System.Drawing.Font("Arial", 9F);
            toolStripStatusLabel5.Name = "toolStripStatusLabel5";
            toolStripStatusLabel5.Size = new System.Drawing.Size(669, 17);
            toolStripStatusLabel5.Spring = true;
            // 
            // toolStripStatusLabel6
            // 
            toolStripStatusLabel6.Font = new System.Drawing.Font("Arial", 9F);
            toolStripStatusLabel6.Name = "toolStripStatusLabel6";
            toolStripStatusLabel6.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripStatusLabel7
            // 
            toolStripStatusLabel7.Font = new System.Drawing.Font("Arial", 9F);
            toolStripStatusLabel7.Name = "toolStripStatusLabel7";
            toolStripStatusLabel7.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripStatusLabel8
            // 
            toolStripStatusLabel8.Font = new System.Drawing.Font("Arial", 9F);
            toolStripStatusLabel8.Name = "toolStripStatusLabel8";
            toolStripStatusLabel8.Size = new System.Drawing.Size(0, 17);
            // 
            // panel
            // 
            panel.Dock = System.Windows.Forms.DockStyle.Fill;
            panel.Location = new System.Drawing.Point(0, 25);
            panel.Name = "panel";
            panel.Size = new System.Drawing.Size(684, 451);
            panel.TabIndex = 2;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(684, 498);
            Controls.Add(panel);
            Controls.Add(StatusBar);
            Controls.Add(MainMenu);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = MainMenu;
            Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            Name = "FrmMain";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "ScePSX";
            MainMenu.ResumeLayout(false);
            MainMenu.PerformLayout();
            StatusBar.ResumeLayout(false);
            StatusBar.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip MainMenu;
        private System.Windows.Forms.ToolStripMenuItem MnuFile;
        private System.Windows.Forms.ToolStripMenuItem MemEditMnu;
        private System.Windows.Forms.ToolStripMenuItem MnuRender;
        private System.Windows.Forms.ToolStripMenuItem directx3DRender;
        private System.Windows.Forms.ToolStripMenuItem openGLRender;
        private System.Windows.Forms.ToolStripMenuItem LoadDIsk;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem KeyTool;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem MnuSaveState;
        private System.Windows.Forms.ToolStripMenuItem MnuLoadState;
        private System.Windows.Forms.ToolStripMenuItem MnuUnloadState;
        private System.Windows.Forms.ToolStripMenuItem SwapDisk;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem CheatCode;
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
        private System.Windows.Forms.ToolStripMenuItem HelpMnu;
        private System.Windows.Forms.ToolStripMenuItem CloseRomMnu;
        private System.Windows.Forms.ToolStripMenuItem SearchMnu;
        private System.Windows.Forms.StatusStrip StatusBar;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel3;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel4;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel5;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel6;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel7;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel8;
        private System.Windows.Forms.ToolStripMenuItem VulkanRenderMnu;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem7;
        private System.Windows.Forms.ToolStripMenuItem fullScreenF2;
        private System.Windows.Forms.Panel panel;
        private System.Windows.Forms.ToolStripMenuItem gitHubMnu;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem8;
        private System.Windows.Forms.ToolStripMenuItem supportKoficomMnu;
        private System.Windows.Forms.ToolStripMenuItem supportWeChatMnu;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem9;
        private System.Windows.Forms.ToolStripMenuItem AboutMnu;
    }
}
