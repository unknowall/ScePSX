using System;
using System.Diagnostics;
using System.IO;
using Android.OS;
using ScePSX.Core.GPU;
using static ScePSX.Controller;

#pragma warning disable CS8602
#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8618

namespace ScePSX
{
    public class PSXHandler : IAudioHandler, IRenderHandler, IRumbleHandler, IDisposable
    {
        public static string RootPath;
        public static IniFile ini;

        public IntPtr NativeHandle;
        public int NativeWidth, NativeHeight;

        private PowerManager.WakeLock? wakeLock;

        public AndroidAudioHandler AudioHanlder;

        public PSXCore? Core;
        public PixelsScaler Scaler;
        public SoftRender softRender;

        public GPUType GpuType = GPUType.OpenGL;
        public ScaleParam ScaleParam;
        public string? GameName;
        public bool isAnalog;
        public int CoreWidth, CoreHeight, Msaa;

        public bool PGXP, PGXPT, Realcolor, KeepAR;
        public int IRScale = 2, SaveSlot;

        private int _frameCount = 0;
        private Stopwatch _fpsStopwatch = Stopwatch.StartNew();
        public float currentFps = 0;

        public PSXHandler()
        {
            AudioHanlder = new AndroidAudioHandler();
            Scaler = new PixelsScaler();
            softRender = new SoftRender();

            VulkanGPU.AppPath = RootPath;

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
            IRScale = IRScale > 0 ? IRScale : 2;
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
                AudioHanlder.Dispose();
                softRender.Dispose();
                Core.Dispose();
                Core = null;
            }
        }

        public bool isRun()
        {
            if (Core == null || !Core.Running)
                return false;

            return Core.Running;
        }

        public void KeyPress(InputAction K, bool Press)
        {
            if (Core == null || !Core.Running)
                return;

            Core.Button(K, Press, 0);
        }

        public void AnalogAxis(float lx, float ly, float rx, float ry, int conidx = 0)
        {
            if (Core == null || !Core.Running)
                return;

            Core.AnalogAxis(lx, ly, rx, ry, conidx);
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

            AudioHanlder.Stop();
            Core.Stop();
            Core.Dispose();
            softRender.Dispose();
            Core = null;
        }

        public void ReCreateBackEnd()
        {
            GPUBackend.HWND = NativeHandle;
            GPUBackend.HINST = 0;
            GPUBackend.ClientHeight = NativeHeight;
            GPUBackend.ClientWidth = NativeWidth;
            GPUBackend.IRScale = IRScale;

            if (GpuType != GPUType.Software)
            {
                GPUBackend.ReCreate = true;
                Core.PsxBus.gpu.SelectGPU(GpuType);
                Core.GpuBackend = GpuType;
                Core.GPU = Core.PsxBus.gpu.Backend.GPU;
                SetGPUParam();
            } else
            {
                softRender.Dispose();
                softRender.Height = NativeHeight;
                softRender.Width = NativeWidth;
                softRender.KeepAspect = KeepAR;
                softRender.InitRender(NativeHandle);
            }
        }

        public void SwitchBackEnd(GPUType mode)
        {
            if (Core == null || !Core.Running || Core.GPU.type == mode)
                return;

            ini.WriteInt("Main", "Render", (int)mode);
            //GPUType gpumode = (GPUType)ini.ReadInt("main", "GpuMode");

            GPUBackend.HWND = NativeHandle;
            GPUBackend.HINST = 0;
            GPUBackend.ClientHeight = NativeHeight;
            GPUBackend.ClientWidth = NativeWidth;
            GPUBackend.IRScale = IRScale;

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
                Core.PsxBus.gpu.SelectGPU(GPUType.Software);

                softRender.InitRender(NativeHandle);
                softRender.Height = NativeHeight;
                softRender.Width = NativeWidth;
                softRender.KeepAspect = KeepAR;

                Core.GpuBackend = GPUType.Software;
            }

            Core.GPU = Core.PsxBus.gpu.Backend.GPU;

            GpuType = mode;

            SetGPUParam();

            Core.Pauseing = false;

            OSD.Show($"GPU [ {mode.ToString()} ]", 5000);
        }

        public void SetGPUParam()
        {
            switch (GpuType)
            {
                case GPUType.OpenGL:
                    GPUBackend.IRScale = IRScale;
                    (Core.GPU as OpenglGPU).PGXP = PGXPVector.use_pgxp_highpos && PGXPVector.use_pgxp;
                    (Core.GPU as OpenglGPU).PGXPT = PGXPT;
                    (Core.GPU as OpenglGPU).KEEPAR = KeepAR;
                    (Core.GPU as OpenglGPU).RealColor = Realcolor;
                    break;
                case GPUType.Vulkan:
                    GPUBackend.IRScale = IRScale;
                    (Core.GPU as VulkanGPU).PGXP = PGXPVector.use_pgxp_highpos && PGXPVector.use_pgxp;
                    (Core.GPU as VulkanGPU).PGXPT = PGXPT;
                    (Core.GPU as VulkanGPU).KEEPAR = KeepAR;
                    (Core.GPU as VulkanGPU).RealColor = Realcolor;
                    break;
                case GPUType.Software:
                    softRender.KeepAspect = KeepAR;
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

        public void LoadGame(string RomFile, string BiosFile, string ID = "")
        {
            if (!File.Exists(BiosFile))
            {
                OSD.Show(Translations.GetText("NotBios"), 9999999);
                return;
            }

            GPUBackend.HWND = NativeHandle;
            GPUBackend.HINST = 0;
            GPUBackend.ClientHeight = NativeHeight;
            GPUBackend.ClientWidth = NativeWidth;
            GPUBackend.IRScale = IRScale;

            if (GameName == "")
                GameName = Path.GetFileNameWithoutExtension(RomFile);

            Core = new PSXCore(this, this, this, RomFile, BiosFile, GpuType, ID, RootPath);

            ini.Write("history", Core.DiskID, $"{RomFile}|{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");

            ini.WriteInt("Main", "Render", (int)GpuType);

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

            SetGPUParam();

            if (GpuType == GPUType.Software)
            {
                softRender.InitRender(NativeHandle);
                softRender.KeepAspect = KeepAR;
                softRender.Height = NativeHeight;
                softRender.Width = NativeWidth;
            }

            AudioHanlder.Play();

            var pm = (PowerManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.PowerService);
            wakeLock = pm.NewWakeLock(WakeLockFlags.Partial, "ScePSX:GameLoop");
            wakeLock.Acquire();

            Core.Boost = true;
            Core.PsxBus.cpu.ttydebug = true;
            Core.Start();

            Core.PsxBus.controller1.IsAnalog = isAnalog;
        }

        public void PlaySamples(byte[] samples)
        {
            AudioHanlder.samplesBuffer.Write(samples);
        }

        public void RenderFrame(int[] pixels, int width, int height)
        {
            if (Core == null || !Core.Running)
                return;

            CoreWidth = width;
            CoreHeight = height;

            if (width > 0 && GpuType == GPUType.Software && softRender.Inited)
            {
                softRender.RenderToWindow(pixels, width, height, ScaleParam);
            }

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

        }
    }

}
