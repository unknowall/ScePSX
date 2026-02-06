using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ScePSX.Core.GPU;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ScePSX.UI;

public partial class MainWindow : Window
{
    public static string version = "ScePSX v0.1.8.0";

    public PSXHandler PSX;

    int StatusDelay, StateSlot, IRscale;
    bool AutoIR = true;

    DispatcherTimer timer;

    public MainWindow()
    {
        InitializeComponent();

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
        if (!Path.Exists("./Icons"))
            Directory.CreateDirectory("./Icons");

        RomList.OnDbClick += RomListSeelected;

        PSX = new PSXHandler();

        this.AddHandler(KeyDownEvent, MainWindow_KeyDown, RoutingStrategies.Tunnel);
        this.AddHandler(KeyUpEvent, MainWindow_KeyUp, RoutingStrategies.Tunnel);

        this.Closing += MainWindow_Closing;

        timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += Timer_Elapsed;
        timer.Start();
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
    }

    private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        string PosStr = $"{this.Position.X}|{this.Position.Y}|{(int)this.Width}|{(int)this.Height}";
        PSXHandler.ini.Write("Main", "FromPos", PosStr);
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
        }
        else
        {
            StatusLabel2.Text = $"F3+ F4- {Translations.GetText("SaveSlot")} [{StateSlot}]";
            StatusLabel3.Text = $"F9[{(PSX.KeyFirst ? Translations.GetText("KeyFirst") : Translations.GetText("JoyFirst"))}]";
            StatusLabel4.Text = $"F10[{(PSX.isAnalog ? Translations.GetText("Analog") : Translations.GetText("Digital"))}]";
        }

        if (PSX.Core.Pauseed)
        {
            //SimpleOSD.Show(Render._currentRenderer as UserControl, " ¨„¨„");
            StatusLabel2.Text = Translations.GetText("Pause");
            CleanStatus();
        }

        if (AutoIR)
        {
            if (this.ClientSize.Width >= 320 && this.ClientSize.Height >= 240)
                IRscale = 2;
            if (this.ClientSize.Width >= 800 && this.ClientSize.Height >= 600)
                IRscale = 3;
            if (this.ClientSize.Width >= 1280 && this.ClientSize.Height >= 800)
                IRscale = 4;
            if (this.ClientSize.Width >= 1920 && this.ClientSize.Height >= 1080)
                IRscale = 5;
        }

        if (PSX.Core.GPU.type == GPUType.OpenGL && AutoIR)
        {
            (PSX.Core.GPU as OpenglGPU).IRScale = IRscale;
        }
        if (PSX.Core.GPU.type == GPUType.Vulkan && AutoIR)
        {
            (PSX.Core.GPU as VulkanGPU).IRScale = IRscale;
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

        StatusLabel5.Text = $"{str_pgxp}{rendername} {PSX.Msaa}xMSAA {IRscale}xIR";

        StatusLabel6.Text = $"{(PSX.ScaleParam.scale > 0 ? PSX.ScaleParam.mode.ToString() : "")} {scalew}*{scaleh}";

        StatusLabel7.Text = $"FPS {PSX.currentFps:F1}";
    }

    private void MainWindow_KeyUp(object? sender, KeyEventArgs e)
    {
        PSX.KeyPress(e.Key, false);

        if (e.Key == Key.Tab) e.Handled = true;
    }

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        PSX.KeyPress(e.Key, true);

        if (e.Key == Key.Tab) e.Handled = true;
    }

    private void RomListSeelected(GameInfo info)
    {
        RomListView.IsVisible = false;
        RenderHost.IsVisible = true;

        PSX.GameName = info.Name;
        PSX.LoadGame(info.fullName, RenderHost, info.ID);
    }

    private async Task<string> SelectFile()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return "";
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
        if (files == null || files.Count == 0) return "";
        var filePath = files[0].Path.LocalPath;
        if (!File.Exists(filePath)) return "";
        PSXHandler.ini.Write("main", "LastPath", Path.GetDirectoryName(filePath));
        return filePath;
    }

    private async Task<string> SelectPath()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return "";
        var paths = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Games Path"
        });
        if (paths == null || paths.Count == 0) return "";
        var filePath = paths[0].Path.LocalPath;
        if (!Path.Exists(filePath)) return "";
        return filePath;
    }

    private async void LoadDisk_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var File = await SelectFile();
        if (File == "") return;

        PSX.LoadGame(File, RenderHost);

        RomListView.IsVisible = false;
        RenderHost.IsVisible = true;
    }

    private async void SwapDisk_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!PSX.isRun()) return;
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
        StatusDelay = 3;
    }

    private void CloseRomMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PSX.Stop();

        RomListView.FillByini();

        RomListView.IsVisible = true;
        RenderHost.IsVisible = false;

        CleanStatus();
    }

    private async void SearchMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (PSX.isRun()) return;
        var path = await SelectPath();
        if (path == "") return;
        CleanStatus();
        StatusLabel1.Text = $"Searching {path}";
        await RomListView.SearchDir(path);
        CleanStatus();
    }

    private void SysSetMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void KeyMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void CheatCode_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void MnuDebug_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void MnuPause_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void SdlRenderMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void OpenGLRenderMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void VulkanRenderMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void UpScale_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void DownScale_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void fullScreenF2_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void NetPlaySetMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void gitHubMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void supportKoficomMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void supportWeChatMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void AboutMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }
}