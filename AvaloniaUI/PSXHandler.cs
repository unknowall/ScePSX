using Avalonia.Input;
using System;
using System.Diagnostics;
using System.IO;
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

        public ScaleParam ScaleParam;
        public string GameName;
        public bool KeyFirst, isAnalog;
        public int CoreWidth, CoreHeight, Msaa;

        private int _frameCount = 0;
        private Stopwatch _fpsStopwatch = Stopwatch.StartNew();
        public float currentFps = 0;

        public PSXHandler()
        {
            SDLHanlder = new SDLHanlder(ini);
            KeyMange = new KeyMange(ini);
            Scaler = new PixelsScaler();
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
        }

        public void Dispose()
        {
            if (Core != null)
            {
                Core.Dispose();
            }
        }

        public bool isRun()
        {
            if (Core == null || !Core.Running) return false;

            return Core.Running;
        }

        public void KeyPress(Key K, bool Press)
        {
            if (Core == null || !Core.Running) return;

            if (K == Key.Tab && Press)
                Core.Boost = true;
            if (K == Key.Tab && !Press)
                Core.Boost = false;

            InputAction button = KeyMange.KMM1.GetKeyButton(K);
            if ((int)button != 0xFF) Core.Button(button, Press, 0);

            InputAction button2 = KeyMange.KMM2.GetKeyButton(K);
            if ((int)button2 != 0xFF) Core.Button(button2, Press, 1);

            if (!KeyFirst) KeyFirst = true;
        }

        public void Pause()
        {
            if (Core == null || !Core.Running) return;

            if (!Core.Pauseed) Core.WaitPaused();
        }

        public void Resume()
        {
            if (Core == null || !Core.Running) return;

            if (Core.Pauseed) Core.Pauseing = false;
        }

        public void Stop()
        {
            if (Core == null || !Core.Running) return;

            Core.Stop();
        }

        public void LoadGame(string RomFile, RenderHost Render, string ID = "")
        {
            GPUBackend.HWND = Render.NativeHandle;
            GPUBackend.HINST = Render.hInstance;
            GPUBackend.ClientHeight = (int)Render.Bounds.Height;
            GPUBackend.ClientWidth = (int)Render.Bounds.Width;

            if (GameName == "") GameName = Path.GetFileNameWithoutExtension(RomFile);

            Core = new PSXCore(this, this, this, RomFile, RootPath + "BIOS/" + ini.Read("main", "bios"), GPUType.OpenGL, ID);

            (Core.GPU as OpenglGPU).IRScale = 3;

            ini.Write("history", Core.DiskID, $"{RomFile}|{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");

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

            Core.Start();

            Core.PsxBus.controller1.IsAnalog = isAnalog;
        }

        public void PlaySamples(byte[] samples)
        {
            SDLHanlder.SamplesBuffer.Write(samples);
        }

        public void RenderFrame(int[] pixels, int width, int height)
        {
            if (Core == null || !Core.Running) return;

            CoreWidth = width;
            CoreHeight = height;

            //SDLHanlder.CheckController();

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