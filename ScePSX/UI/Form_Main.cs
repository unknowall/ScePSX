using System;
using System.Diagnostics;
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

        private bool SdlQuit = false;
        public static nint joystickid;

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

        private System.Timers.Timer timer;

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

            CheckForIllegalCrossThreadCalls = false;

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
                    //D3DRENDER.BringToFront();
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


            SDLInit();

            FrmInput.InitKeyMap();
            FrmInput.InitControllerMap();

            InitBiosMnu();
            InitShaderMnu();

            timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
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
            if (joystickid != 0)
            {
                SDL_GameControllerClose(joystickid);
                SDL_JoystickClose(joystickid);
            }
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

            Task _Task = Task.Factory.StartNew(JOYSTICKHANDLER, TaskCreationOptions.LongRunning);
        }

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

        private void SaveState(int Slot = 0)
        {
            if (Core != null && Core.Running)
                Core.SaveState(Slot.ToString());
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
                AllocConsole();
            }
            if (e.KeyCode == Keys.F10)
            {
                Core.PsxBus.controller1.IsAnalog = !Core.PsxBus.controller1.IsAnalog;
                Console.WriteLine($"Analog Controller Mode: {Core.PsxBus.controller1.IsAnalog}");
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
            }
            if (e.KeyCode == Keys.F12)
            {
                if (Core != null && Core.Running)
                    if (scale > 0)
                        scale -= 2;
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
                CoreHeight = XbrScaler.CutBlackLine(pixels,cutbuff,width,height);
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
        private void HandleButtonEvent(SDL_GameControllerButton button, bool isDown)
        {
            if (Core == null)
                return;

            if ((int)button == 10 || (int)button == 15 || (int)button == 18)
            {
                Core.PsxBus.controller1.IsAnalog = !Core.PsxBus.controller1.IsAnalog;
            }

            if (FrmInput.AnalogMap.TryGetValue(button, out var gamepadInput))
            {
                if (isDown)
                {
                    Core.Button(gamepadInput, true);
                } else
                {
                    Core.Button(gamepadInput);
                }
            }

            //GetAxis();
        }

        private void HandleHatEvent(int hatIndex, int hatValue)
        {
            // 清除之前的 D-Pad 状态
            Core.Button(InputAction.DPadUp);
            Core.Button(InputAction.DPadDown);
            Core.Button(InputAction.DPadLeft);
            Core.Button(InputAction.DPadRight);

            // 根据 Hat 值设置新的 D-Pad 状态
            if ((hatValue & SDL_HAT_UP) != 0)
            {
                Core.Button(InputAction.DPadUp, true);
            }
            if ((hatValue & SDL_HAT_DOWN) != 0)
            {
                Core.Button(InputAction.DPadDown, true);
            }
            if ((hatValue & SDL_HAT_LEFT) != 0)
            {
                Core.Button(InputAction.DPadLeft, true);
            }
            if ((hatValue & SDL_HAT_RIGHT) != 0)
            {
                Core.Button(InputAction.DPadRight, true);
            }

            //GetAxis();
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
        private void GetAxis()
        {
            float lx = 0.0f, ly = 0.0f, rx = 0.0f, ry = 0.0f;

            short leftX = SDL_GameControllerGetAxis(joystickid, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX);
            short leftY = SDL_GameControllerGetAxis(joystickid, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY);
            short rightX = SDL_GameControllerGetAxis(joystickid, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX);
            short rightY = SDL_GameControllerGetAxis(joystickid, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY);

            lx = NormalizeAxis(leftX);
            ly = NormalizeAxis(leftY);
            rx = NormalizeAxis(rightX);
            ry = NormalizeAxis(rightY);

            Core.AnalogAxis(lx, ly, rx, ry);
        }
        private void HandleAxisEvent(byte axis, short value)
        {
            if (Core == null)
                return;

            float normalizedValue = NormalizeAxis(value);

            switch ((SDL_GameControllerAxis)axis)
            {
                case SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT:
                    if (normalizedValue < 0.5f)
                    {
                        Core.Button(InputAction.L2, true);
                    } else if (normalizedValue >= 0.5f)
                    {
                        Core.Button(InputAction.L2);
                    }
                    return;

                case SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT:
                    if (normalizedValue < 0.5f)
                    {
                        Core.Button(InputAction.R2, true);
                    } else if (normalizedValue >= 0.5f)
                    {
                        Core.Button(InputAction.R2);
                    }
                    return;
            }

            GetAxis();
        }

        private void JOYSTICKHANDLER()
        {
            SDL_Event e;

            while (SdlQuit == false)
            {
                Thread.Sleep(50);
                while (SDL_PollEvent(out e) != 0)
                {
                    switch (e.type)
                    {
                        case SDL_EventType.SDL_QUIT:
                            SdlQuit = true;
                            return;

                        // 手柄按钮按下
                        case SDL_EventType.SDL_JOYBUTTONDOWN:
                            HandleButtonEvent((SDL_GameControllerButton)e.cbutton.button, isDown: true);
                            break;

                        // 手柄按钮释放
                        case SDL_EventType.SDL_JOYBUTTONUP:
                            HandleButtonEvent((SDL_GameControllerButton)e.cbutton.button, isDown: false);
                            break;

                        // 手柄轴移动
                        case SDL_EventType.SDL_JOYAXISMOTION:
                            HandleAxisEvent(e.caxis.axis, e.caxis.axisValue);
                            break;

                        // 手柄方向键（HAT）事件
                        case SDL_EventType.SDL_JOYHATMOTION:
                            HandleHatEvent(e.jhat.hat, e.jhat.hatValue);
                            break;

                        // 手柄连接
                        case SDL_EventType.SDL_JOYDEVICEADDED:
                            if (SDL_IsGameController(0) == SDL_bool.SDL_TRUE)
                            {
                                joystickid = SDL_GameControllerOpen(0);
                            } else
                            {
                                joystickid = SDL_JoystickOpen(0);
                            }
                            Console.WriteLine($"Controller Device {SDL_NumJoysticks()} : {SDL_JoystickNameForIndex(0)} Connected");
                            break;

                        case SDL_EventType.SDL_JOYDEVICEREMOVED:
                            break;

                        default:
                            if (SdlQuit)
                                return;
                            break;
                    }
                }
            }
        }
        #endregion

    }

    #region INIFILE
    public class IniFile
    {
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string name, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        public string path;
        public IniFile(string inipath)
        {
            path = inipath;
        }
        public void Write(string name, string key, string value)
        {
            WritePrivateProfileString(name, key, value, this.path);
        }
        public string Read(string name, string key)
        {
            StringBuilder sb = new StringBuilder(255);
            int ini = GetPrivateProfileString(name, key, "", sb, 255, this.path);
            return sb.ToString();
        }
        public void WriteInt(string name, string key, int value)
        {
            WritePrivateProfileString(name, key, value.ToString(), this.path);
        }
        public int ReadInt(string name, string key)
        {
            string str = Read(name, key);
            if (str == "")
                return 0;
            else
                return Convert.ToInt32(str);
        }
    }
    #endregion
}
