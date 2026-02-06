using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using System;

namespace ScePSX.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();

            try
            {
                using (var iconStream = AssetLoader.Open(new Uri("avares://ScePSX/001.ico")))
                {
                    desktop.MainWindow.Icon = new WindowIcon(iconStream);
                }
            }
            catch
            {
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}