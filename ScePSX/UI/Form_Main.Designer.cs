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
            SaveStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            LoadStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            UnLoadStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            CheatCode = new System.Windows.Forms.ToolStripMenuItem();
            MemEditMnu = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
            FreeSpeed = new System.Windows.Forms.ToolStripMenuItem();
            MnuPause = new System.Windows.Forms.ToolStripMenuItem();
            RenderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            toolStripMenuItem7 = new System.Windows.Forms.ToolStripSeparator();
            fullScreenF2 = new System.Windows.Forms.ToolStripMenuItem();
            NetPlayMnu = new System.Windows.Forms.ToolStripMenuItem();
            NetPlaySetMnu = new System.Windows.Forms.ToolStripMenuItem();
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
            MainMenu.SuspendLayout();
            StatusBar.SuspendLayout();
            SuspendLayout();
            // 
            // MainMenu
            // 
            MainMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            MainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { MnuFile, RenderToolStripMenuItem, NetPlayMnu, AboutMnu });
            MainMenu.Location = new System.Drawing.Point(0, 0);
            MainMenu.Name = "MainMenu";
            MainMenu.Padding = new System.Windows.Forms.Padding(5, 2, 0, 2);
            MainMenu.Size = new System.Drawing.Size(684, 25);
            MainMenu.TabIndex = 0;
            MainMenu.Text = "menuStrip1";
            // 
            // MnuFile
            // 
            MnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { LoadDIsk, SwapDisk, CloseRomMnu, toolStripMenuItem1, SearchMnu, SysSetMnu, KeyTool, toolStripMenuItem2, SaveStripMenuItem, LoadStripMenuItem, UnLoadStripMenuItem, toolStripMenuItem3, CheatCode, MemEditMnu, toolStripMenuItem5, FreeSpeed, MnuPause });
            MnuFile.Name = "MnuFile";
            MnuFile.Size = new System.Drawing.Size(57, 21);
            MnuFile.Text = Properties.Resources.File;
            // 
            // LoadDIsk
            // 
            LoadDIsk.Name = "LoadDIsk";
            LoadDIsk.Size = new System.Drawing.Size(191, 22);
            LoadDIsk.Text = Properties.Resources.LoadDIsk;
            LoadDIsk.Click += LoadDisk_Click;
            // 
            // SwapDisk
            // 
            SwapDisk.Name = "SwapDisk";
            SwapDisk.Size = new System.Drawing.Size(191, 22);
            SwapDisk.Text = Properties.Resources.SwapDisc;
            SwapDisk.Click += SwapDisk_Click;
            // 
            // CloseRomMnu
            // 
            CloseRomMnu.Name = "CloseRomMnu";
            CloseRomMnu.Size = new System.Drawing.Size(191, 22);
            CloseRomMnu.Text = Properties.Resources.BackToMain;
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
            SearchMnu.Text = Properties.Resources.SearchDir;
            SearchMnu.Click += SearchMnu_Click;
            // 
            // SysSetMnu
            // 
            SysSetMnu.Name = "SysSetMnu";
            SysSetMnu.Size = new System.Drawing.Size(191, 22);
            SysSetMnu.Text = Properties.Resources.Setting;
            SysSetMnu.Click += SysSetMnu_Click;
            // 
            // KeyTool
            // 
            KeyTool.Name = "KeyTool";
            KeyTool.Size = new System.Drawing.Size(191, 22);
            KeyTool.Text = Properties.Resources.KeySet;
            KeyTool.Click += KeyToolStripMenuItem_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new System.Drawing.Size(188, 6);
            // 
            // SaveStripMenuItem
            // 
            SaveStripMenuItem.Enabled = false;
            SaveStripMenuItem.Name = "SaveStripMenuItem";
            SaveStripMenuItem.Size = new System.Drawing.Size(191, 22);
            SaveStripMenuItem.Text = Properties.Resources.SaveState;
            // 
            // LoadStripMenuItem
            // 
            LoadStripMenuItem.Enabled = false;
            LoadStripMenuItem.Name = "LoadStripMenuItem";
            LoadStripMenuItem.Size = new System.Drawing.Size(191, 22);
            LoadStripMenuItem.Text = Properties.Resources.LoadState;
            // 
            // UnLoadStripMenuItem
            // 
            UnLoadStripMenuItem.Enabled = false;
            UnLoadStripMenuItem.Name = "UnLoadStripMenuItem";
            UnLoadStripMenuItem.Size = new System.Drawing.Size(191, 22);
            UnLoadStripMenuItem.Text = Properties.Resources.UnLoad;
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
            CheatCode.Text = Properties.Resources.CheatCode;
            CheatCode.Click += CheatCode_Click;
            // 
            // MemEditMnu
            // 
            MemEditMnu.Name = "MemEditMnu";
            MemEditMnu.Size = new System.Drawing.Size(191, 22);
            MemEditMnu.Text = Properties.Resources.memedit;
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
            FreeSpeed.Text = Properties.Resources.FastSpeed;
            // 
            // MnuPause
            // 
            MnuPause.Name = "MnuPause";
            MnuPause.Size = new System.Drawing.Size(191, 22);
            MnuPause.Text = Properties.Resources.pause;
            MnuPause.Click += MnuPause_Click;
            // 
            // RenderToolStripMenuItem
            // 
            RenderToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { CutBlackLineMnu, frameskipmnu, toolStripMenuItem6, directx2DRender, directx3DRender, openGLRender, VulkanRenderMnu, toolStripMenuItem4, xBRScaleAdd, xBRScaleDec, fullScreenF2, toolStripMenuItem7 });
            RenderToolStripMenuItem.Name = "RenderToolStripMenuItem";
            RenderToolStripMenuItem.Size = new System.Drawing.Size(78, 21);
            RenderToolStripMenuItem.Text = Properties.Resources.Render;
            // 
            // CutBlackLineMnu
            // 
            CutBlackLineMnu.CheckOnClick = true;
            CutBlackLineMnu.Name = "CutBlackLineMnu";
            CutBlackLineMnu.Size = new System.Drawing.Size(384, 22);
            CutBlackLineMnu.Text = Properties.Resources.cutline;
            CutBlackLineMnu.CheckedChanged += CutBlackLineMnu_CheckedChanged;
            // 
            // frameskipmnu
            // 
            frameskipmnu.Checked = true;
            frameskipmnu.CheckOnClick = true;
            frameskipmnu.CheckState = System.Windows.Forms.CheckState.Checked;
            frameskipmnu.Name = "frameskipmnu";
            frameskipmnu.Size = new System.Drawing.Size(384, 22);
            frameskipmnu.Text = Properties.Resources.Frameskip;
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
            xBRScaleAdd.Click += xBRScaleAdd_Click;
            // 
            // xBRScaleDec
            // 
            xBRScaleDec.Name = "xBRScaleDec";
            xBRScaleDec.Size = new System.Drawing.Size(384, 22);
            xBRScaleDec.Text = "IR Scale --  (F12)";
            xBRScaleDec.Click += xBRScaleDec_Click;
            // 
            // toolStripMenuItem7
            // 
            toolStripMenuItem7.Name = "toolStripMenuItem7";
            toolStripMenuItem7.Size = new System.Drawing.Size(381, 6);
            // 
            // fullScreenF2
            // 
            fullScreenF2.Name = "fullScreenF2";
            fullScreenF2.Size = new System.Drawing.Size(384, 22);
            fullScreenF2.Text = Properties.Resources.FrmMain_InitializeComponent_FullScreenF2;
            fullScreenF2.Click += fullScreenF2_Click;
            // 
            // NetPlayMnu
            // 
            NetPlayMnu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { NetPlaySetMnu });
            NetPlayMnu.Name = "NetPlayMnu";
            NetPlayMnu.Size = new System.Drawing.Size(82, 21);
            NetPlayMnu.Text = Properties.Resources.netplay;
            // 
            // NetPlaySetMnu
            // 
            NetPlaySetMnu.Name = "NetPlaySetMnu";
            NetPlaySetMnu.Size = new System.Drawing.Size(116, 22);
            NetPlaySetMnu.Text = Properties.Resources.netplayset;
            NetPlaySetMnu.Click += NetPlaySetMnu_Click;
            // 
            // AboutMnu
            // 
            AboutMnu.Name = "AboutMnu";
            AboutMnu.Size = new System.Drawing.Size(71, 21);
            AboutMnu.Text = Properties.Resources.about;
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
            // FrmMain
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(684, 498);
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
        private System.Windows.Forms.ToolStripMenuItem RenderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem directx3DRender;
        private System.Windows.Forms.ToolStripMenuItem openGLRender;
        private System.Windows.Forms.ToolStripMenuItem LoadDIsk;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem KeyTool;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem SaveStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem LoadStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UnLoadStripMenuItem;
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
        private System.Windows.Forms.ToolStripMenuItem AboutMnu;
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
    }
}
