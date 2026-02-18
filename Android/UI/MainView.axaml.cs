using System;
using System.IO;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Annotations;
using Avalonia.Controls;
using Avalonia.Interactivity;

#pragma warning disable CS8602
#pragma warning disable CS8604
#pragma warning disable CS8600
#pragma warning disable CS8618

namespace ScePSX
{
    public partial class MainView : UserControl
    {
        GameActivityMange ActivityMange;
        PSXHandler PSX;

        public MainView()
        {
            InitializeComponent();

            AHelper.InitAssert();

            Translations.LangFile = AHelper.RootPath + "/lang.xml";
            Translations.DefaultLanguage = "en";
            Translations.Init();

            PSXHandler.RootPath = AHelper.RootPath;
            PSXHandler.ini = new IniFile(AHelper.RootPath + "/ScePSX.ini");

            ActivityMange = new GameActivityMange();

            PSX = ActivityMange.PSX;
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            RomListView.Init();

            RomListView.FillByini();

            RomListView.DoubleTapped += RomListView_DoubleTapped;

            cbgpu.SelectedIndex = 1;
            cbgpures.SelectedIndex = 2;
            cbar.SelectedIndex = 1;

            AHelper.CheckPermission();

            //AHelper.MainActivity.SetTheme(Resource.Style.Theme_AppCompat_NoActionBar);
        }

        private async void RunGame(string file, string id)
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

            var Bios = PSXHandler.ini.Read("main", "bios");
            if (Bios == "" || !File.Exists(Bios))
            {
                Bios = await AHelper.ShowBiosDialog();
                if (!File.Exists(Bios))
                    return;
                PSXHandler.ini.Write("main", "bios", Bios);
            }

            switch (cbgpu.SelectedIndex)
            {
                case 0:
                    PSX.GpuType = GPUType.Software;
                    break;
                case 1:
                    PSX.GpuType = GPUType.OpenGL;
                    break;
                case 2:
                    PSX.GpuType = GPUType.Vulkan;
                    break;
            }
            switch (cbgpures.SelectedIndex)
            {
                case 0:
                    GPUBackend.IRScale = 1;
                    break;
                case 1:
                    GPUBackend.IRScale = 2;
                    break;
                case 2:
                    GPUBackend.IRScale = 3;
                    break;
                case 3:
                    GPUBackend.IRScale = 5;
                    break;
                case 4:
                    GPUBackend.IRScale = 9;
                    break;
            }
            switch (cbar.SelectedIndex)
            {
                case 0:
                    PSX.KeepAR = false;
                    break;
                case 1:
                    PSX.KeepAR = true;
                    break;
            }

            await ActivityMange.CreateGameActivity();

            //var Bios = AHelper.DownloadPath + "/SCPH1001.BIN";
            PSX.LoadGame(file, Bios, id);
        }

        private void RomListView_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            GameInfo item = (GameInfo)RomListView.GameListBox.SelectedItem;
            if (item != null)
            {
                PSX.GameName = item.Name;
                RunGame(item.fullName, item.ID);
            }
        }

        private async void BtnScan_Click(object? sender, RoutedEventArgs e)
        {
            await RomListView.SearchDir(AHelper.DownloadPath);
        }

        private void BtnSet_Click(object? sender, RoutedEventArgs e)
        {
        }

        private void BtnCheat_Click(object? sender, RoutedEventArgs e)
        {
        }

        private void BtnMcr_Click(object? sender, RoutedEventArgs e)
        {
        }

        private async void BtnOpen_Click(object? sender, RoutedEventArgs e)
        {
            var result = await AHelper.SelectFile("Rom", "Rom Files", new[] { "*.bin", "*.iso", "*.cue", "*.img", "*.exe" });
            if (File.Exists(result))
            {
                PSX.GameName = "";
                RunGame(result, "");
            }
        }

        private async void BtnBios_Click(object? sender, RoutedEventArgs e)
        {
            string result = await AHelper.SelectFile("BIOS", "Bios Files", new[] { "*.bin" });
            if (File.Exists(result))
            {
                PSXHandler.ini.Write("main", "bios", result);
            }
        }

        private void BtnAbout_Click(object? sender, RoutedEventArgs e)
        {
            var view = new AboutFrm();
            ShowForm(view);
        }

        private void ShowForm(UserControl userControl)
        {
            CommonActivity.userControl = userControl;
            var intent = new Intent(Android.App.Application.Context, typeof(CommonActivity));
            intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
            Android.App.Application.Context.StartActivity(intent);
        }
    }

    public static class OSD
    {
        private static Handler? _mainHandler;
        private static Toast? _currentToast;

        static OSD()
        {
            _mainHandler = new Handler(Looper.MainLooper);
        }

        public static void Show(string message = "", int durationMs = 3000)
        {
            _mainHandler?.Post(() =>
            {
                try
                {
                    _currentToast?.Cancel();

                    var duration = durationMs <= 2000 ? ToastLength.Short : ToastLength.Long;
                    _currentToast = Toast.MakeText(AHelper.MainActivity, message, duration);
                    _currentToast.Show();
                } catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Toast Error: {ex.Message}");
                }
            });
        }

        public static void Cancel()
        {
            _mainHandler?.Post(() =>
            {
                _currentToast?.Cancel();
                _currentToast = null;
            });
        }
    }
}
