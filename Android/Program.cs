using System;
using Avalonia;

namespace ScePSX;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UseAndroid()
            .LogToTrace();
}
