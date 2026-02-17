using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Widget;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LightVK;
using static ScePSX.Controller;

#pragma warning disable CS8602
#pragma warning disable CS8604
#pragma warning disable CS8600
#pragma warning disable CS8618

namespace ScePSX
{
    public partial class MainView : UserControl
    {
        PSXHandler PSX;

        public MainView()
        {
            InitializeComponent();

            AHelper.InitAssert();

            Translations.LangFile = PSXHandler.RootPath + "/lang.xml";
            Translations.DefaultLanguage = "en";

            PSXHandler.RootPath = AHelper.RootPath;
            PSX = new PSXHandler();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            AHelper.CheckPermission();
        }

        private async Task CreateGameActivity()
        {
            var tcs = new TaskCompletionSource<IntPtr>();

            Action<IntPtr, int, int> actionhandler = null;
            actionhandler = (handle, width, height) =>
            {
                PSX.NativeHandle = handle;
                PSX.NativeWidth = width;
                PSX.NativeHeight = height;
                Console.WriteLine($"Get ANativeWindow 0x{handle:X} {width}x{height}");
                GameActivity.OnActivityCreated -= actionhandler;
                tcs.TrySetResult(IntPtr.Zero);
            };
            GameActivity.OnActivityCreated += actionhandler;
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
                GameActivity.OnActivityCreated += OnActivityCreated;
                GameActivity.OnActivitySizeChanged += OnActivitySizeChanged;
                GameActivity.OnActivityHandleDestroyed += OnActivityHandleDestroyed;

                if (AHelper.GamepadOverlay != null)
                {
                    AHelper.GamepadOverlay.OnButtonStateChanged += GamepadOverlay_OnButtonStateChanged;
                    AHelper.GamepadOverlay.OnTopBarAction += GamepadOverlay_OnTopBarAction;
                }
                if (AHelper.androidInput != null)
                {
                    AHelper.androidInput.OnButtonChanged += AndroidInput_OnButtonChanged;
                    AHelper.androidInput.OnAnalogAxisChanged += AndroidInput_OnAnalogAxisChanged;
                }
            }
        }

        private void GamepadOverlay_OnTopBarAction(string action)
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

        private void AndroidInput_OnAnalogAxisChanged(float lx, float ly, float rx, float ry)
        {
            if (!PSX.isRun())
                return;

            PSX.AnalogAxis(lx, ly, rx, ry);
        }

        private void AndroidInput_OnButtonChanged(InputAction Btn, bool pressed)
        {
            if (!PSX.isRun())
                return;

            PSX.KeyPress(Btn, pressed);
        }

        private void OnActivityCreated(IntPtr handle, int width, int height)
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
                    AHelper.GamepadOverlay.OnButtonStateChanged += GamepadOverlay_OnButtonStateChanged;
                    AHelper.GamepadOverlay.OnTopBarAction += GamepadOverlay_OnTopBarAction;
                }
                if (AHelper.androidInput != null)
                {
                    AHelper.androidInput.OnButtonChanged += AndroidInput_OnButtonChanged;
                    AHelper.androidInput.OnAnalogAxisChanged += AndroidInput_OnAnalogAxisChanged;
                }
            }
        }

        private void OnActivitySizeChanged(int width, int height)
        {
        }

        private void OnActivityHandleDestroyed()
        {
            if (PSX.isRun())
            {
                PSX.Core.WaitPausedAndSync();

                if (AHelper.GamepadOverlay != null)
                {
                    AHelper.GamepadOverlay.OnButtonStateChanged -= GamepadOverlay_OnButtonStateChanged;
                    AHelper.GamepadOverlay.OnTopBarAction -= GamepadOverlay_OnTopBarAction;
                }
                if (AHelper.androidInput != null)
                {
                    AHelper.androidInput.OnButtonChanged -= AndroidInput_OnButtonChanged;
                    AHelper.androidInput.OnAnalogAxisChanged -= AndroidInput_OnAnalogAxisChanged;
                }
            }
        }

        private void GamepadOverlay_OnButtonStateChanged(string label, InputAction button, bool pressed)
        {
            if (!PSX.isRun())
                return;

            PSX.KeyPress(button, pressed);
        }

        private async void RunTest()
        {
            if (!AHelper.HasPermission)
            {
                AHelper.ShowPermissionDialog();
                return;
            }

            if (PSX.Core != null)
            {
                PSX.Stop();
            }

            await CreateGameActivity();

            string Rom = AHelper.DownloadPath + "/test.iso";
            string Bios = AHelper.DownloadPath + "/SCPH1001.BIN";

            PSX.LoadGame(Rom, Bios, "");
        }

        private void BtnScan_Click(object? sender, RoutedEventArgs e)
        {
            PSX.GpuType = GPUType.OpenGL;
            RunTest();
        }

        private void BtnSet_Click(object? sender, RoutedEventArgs e)
        {
            PSX.GpuType = GPUType.Software;
            RunTest();
        }

        private void BtnCheat_Click(object? sender, RoutedEventArgs e)
        {
            VulkanDevice.OsEnv = VulkanDevice.vkOsEnv.ANDROID;
            PSX.GpuType = GPUType.Vulkan;
            RunTest();
        }
    }

    public static class OSD
    {
        static DispatcherTimer? _osdTimer = new DispatcherTimer();

        public static void Show(string message = "", int durationMs = 3000)
        {
            Toast.MakeText(AHelper.MainActivity, message, ToastLength.Long).Show();
        }
    }
}
