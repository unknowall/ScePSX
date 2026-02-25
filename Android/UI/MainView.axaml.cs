using System.IO;
using Android.Content;
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
        public const string Ver = "ScePSX v0.2.1.1";
        public const string Version = "Version 0.2.1.1";

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

            labver.Text = Ver;

            lablang.Text = Translations.GetText("MnuLang", "menus");
            foreach (var lang in Translations.Languages)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = lang.Value;
                item.Tag = lang.Key;
                cblang.Items.Add(item);
                if (lang.Key == Translations.CurrentLangId)
                {
                    item.IsSelected = true;
                    cblang.SelectedItem = item;
                }
            }

            ActivityMange = new GameActivityMange();

            PSX = ActivityMange.PSX;
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            RomListView.Init();

            RomListView.DoubleTapped += RomListView_DoubleTapped;

            cbgpu.SelectedIndex = 1;
            cbgpures.SelectedIndex = 2;
            cbar.SelectedIndex = 1;

            AHelper.CheckPermission();
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
                {
                    OSD.Show(Translations.GetText("invbios"));
                    return;
                }
                FileInfo fileInfo = new FileInfo(Bios);
                if (fileInfo.Length != 524288)
                {
                    OSD.Show(Translations.GetText("invbios"));
                    return;
                }
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

            RomListView.FillByini();
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
            if (!AHelper.HasPermission)
            {
                AHelper.ShowPermissionDialog();
                return;
            }
            await RomListView.SearchDir(AHelper.DownloadPath);
            await RomListView.SearchDir(AHelper.DocumentsPath);
        }

        private GameInfo? CheckSelect()
        {
            GameInfo item = (GameInfo)RomListView.GameListBox.SelectedItem;
            if (item == null)
            {
                OSD.Show(Translations.GetText("notselected"));
                return null;
            }
            return item;
        }

        private void BtnSet_Click(object? sender, RoutedEventArgs e)
        {
            //var item = CheckSelect();
            //if (item == null)
            //{
            var view = new Setting("");
            Translations.UpdateLang(view);
            ShowForm(view);
            //} else
            //{
            //    var view = new Setting(item.ID);
            //    Translations.UpdateLang(view);
            //    ShowForm(view);
            //}
        }

        private void BtnCheat_Click(object? sender, RoutedEventArgs e)
        {
            var item = CheckSelect();
            if (item == null)
                return;

            var view = new CheatFrm(item.ID, null);
            Translations.UpdateLang(view);
            ShowForm(view);
        }

        private void BtnMcr_Click(object? sender, RoutedEventArgs e)
        {
            var item = CheckSelect();
            if (item == null)
                return;

            var view = new McrMangeFrm(item.ID);
            Translations.UpdateLang(view);
            ShowForm(view);
        }

        private async void BtnOpen_Click(object? sender, RoutedEventArgs e)
        {
            var result = await AHelper.SelectFile("Rom");
            if (File.Exists(result))
            {
                FileInfo fileInfo = new FileInfo(result);
                if (fileInfo.Length <= 1024 * 1024 * 50)
                {
                    OSD.Show(Translations.GetText("invrom"));
                    return;
                }
                PSX.GameName = "";
                RunGame(result, "");
            }
        }

        private async void BtnBios_Click(object? sender, RoutedEventArgs e)
        {
            string result = await AHelper.SelectFile("BIOS");
            if (File.Exists(result))
            {
                FileInfo fileInfo = new FileInfo(result);
                if (fileInfo.Length != 524288)
                {
                    OSD.Show(Translations.GetText("invbios"));
                    return;
                }
                PSXHandler.ini.Write("main", "bios", result);
            } else
            {
                OSD.Show(Translations.GetText("invbios"));
            }
        }

        private void BtnDel_Click(object? sender, RoutedEventArgs e)
        {
            var item = CheckSelect();
            if (item == null)
                return;
            RomListView.GameListBox.Items.Remove(item);
            PSXHandler.ini.DeleteKey("history", item.ID);
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

        private void cblang_selected(object? sender, SelectionChangedEventArgs e)
        {
            var item = (ComboBoxItem)cblang.SelectedItem;
            if (item != null)
            {
                if (item.Tag != null)
                {
                    Translations.CurrentLangId = (string)item.Tag;
                    Translations.UpdateLang(this);
                    Translations.UpdateLang(RomListView);
                    lablang.Text = Translations.GetText("MnuLang", "menus");
                    RomListView.FillByini();
                }
            }
        }
    }
}
