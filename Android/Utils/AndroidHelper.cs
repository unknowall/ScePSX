using System;
using System.IO;
using System.Runtime.InteropServices;
using Android.App;
using Android.Content;
using Android.Widget;

#pragma warning disable CS8602
#pragma warning disable CS8604
#pragma warning disable CS8618

namespace ScePSX;

public static class AHelper
{
    public static Activity MainActivity;
    public static Activity GameActivity;

    public static MainView MainView;

    public static VirtualGamepadOverlay? GamepadOverlay;
    public static AndroidInputHandler? androidInput;

    public static string RootPath = Android.App.Application.Context.FilesDir.AbsolutePath;
    public static string DownloadPath;
    public static bool HasPermission;

    private const int R_OK = 4;
    [DllImport("libc")]
    private static extern int access(string path, int mode);

    public static void InitAssert()
    {
        var downloadDir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
        DownloadPath = downloadDir?.AbsolutePath ?? "/storage/emulated/0/Download";

        foreach (string dir in new[] { "SaveState", "Save", "Icons", "Cheats", "Shaders" })
        {
            Directory.CreateDirectory(Path.Combine(RootPath, dir));
        }

        string[] files = new[] {
                "ScePSX.ini",
                "lang.xml",
                "Shaders/draw.frag.spv",
                "Shaders/draw.vert.spv",
                "Shaders/out.frag.spv",
                "Shaders/out.vert.spv"
            };

        foreach (string file in files)
        {
            string targetPath = Path.Combine(RootPath, file);

            if (!File.Exists(targetPath))
            {
                using (var assetStream = Android.App.Application.Context.Assets.Open(file))
                using (var fileStream = File.Create(targetPath))
                {
                    assetStream.CopyTo(fileStream);
                }
            }
        }
    }

    public static void CheckPermission()
    {
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
        {
            if (!Android.OS.Environment.IsExternalStorageManager)
            {
                ShowPermissionDialog();
            } else
                HasPermission = true;
        } else
        if (access(DownloadPath, R_OK) != 0)
        {
            ShowPermissionDialog();
        } else
            HasPermission = true;
    }

    public static void ShowPermissionDialog()
    {
        try
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(MainActivity);
            builder.SetTitle("需要存储权限");
            builder.SetMessage("为了读取游戏ROM文件，需要授予「存储] 或 [所有文件访问」权限。\n\n点击确定后，请在设置页面开启权限。");

            builder.SetPositiveButton("去设置", (sender, args) =>
            {
                NavToPermission();
            });

            builder.SetNegativeButton("取消", (sender, args) =>
            {
                HasPermission = false;
            });

            builder.SetCancelable(false);

            AlertDialog? dialog = builder.Create();
            dialog?.Show();
        } catch (Exception ex)
        {
            Console.WriteLine($"ShowPermissionDialog Error: {ex.Message}");
            NavToPermission();
        }
    }

    public static void NavToPermission()
    {
        try
        {
            Intent intent;

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
            {
                intent = new Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
            } else
            {
                intent = new Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
            }
            intent.SetData(Android.Net.Uri.Parse("package:" + MainActivity.PackageName));
            intent.SetFlags(ActivityFlags.NewTask);
            MainActivity.StartActivity(intent);

            Toast.MakeText(MainActivity, "请手动开启「存储] 或 [所有文件访问」权限", ToastLength.Long).Show();
        } catch (Exception ex)
        {
            Console.WriteLine($"NavToPermission Error: {ex.Message}");
        }
    }
}
