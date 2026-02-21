using System;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using static ScePSX.Controller;
using static ScePSX.VirtualGamepadOverlay;

#pragma warning disable CS8602
#pragma warning disable CS8604
#pragma warning disable CS8600
#pragma warning disable CS8618

namespace ScePSX;

public class GameActivityMange
{
    public PSXHandler PSX;

    public GameActivityMange()
    {
        PSX = new PSXHandler();
    }

    public async Task CreateGameActivity()
    {
        var tcs = new TaskCompletionSource<IntPtr>();

        Action<IntPtr, int, int> actionhandler = null;
        actionhandler = (handle, width, height) =>
        {
            PSX.NativeHandle = handle;
            PSX.NativeWidth = width;
            PSX.NativeHeight = height;
            Console.WriteLine($"Get ANativeWindow 0x{handle:X} {width}x{height}");
            GameActivity.OnSurfaceCreated -= actionhandler;
            tcs.TrySetResult(IntPtr.Zero);
        };
        GameActivity.OnSurfaceCreated += actionhandler;
        var intent = new Intent(Android.App.Application.Context, typeof(GameActivity));
        intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
        Android.App.Application.Context.StartActivity(intent);

        try
        {
            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        } catch (TimeoutException)
        {
            Console.WriteLine("Get ANativeWindow Timeout");
        } finally
        {
            GameActivity.OnSurfaceCreated += OnSurfaceCreated;
            GameActivity.OnSurfaceSizeChanged += OnSurfaceSizeChanged;
            GameActivity.OnSurfaceDestroyed += OnSurfaceDestroyed;
            GameActivity.OnActivityDestroyed += OnActivityDestroyed;

            if (AHelper.GamepadOverlay != null)
            {
                AHelper.GamepadOverlay.OnButtonStateChanged += OnButtonStateChanged;
                AHelper.GamepadOverlay.OnTopBarAction += OnTopBarAction;
            }
            if (AHelper.androidInput != null)
            {
                AHelper.androidInput.OnButtonChanged += OnButtonChanged;
                AHelper.androidInput.OnAnalogAxisChanged += OnAnalogAxisChanged;
                PSX.inputHandler = AHelper.androidInput;
            }
        }
    }

    private async void ChangeDisc()
    {
        if (PSX.isRun())
        {
            var iso = await AHelper.SelectFile("ISO", "", true, AHelper.GameActivity);
            if (File.Exists(iso))
            {
                var info = new FileInfo(iso);
                if (info.Length > 1024 * 1024 * 50)
                {
                    Console.WriteLine($"ScePSX ChangeDisc {iso}");
                    PSX.Core.SwapDisk(iso);
                    OSD.Show($"{Translations.GetText("FrmMain_SwapDisc")} {PSX.Core.DiskID}");
                } else
                    OSD.Show(Translations.GetText("invrom"));
            } else
            {
                OSD.Show(Translations.GetText("invrom"));
            }
        }
    }

    bool Proces = false;

    private void OnTopBarAction(TopBarEvent action, int param)
    {
        if (PSX.Core == null)
            return;
        if (Proces)
            return;
        Proces = true;
        switch (action)
        {
            case TopBarEvent.Cheat:
                if (param == 0)
                {
                    PSX.Core.LoadCheats();
                    OSD.Show(Translations.GetText("cheatapply"));
                } else
                {
                    PSX.Core.CleanCheats();
                    OSD.Show(Translations.GetText("txtcancel") + " " + Translations.GetText("vcheat"));
                }
                break;
            case TopBarEvent.StateSave:
                PSX.SaveState();
                OSD.Show(Translations.GetText("FrmMain_SaveState_saved"));
                break;
            case TopBarEvent.StateLoad:
                PSX.LoadState();
                OSD.Show(Translations.GetText("FrmMain_SaveState_load"));
                break;
            case TopBarEvent.SwapDisc:
                ChangeDisc();
                break;
            case TopBarEvent.SlotInc:
                PSX.SaveSlot = param;
                break;
            case TopBarEvent.SlotDec:
                PSX.SaveSlot = param;
                break;
        }
        Proces = false;
    }

    private void OnAnalogAxisChanged(float lx, float ly, float rx, float ry)
    {
        if (!PSX.isRun())
            return;

        PSX.AnalogAxis(lx, ly, rx, ry);
    }

    private void OnButtonChanged(InputAction Btn, bool pressed)
    {
        if (!PSX.isRun())
            return;

        PSX.KeyPress(Btn, pressed);
    }

    private void OnSurfaceCreated(IntPtr handle, int width, int height)
    {
        if (PSX.Core != null && PSX.Core.Pauseed)
        {
            PSX.NativeHandle = handle;
            PSX.NativeWidth = width;
            PSX.NativeHeight = height;
            PSX.ReCreateBackEnd();
            PSX.Resume();

            if (AHelper.GamepadOverlay != null)
            {
                AHelper.GamepadOverlay.OnButtonStateChanged += OnButtonStateChanged;
                AHelper.GamepadOverlay.OnTopBarAction += OnTopBarAction;
            }
            if (AHelper.androidInput != null)
            {
                AHelper.androidInput.OnButtonChanged += OnButtonChanged;
                AHelper.androidInput.OnAnalogAxisChanged += OnAnalogAxisChanged;
                PSX.inputHandler = AHelper.androidInput;
            }
        }
    }

    private void OnSurfaceSizeChanged(int width, int height)
    {
    }

    private void OnSurfaceDestroyed()
    {
        if (PSX.isRun())
        {
            PSX.Core.WaitPausedAndSync();

            if (AHelper.GamepadOverlay != null)
            {
                AHelper.GamepadOverlay.OnButtonStateChanged -= OnButtonStateChanged;
                AHelper.GamepadOverlay.OnTopBarAction -= OnTopBarAction;
            }
            if (AHelper.androidInput != null)
            {
                AHelper.androidInput.OnButtonChanged -= OnButtonChanged;
                AHelper.androidInput.OnAnalogAxisChanged -= OnAnalogAxisChanged;
                PSX.inputHandler = null;
            }
        }
    }

    private void OnActivityDestroyed()
    {
        if (PSX.isRun())
        {
            PSX.Stop();
        }
    }

    private void OnButtonStateChanged(string label, InputAction button, bool pressed)
    {
        if (!PSX.isRun())
            return;

        PSX.KeyPress(button, pressed);
    }
}
