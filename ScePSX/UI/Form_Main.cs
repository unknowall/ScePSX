using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using ScePSX.Render;
using static ScePSX.Controller;
using static SDL2.SDL;

namespace ScePSX.UI
{

    public partial class FrmMain : Form, IAudioHandler, IRenderHandler
    {
        [DllImport("kernel32.dll")]
        public static extern Boolean AllocConsole();
        [DllImport("kernel32.dll")]
        public static extern Boolean FreeConsole();

        public static string version = "ScePSX Beta 0.1.0";

        private static string mypath = Application.StartupPath;
        public static IniFile ini = new IniFile(mypath + "ScePSX.ini");
        private string currbios;
        private string shaderpath;
        private int StatusDelay;
        private string gamename;

        private int StateSlot;
        private int CoreWidth, CoreHeight;

        public static PSXCore Core;

        private nint controller1, controller2;
        private bool KeyFirst, isAnalog = false;
        private int concount = 0;

        private int _frameCount = 0;
        private Stopwatch _fpsStopwatch = Stopwatch.StartNew();
        private float _currentFps = 0;

        private static uint audiodeviceid;
        private SDL_AudioCallback audioCallbackDelegate;
        private CircularBuffer<byte> SamplesBuffer;

        private System.Windows.Forms.Timer timer;

        public int scale;
        private bool cutblackline = false;
        private int[] cutbuff = new int[1024 * 512];

        private RomList romList;

        RenderMode Rendermode = RenderMode.OpenGL;
        RendererManager Render = new RendererManager();

        public FrmMain()
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            InitializeComponent();

            //MainMenu.RenderMode = ToolStripRenderMode.Professional;
            //MainMenu.Renderer = new MainMenuRenderer();
            //MainMenu.BackColor = Color.FromArgb(45, 45, 45);

            //AddCustomTitleBar();
            //AddStatusBar();
            //AddStatusLabel("");
            //AddStatusLabel("");
            //AddStatusLabel("");
            //AddStatusLabel("");
            //AddStatusSpring();
            //AddStatusLabel("", true);
            //AddStatusLabel("", true);
            //AddStatusLabel("", true);

            if (ini.ReadInt("Main", "Console") == 1)
            {
                AllocConsole();
            }

            //CheckForIllegalCrossThreadCalls = false;

            KeyDown += new KeyEventHandler(ButtonsDown);
            KeyUp += new KeyEventHandler(ButtonsUp);

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

            CloseRomMnu_Click(null, null);

            shaderpath = ini.Read("main", "shader");
            Rendermode = (RenderMode)ini.ReadInt("Main", "Render");

            openGLRender.Checked = Rendermode == RenderMode.OpenGL;
            directx2DRender.Checked = Rendermode == RenderMode.Directx2D;
            directx3DRender.Checked = Rendermode == RenderMode.Directx3D;

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

            FrmInput.InitKeyMap();
            FrmInput.InitControllerMap();

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Elapsed;
            timer.Enabled = true;
            timer.Start();

            currbios = ini.Read("main", "bios");
            if (currbios == null)
                currbios = "SCPH1001.BIN";

            SDLInit();

            InitShaderMnu();
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

        private void Timer_Elapsed(object sender, EventArgs e)
        {
            if (Core == null)
            {
                this.Text = version;
                return;
            }

            if (StatusDelay > 0)
            {
                StatusDelay--;
            } else
            {
                UpdateStatus(1, $"F3+ F4- {ScePSX.Properties.Resources.FrmMain_Timer_Elapsed_存档槽} [{StateSlot}]");
                UpdateStatus(2, $"F9 [{(KeyFirst ? ScePSX.Properties.Resources.FrmMain_Timer_Elapsed_键盘优先 : ScePSX.Properties.Resources.FrmMain_Timer_Elapsed_手柄优先)}]");
                UpdateStatus(3, $"F10[{(isAnalog ? ScePSX.Properties.Resources.FrmMain_Timer_Elapsed_多轴手柄 : ScePSX.Properties.Resources.FrmMain_Timer_Elapsed_数字手柄)}]");
            }

            if (Core.Pauseed)
            {
                UpdateStatus(1, ScePSX.Properties.Resources.FrmMain_Timer_Elapsed_暂停中, true);
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
            if (scale > 0)
            {
                scalew *= scale;
                scaleh *= scale;
            }
            UpdateStatus(0, Core.DiskID);
            UpdateStatus(5, rendername);
            UpdateStatus(6, $"IR {scalew}*{scaleh}");
            UpdateStatus(7, $"FPS {_currentFps:F1}");
        }

        ~FrmMain()
        {
            if (Core != null)
                Core.Stop();
            if (audiodeviceid != 0)
                SDL_CloseAudioDevice(audiodeviceid);
            if (controller1 != 0)
            {
                SDL_GameControllerClose(controller1);
                SDL_JoystickClose(controller1);
            }
            SDL_Quit();
        }

        private void SDLInit()
        {
            SDL_Init(SDL_INIT_AUDIO | SDL_INIT_GAMECONTROLLER);

            audioCallbackDelegate = AudioCallbackImpl;

            SDL_AudioSpec desired = new SDL_AudioSpec
            {
                channels = 2,
                format = AUDIO_S16LSB,
                freq = 44100,
                samples = 2048,
                callback = audioCallbackDelegate,
                userdata = IntPtr.Zero

            };
            SDL_AudioSpec obtained = new SDL_AudioSpec();

            SetAudioBuffer();

            audiodeviceid = SDL_OpenAudioDevice(null, 0, ref desired, out obtained, 0);
            if (audiodeviceid != 0)
                SDL_PauseAudioDevice(audiodeviceid, 0);
        }

        private void SetAudioBuffer()
        {
            if (audiodeviceid != 0)
                SDL_PauseAudioDevice(audiodeviceid, 1);

            int bufms = ini.ReadInt("Audio", "Buffer");
            if (bufms < 50)
                bufms = 50;

            int alignedSize = ((bufms * 176 + 2048 - 1) / 2048) * 2048;

            SamplesBuffer = new CircularBuffer<byte>(alignedSize); // 300 ms = 52920

            if (audiodeviceid != 0)
                SDL_PauseAudioDevice(audiodeviceid, 0);
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
            SaveStripMenuItem.DropDownItems.Clear();
            LoadStripMenuItem.DropDownItems.Clear();

            SaveStripMenuItem.Enabled = true;
            LoadStripMenuItem.Enabled = true;
            UnLoadStripMenuItem.Enabled = true;

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
                } else
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
                SaveStripMenuItem.DropDownItems.Add(mnusave);

                mnuload.Name = statefile;
                mnuload.Text = statename;
                mnuload.Tag = 40 + i;
                mnuload.Click += Mnu_Click;
                LoadStripMenuItem.DropDownItems.Add(mnuload);
            }
        }

        private void AddMenu(string name, string Text, object tag, ToolStripMenuItem Parent, bool chk = false, bool ch = true)
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
                foreach (ToolStripMenuItem item in LoadStripMenuItem.DropDownItems)
                {
                    if ((int)item.Tag == 40 + StateSlot)
                    {
                        item.Text = statename;
                        break;
                    }
                }
                return;
            }
            if ((int)mnu.Tag >= 40 && (int)mnu.Tag < 50)
            {
                StateSlot = (int)mnu.Tag - 40;
                LoadState(StateSlot);

                return;
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
                OpenFileDialog FD = new OpenFileDialog();
                FD.InitialDirectory = ini.Read("main", "LastPath");
                FD.ValidateNames = false;
                FD.CheckFileExists = false;
                FD.CheckPathExists = true;
                FD.FileName = "select";
                if (FD.ShowDialog() == DialogResult.Cancel)
                {
                    return;
                }
                if (FD.FileName != "")
                    romList.SearchDir(Path.GetDirectoryName(FD.FileName));
            }
        }

        private void CloseRomMnu_Click(object sender, EventArgs e)
        {
            if (Core != null)
            {
                Core.Stop();
                Core = null;
            }

            romList = new RomList();
            romList.Parent = this;
            romList.Dock = DockStyle.Fill;
            Controls.Add(romList);
            romList.Enabled = true;
            romList.Visible = true;
            romList.BorderStyle = BorderStyle.FixedSingle;
            romList.DoubleClick += new EventHandler(romList_DoubleClick);
            romList.FillByini();
            romList.BringToFront();

            UpdateStatus(0, "", true);
        }

        private void directx2DRender_Click(object sender, EventArgs e)
        {
            directx3DRender.Checked = false;
            openGLRender.Checked = false;

            Rendermode = RenderMode.Directx2D;
            ini.WriteInt("Main", "Render", (int)Rendermode);

            if (Core != null && Core.Running)
                Render.SelectRenderer(Rendermode, this);
        }

        private void directx3DToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openGLRender.Checked = false;
            directx2DRender.Checked = false;

            Rendermode = RenderMode.Directx3D;
            ini.WriteInt("Main", "Render", (int)Rendermode);

            if (Core != null && Core.Running)
                Render.SelectRenderer(Rendermode, this);
        }

        private void openGLToolStripMenuItem_Click(object sender, EventArgs e)
        {

            directx2DRender.Checked = false;
            directx3DRender.Checked = false;

            Render.oglShaderPath = "./Shaders/" + shaderpath;

            Rendermode = RenderMode.OpenGL;
            ini.WriteInt("Main", "Render", (int)Rendermode);

            if (Core != null && Core.Running)
                Render.SelectRenderer(Rendermode, this);
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

        private void xBRScaleAdd_Click(object sender, EventArgs e)
        {
            if (Core != null && Core.Running)
                if (scale < 8)
                    scale += 2;
        }

        private void xBRScaleDec_Click(object sender, EventArgs e)
        {
            if (Core != null && Core.Running)
                if (scale > 0)
                    scale -= 2;
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
                UpdateStatus(1, $"{ScePSX.Properties.Resources.FrmMain_SaveState_saved} [{StateSlot}]", true);
                StatusDelay = 3;
            }
        }

        private void LoadState(int Slot = 0)
        {
            if (Core != null && Core.Running)
            {
                Core.SaveState("~");
                Core.LoadState(Slot.ToString());
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
                    Core.Pause();
                return;
            }
            if (e.KeyCode == Keys.F3)
            {
                StateSlot = StateSlot < 9 ? StateSlot + 1 : StateSlot;
                return;
            }
            if (e.KeyCode == Keys.F4)
            {
                StateSlot = StateSlot > 0 ? StateSlot - 1 : StateSlot;
                return;
            }
            if (e.KeyCode == Keys.F5)
            {
                foreach (ToolStripMenuItem item in SaveStripMenuItem.DropDownItems)
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
                return;
            }
            if (e.KeyCode == Keys.Tab && Core != null)
            {
                Core.Boost = true;
                return;
            }
            if (e.KeyCode == Keys.F11)
            {
                if (Core != null && Core.Running)
                    if (scale < 8)
                        scale = scale ==0 ? 2 : scale * 2;
                return;
            }
            if (e.KeyCode == Keys.F12)
            {
                if (Core != null && Core.Running)
                    if (scale > 0)
                        scale /= 2;
                return;
            }

            InputAction button = FrmInput.KMM1.GetKeyButton(e.KeyCode);
            if ((int)button != 0xFF && Core != null && Core.Running)
                Core.Button(button, true, 0);

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

        private void LoadRom(string fn = "", RomList.Game game = null)
        {
            if (!File.Exists("./BIOS/" + currbios))
            {
                UpdateStatus(0, $"{ScePSX.Properties.Resources.FrmMain_LoadRom_nobios} (Bios Not Found)", true);
                timer.Enabled = false;
                timer.Stop();
                return;
            }

            if (fn == "")
            {
                OpenFileDialog FD = new OpenFileDialog();
                FD.InitialDirectory = ini.Read("main", "LastPath");
                FD.Filter = "ISO|*.bin;*.iso;*.cue;*.img";
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
                scale = 0;
            }

            IniFile bootini = null;
            IniFile sysini = null;
            string gameid = "";
            if (game != null)
            {
                gamename = game.Name;
                gameid = game.ID;
                string inifn = $"./Save/{game.ID}.ini";
                if (File.Exists(inifn))
                {
                    bootini = new IniFile(inifn);
                    sysini = ini;

                    ini = bootini;
                }
            }

            currbios = ini.Read("main", "bios");

            Core = new PSXCore(this, this, fn, mypath + "/BIOS/" + currbios, gameid);

            if (Core.DiskID == "")
            {
                Core = null;
                return;
            }

            if (sysini != null)
                sysini.Write("history", Core.DiskID, $"{fn}|{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            else
                ini.Write("history", Core.DiskID, $"{fn}|{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");

            SetAudioBuffer();

            if (ini.ReadInt("Main", "Console") == 1)
            {
                Core.PsxBus.cpu.biosdebug = ini.ReadInt("Main", "BiosDebug") == 1;
                Core.PsxBus.cpu.debug = ini.ReadInt("Main", "CPUDebug") == 1;
                Core.PsxBus.cpu.ttydebug = ini.ReadInt("Main", "TTYDebug") == 1;
            }

            Core.SYNC_CYCLES_IDLE = ini.ReadInt("CPU", "FrameIdle");
            Core.SYNC_CPU_TICK = ini.ReadInt("CPU", "CpuTicks");
            Core.SYNC_CYCLES_BUS = ini.ReadInt("CPU", "BusCycles");
            Core.SYNC_CYCLES_FIX = ini.ReadInt("CPU", "CyclesFix");

            romList.Dispose();

            Render.SelectRenderer(Rendermode, this);

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

            Core.PsxBus.SwapDisk(FD.FileName);

            Core.Pauseing = false;

            UpdateStatus(1, $"{ScePSX.Properties.Resources.FrmMain_SwapDisc_更换光盘} {Core.DiskID}", true);
            StatusDelay = 3;
        }

        public void RenderFrame(int[] pixels, int width, int height)
        {
            CoreWidth = width;
            CoreHeight = height;

            if (cutblackline)
            {
                CoreHeight = XbrScaler.CutBlackLine(pixels, cutbuff, width, height);
                if (CoreHeight == 0)
                {
                    CoreHeight = height;
                } else
                {
                    //pixels 在GPU中仅用于输出
                    pixels = cutbuff;
                    height = CoreHeight;
                }
            }

            QueryControllerState(1);
            QueryControllerState(2);

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

        private unsafe void AudioCallbackImpl(IntPtr userdata, IntPtr stream, int len)
        {
            byte[] tempBuffer = new byte[len];

            int bytesRead = SamplesBuffer.Read(tempBuffer, 0, len);

            fixed (byte* ptr = tempBuffer)
            {
                Buffer.MemoryCopy(ptr, (void*)stream, len, bytesRead);
            }

            if (bytesRead < len)
            {
                new Span<byte>((void*)(stream + bytesRead), len - bytesRead).Fill(0);
            }
        }

        public void PlaySamples(byte[] samples)
        {
            SamplesBuffer.Write(samples);
        }

        #region SDLController

        private void QueryControllerState(int conidx)
        {
            nint controller;

            if (conidx == 1)
            {
                controller = controller1;
            } else if (conidx == 2)
            {
                controller = controller2;
            } else
            {
                return;
            }

            if (controller == 0)
            {
                concount = SDL_NumJoysticks();

                if (concount < conidx)
                {
                    return;
                }

                if (controller1 == 0 && concount >= 1)
                {
                    if (SDL_IsGameController(0) == SDL_bool.SDL_TRUE)
                    {
                        controller1 = SDL_GameControllerOpen(0);
                    } else
                    {
                        controller1 = SDL_JoystickOpen(0);
                    }
                    Console.WriteLine($"Controller Device 1 : {SDL_JoystickNameForIndex(0)} Connected");
                }

                if (controller2 == 0 && concount >= 2)
                {
                    if (SDL_IsGameController(1) == SDL_bool.SDL_TRUE)
                    {
                        controller2 = SDL_GameControllerOpen(1);
                    } else
                    {
                        controller2 = SDL_JoystickOpen(1);
                    }
                    Console.WriteLine($"Controller Device 2 : {SDL_JoystickNameForIndex(1)} Connected");
                }

            }

            if (Core == null || KeyFirst)
                return;

            conidx--;

            //Button
            bool isPadPressed = false;
            foreach (SDL_GameControllerButton button in Enum.GetValues(typeof(SDL_GameControllerButton)))
            {
                bool isPressed = SDL_GameControllerGetButton(controller, button) == 1;

                if (!isAnalog && isPressed)
                {
                    if (isPressed && (int)button >= 11 && (int)button <= 15)
                    {
                        isPadPressed = true;
                    }
                }
                if (FrmInput.AnalogMap.TryGetValue(button, out var gamepadInput))
                {
                    Core.Button(gamepadInput, isPressed, conidx);
                }
            }

            //AnalogAxis
            float lx = 0.0f, ly = 0.0f, rx = 0.0f, ry = 0.0f;

            short leftX = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX);
            short leftY = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY);
            short rightX = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX);
            short rightY = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY);

            lx = NormalizeAxis(leftX);
            ly = NormalizeAxis(leftY);
            rx = NormalizeAxis(rightX);
            ry = NormalizeAxis(rightY);

            //TRIGGER
            short tl = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT);
            short tr = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT);

            Core.Button(InputAction.L2, tl > 16384 ? true : false, conidx);
            Core.Button(InputAction.R2, tr > 16384 ? true : false, conidx);

            Core.AnalogAxis(lx, ly, rx, ry, conidx);

            if (isPadPressed)
                return;

            //Hat
            int hatIndex = 0;
            int hatState = 0;
            IntPtr joystick = SDL_GameControllerGetJoystick(controller);
            if (joystick != IntPtr.Zero)
            {
                hatState = SDL_JoystickGetHat(joystick, hatIndex);

                Core.Button(InputAction.DPadUp, (hatState & SDL_HAT_UP) != 0, conidx);
                Core.Button(InputAction.DPadDown, (hatState & SDL_HAT_DOWN) != 0, conidx);
                Core.Button(InputAction.DPadLeft, (hatState & SDL_HAT_LEFT) != 0, conidx);
                Core.Button(InputAction.DPadRight, (hatState & SDL_HAT_RIGHT) != 0, conidx);
            }

            if (!isAnalog && hatState == 0)
            {
                // 将左摇杆的值转换为方向键状态
                Core.Button(InputAction.DPadUp, ly < -0.5f, conidx);    // 上
                Core.Button(InputAction.DPadDown, ly > 0.5f, conidx);   // 下
                Core.Button(InputAction.DPadLeft, lx < -0.5f, conidx);  // 左
                Core.Button(InputAction.DPadRight, lx > 0.5f, conidx);  // 右
            }

        }

        private float NormalizeAxis(short value)
        {
            float ret = Math.Clamp(value / 32767.0f, -1.0f, 1.0f);
            if (Math.Abs(ret) < 0.1f)
            {
                ret = 0.0f;
            }
            return ret;
        }

        #endregion

    }

}
