using System;
using System.Threading.Tasks;
using Android.Content;
using static ScePSX.Controller;

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
            }
        }
    }

    private void OnTopBarAction(string action)
    {
        switch (action)
        {
            case "cheats": // 打开金手指
                break;
            case "save": // 即时保存
                break;
            case "load": // 即时加载
                break;
            case "undo": // 撤销
                break;
            case var s when s.StartsWith("slot_change:"):
                int slot = int.Parse(s.Split(':')[1]); // 存档位变化
                break;
        }
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
