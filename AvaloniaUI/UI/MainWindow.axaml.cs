using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ScePSX.Core.GPU;

namespace ScePSX.UI;

public partial class MainWindow : Window
{
    public static string version = "ScePSX v0.2.0.1";
    private static string RootPath = AppContext.BaseDirectory;

    public PSXHandler PSX;

    int StatusDelay;
    bool bFullScreen;

    DispatcherTimer? timer;

    [DllImport("kernel32.dll")]
    public static extern Boolean AllocConsole();

    public MainWindow()
    {
        InitializeComponent();

        if (OperatingSystem.IsWindows())
        {
            if (File.Exists("./opengl32.dll") && !File.Exists("./ReShader.dll"))
            {
                File.Copy("./opengl32.dll", "./ReShader.dll");
            }
        }

        RomList.OnDbClick += RomListSeelected;

        PSX = new PSXHandler();

        this.AddHandler(KeyDownEvent, MainWindow_KeyDown, RoutingStrategies.Tunnel);
        this.AddHandler(KeyUpEvent, MainWindow_KeyUp, RoutingStrategies.Tunnel);

        this.Closing += MainWindow_Closing;

        CleanCheckSet(MnuRender, (int)PSX.GpuType - 1);

        timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += Timer_Elapsed;
        timer.Start();

        SetIcon.EnsureDesktopFile();

        //Translations.CurrentLangId = "en";
        Translations.DefaultLanguage = "en";
        Translations.Init();
        Translations.UpdateLang(this);

        InitLangMenu();

        if (!CheckMac())
            VulkanMnu.IsVisible = false;

        if (OperatingSystem.IsWindows())
            ConsoleMnu.IsVisible = true;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        string[] PosStr = PSXHandler.ini.Read("Main", "FromPos").Split('|');
        if (PosStr.Length >= 4)
        {
            this.Position = new PixelPoint(Convert.ToInt16(PosStr[0]), Convert.ToInt16(PosStr[1]));
            this.Width = Convert.ToInt16(PosStr[2]);
            this.Height = Convert.ToInt16(PosStr[3]);
        }

        if (!File.Exists("./BIOS/" + PSXHandler.ini.Read("main", "bios")))
        {
            OSD.Show(Translations.GetText("NotBios"), 9999999);
        }

        PSX.SoftDrawView = SoftDrawView;
        PSX.Render = RenderHostView;
    }

    private void ConsoleMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (OperatingSystem.IsWindows())
        {
            AllocConsole();
            var writer = new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = true
            };
            Console.SetOut(writer);
        }
    }

    private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        string PosStr = $"{this.Position.X}|{this.Position.Y}|{(int)this.Width}|{(int)this.Height}";
        PSXHandler.ini.Write("Main", "FromPos", PosStr);
    }

    private void InitLangMenu()
    {
        foreach (var lang in Translations.Languages)
        {
            MenuItem item = new MenuItem();
            item.Header = lang.Value;
            item.Tag = lang.Key;
            item.Click += LangClick;

            MnuLang.Items.Add(item);
        }
    }

    private void InitStateMenu()
    {
        if (PSX.Core == null || PSX.Core.DiskID == "")
            return;

        MnuSaveState.Items.Clear();
        MnuLoadState.Items.Clear();

        string statefile, statename;

        var StateSlot = PSX.SaveSlot;

        for (int i = 0; i < 10; i++)
        {
            MenuItem mnusave = new MenuItem();
            MenuItem mnuload = new MenuItem();

            statefile = RootPath + "/SaveState/" + PSX.Core.DiskID + "_Save" + i.ToString() + ".dat";
            if (File.Exists(statefile))
            {
                statename = i.ToString() + " - " + File.GetLastWriteTime(statefile).ToLocalTime();
            } else
            {
                statename = i.ToString() + " - None";
                mnuload.IsEnabled = false;
            }
            if (i == StateSlot)
                mnusave.IsChecked = true;

            mnusave.Name = statefile;
            mnusave.Header = statename;
            mnusave.Tag = 30 + i;
            mnusave.IsChecked = StateSlot == i ? true : false;
            mnusave.Click += StateSaveMnu_Click;
            mnusave.Classes.Add("SubMenuItem");
            mnusave.GroupName = "StateSaveMnu";
            MnuSaveState.Items.Add(mnusave);

            mnuload.Name = statefile;
            mnuload.Header = statename;
            mnuload.Tag = 40 + i;
            mnuload.IsChecked = StateSlot == i ? true : false;
            mnuload.Click += StateLoadMnu_Click;
            mnuload.Classes.Add("SubMenuItem");
            mnuload.GroupName = "StateLoadMnu";
            MnuLoadState.Items.Add(mnuload);
        }

        MnuSaveState.IsEnabled = true;
        MnuLoadState.IsEnabled = true;
        MnuUnloadState.IsEnabled = true;
    }

    private void LangClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender != null)
        {
            MenuItem item = (MenuItem)sender;
            if (item.Tag != null)
            {
                Translations.CurrentLangId = (string)item.Tag;
                Translations.UpdateLang(this);
                Translations.UpdateLang(RomListView);
                RomListView.FillByini();
            }
        }
    }

    private void StateSaveMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var tagobj = (sender as MenuItem).Tag;
        if (tagobj == null)
            return;
        PSX.SaveSlot = (int)tagobj - 30;
        var TimeStr = PSX.SaveState();
        (MnuSaveState.Items[PSX.SaveSlot] as MenuItem).Header = TimeStr;
        (MnuLoadState.Items[PSX.SaveSlot] as MenuItem).Header = TimeStr;
        (MnuLoadState.Items[PSX.SaveSlot] as MenuItem).IsEnabled = true;
    }

    private void StateLoadMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PSX.LoadState();
    }

    private void CleanStatus()
    {
        new[] { StatusLabel1, StatusLabel2, StatusLabel3, StatusLabel4, StatusLabel5, StatusLabel6, StatusLabel7 }.ToList().ForEach(l => l.Text = "");
    }

    private unsafe void Timer_Elapsed(object? sender, EventArgs e)
    {
        PSX.SDLHanlder.CheckController();

        if (PSX.Core == null || !PSX.Core.Running)
        {
            this.Title = version;
            return;
        }

        if (StatusDelay > 0)
        {
            StatusDelay--;
        } else
        {
            StatusLabel2.Text = $"F3+ F4- {Translations.GetText("SaveSlot")} [{PSX.SaveSlot}]";
            StatusLabel3.Text = $"F9[{(PSX.KeyFirst ? Translations.GetText("KeyFirst") : Translations.GetText("JoyFirst"))}]";
            StatusLabel4.Text = $"F10[{(PSX.isAnalog ? Translations.GetText("Analog") : Translations.GetText("Digital"))}]";
        }

        if (PSX.Core.Pauseed)
        {
            OSD.Show(" ▌▌");
            StatusLabel2.Text = Translations.GetText("Pause");
            CleanStatus();
        }

        if (PSX.AutoIR)
        {
            if (this.ClientSize.Width >= 320 && this.ClientSize.Height >= 240)
                GPUBackend.IRScale = 2;
            if (this.ClientSize.Width >= 800 && this.ClientSize.Height >= 600)
                GPUBackend.IRScale = 3;
            if (this.ClientSize.Width >= 1280 && this.ClientSize.Height >= 800)
                GPUBackend.IRScale = 4;
            if (this.ClientSize.Width >= 1920 && this.ClientSize.Height >= 1080)
                GPUBackend.IRScale = 5;
        }

        this.Title = $"{version}  -  {PSX.GameName}";

        int scalew = PSX.CoreWidth;
        int scaleh = PSX.CoreHeight;

        string rendername = PSX.Core.GPU.type.ToString();

        if (PSX.ScaleParam.scale > 0)
        {
            scalew *= PSX.ScaleParam.scale;
            scaleh *= PSX.ScaleParam.scale;
        }

        StatusLabel1.Text = PSX.Core.DiskID;

        string str_pgxp = PGXPVector.use_pgxp ? "PGXP " : "";

        StatusLabel5.Text = $"{str_pgxp}{rendername} ";
        if (PSX.Core.GPU.type != GPUType.Software)
            StatusLabel5.Text += $"{PSX.Msaa}xMSAA {GPUBackend.IRScale}xIR";

        StatusLabel6.Text = $"{scalew}*{scaleh}";

        if (PSX.Core.GPU.type == GPUType.Software)
            StatusLabel6.Text = $"{(PSX.ScaleParam.scale > 0 ? PSX.ScaleParam.mode.ToString() : "")} " + StatusLabel6.Text;

        StatusLabel7.Text = $"FPS {PSX.currentFps:F1}";
    }

    private void CleanCheckSet(MenuItem Menu, int CheckIdx)
    {
        foreach (MenuItem child in Menu.Items.OfType<MenuItem>())
            child.IsChecked = false;

        (Menu.Items[CheckIdx] as MenuItem).IsChecked = true;
    }

    private void MainWindow_KeyUp(object? sender, KeyEventArgs e)
    {
        PSX.KeyPress(e.Key, false);

        if (e.Key == Key.Tab)
            e.Handled = true;

        switch (e.Key)
        {
            case Key.Space:
                MnuPause_Click(sender, e);
                break;
            case Key.F2:
                fullScreenF2_Click(sender, e);
                break;
            case Key.F3:
                PSX.SaveSlot = PSX.SaveSlot < 9 ? PSX.SaveSlot + 1 : PSX.SaveSlot;
                CleanCheckSet(MnuSaveState, PSX.SaveSlot);
                CleanCheckSet(MnuLoadState, PSX.SaveSlot);
                OSD.Show($"{Translations.GetText("SaveSlot")} [ {PSX.SaveSlot} ]");
                break;
            case Key.F4:
                PSX.SaveSlot = PSX.SaveSlot > 0 ? PSX.SaveSlot - 1 : PSX.SaveSlot;
                CleanCheckSet(MnuSaveState, PSX.SaveSlot);
                CleanCheckSet(MnuLoadState, PSX.SaveSlot);
                OSD.Show($"{Translations.GetText("SaveSlot")} [ {PSX.SaveSlot} ]");
                break;
            case Key.F5:
                var TimeStr = PSX.SaveState();
                (MnuSaveState.Items[PSX.SaveSlot] as MenuItem).Header = TimeStr;
                (MnuLoadState.Items[PSX.SaveSlot] as MenuItem).Header = TimeStr;
                (MnuLoadState.Items[PSX.SaveSlot] as MenuItem).IsEnabled = true;
                break;
            case Key.F6:
                PSX.LoadState();
                break;
            case Key.F7:
                PSX.UnLoadState();
                break;
            case Key.F9:
                PSX.KeyFirst = !PSX.KeyFirst;
                PSXHandler.ini.WriteInt("main", "keyfirst", PSX.KeyFirst ? 1 : 0);
                break;
            case Key.F10:
                PSX.isAnalog = !PSX.isAnalog;
                PSXHandler.ini.WriteInt("main", "isAnalog", PSX.isAnalog ? 1 : 0);
                if (PSX.Core != null)
                {
                    PSX.Core.PsxBus.controller1.IsAnalog = PSX.isAnalog;
                    PSX.Core.PsxBus.controller2.IsAnalog = PSX.isAnalog;
                }
                OSD.Show($"{(PSX.isAnalog ? Translations.GetText("Analog") : Translations.GetText("Digital"))}");
                break;
            case Key.F11:
                UpScale_Click(sender, e);
                break;
            case Key.F12:
                DownScale_Click(sender, e);
                break;
        }
    }

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        PSX.KeyPress(e.Key, true);

        if (e.Key == Key.Tab)
            e.Handled = true;
    }

    private async Task<string> SelectFile()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return "";
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Games",
            FileTypeFilter = new[]
            {
            new FilePickerFileType("Games")
            {
                Patterns = new[] { "*.bin", "*.iso", "*.cue", "*.img", "*.exe" }
            }
        },
            SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(PSXHandler.ini.Read("main", "LastPath"))
        });
        if (files == null || files.Count == 0)
            return "";
        var filePath = files[0].Path.LocalPath;
        if (!File.Exists(filePath))
            return "";
        PSXHandler.ini.Write("main", "LastPath", Path.GetDirectoryName(filePath));
        return filePath;
    }

    private async Task<string> SelectPath()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return "";
        var paths = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Games Path"
        });
        if (paths == null || paths.Count == 0)
            return "";
        var filePath = paths[0].Path.LocalPath;
        if (!Path.Exists(filePath))
            return "";
        return filePath;
    }

    private void SetViewVisible()
    {
        if (PSX.GpuType == GPUType.Software && !PSX.SoftDrawViaGL)
        {
            RenderHostView.IsVisible = false;
            SoftDrawView.IsVisible = true;
        } else
        {
            RenderHostView.CancelSizeChanged();
            SoftDrawView.IsVisible = false;
            RenderHostView.IsVisible = false;

            RenderHostContainer.Child = null;
            RenderHostView = null;
            PSX.Render = null;
            var newRenderHost = new RenderHost
            {
                IsVisible = true,
                Name = "RenderHostView",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            };
            RenderHostView = newRenderHost;
            RenderHostContainer.Child = newRenderHost;
            RenderHostView.IsVisible = true;

            RenderHostContainer.InvalidateMeasure();
            RenderHostContainer.InvalidateArrange();

            MainDockPanel.InvalidateMeasure();
            MainDockPanel.InvalidateArrange();

            PSX.Render = RenderHostView;
        }
    }

    private async void RomListSeelected(GameInfo info)
    {
        RomListView.IsVisible = false;
        SetViewVisible();
        await Task.Delay(16);
        PSX.SoftDrawView = SoftDrawView;
        PSX.GameName = info.Name;
        PSX.LoadGame(info.fullName, info.ID);

        InitStateMenu();
    }

    private async void LoadDisk_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var File = await SelectFile();
        if (File == "")
            return;

        RomListView.IsVisible = false;
        SetViewVisible();
        await Task.Delay(16);
        PSX.SoftDrawView = SoftDrawView;
        PSX.LoadGame(File);

        InitStateMenu();
    }

    private async void SwapDisk_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!PSX.isRun())
            return;
        PSX.Pause();
        var File = await SelectFile();
        if (File == "")
        {
            PSX.Resume();
            return;
        }
        PSX.Core.SwapDisk(File);
        PSX.Core.Pauseing = false;

        CleanStatus();
        StatusLabel1.Text = $"{Translations.GetText("FrmMain_SwapDisc")} {PSX.Core.DiskID}";
        OSD.Show(StatusLabel1.Text);
        StatusDelay = 3;
    }

    private void CloseRomMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PSX.Stop();

        RomListView.FillByini();

        RomListView.IsVisible = true;
        RenderHostView.IsVisible = false;
        SoftDrawView.IsVisible = false;

        CleanStatus();

        MnuSaveState.Items.Clear();
        MnuLoadState.Items.Clear();
    }

    private async void SearchMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (PSX.isRun())
            return;
        var path = await SelectPath();
        if (path == "")
            return;
        await RomListView.SearchDir(path);
        OSD.Show();
    }

    private void MnuPause_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (PSX.isRun())
        {
            if (PSX.Core.Pauseed)
                PSX.Resume();
            else
                PSX.Pause();
        }
    }

    private async void SoftwareMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PSX.GpuType = GPUType.Software;

        if (PSX.isRun())
        {
            SetViewVisible();
            await Task.Delay(32);
            PSX.SwitchBackEnd(GPUType.Software);
        }

        CleanCheckSet(MnuRender, 0);

        PSXHandler.ini.WriteInt("Main", "Render", 1);
    }

    private async void OpenGLMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PSX.GpuType = GPUType.OpenGL;

        if (PSX.isRun())
        {
            // for Vulkan SwapChain
            PSX.Pause();
            SetViewVisible();
            await Task.Delay(32);
            PSX.SwitchBackEnd(GPUType.OpenGL);
        }

        CleanCheckSet(MnuRender, 1);

        PSXHandler.ini.WriteInt("Main", "Render", 2);
    }

    private async void VulkanMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PSX.GpuType = GPUType.Vulkan;

        if (PSX.isRun())
        {
            SetViewVisible();
            await Task.Delay(32);
            PSX.SwitchBackEnd(GPUType.Vulkan);
        }

        CleanCheckSet(MnuRender, 2);

        PSXHandler.ini.WriteInt("Main", "Render", 3);
    }

    private void UpScale_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (PSX.IRScale < 12)
            PSX.IRScale++;
        if (PSX.ScaleParam.scale < 10)
            PSX.ScaleParam.scale += 2;

        GPUBackend.IRScale = PSX.IRScale;
    }

    private void DownScale_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (PSX.IRScale >= 2)
            PSX.IRScale--;
        if (PSX.ScaleParam.scale >= 2)
            PSX.ScaleParam.scale -= 2;

        GPUBackend.IRScale = PSX.IRScale;
    }

    private void fullScreenF2_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!PSX.isRun())
            return;

        bFullScreen = !bFullScreen;

        if (bFullScreen)
        {
            this.MainMenu.IsVisible = false;
            this.StatusGrid.IsVisible = false;
            this.SystemDecorations = SystemDecorations.None;
            this.WindowState = WindowState.FullScreen;
        } else
        {
            this.MainMenu.IsVisible = true;
            this.StatusGrid.IsVisible = true;
            this.SystemDecorations = SystemDecorations.Full;
            this.WindowState = WindowState.Normal;
        }
    }

    private void SysSetMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var SetForm = new Setting("");
        Translations.UpdateLang(SetForm);
        SetForm.Show(this);
    }

    private void KeyMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var KeyForm = new KeyConfig(PSX.KeyMange);
        Translations.UpdateLang(KeyForm);
        KeyForm.Show(this);
    }

    private void CheatCode_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (PSX.isRun())
        {
            var RCheatForm = new CheatFrm(PSX.Core.DiskID, PSX.Core);
            Translations.UpdateLang(RCheatForm);
            RCheatForm.Show(this);
            return;
        }
        if (RomListView.GameListBox.SelectedItem == null)
        {
            OSD.Show(Translations.GetText("notselected"));
            return;
        }
        var item = (RomListView.GameListBox.SelectedItem as GameInfo);
        var CheatForm = new CheatFrm(item.ID, PSX.Core);
        Translations.UpdateLang(CheatForm);
        CheatForm.Show(this);
    }

    private void MemEditMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!PSX.isRun())
        {
            OSD.Show(Translations.GetText("notruning"));
            return;
        }
        var MemForm = new MemEdit(PSX.Core);
        Translations.UpdateLang(MemForm);
        MemForm.Show(this);
    }

    private void McrEditMnu_Click(object? sender, RoutedEventArgs e)
    {
        if (RomListView.GameListBox.SelectedItem == null)
        {
            OSD.Show(Translations.GetText("notselected"));
            return;
        }
        var item = (RomListView.GameListBox.SelectedItem as GameInfo);
        var McrForm = new McrMangeFrm(item.ID);
        Translations.UpdateLang(McrForm);
        McrForm.Show(this);
    }

    private void iniEditMnu_Click(object? sender, RoutedEventArgs e)
    {
        if (PSX.isRun())
        {
            var RSetForm = new Setting(PSX.Core.DiskID);
            Translations.UpdateLang(RSetForm);
            RSetForm.Show(this);
            return;
        }
        if (RomListView.GameListBox.SelectedItem == null)
        {
            OSD.Show(Translations.GetText("notselected"));
            return;
        }
        var item = (RomListView.GameListBox.SelectedItem as GameInfo);
        var SetForm = new Setting(item.ID);
        Translations.UpdateLang(SetForm);
        SetForm.Show(this);
    }

    private void NetPlaySetMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!PSX.isRun())
        {
            OSD.Show(Translations.GetText("notruning"));
            return;
        }
        if (PSX.Core != null)
        {
            var NetForm = new NetPlayFrm(PSX.Core);
            Translations.UpdateLang(NetForm);
            NetForm.Show(this);
        }
    }

    private void gitHubMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        OpenUrl("https://github.com/unknowall/ScePSX");
    }

    private void supportKoficomMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        OpenUrl("https://ko-fi.com/unknowall");
    }

    private void supportWeChatMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        OpenUrl("https://gitee.com/unknowall/ScePSX/raw/master/Others/support_via_wechat.png");
    }

    private void AboutMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var About = new AboutFrm();
        About.Show(this);
    }

    private void OpenUrl(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        } catch (Exception ex)
        {
            Console.WriteLine($"Failed to open URL: {ex.Message}");
        }
    }

    private bool CheckMac()
    {
        if (OperatingSystem.IsMacOS())
        {
            try
            {
                return File.Exists("/usr/lib/libvulkan.dylib") ||
                       File.Exists("/usr/local/lib/libvulkan.dylib");
            } catch
            {
                return false;
            }
        }
        return true;
    }
}

public static class SetIcon
{
    public static void EnsureDesktopFile()
    {
        if (!OperatingSystem.IsLinux())
            return;

        var desktopPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local/share/applications",
            "scepsx.desktop"
        );

        if (File.Exists(desktopPath))
            return;

        var content = $"""
        [Desktop Entry]
        Name=ScePSX
        Comment=PlayStation 1 Emulator
        Exec={Environment.ProcessPath}
        Icon={Path.Combine(AppContext.BaseDirectory, "001.ico")}
        Terminal=false
        Type=Application
        Categories=Game;Emulator;
        """;

#pragma warning disable CS8604
        Directory.CreateDirectory(Path.GetDirectoryName(desktopPath));
        File.WriteAllText(desktopPath, content);

        System.Diagnostics.Process.Start("update-desktop-database", Path.GetDirectoryName(desktopPath));
#pragma warning restore CS8604
    }
}

public static class OSD
{
    static DispatcherTimer? _osdTimer = new DispatcherTimer();

    public static void Show(string message = "", int durationMs = 3000)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow as MainWindow;

            if (mainWindow != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (mainWindow.OsdPopup == null)
                        return;
                    if (message == "")
                    {
                        _osdTimer.Stop();
                        mainWindow.OsdPopup.IsOpen = false;
                        return;
                    }
                    mainWindow.OsdPopup.Placement = PlacementMode.Top;
                    mainWindow.OsdText.Text = message;
                    mainWindow.OsdPopup.IsOpen = true;
                    _osdTimer.Stop();
                    _osdTimer.Interval = TimeSpan.FromMilliseconds(durationMs);
                    _osdTimer.Tick += (s, e) =>
                    {
                        mainWindow.OsdPopup.IsOpen = false;
                        _osdTimer.Stop();
                    };
                    _osdTimer.Start();
                });
            }
        }
    }
}
