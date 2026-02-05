using Avalonia.Controls;
using Avalonia.Platform;

namespace ScePSX.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

    }

    private void LoadDisk_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void SwapDisk_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void CloseRomMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void SearchMnu_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
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

public class GLRenderHost : NativeControlHost
{
    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        return base.CreateNativeControlCore(parent);
    }
}