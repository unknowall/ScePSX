using Microsoft.Win32;
using ScePSX.Core.GPU;
using ScePSX.Render;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using static ScePSX.Controller;

namespace ScePSX.UI
{
    //如果您想深入了解本项目的代码实现，建议使用AI辅助阅读，例如GitHub Copilot、ChatGPT、doubao、DeepSeek、等
    //推荐 https://deepwiki.com/unknowall/ScePSP

    // If you want to gain an in-depth understanding of the code implementation of this project,
    // it is recommended to use AI-assisted reading tools such as GitHub Copilot, ChatGPT, Doubao, DeepSeek, etc.
    // recommended: https://deepwiki.com/unknowall/ScePSP

    public partial class FrmMain : Form, IAudioHandler, IRenderHandler, IRumbleHandler
    {
        [DllImport("kernel32.dll")]
        public static extern Boolean AllocConsole();
        [DllImport("kernel32.dll")]
        public static extern Boolean FreeConsole();

        public static string version = "ScePSX v0.2.0.1";

        private static string mypath = Application.StartupPath;
        public static IniFile ini = new IniFile(mypath + "ScePSX.ini");
        private string currbios;
        private string shaderpath;
        private int StatusDelay;
        private string gamename;

        private int StateSlot;
        private int CoreWidth, CoreHeight;

        public static PSXCore Core;

        private bool KeyFirst, isAnalog = false;
        private SDLHanlder SDLHanlder;

        private int _frameCount = 0;
        private Stopwatch _fpsStopwatch = Stopwatch.StartNew();
        private float _currentFps = 0;

        private System.Windows.Forms.Timer timer;

        public ScaleParam scale;
        public int IRscale = 1;
        public bool PGXP, PGXPT, Realcolor, AutoIR, KeepAR, bFullScreen;
        private bool cutblackline = false;
        private int[] cutbuff = new int[1024 * 512];

        private RomList romList;

        RenderMode Rendermode = RenderMode.OpenGL;

        GPUType gputype = GPUType.Advite;

        ToolStripMenuItem gpumnu;

        RendererManager Render = new RendererManager();

        public FrmMain()
        {
            InitializeComponent();

            if (ini.ReadInt("Main", "Console") == 1)
            {
                AllocConsole();
            }

            CheckForIllegalCrossThreadCalls = false;

            panel.KeyDown += new KeyEventHandler(ButtonsDown);
            panel.KeyUp += new KeyEventHandler(ButtonsUp);

            if (!Path.Exists("./Save"))
                Directory.CreateDirectory("./Save");
            if (!Path.Exists("./BIOS"))
                Directory.CreateDirectory("./BIOS");
            if (!Path.Exists("./Cheats"))
                Directory.CreateDirectory("./Cheats");
            if (!Path.Exists("./SaveState"))
                Directory.CreateDirectory("./SaveState");
            if (!Path.Exists("./Shaders"))
                Directory.CreateDirectory("./Shaders");
            if (!Path.Exists("./Icons"))
                Directory.CreateDirectory("./Icons");

            if (File.Exists("./opengl32.dll") && !File.Exists("./ReShader.dll"))
            {
                File.Copy("./opengl32.dll", "./ReShader.dll");
            }

            CloseRomMnu_Click(null, null);

            shaderpath = ini.Read("main", "shader");
            Rendermode = (RenderMode)ini.ReadInt("Main", "Render");
            gputype = (GPUType)ini.ReadInt("Main", "GpuMode");

            openGLRender.Checked = Rendermode == RenderMode.OpenGL;
            directx2DRender.Checked = Rendermode == RenderMode.Directx2D;
            directx3DRender.Checked = Rendermode == RenderMode.Directx3D;
            VulkanRenderMnu.Checked = Rendermode == RenderMode.Vulkan;

            switch (ini.ReadInt("OpenGL", "MSAA"))
            {
                case 0:
                    Render.oglMSAA = 0;
                    break;
                case 1:
                    Render.oglMSAA = 4;
                    break;
                case 2:
                    Render.oglMSAA = 6;
                    break;
                case 3:
                    Render.oglMSAA = 8;
                    break;
                case 4:
                    Render.oglMSAA = 16;
                    break;
            }
            Render.frameskip = ini.ReadInt("main", "skipframe");
            Render.oglShaderPath = ("./Shaders/" + shaderpath);

            frameskipmnu.Checked = ini.ReadInt("main", "frameskip") == 1;
            cutblackline = ini.ReadInt("main", "cutblackline") == 1;

            CutBlackLineMnu.Checked = cutblackline;

            KeyFirst = ini.ReadInt("main", "keyfirst") == 1;
            isAnalog = ini.ReadInt("main", "isAnalog") == 1;

            Translations.DefaultLanguage = "en";
            Translations.Init();
            Translations.UpdateLang(this);

            FrmInput.InitKeyMap();
            FrmInput.InitControllerMap();

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Elapsed;
            timer.Enabled = true;
            timer.Start();

            currbios = ini.Read("main", "bios");
            if (currbios == null) currbios = "SCPH1001.BIN";

            SDLHanlder = new SDLHanlder(ini);

            InitShaderMnu();
            InitLangMenu();

            BackColor = Color.Black;
            StatusBar.BackColor = Color.White;
            this.Load += MainForm_Load;
            this.FormClosing += MainForm_Closing;

            gpumnu = AddMenu("gpumode", $"GPU: {gputype.ToString()}", 88, MnuRender);
            gpumnu.Enabled = false;

            openGLRender.Text = openGLRender.Text + Translations.GetText("recommend");
        }

        private void InitLangMenu()
        {
            foreach (var lang in Translations.Languages)
            {
                ToolStripMenuItem item = new ToolStripMenuItem();
                item.Text = lang.Value;
                item.Tag = lang.Key;
                item.Click += LangClick;

                MnuLang.DropDownItems.Add(item);
            }
        }

        private void LangClick(object sender, EventArgs e)
        {
            if (sender != null)
            {
                ToolStripMenuItem item = (ToolStripMenuItem)sender;
                if (item.Tag != null)
                {
                    Translations.CurrentLangId = (string)item.Tag;
                    Translations.UpdateLang(this);
                    Translations.UpdateLang(romList);
                    romList.FillByini();
                    openGLRender.Text = openGLRender.Text + Translations.GetText("recommend");
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            string[] PosStr = ini.Read("Main", "FromPos").Split('|');
            if (PosStr.Length >= 4)
            {
                this.Location = new Point(Convert.ToInt16(PosStr[0]), Convert.ToInt16(PosStr[1]));
                this.Size = new Size(Convert.ToInt16(PosStr[2]), Convert.ToInt16(PosStr[3]));
                Rectangle screenBounds = Screen.PrimaryScreen.WorkingArea;
                if (!screenBounds.Contains(this.Bounds))
                {
                    this.Location = new Point(0, 0);
                }
            }
        }

        private void MainForm_Closing(object sender, FormClosingEventArgs e)
        {
            string PosStr = $"{this.Location.X}|{this.Location.Y}|{this.Size.Width}|{this.Size.Height}";
            ini.Write("Main", "FromPos", PosStr);
        }

        private void UpdateStatus(int index, string text, bool clean = false)
        {
            if (index < 0 || index >= StatusBar.Items.Count)
                return;
            if (clean)
                foreach (ToolStripItem item in StatusBar.Items)
                {
                    item.Text = "";
                }
            StatusBar.Items[index].Text = text;
        }

        private unsafe void Timer_Elapsed(object sender, EventArgs e)
        {
            SDLHanlder.CheckController();

            if (Core == null)
            {
                this.Text = version;
                return;
            }

            if (StatusDelay > 0)
            {
                StatusDelay--;
            }
            else
            {
                UpdateStatus(1, $"F3+ F4- {Translations.GetText("SaveSlot")} [{StateSlot}]");
                UpdateStatus(2, $"F9[{(KeyFirst ? Translations.GetText("KeyFirst") : Translations.GetText("JoyFirst"))}]");
                UpdateStatus(3, $"F10[{(isAnalog ? Translations.GetText("Analog") : Translations.GetText("Digital"))}]");
            }

            if (Core.Pauseed)
            {
                SimpleOSD.Show(Render._currentRenderer as UserControl, " ▌▌");
                UpdateStatus(1, Translations.GetText("Pause"), true);
            }

            if (AutoIR)
            {
                if (this.ClientSize.Width >= 320 && this.ClientSize.Height >= 240)
                    IRscale = 2;
                if (this.ClientSize.Width >= 800 && this.ClientSize.Height >= 600)
                    IRscale = 3;
                if (this.ClientSize.Width >= 1280 && this.ClientSize.Height >= 800)
                    IRscale = 4;
                if (this.ClientSize.Width >= 1920 && this.ClientSize.Height >= 1080)
                    IRscale = 5;
            }

            if (Core.GPU.type == GPUType.OpenGL && AutoIR)
            {
                GPUBackend.IRScale = IRscale;
                (Core.GPU as OpenglGPU).IRScale = IRscale;
            }
            if (Core.GPU.type == GPUType.Vulkan && AutoIR)
            {
                GPUBackend.IRScale = IRscale;
                (Core.GPU as VulkanGPU).IRScale = IRscale;
            }

            this.Text = $"{version}  -  {gamename}";

            int scalew = CoreWidth;
            int scaleh = CoreHeight;

            string rendername = Rendermode.ToString();
            if (Rendermode == RenderMode.OpenGL)
            {
                if (Render.oglMSAA > 0)
                    rendername += $" {Render.oglMSAA}xMSAA";
            }
            if (scale.scale > 0)
            {
                scalew *= scale.scale;
                scaleh *= scale.scale;
            }
            UpdateStatus(0, Core.DiskID);
            string str_pgxp = PGXPVector.use_pgxp ? "PGXP " : "";
            if (Core.GPU.type == GPUType.OpenGL || (Core.GPU.type == GPUType.Advite && Rendermode == RenderMode.OpenGL))
            {
                UpdateStatus(5, $"{str_pgxp}OpenGL {Render.oglMSAA}xMSAA  {IRscale}xIR");
            }
            else if (Core.GPU.type == GPUType.Vulkan || (Core.GPU.type == GPUType.Advite && Rendermode == RenderMode.Vulkan))
            {
                UpdateStatus(5, $"{str_pgxp}Vulkan  {IRscale}xIR");
            }
            else
            {
                UpdateStatus(5, $"{str_pgxp}{Core.GPU.type.ToString()} {rendername}");
            }
            UpdateStatus(6, $"{(scale.scale > 0 ? scale.mode.ToString() : "")} {scalew}*{scaleh}");
            UpdateStatus(7, $"FPS {_currentFps:F1}");
        }

        ~FrmMain()
        {
            if (Core != null) Core.Stop();
        }

        #region MENU
        private void InitShaderMnu()
        {
            if (shaderpath == null)
                shaderpath = "Base";

            DirectoryInfo dir2 = new DirectoryInfo(mypath + "/Shaders");
            foreach (DirectoryInfo f in dir2.GetDirectories())
            {
                ToolStripMenuItem mnu = new ToolStripMenuItem();
                mnu.Name = f.Name;
                mnu.Text = f.Name;
                mnu.Tag = 20;
                mnu.CheckOnClick = true;
                mnu.Click += Mnu_Click;
                mnu.CheckedChanged += Mnu_CheckedChanged;
                openGLRender.DropDownItems.Add(mnu);
                if (shaderpath == f.Name)
                {
                    mnu.Checked = true;
                    shaderpath = mnu.Text;
                }
            }
        }

        private void InitStateMnu()
        {
            MnuSaveState.DropDownItems.Clear();
            MnuLoadState.DropDownItems.Clear();

            MnuSaveState.Enabled = true;
            MnuLoadState.Enabled = true;
            MnuUnloadState.Enabled = true;

            StateSlot = ini.ReadInt("main", "StateSlot");

            string statefile, statename;

            for (int i = 0; i < 10; i++)
            {
                ToolStripMenuItem mnusave = new ToolStripMenuItem();
                ToolStripMenuItem mnuload = new ToolStripMenuItem();

                statefile = mypath + "/SaveState/" + Core.DiskID + "_Save" + i.ToString() + ".dat";
                if (File.Exists(statefile))
                {
                    statename = i.ToString() + " - " + File.GetLastWriteTime(statefile).ToLocalTime();
                }
                else
                {
                    statename = i.ToString() + " - None";
                    mnuload.Enabled = false;
                }
                if (i == StateSlot)
                    mnusave.Checked = true;
                mnusave.Name = statefile;
                mnusave.Text = statename;
                mnusave.Tag = 30 + i;
                mnusave.CheckOnClick = true;
                mnusave.Click += Mnu_Click;
                mnusave.CheckedChanged += Mnu_CheckedChanged;
                MnuSaveState.DropDownItems.Add(mnusave);

                mnuload.Name = statefile;
                mnuload.Text = statename;
                mnuload.Tag = 40 + i;
                mnuload.Click += Mnu_Click;
                MnuLoadState.DropDownItems.Add(mnuload);
            }
        }

        private ToolStripMenuItem AddMenu(string name, string Text, object tag, ToolStripMenuItem Parent, bool chk = false, bool ch = true)
        {
            ToolStripMenuItem mnu = new ToolStripMenuItem();
            mnu.Text = Text;
            mnu.Name = name;
            mnu.CheckOnClick = ch;
            mnu.Checked = chk;
            mnu.Tag = tag;
            mnu.Click += Mnu_Click;
            if (ch)
            {
                mnu.CheckedChanged += Mnu_CheckedChanged;
            }
            Parent.DropDownItems.Add(mnu);

            return mnu;
        }

        private void Mnu_CheckedChanged(object sender, EventArgs e)
        {
            var mnu = (ToolStripMenuItem)sender;
            var parent = mnu.GetCurrentParent();
            if (!mnu.Checked || parent == null)
                return;
            switch (mnu.Tag)
            {
                case 20:
                    shaderpath = mnu.Text;
                    ini.Write("main", "shader", shaderpath);
                    break;
            }
            foreach (ToolStripMenuItem item in parent.Items)
            {
                if (item != null && item != mnu && item.Checked)
                {
                    item.Checked = false;
                    return;
                }
            }
        }

        private void Mnu_Click(object sender, EventArgs e)
        {
            var mnu = (ToolStripMenuItem)sender;

            if ((int)mnu.Tag >= 30 && (int)mnu.Tag < 40)
            {
                StateSlot = (int)mnu.Tag - 30;
                SaveState(StateSlot);

                Mnu_CheckedChanged(sender, e);

                ini.WriteInt("main", "StateSlot", StateSlot);
                string statefile = mypath + "/SaveState/" + Core.DiskID + "_Save" + StateSlot.ToString() + ".dat";
                string statename = StateSlot.ToString() + " - " + File.GetLastWriteTime(statefile).ToLocalTime();

                mnu.Text = statename;
                foreach (ToolStripMenuItem item in MnuLoadState.DropDownItems)
                {
                    if ((int)item.Tag == 40 + StateSlot)
                    {
                        item.Text = statename;
                        item.Enabled = true;
                        break;
                    }
                }
                return;
            }
            if ((int)mnu.Tag >= 40 && (int)mnu.Tag < 50)
            {
                StateSlot = (int)mnu.Tag - 40;
                LoadState(StateSlot);
                SetGpuParam();
                return;
            }
        }

        private void fullScreenF2_Click(object sender, EventArgs e)
        {
            if (Core == null)
            {
                return;
            }

            bFullScreen = !bFullScreen;

            if (bFullScreen)
            {
                this.MainMenu.Visible = false;
                this.StatusBar.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.MainMenu.Visible = true;
                this.StatusBar.Visible = true;
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void SearchMnu_Click(object sender, EventArgs e)
        {
            if (Core != null)
            {
                return;
            }
            if (romList != null)
            {
                var folderDialog = new FolderBrowserDialog();
                folderDialog.InitialDirectory = ini.Read("main", "LastPath");
                folderDialog.Description = "";

                if (folderDialog.ShowDialog() == DialogResult.Cancel)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(folderDialog.SelectedPath))
                {
                    romList.SearchDir(folderDialog.SelectedPath);
                }
            }
        }

        private void CloseRomMnu_Click(object sender, EventArgs e)
        {
            if (Core != null)
            {
                Core.Stop();
                Core.Dispose();
                Core = null;
            }

            romList = new RomList();
            romList.Parent = this;
            romList.Dock = DockStyle.Fill;
            panel.Controls.Add(romList);
            romList.Enabled = true;
            romList.Visible = true;
            romList.BorderStyle = BorderStyle.FixedSingle;
            romList.DoubleClick += new EventHandler(romList_DoubleClick);
            romList.FillByini();
            romList.BringToFront();

            UpdateStatus(0, "", true);
        }

        private void SetGpuParam()
        {
            GPUBackend.HWND = NullRenderer.hwnd;
            GPUBackend.HINST = NullRenderer.hinstance;
            GPUBackend.ClientHeight = NullRenderer.ClientHeight;
            GPUBackend.ClientWidth = NullRenderer.ClientWidth;

            if (Rendermode == RenderMode.OpenGL && (Core.GPU.type == GPUType.OpenGL || Core.GPU.type == GPUType.Advite))
            {
                IRscale = IRscale < 1 ? 1 : IRscale;
                (Core.GPU as OpenglGPU).IRScale = IRscale;
                (Core.GPU as OpenglGPU).PGXP = PGXPVector.use_pgxp_highpos && PGXPVector.use_pgxp;
                (Core.GPU as OpenglGPU).PGXPT = PGXPT;
                (Core.GPU as OpenglGPU).KEEPAR = KeepAR;
                (Core.GPU as OpenglGPU).RealColor = Realcolor;
            }
            if (Rendermode == RenderMode.Vulkan && (Core.GPU.type == GPUType.Vulkan || Core.GPU.type == GPUType.Advite))
            {
                IRscale = IRscale < 1 ? 1 : IRscale;
                (Core.GPU as VulkanGPU).IRScale = IRscale;
                (Core.GPU as VulkanGPU).PGXP = PGXPVector.use_pgxp_highpos && PGXPVector.use_pgxp;
                (Core.GPU as VulkanGPU).PGXPT = PGXPT;
                (Core.GPU as VulkanGPU).KEEPAR = KeepAR;
                (Core.GPU as VulkanGPU).RealColor = Realcolor;
            }
        }

        private void SeleRender(RenderMode mode)
        {
            Rendermode = mode;
            ini.WriteInt("Main", "Render", (int)Rendermode);
            GPUType gpumode = (GPUType)ini.ReadInt("main", "GpuMode");

            if (Core != null && Core.Running)
            {
                Core.WaitPausedAndSync();

                if (mode == RenderMode.OpenGL && (gpumode == GPUType.OpenGL || gpumode == GPUType.Advite))
                {
                    Render.DisposeCurrentRenderer();
                    Render.SelectRenderer(RenderMode.Null, panel);

                    while (NullRenderer.hwnd == 0) Thread.Sleep(10);
                    GPUBackend.HWND = NullRenderer.hwnd;
                    GPUBackend.HINST = NullRenderer.hinstance;
                    GPUBackend.ClientHeight = NullRenderer.ClientHeight;
                    GPUBackend.ClientWidth = NullRenderer.ClientWidth;

                    Core.PsxBus.gpu.SelectGPU(GPUType.OpenGL);

                    Core.GPU = Core.PsxBus.gpu.Backend.GPU;

                    IRscale = IRscale < 1 ? 1 : IRscale;
                    (Core.GPU as OpenglGPU).IRScale = IRscale;
                    (Core.GPU as OpenglGPU).PGXP = PGXPVector.use_pgxp_highpos && PGXPVector.use_pgxp;
                    (Core.GPU as OpenglGPU).PGXPT = PGXPT;
                    (Core.GPU as OpenglGPU).KEEPAR = KeepAR;
                    (Core.GPU as OpenglGPU).RealColor = Realcolor;

                    Core.GpuBackend = GPUType.OpenGL;
                }
                if (mode == RenderMode.Vulkan && (gpumode == GPUType.Vulkan || gpumode == GPUType.Advite))
                {
                    Render.DisposeCurrentRenderer();
                    Render.SelectRenderer(RenderMode.Null, panel);

                    while (NullRenderer.hwnd == 0) Thread.Sleep(00);
                    GPUBackend.HWND = NullRenderer.hwnd;
                    GPUBackend.HINST = NullRenderer.hinstance;
                    GPUBackend.ClientHeight = NullRenderer.ClientHeight;
                    GPUBackend.ClientWidth = NullRenderer.ClientWidth;

                    Core.PsxBus.gpu.SelectGPU(GPUType.Vulkan);

                    Core.GPU = Core.PsxBus.gpu.Backend.GPU;

                    IRscale = IRscale < 1 ? 1 : IRscale;
                    (Core.GPU as VulkanGPU).IRScale = IRscale;
                    (Core.GPU as VulkanGPU).PGXP = PGXPVector.use_pgxp_highpos && PGXPVector.use_pgxp;
                    (Core.GPU as VulkanGPU).PGXPT = PGXPT;
                    (Core.GPU as VulkanGPU).KEEPAR = KeepAR;
                    (Core.GPU as VulkanGPU).RealColor = Realcolor;

                    Core.GpuBackend = GPUType.Vulkan;
                }
                if (mode != RenderMode.OpenGL && gpumode == GPUType.OpenGL)
                {
                    Render.SelectRenderer(Rendermode, panel);

                    if (Core.GpuBackend != GPUType.Software)
                        Core.PsxBus.gpu.SelectGPU(GPUType.Software);

                    Core.GpuBackend = GPUType.Software;
                }
                if (mode != RenderMode.Vulkan && gpumode == GPUType.Vulkan)
                {
                    Render.SelectRenderer(Rendermode, panel);

                    if (Core.GpuBackend != GPUType.Software)
                        Core.PsxBus.gpu.SelectGPU(GPUType.Software);

                    Core.GpuBackend = GPUType.Software;
                }
                if (gpumode == GPUType.Software || (mode != RenderMode.Vulkan && mode != RenderMode.OpenGL))
                {
                    Render.SelectRenderer(Rendermode, panel);

                    if (Core.GpuBackend != GPUType.Software)
                        Core.PsxBus.gpu.SelectGPU(GPUType.Software);

                    Core.GpuBackend = GPUType.Software;
                }

                Core.GPU = Core.PsxBus.gpu.Backend.GPU;

                gpumnu.Text = $"GPU: {Core.GpuBackend.ToString()}";

                Core.Pauseing = false;

                SimpleOSD.Show(Render._currentRenderer as UserControl, $"GPU {Core.GpuBackend.ToString()} Render {mode.ToString()}", 5000);
            }
        }

        private void directx2DRender_Click(object sender, EventArgs e)
        {
            directx3DRender.Checked = false;
            openGLRender.Checked = false;
            VulkanRenderMnu.Checked = false;

            SeleRender(RenderMode.Directx2D);
        }

        private void directx3DToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openGLRender.Checked = false;
            directx2DRender.Checked = false;
            VulkanRenderMnu.Checked = false;

            SeleRender(RenderMode.Directx3D);
        }

        private void openGLToolStripMenuItem_Click(object sender, EventArgs e)
        {

            directx2DRender.Checked = false;
            directx3DRender.Checked = false;
            VulkanRenderMnu.Checked = false;

            Render.oglShaderPath = "./Shaders/" + shaderpath;

            SeleRender(RenderMode.OpenGL);
        }

        private void VulkanRenderMnu_Click(object sender, EventArgs e)
        {
            openGLRender.Checked = false;
            directx2DRender.Checked = false;
            directx3DRender.Checked = false;

            SeleRender(RenderMode.Vulkan);
        }

        private void frameskipmnu_CheckedChanged(object sender, EventArgs e)
        {
            if (Rendermode != RenderMode.OpenGL && Render._currentRenderer != null)
            {
                if (frameskipmnu.Checked == false)
                    Render._currentRenderer.SetParam(0);
                else
                    Render._currentRenderer.SetParam(ini.ReadInt("Main", "SkipFrame"));
            }

            ini.WriteInt("main", "frameskip", Convert.ToInt16(frameskipmnu.Checked));
        }

        private void CutBlackLineMnu_CheckedChanged(object sender, EventArgs e)
        {
            cutblackline = CutBlackLineMnu.Checked;

            ini.WriteInt("main", "cutblackline", Convert.ToInt16(cutblackline));
        }

        private void LoadDisk_Click(object sender, EventArgs e)
        {
            LoadRom();
        }

        private void romList_DoubleClick(object sender, EventArgs e)
        {
            var Game = romList.SelectedGame();
            if (Game != null)
            {
                LoadRom(Game.fullName, Game);
            }
        }

        private void SwapDisk_Click(object sender, EventArgs e)
        {
            SwapDisc();
        }

        private void UpScale_Click(object sender, EventArgs e)
        {
            if (Core != null && Core.Running)
            {
                IRscale = IRscale < 9 ? IRscale + 1 : 9;
                int xscale = IRscale;
                AutoIR = false;
                switch (Core.PsxBus.gpu.Backend.GPU.type)
                {
                    case GPUType.OpenGL:
                        GPUBackend.IRScale = IRscale;
                        (Core.GPU as OpenglGPU).IRScale = IRscale;
                        break;
                    case GPUType.Vulkan:
                        GPUBackend.IRScale = IRscale;
                        (Core.GPU as VulkanGPU).IRScale = IRscale;
                        break;
                    default:
                        if (scale.scale < 8)
                            scale.scale = scale.scale == 0 ? 2 : scale.scale * 2;
                        xscale = scale.scale;
                        break;
                }

                SimpleOSD.Show(Render._currentRenderer as UserControl, $"{xscale}xIR");
            }
        }

        private void DownScale_Click(object sender, EventArgs e)
        {
            if (Core != null && Core.Running)
            {
                IRscale = IRscale > 1 ? IRscale - 1 : 1;
                int xscale = IRscale;
                AutoIR = false;
                switch (Core.PsxBus.gpu.Backend.GPU.type)
                {
                    case GPUType.OpenGL:
                        GPUBackend.IRScale = IRscale;
                        (Core.GPU as OpenglGPU).IRScale = IRscale;
                        break;
                    case GPUType.Vulkan:
                        GPUBackend.IRScale = IRscale;
                        (Core.GPU as VulkanGPU).IRScale = IRscale;
                        break;
                    default:
                        if (scale.scale > 0)
                            scale.scale /= 2;
                        xscale = scale.scale;
                        break;
                }

                SimpleOSD.Show(Render._currentRenderer as UserControl, $"{xscale}xIR");
            }
        }

        private void MnuPause_Click(object sender, EventArgs e)
        {
            if (Core != null && Core.Running)
                Core.Pause();
        }

        private void UnLoadStripMenuItem_Click(object sender, EventArgs e)
        {
            UnLoadState();
        }

        private void ShowFrom(Form Frm)
        {
            Frm.StartPosition = FormStartPosition.Manual;
            Frm.Owner = this;

            Point parentCenterClient = new Point(
                this.ClientSize.Width / 2,
                this.ClientSize.Height / 2
            );

            Point parentCenterScreen = this.PointToScreen(parentCenterClient);

            Frm.Location = new Point(
                parentCenterScreen.X - Frm.Width / 2,
                parentCenterScreen.Y - Frm.Height / 2
                );

            Translations.UpdateLang(Frm);

            Frm.Show();
        }

        private void CheatCode_Click(object sender, EventArgs e)
        {
            if (Core != null && Core.Running)
            {
                ShowFrom(new Form_Cheat(Core.DiskID));
            }
        }

        private void MnuDebug_Click(object sender, EventArgs e)
        {
            ShowFrom(new Form_Mem());
        }

        private void KeyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowFrom(new FrmInput());
        }

        private string GetDefaultBrowserPath()
        {
            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"HTTP\shell\open\command", false))
            {
                if (key != null)
                {
                    string command = key.GetValue(null) as string;
                    if (!string.IsNullOrEmpty(command))
                    {
                        int firstQuote = command.IndexOf('"');
                        int secondQuote = command.IndexOf('"', firstQuote + 1);
                        if (firstQuote >= 0 && secondQuote > firstQuote)
                        {
                            return command.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                        }
                    }
                }
                return null;
            }
        }

        private void gitHubMnu_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(GetDefaultBrowserPath(), "https://github.com/unknowall/ScePSX");
            }
            catch
            {
            }
        }

        private void supportKoficomMnu_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(GetDefaultBrowserPath(), "https://ko-fi.com/unknowall");
            }
            catch
            {
            }
        }

        private void supportWeChatMnu_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(GetDefaultBrowserPath(), "https://gitee.com/unknowall/ScePSX/raw/master/Others/support_via_wechat.png");
            }
            catch
            {
            }
        }

        private void AboutMnu_Click(object sender, EventArgs e)
        {
            ShowFrom(new FrmAbout());
        }

        private void NetPlaySetMnu_Click(object sender, EventArgs e)
        {
            ShowFrom(new FrmNetPlay());
        }

        private void SysSetMnu_Click(object sender, EventArgs e)
        {
            ShowFrom(new Form_Set());
        }
        #endregion

        private void SaveState(int Slot = 0)
        {
            if (Core != null && Core.Running)
            {
                Core.SaveState(Slot.ToString());
                SimpleOSD.Show(Render._currentRenderer as UserControl, $"{Translations.GetText("FrmMain_SaveState_saved")} [ {StateSlot} ]");
                UpdateStatus(1, $"{Translations.GetText("FrmMain_SaveState_saved")} [{StateSlot}]", true);
                StatusDelay = 3;
            }
        }

        private void LoadState(int Slot = 0)
        {
            if (Core != null && Core.Running)
            {
                Core.SaveState("~");
                Core.LoadState(Slot.ToString());
                SimpleOSD.Show(Render._currentRenderer as UserControl, $"Load [{Slot}]");
            }
        }

        private void UnLoadState()
        {
            if (Core != null && Core.Running)
            {
                Core.LoadState("~");
            }
        }

        private void ButtonsDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                if (Core != null && Core.Running)
                {
                    if (Core.Pauseed)
                        SimpleOSD.Close();
                    Core.Pause();
                }
                return;
            }
            if (e.KeyCode == Keys.Escape)
            {
                if (bFullScreen)
                    fullScreenF2_Click(null, null);
                return;
            }
            if (e.KeyCode == Keys.F2)
            {
                fullScreenF2_Click(null, null);
                return;
            }
            if (e.KeyCode == Keys.F3)
            {
                StateSlot = StateSlot < 9 ? StateSlot + 1 : StateSlot;
                SimpleOSD.Show(Render._currentRenderer as UserControl, $"{Translations.GetText("SaveSlot")} [ {StateSlot} ]");
                return;
            }
            if (e.KeyCode == Keys.F4)
            {
                StateSlot = StateSlot > 0 ? StateSlot - 1 : StateSlot;
                SimpleOSD.Show(Render._currentRenderer as UserControl, $"{Translations.GetText("SaveSlot")} [ {StateSlot} ]");
                return;
            }
            if (e.KeyCode == Keys.F5)
            {
                foreach (ToolStripMenuItem item in MnuSaveState.DropDownItems)
                {
                    if ((int)item.Tag == 30 + StateSlot)
                    {
                        Mnu_Click(item, null);
                        break;
                    }
                }
                return;
            }
            if (e.KeyCode == Keys.F6)
            {
                LoadState(StateSlot);
                SetGpuParam();
                return;
            }
            if (e.KeyCode == Keys.F7)
            {
                UnLoadState();
                return;
            }
            if (e.KeyCode == Keys.F9)
            {
                KeyFirst = !KeyFirst;
                ini.WriteInt("main", "keyfirst", KeyFirst ? 1 : 0);
                return;
            }
            if (e.KeyCode == Keys.F10)
            {
                isAnalog = !isAnalog;
                ini.WriteInt("main", "isAnalog", isAnalog ? 1 : 0);
                if (Core != null)
                {
                    Core.PsxBus.controller1.IsAnalog = isAnalog;
                }
                SimpleOSD.Show(Render._currentRenderer as UserControl,
                    $"{(isAnalog ? Translations.GetText("Analog") : Translations.GetText("Digital"))}"
                    );
                return;
            }
            if (e.KeyCode == Keys.Tab && Core != null)
            {
                Core.Boost = true;
                return;
            }
            if (e.KeyCode == Keys.F11)
            {
                UpScale_Click(null, null);
                return;
            }
            if (e.KeyCode == Keys.F12)
            {
                DownScale_Click(null, null);
                return;
            }

            InputAction button = FrmInput.KMM1.GetKeyButton(e.KeyCode);
            if ((int)button != 0xFF && Core != null && Core.Running)
            {
                Core.Button(button, true, 0);
                if (!KeyFirst)
                    KeyFirst = true;
            }

            InputAction button1 = FrmInput.KMM2.GetKeyButton(e.KeyCode);
            if ((int)button1 != 0xFF && Core != null && Core.Running)
                Core.Button(button1, true, 1);
        }

        private void ButtonsUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab && Core != null)
            {
                Core.Boost = false;
                return;
            }

            InputAction button = FrmInput.KMM1.GetKeyButton(e.KeyCode);
            if ((int)button != 0xFF && Core != null && Core.Running)
                Core.Button(button, false, 0);

            InputAction button1 = FrmInput.KMM2.GetKeyButton(e.KeyCode);
            if ((int)button1 != 0xFF && Core != null && Core.Running)
                Core.Button(button1, false, 1);
        }

        private void applypgxp()
        {
            PGXPVector.use_pgxp = ini.ReadInt("PGXP", "base") == 1;
            PGXPVector.use_pgxp_aff = ini.ReadInt("PGXP", "aff") == 1;
            PGXPVector.use_pgxp_avs = ini.ReadInt("PGXP", "avs") == 1;
            PGXPVector.use_pgxp_clip = ini.ReadInt("PGXP", "clip") == 1;
            PGXPVector.use_pgxp_nc = ini.ReadInt("PGXP", "nc") == 1;
            PGXPVector.use_pgxp_highpos = ini.ReadInt("PGXP", "highpos") == 1;
            PGXPVector.use_pgxp_memcap = ini.ReadInt("PGXP", "memcap") == 1;
            PGXPVector.use_perspective_correction = ini.ReadInt("PGXP", "ppc") == 1;
        }

        public void LoadRom(string fn = "", RomList.Game game = null, bool FastBoot = false)
        {
            if (!File.Exists("./BIOS/" + currbios))
            {
                SimpleOSD.Show(this.romList, "Bios Not Found!");
                UpdateStatus(0, $"{Translations.GetText("FrmMain_LoadRom_nobios")} (Bios Not Found)", true);
                timer.Enabled = false;
                timer.Stop();
                return;
            }

            if (fn == "")
            {
                OpenFileDialog FD = new OpenFileDialog();
                FD.InitialDirectory = ini.Read("main", "LastPath");
                FD.Filter = "Games|*.bin;*.iso;*.cue;*.img;*.exe";
                if (FD.ShowDialog() == DialogResult.Cancel)
                {
                    return;
                }
                if (!File.Exists(FD.FileName))
                    return;

                fn = FD.FileName;
                gamename = Path.GetFileNameWithoutExtension(fn);
            }

            ini.Write("main", "LastPath", Path.GetDirectoryName(fn));

            if (Core != null)
            {
                Core.Stop();
            }

            IniFile bootini = null;
            IniFile sysini = ini;
            string gameid = "";
            if (game != null)
            {
                gamename = game.Name;
                gameid = game.ID;
                string inifn = $"./Save/{game.ID}.ini";
                if (File.Exists(inifn))
                {
                    bootini = new IniFile(inifn);
                    ini = bootini;
                }
            }

            currbios = ini.Read("main", "bios");

            GPUType gpumode = (GPUType)ini.ReadInt("main", "GpuMode");

            GPUType bootmode;

            IRscale = ini.ReadInt("main", "GpuModeScale");

            AutoIR = IRscale == 0;

            PGXP = ini.ReadInt("main", "PGXP") == 1;

            PGXPT = ini.ReadInt("main", "PGXPT") == 1;

            Realcolor = ini.ReadInt("main", "RealColor") == 1;

            KeepAR = ini.ReadInt("main", "KeepAR") == 1;

            romList.Dispose();

            if ((gpumode == GPUType.OpenGL || gpumode == GPUType.Advite) && Rendermode == RenderMode.OpenGL)
            {
                bootmode = GPUType.OpenGL;
                Render.DisposeCurrentRenderer();
                Render.SelectRenderer(RenderMode.Null, panel);
                while (NullRenderer.hwnd == 0) Thread.Sleep(10);
                GPUBackend.HWND = NullRenderer.hwnd;
                GPUBackend.HINST = NullRenderer.hinstance;
                GPUBackend.ClientHeight = NullRenderer.ClientHeight;
                GPUBackend.ClientWidth = NullRenderer.ClientWidth;
                GPUBackend.IRScale = IRscale;
            }
            else if ((gpumode == GPUType.Vulkan || gpumode == GPUType.Advite) && Rendermode == RenderMode.Vulkan)
            {
                bootmode = GPUType.Vulkan;
                Render.DisposeCurrentRenderer();
                Render.SelectRenderer(RenderMode.Null, panel);
                while (NullRenderer.hwnd == 0) Thread.Sleep(10);
                GPUBackend.HWND = NullRenderer.hwnd;
                GPUBackend.HINST = NullRenderer.hinstance;
                GPUBackend.ClientHeight = NullRenderer.ClientHeight;
                GPUBackend.ClientWidth = NullRenderer.ClientWidth;
                GPUBackend.IRScale = IRscale;
            }
            else
            {
                gpumode = GPUType.Software;
                bootmode = GPUType.Software;
                Render.SelectRenderer(Rendermode, panel);
            }

            Core = new PSXCore(this, this, this, fn, mypath + "/BIOS/" + currbios, bootmode, gameid);

            if (Core.DiskID == "")
            {
                Core = null;
                return;
            }

            //Core.PsxBus.cpu.FastBoot = ini.ReadInt("main", "FastBoot") == 1;

            Core.PsxBus.cpu.FastBoot = FastBoot;

            applypgxp();

            sysini.Write("history", Core.DiskID, $"{fn}|{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");

            SDLHanlder.SetAudioBuffer();

            if ((gpumode == GPUType.OpenGL || gpumode == GPUType.Advite) && Rendermode == RenderMode.OpenGL)
            {
                IRscale = IRscale < 1 ? 1 : IRscale;
                (Core.GPU as OpenglGPU).IRScale = IRscale;
                (Core.GPU as OpenglGPU).PGXP = PGXPVector.use_pgxp_highpos && PGXPVector.use_pgxp;
                (Core.GPU as OpenglGPU).PGXPT = PGXPT;
                (Core.GPU as OpenglGPU).KEEPAR = KeepAR;
                (Core.GPU as OpenglGPU).RealColor = Realcolor;
                GPUBackend.IRScale = IRscale;
            }
            else if ((gpumode == GPUType.Vulkan || gpumode == GPUType.Advite) && Rendermode == RenderMode.Vulkan)
            {
                IRscale = IRscale < 1 ? 1 : IRscale;
                (Core.GPU as VulkanGPU).IRScale = IRscale;
                (Core.GPU as VulkanGPU).PGXP = PGXPVector.use_pgxp_highpos && PGXPVector.use_pgxp;
                (Core.GPU as VulkanGPU).PGXPT = PGXPT;
                (Core.GPU as VulkanGPU).KEEPAR = KeepAR;
                (Core.GPU as VulkanGPU).RealColor = Realcolor;
                GPUBackend.IRScale = IRscale;
            }
            else
            {
                IRscale = 1;
            }

            if (ini.ReadInt("Main", "Console") == 1)
            {
                Core.PsxBus.cpu.biosdebug = ini.ReadInt("Main", "BiosDebug") == 1;
                Core.PsxBus.cpu.debug = ini.ReadInt("Main", "CPUDebug") == 1;
                Core.PsxBus.cpu.ttydebug = ini.ReadInt("Main", "TTYDebug") == 1;
            }

            Core.CdromSpeed(ini.ReadInt("Main", "CdSpeed"));

            Core.SYNC_CYCLES_IDLE = ini.ReadFloat("CPU", "FrameIdle");
            Core.SYNC_CPU_TICK = ini.ReadInt("CPU", "CpuTicks");
            Core.SYNC_CYCLES_BUS = ini.ReadInt("CPU", "BusCycles");
            Core.SYNC_CYCLES_FIX = ini.ReadInt("CPU", "CyclesFix");

            scale.scale = 0;
            scale.mode = (ScaleMode)ini.ReadInt("Main", "ScaleMode");

            CPU.SetExecution((ini.ReadInt("Main", "CpuMode") == 1));

            Core.Start();

            Core.PsxBus.controller1.IsAnalog = isAnalog;

            CheatCode.Enabled = true;

            if (sysini != null)
            {
                ini = sysini;
            }

            InitStateMnu();
        }

        private void SwapDisc()
        {
            if (Core == null)
            {
                return;
            }

            OpenFileDialog FD = new OpenFileDialog();
            FD.InitialDirectory = ini.Read("main", "LastPath");
            FD.Filter = "ISO|*.bin;*.iso;*.cue;*.img";
            FD.ShowDialog();
            if (!File.Exists(FD.FileName))
                return;

            ini.Write("main", "LastPath", Path.GetDirectoryName(FD.FileName));

            Core.Pauseing = true;
            while (!Core.Pauseed)
            {
                Core.Pauseing = true;
                Application.DoEvents();
                Thread.Sleep(10);
            }
            ;

            Core.SwapDisk(FD.FileName);

            Core.Pauseing = false;

            UpdateStatus(1, $"{Translations.GetText("FrmMain_SwapDisc")} {Core.DiskID}", true);
            StatusDelay = 3;
        }

        public void RenderFrame(int[] pixels, int width, int height)
        {
            CoreWidth = width;
            CoreHeight = height;

            if (cutblackline && Core.GPU.type == GPUType.Software)
            {
                CoreHeight = PixelsScaler.CutBlackLine(pixels, cutbuff, width, height);
                if (CoreHeight == 0)
                {
                    CoreHeight = height;
                }
                else
                {
                    //pixels 在GPU中仅用于输出
                    pixels = cutbuff;
                    height = CoreHeight;
                }
            }

            KeyFirst = SDLHanlder.QueryControllerState(1, Core, isAnalog, KeyFirst);
            KeyFirst = SDLHanlder.QueryControllerState(2, Core, isAnalog, KeyFirst);

            Render.RenderBuffer(pixels, width, height, scale);

            _frameCount++;
            var elapsedSeconds = _fpsStopwatch.Elapsed.TotalSeconds;
            if (elapsedSeconds >= 1.0)
            {
                _currentFps = (float)(_frameCount / elapsedSeconds);
                _frameCount = 0;
                _fpsStopwatch.Restart();
            }
        }

        public void ControllerRumble(byte VibrationRight, byte VibrationLeft)
        {
            if (!isAnalog) return;
            SDLHanlder.ControllerRumble(VibrationRight, VibrationLeft);
        }

        public void PlaySamples(byte[] samples)
        {
            SDLHanlder.SamplesBuffer.Write(samples);
        }
    }
}
