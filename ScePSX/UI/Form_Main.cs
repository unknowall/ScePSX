using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using static ScePSX.Controller;
using static SDL2.SDL;

namespace ScePSX
{

    public partial class FrmMain : Form, IAudioHandler, IRenderHandler
    {
        [DllImport("kernel32.dll")]
        public static extern Boolean AllocConsole();
        [DllImport("kernel32.dll")]
        public static extern Boolean FreeConsole();

        public static IniFile ini = new IniFile(Application.StartupPath + "ScePSX.ini");
        private string mypath;
        private string currbios;
        private string shaderpath;
        private int StateSlot;
        private int CoreWidth, CoreHeight;

        public static PSXCore Core;

        private nint controller1, controller2;
        private bool KeyFirst, isAnalog = false;
        private string temphint;
        private int hintdelay;

        private int _frameCount = 0;
        private Stopwatch _fpsStopwatch = Stopwatch.StartNew();
        private float _currentFps = 0;

        private static uint audiodeviceid;
        private SDL_AudioCallback audioCallbackDelegate;
        private byte[] ringBuffer;
        private int writePos = 0;
        private int readPos = 0;
        private int bufferCount = 0;
        private readonly object bufferLock = new object();

        private System.Windows.Forms.Timer timer;
        private Label lbHint;

        public int scale;
        private bool cutblackline = false;
        private int[] cutbuff = new int[1024 * 512];

        private OpenGLRenderer OGLRENDER = new OpenGLRenderer();
        private SDL2Renderer D3DRENDER = new SDL2Renderer();
        private D2DRenderer D2DRENDER = new D2DRenderer();

        private enum RenderMode
        {
            Directx2D,
            Directx3D,
            OpenGL
        }
        RenderMode Rendermode = RenderMode.OpenGL;

        public FrmMain()
        {
            InitializeComponent();

            OGLRENDER.Parent = this;
            OGLRENDER.Dock = DockStyle.Fill;
            Controls.Add(OGLRENDER);
            OGLRENDER.Enabled = false;
            OGLRENDER.MultisampleBits = 4;
            //OGLRENDER.DepthBits = 24;
            //OGLRENDER.ColorBits = 32;
            //OGLRENDER.Visible = false;

            D3DRENDER.Parent = this;
            D3DRENDER.Dock = DockStyle.Fill;
            Controls.Add(D3DRENDER);
            D3DRENDER.Enabled = false;
            //D3DRENDER.Visible = false;

            D2DRENDER.Parent = this;
            D2DRENDER.Dock = DockStyle.Fill;
            Controls.Add(D2DRENDER);
            D2DRENDER.Enabled = false;
            //D2DRENDER.Visible = false;

            //AllocConsole();

            //CheckForIllegalCrossThreadCalls = false;

            KeyDown += new KeyEventHandler(ButtonsDown);
            KeyUp += new KeyEventHandler(ButtonsUp);

            mypath = Application.StartupPath;

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

            Rendermode = (RenderMode)ini.ReadInt("Main", "Render");
            switch (Rendermode)
            {
                case RenderMode.Directx2D:
                    D2DRENDER.BringToFront();
                    directx2DRender.Checked = true;
                    break;
                case RenderMode.Directx3D:
                    D3DRENDER.BringToFront();
                    directx3DRender.Checked = true;
                    break;
                case RenderMode.OpenGL:
                    OGLRENDER.BringToFront();
                    openGLRender.Checked = true;
                    break;
            }

            frameskipmnu.Checked = ini.ReadInt("main", "frameskip") == 1;
            cutblackline = ini.ReadInt("main", "cutblackline") == 1;

            CutBlackLineMnu.Checked = cutblackline;

            KeyFirst = ini.ReadInt("main", "keyfirst") == 1;
            isAnalog = ini.ReadInt("main", "isAnalog") == 1;
            ToolStripMenuItem springItem = new ToolStripMenuItem();
            springItem.AutoSize = false;
            springItem.Width = 0;
            springItem.Enabled = false;
            lbHint = new Label();
            lbHint.Text = $"F9 [{(KeyFirst ? "键盘优先" : "手柄优先")}]  F10[{(isAnalog ? "多轴手柄" : "数字手柄")}]";
            lbHint.Font = new Font("微软雅黑", 11f, FontStyle.Bold);
            lbHint.AutoSize = true;
            lbHint.Padding = new Padding(0, 2, 10, 0);

            ToolStripControlHost host = new ToolStripControlHost(lbHint);
            host.Alignment = ToolStripItemAlignment.Right;

            MainMenu.Items.Add(springItem);
            MainMenu.Items.Add(host);


            SDLInit();

            FrmInput.InitKeyMap();
            FrmInput.InitControllerMap();

            InitBiosMnu();
            InitShaderMnu();

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Elapsed;
            timer.Enabled = true;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, EventArgs e)
        {
            lbHint.Text = $"{temphint} F9 [{(KeyFirst ? "键盘优先" : "手柄优先")}]  F10[{(isAnalog ? "多轴手柄" : "数字手柄")}]";

            if (hintdelay > 0)
            {
                hintdelay--;
            } else
            {
                temphint = "";
            }

            if (Core == null)
                return;

            int scalew = CoreWidth;
            int scaleh = CoreHeight;

            if (scale > 0)
            {
                scalew *= scale;
                scaleh *= scale;
            }
            this.Text = $"ScePSX | {Core.DiskID} | SaveSlot {StateSlot} | {Rendermode.ToString()} | {scalew}*{scaleh} | FPS {_currentFps:F1}";
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

            ringBuffer = new byte[52920]; // 300 ms

            audiodeviceid = SDL_OpenAudioDevice(null, 0, ref desired, out obtained, 0);
            if (audiodeviceid != 0)
                SDL_PauseAudioDevice(audiodeviceid, 0);

            //Task _Task = Task.Factory.StartNew(QueryControllerStateTask, TaskCreationOptions.LongRunning);
        }

        #region MENU
        private void InitBiosMnu()
        {
            currbios = ini.Read("main", "bios");
            if (currbios == null)
                currbios = "SCPH1001.BIN";
            DirectoryInfo dir = new DirectoryInfo(mypath + "/BIOS");
            MnuFile.Enabled = false;
            MnuBios.Enabled = false;
            if (dir.Exists)
            {
                foreach (FileInfo f in dir.GetFiles())
                {
                    ToolStripMenuItem mnu = new ToolStripMenuItem();
                    mnu.Name = f.Name;
                    mnu.Text = f.Name;
                    mnu.Tag = 10;
                    mnu.CheckOnClick = true;
                    mnu.Click += Mnu_Click;
                    mnu.CheckedChanged += Mnu_CheckedChanged;
                    MnuBios.DropDownItems.Add(mnu);
                    if (currbios == f.Name)
                    {
                        mnu.Checked = true;
                        currbios = mnu.Text;
                    }
                }
                MnuFile.Enabled = true;
                MnuBios.Enabled = true;
            }
        }

        private void InitShaderMnu()
        {
            shaderpath = ini.Read("main", "shader");
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
                case 10:
                    currbios = mnu.Text;
                    ini.Write("main", "bios", currbios);
                    break;
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

        private void directx2DRender_Click(object sender, EventArgs e)
        {
            directx3DRender.Checked = false;
            openGLRender.Checked = false;

            OGLRENDER.Visible = false;
            D3DRENDER.Visible = false;
            D2DRENDER.Visible = true;

            D2DRENDER.BringToFront();

            Rendermode = RenderMode.Directx2D;
            ini.WriteInt("Main", "Render", (int)Rendermode);
        }

        private void directx3DToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openGLRender.Checked = false;
            directx2DRender.Checked = false;

            OGLRENDER.Visible = false;
            D2DRENDER.Visible = false;
            D3DRENDER.Visible = true;

            D3DRENDER.BringToFront();

            Rendermode = RenderMode.Directx3D;
            ini.WriteInt("Main", "Render", (int)Rendermode);
        }

        private void openGLToolStripMenuItem_Click(object sender, EventArgs e)
        {

            directx2DRender.Checked = false;
            directx3DRender.Checked = false;

            if (OGLRENDER.ShadreName == "")
            {
                OGLRENDER.LoadShaders("./Shaders/" + shaderpath);
            }

            D3DRENDER.Visible = false;
            D2DRENDER.Visible = false;
            OGLRENDER.Visible = true;

            OGLRENDER.BringToFront();

            Rendermode = RenderMode.OpenGL;
            ini.WriteInt("Main", "Render", (int)Rendermode);
        }

        private void frameskipmnu_CheckedChanged(object sender, EventArgs e)
        {
            if (frameskipmnu.Checked == false)
            {
                D2DRENDER.frameskip = 0;
                D3DRENDER.frameskip = 0;
            } else
            {
                D2DRENDER.frameskip = 1;
                D3DRENDER.frameskip = 1;
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

        private void CheatCode_Click(object sender, EventArgs e)
        {
            var frmcheat = new Form_Cheat();
            frmcheat.Show(this);
        }

        private void MnuDebug_Click(object sender, EventArgs e)
        {
            var frmmem = new Form_Mem();
            frmmem.Show(this);
        }

        private void KeyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frminput = new FrmInput();
            frminput.Show(this);
        }
        #endregion

        private void SaveState(int Slot = 0)
        {
            if (Core != null && Core.Running)
            {
                Core.SaveState(Slot.ToString());
                temphint = $"已保存到槽位 [{Slot}]";
                hintdelay = 3;
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
                LoadState();
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
                    Console.WriteLine($"Analog Controller Mode: {Core.PsxBus.controller1.IsAnalog}");
                }
                return;
            }
            if (e.KeyCode == Keys.Tab && Core != null)
            {
                Core.MIPS_UNDERCLOCK = 5;
                Core.SYNC_CYCLES_IDLE = 0;
                Core.SYNC_LOOPS = (PSXCore.CYCLES_PER_FRAME / (PSXCore.SYNC_CYCLES * Core.MIPS_UNDERCLOCK)) + 1;
            }
            if (e.KeyCode == Keys.F11)
            {
                if (Core != null && Core.Running)
                    if (scale < 8)
                        scale += 2;
                return;
            }
            if (e.KeyCode == Keys.F12)
            {
                if (Core != null && Core.Running)
                    if (scale > 0)
                        scale -= 2;
                return;
            }

            InputAction button = FrmInput.KMM.GetKeyButton(e.KeyCode);
            if ((int)button != 0xFF && Core != null && Core.Running)
                Core.Button(button, true);
        }

        private void ButtonsUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab && Core != null)
            {
                Core.MIPS_UNDERCLOCK = 1;
                Core.SYNC_CYCLES_IDLE = 15;
                Core.SYNC_LOOPS = (PSXCore.CYCLES_PER_FRAME / (PSXCore.SYNC_CYCLES)) + 1;
            }

            InputAction button = FrmInput.KMM.GetKeyButton(e.KeyCode);
            if ((int)button != 0xFF && Core != null && Core.Running)
                Core.Button(button, false);
        }

        private void LoadRom()
        {
            if (!File.Exists("./BIOS/" + currbios))
                return;

            OpenFileDialog FD = new OpenFileDialog();
            FD.InitialDirectory = ini.Read("main", "LastPath");
            FD.Filter = "ISO|*.bin;*.iso;*.cue;*.img";
            FD.ShowDialog();
            if (!File.Exists(FD.FileName))
                return;

            ini.Write("main", "LastPath", Path.GetDirectoryName(FD.FileName));

            if (Core != null)
            {
                Core.Stop();
                scale = 0;
            }

            if (Rendermode == RenderMode.OpenGL)
            {
                OGLRENDER.LoadShaders("./Shaders/" + shaderpath);
            }

            Core = new PSXCore(this, this, FD.FileName, Application.StartupPath + "/BIOS/" + currbios);
            if (Core.DiskID == "")
            {
                Core = null;
                return;
            } else
            {
                Core.Start();
            }

            Core.PsxBus.controller1.IsAnalog = isAnalog;

            temphint = $"已启动 [{Core.DiskID}]";
            hintdelay = 3;

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

            temphint = $"已换盘 {Core.DiskID}";
            hintdelay = 3;
        }

        private void AudioCallbackImpl(IntPtr userdata, IntPtr stream, int len)
        {
            lock (bufferLock)
            {
                int available = bufferCount;
                if (available > 0)
                {
                    int toCopy = Math.Min(len, available);

                    if (readPos + toCopy <= ringBuffer.Length)
                    {
                        Marshal.Copy(ringBuffer, readPos, stream, toCopy);
                    } else
                    {
                        int firstPart = ringBuffer.Length - readPos;
                        Marshal.Copy(ringBuffer, readPos, stream, firstPart);
                        Marshal.Copy(ringBuffer, 0, stream + firstPart, toCopy - firstPart);
                    }

                    readPos = (readPos + toCopy) % ringBuffer.Length;
                    bufferCount -= toCopy;
                } else
                {
                    for (int i = 0; i < len; i++)
                    {
                        Marshal.WriteByte(stream, i, 0);
                    }
                }
            }
        }

        public void AddSamples(byte[] samples)
        {
            lock (bufferLock)
            {
                foreach (byte sample in samples)
                {
                    if (bufferCount < ringBuffer.Length)
                    {
                        ringBuffer[writePos] = sample;
                        writePos = (writePos + 1) % ringBuffer.Length;
                        bufferCount++;
                    } else
                    {
                        ringBuffer[writePos] = sample;
                        writePos = (writePos + 1) % ringBuffer.Length;
                        readPos = (readPos + 1) % ringBuffer.Length;
                    }
                }
            }
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

            QueryControllerState();

            if (Rendermode == RenderMode.OpenGL)
            {
                OGLRENDER.RenderBuffer(pixels, width, height, scale);
            } else if (Rendermode == RenderMode.Directx3D)
            {
                D3DRENDER.RenderBuffer(pixels, width, height, scale);
            } else if (Rendermode == RenderMode.Directx2D)
            {
                D2DRENDER.RenderBuffer(pixels, width, height, scale);
            }

            _frameCount++;
            var elapsedSeconds = _fpsStopwatch.Elapsed.TotalSeconds;
            if (elapsedSeconds >= 1.0)
            {
                _currentFps = (float)(_frameCount / elapsedSeconds);
                _frameCount = 0;
                _fpsStopwatch.Restart();
            }
        }

        public void PlaySamples(byte[] samples)
        {
            AddSamples(samples);
        }

        #region SDLController

        //private void QueryControllerStateTask()
        //{
        //    while (true)
        //    {
        //        QueryControllerState();

        //        Thread.Sleep(15);
        //    }
        //}

        private void QueryControllerState()
        {
            if (controller1 == 0)
            {
                if (SDL_NumJoysticks() == 0)
                {
                    return;
                }

                if (SDL_IsGameController(0) == SDL_bool.SDL_TRUE)
                {
                    controller1 = SDL_GameControllerOpen(0);
                } else
                {
                    controller1 = SDL_JoystickOpen(0);
                }

                if (SDL_IsGameController(1) == SDL_bool.SDL_TRUE)
                {
                    controller2 = SDL_GameControllerOpen(1);
                } else
                {
                    controller2 = SDL_JoystickOpen(1);
                }

                Console.WriteLine($"Controller Device {SDL_NumJoysticks()} : {SDL_JoystickNameForIndex(0)} Connected");
            }

            if (Core == null || KeyFirst)
                return;

            //Button
            bool isPadPressed = false;
            foreach (SDL_GameControllerButton button in Enum.GetValues(typeof(SDL_GameControllerButton)))
            {
                bool isPressed = SDL_GameControllerGetButton(controller1, button) == 1;

                if (!isAnalog && isPressed)
                {
                    if (isPressed && (int)button >= 11 && (int)button <= 15)
                    {
                        isPadPressed = true;
                    }
                }
                if (FrmInput.AnalogMap.TryGetValue(button, out var gamepadInput))
                {
                    Core.Button(gamepadInput, isPressed);
                }
            }

            //AnalogAxis
            float lx = 0.0f, ly = 0.0f, rx = 0.0f, ry = 0.0f;

            short leftX = SDL_GameControllerGetAxis(controller1, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX);
            short leftY = SDL_GameControllerGetAxis(controller1, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY);
            short rightX = SDL_GameControllerGetAxis(controller1, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX);
            short rightY = SDL_GameControllerGetAxis(controller1, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY);

            lx = NormalizeAxis(leftX);
            ly = NormalizeAxis(leftY);
            rx = NormalizeAxis(rightX);
            ry = NormalizeAxis(rightY);

            //TRIGGER
            short tl = SDL_GameControllerGetAxis(controller1, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT);
            short tr = SDL_GameControllerGetAxis(controller1, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT);

            Core.Button(InputAction.L2, tl > 16384 ? true : false);
            Core.Button(InputAction.R2, tr > 16384 ? true : false);

            Core.AnalogAxis(lx, ly, rx, ry);

            if (isPadPressed)
                return;

            //Hat
            int hatIndex = 0;
            int hatState = 0;
            IntPtr joystick = SDL_GameControllerGetJoystick(controller1);
            if (joystick != IntPtr.Zero)
            {
                hatState = SDL_JoystickGetHat(joystick, hatIndex);

                Core.Button(InputAction.DPadUp, (hatState & SDL_HAT_UP) != 0);
                Core.Button(InputAction.DPadDown, (hatState & SDL_HAT_DOWN) != 0);
                Core.Button(InputAction.DPadLeft, (hatState & SDL_HAT_LEFT) != 0);
                Core.Button(InputAction.DPadRight, (hatState & SDL_HAT_RIGHT) != 0);
            }

            if (!isAnalog && hatState == 0)
            {
                // 将左摇杆的值转换为方向键状态
                Core.Button(InputAction.DPadUp, ly < -0.5f);    // 上
                Core.Button(InputAction.DPadDown, ly > 0.5f);   // 下
                Core.Button(InputAction.DPadLeft, lx < -0.5f);  // 左
                Core.Button(InputAction.DPadRight, lx > 0.5f);  // 右
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

    #region INIFILE
    public class IniFile
    {
        private readonly string path;
        private readonly Dictionary<string, Dictionary<string, string>> data;

        public IniFile(string inipath)
        {
            path = inipath;
            data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            Load();
        }

        private void Load()
        {
            if (!File.Exists(path))
                return;

            string currentSection = null;
            foreach (var line in File.ReadAllLines(path))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
                    continue;

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2).Trim();
                    if (!data.ContainsKey(currentSection))
                    {
                        data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                } else if (currentSection != null)
                {
                    var parts = trimmedLine.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        data[currentSection][key] = value;
                    }
                }
            }
        }

        private void Save()
        {
            var lines = new List<string>();
            foreach (var section in data)
            {
                lines.Add($"[{section.Key}]");
                foreach (var entry in section.Value)
                {
                    lines.Add($"{entry.Key}={entry.Value}");
                }
                lines.Add(""); // 空行分隔节
            }
            File.WriteAllLines(path, lines);
        }

        public void Write(string section, string key, string value)
        {
            if (!data.ContainsKey(section))
            {
                data[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            data[section][key] = value;
            Save();
        }

        public string Read(string section, string key)
        {
            if (data.ContainsKey(section) && data[section].ContainsKey(key))
            {
                return data[section][key];
            }
            return "";
        }

        public void WriteInt(string section, string key, int value)
        {
            Write(section, key, value.ToString());
        }

        public int ReadInt(string section, string key)
        {
            var str = Read(section, key);
            return string.IsNullOrEmpty(str) ? 0 : Convert.ToInt32(str);
        }
    }
    #endregion
}
