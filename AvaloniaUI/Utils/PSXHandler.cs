using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Input;
using ScePSX.Core.GPU;
using static ScePSX.Controller;

namespace ScePSX.UI
{
    public class PSXHandler : IAudioHandler, IRenderHandler, IRumbleHandler, IDisposable
    {
        private static string RootPath = AppContext.BaseDirectory;
        public static IniFile ini = new IniFile(RootPath + "ScePSX.ini");

        public SDLHanlder SDLHanlder;
        public KeyMange KeyMange;

        public PSXCore? Core;
        public PixelsScaler Scaler;
        public SoftRender? softRender;

        public RenderHost? Render;
        public SoftDrawHost SoftDrawView;

        public GPUType GpuType = GPUType.OpenGL;
        public bool SoftDrawViaGL = false;
        public ScaleParam ScaleParam;
        public string GameName;
        public bool KeyFirst, isAnalog;
        public int CoreWidth, CoreHeight, Msaa;

        public bool AutoIR, PGXP, PGXPT, Realcolor, KeepAR;
        public int IRScale, SaveSlot;

        private int _frameCount = 0;
        private Stopwatch _fpsStopwatch = Stopwatch.StartNew();
        public float currentFps = 0;

        public PSXHandler()
        {
            SDLHanlder = new SDLHanlder(ini);
            KeyMange = new KeyMange(ini);
            Scaler = new PixelsScaler();

            LoadSetting();
        }

        public void LoadSetting()
        {
            switch (ini.ReadInt("OpenGL", "MSAA"))
            {
                case 0:
                    Msaa = 0;
                    break;
                case 1:
                    Msaa = 4;
                    break;
                case 2:
                    Msaa = 6;
                    break;
                case 3:
                    Msaa = 8;
                    break;
                case 4:
                    Msaa = 16;
                    break;
            }

            IRScale = ini.ReadInt("main", "GpuModeScale");
            AutoIR = IRScale <= 0;
            PGXP = ini.ReadInt("main", "PGXP") == 1;
            PGXPT = ini.ReadInt("main", "PGXPT") == 1;
            Realcolor = ini.ReadInt("main", "RealColor") == 1;
            KeepAR = ini.ReadInt("main", "KeepAR") == 1;
            SaveSlot = ini.ReadInt("main", "StateSlot");
            GpuType = (GPUType)ini.ReadInt("Main", "Render");
        }

        public void Dispose()
        {
            if (Core != null)
            {
                Core.Dispose();
                softRender?.Dispose();
            }
        }

        public bool isRun()
        {
            if (Core == null || !Core.Running)
                return false;

            return Core.Running;
        }

        public void KeyPress(Key K, bool Press)
        {
            if (Core == null || !Core.Running)
                return;

            if (K == Key.Tab && Press)
                Core.Boost = true;
            if (K == Key.Tab && !Press)
                Core.Boost = false;

            InputAction button = KeyMange.KMM1.GetKeyButton(K);
            if ((int)button != 0xFF)
                Core.Button(button, Press, 0);

            InputAction button2 = KeyMange.KMM2.GetKeyButton(K);
            if ((int)button2 != 0xFF)
                Core.Button(button2, Press, 1);

            if (!KeyFirst)
                KeyFirst = true;
        }

        public string SaveState()
        {
            if (Core == null || !Core.Running)
                return "";

            ini.WriteInt("main", "StateSlot", SaveSlot);
            string statefile = RootPath + "/SaveState/" + Core.DiskID + "_Save" + SaveSlot.ToString() + ".dat";
            string statename = SaveSlot.ToString() + " - " + File.GetLastWriteTime(statefile).ToLocalTime();

            Core.SaveState(SaveSlot.ToString());

            OSD.Show($"{Translations.GetText("FrmMain_SaveState_saved")} [ {SaveSlot} ]");

            return statename;
        }

        public void LoadState()
        {
            if (Core == null || !Core.Running)
                return;

            string statefile = RootPath + "/SaveState/" + Core.DiskID + "_Save" + SaveSlot.ToString() + ".dat";

            Core.SaveState("~");
            Core.LoadState(SaveSlot.ToString());

            SetGPUParam();

            OSD.Show($"{Translations.GetText("FrmMain_SaveState_load")} [ {SaveSlot} ]");
        }

        public void UnLoadState()
        {
            if (Core == null || !Core.Running)
                return;

            Core.LoadState("~");

            SetGPUParam();

            OSD.Show($"{Translations.GetText("FrmMain_SaveState_unload")} [ {SaveSlot} ]");
        }

        public void Pause()
        {
            if (Core == null || !Core.Running)
                return;

            if (!Core.Pauseed)
                Core.WaitPaused();
        }

        public void Resume()
        {
            if (Core == null || !Core.Running)
                return;

            if (Core.Pauseed)
                Core.Pauseing = false;
        }

        public void Stop()
        {
            if (Core == null || !Core.Running)
                return;

            Core.Stop();
            Core.Dispose();
            Core = null;
            softRender?.Dispose();
            softRender = null;
        }

        public void SwitchBackEnd(GPUType mode)
        {
            if (Core == null || !Core.Running || Core.GPU.type == mode)
                return;

            ini.WriteInt("Main", "Render", (int)mode);
            //GPUType gpumode = (GPUType)ini.ReadInt("main", "GpuMode");

            GPUBackend.HWND = Render.NativeHandle;
            GPUBackend.HINST = Render.hInstance;
            GPUBackend.ClientHeight = (int)Render.Bounds.Height;
            GPUBackend.ClientWidth = (int)Render.Bounds.Width;
            GPUBackend.IRScale = AutoIR ? 3 : IRScale;

            Core.WaitPausedAndSync();

            if (mode == GPUType.OpenGL)
            {
                Core.PsxBus.gpu.SelectGPU(GPUType.OpenGL);

                Core.GpuBackend = GPUType.OpenGL;
            }
            if (mode == GPUType.Vulkan)
            {
                Core.PsxBus.gpu.SelectGPU(GPUType.Vulkan);

                Core.GpuBackend = GPUType.Vulkan;
            }
            if (mode == GPUType.Software)
            {
                if (SoftDrawViaGL)
                    InitSoftRender();

                Core.PsxBus.gpu.SelectGPU(GPUType.Software);

                Core.GpuBackend = GPUType.Software;
            }

            Core.GPU = Core.PsxBus.gpu.Backend.GPU;

            GpuType = mode;

            SetGPUParam();

            Core.Pauseing = false;

            OSD.Show($"GPU [ {mode.ToString()} ]", 5000);
        }

        private void InitSoftRender()
        {
            if (softRender == null)
            {
                softRender = new SoftRender();
                softRender.InitRender(GPUBackend.HWND);
            }
            softRender.Width = GPUBackend.ClientWidth;
            softRender.Height = GPUBackend.ClientHeight;
        }

        public void SetGPUParam()
        {
            switch (GpuType)
            {
                case GPUType.OpenGL:
                    (Core.GPU as OpenglGPU).PGXP = PGXPVector.use_pgxp_highpos && PGXPVector.use_pgxp;
                    (Core.GPU as OpenglGPU).PGXPT = PGXPT;
                    (Core.GPU as OpenglGPU).KEEPAR = KeepAR;
                    (Core.GPU as OpenglGPU).RealColor = Realcolor;
                    break;
                case GPUType.Vulkan:
                    (Core.GPU as VulkanGPU).PGXP = PGXPVector.use_pgxp_highpos && PGXPVector.use_pgxp;
                    (Core.GPU as VulkanGPU).PGXPT = PGXPT;
                    (Core.GPU as VulkanGPU).KEEPAR = KeepAR;
                    (Core.GPU as VulkanGPU).RealColor = Realcolor;
                    break;
                case GPUType.Software:
                    SoftDrawView.KeepAR = KeepAR;
                    break;
            }
        }

        public void ApplyPGXPSet()
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

        public void LoadGame(string RomFile, string ID = "")
        {
            GPUBackend.HWND = Render.NativeHandle;
            GPUBackend.HINST = Render.hInstance;
            GPUBackend.ClientHeight = (int)Render.Bounds.Height;
            GPUBackend.ClientWidth = (int)Render.Bounds.Width;
            GPUBackend.IRScale = 3;

            if (GameName == "")
                GameName = Path.GetFileNameWithoutExtension(RomFile);

            Core = new PSXCore(this, this, this, RomFile, RootPath + "BIOS/" + ini.Read("main", "bios"), GpuType, ID);

            ini.Write("history", Core.DiskID, $"{RomFile}|{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");

            ini.WriteInt("Main", "Render", (int)GpuType);

            SDLHanlder.SetAudioBuffer();

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

            ScaleParam.scale = 0;
            ScaleParam.mode = (ScaleMode)ini.ReadInt("Main", "ScaleMode");

            CPU.SetExecution((ini.ReadInt("Main", "CpuMode") == 1));

            ApplyPGXPSet();

            if (GpuType == GPUType.Software && SoftDrawViaGL)
            {
                InitSoftRender();
                softRender.FrameSkip = 0;
            }

            SetGPUParam();

            Core.Start();

            Core.PsxBus.controller1.IsAnalog = isAnalog;
        }

        public void PlaySamples(byte[] samples)
        {
            SDLHanlder.SamplesBuffer.Write(samples);
        }

        public void RenderFrame(int[] pixels, int width, int height)
        {
            if (Core == null || !Core.Running)
                return;

            CoreWidth = width;
            CoreHeight = height;

            //SDLHanlder.CheckController();
            if (width > 0 && GpuType == GPUType.Software && SoftDrawViaGL && softRender != null)
            {
                softRender.Width = GPUBackend.ClientWidth;
                softRender.Height = GPUBackend.ClientHeight;

                softRender.RenderToWindow(pixels, width, height, ScaleParam);
            } else if (width > 0)
            {
                SoftDrawView.RenderPixels(pixels, width, height, ScaleParam);
            }

            KeyFirst = SDLHanlder.QueryControllerState(1, Core, false, KeyFirst);
            KeyFirst = SDLHanlder.QueryControllerState(2, Core, false, KeyFirst);

            _frameCount++;
            var elapsedSeconds = _fpsStopwatch.Elapsed.TotalSeconds;
            if (elapsedSeconds >= 1.0)
            {
                currentFps = (float)(_frameCount / elapsedSeconds);
                _frameCount = 0;
                _fpsStopwatch.Restart();
            }
        }

        public void ControllerRumble(byte VibrationRight, byte VibrationLeft)
        {
            SDLHanlder.ControllerRumble(VibrationRight, VibrationLeft);
        }
    }

}
