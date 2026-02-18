using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Widget;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Kotlin.IO;
using ScePSX.CdRom;

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
    public static string DocumentsPath;
    public static bool HasPermission;

    private const int R_OK = 4;
    [DllImport("libc")]
    private static extern int access(string path, int mode);

    public static void InitAssert()
    {
        var Dir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
        DownloadPath = Dir?.AbsolutePath ?? "/storage/emulated/0/Download";

        Dir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments);
        DocumentsPath = Dir?.AbsolutePath ?? "/storage/emulated/0/Documents";

        foreach (string dir in new[] { "SaveState", "Save", "Icons", "Cheats", "Shaders" })
        {
            Directory.CreateDirectory(Path.Combine(RootPath, dir));
        }

        string[] files = new[] {
                "ScePSX.ini",
                "lang.xml",
                "icon.png",
                //"gamedb.yaml",
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
            Console.WriteLine($"ShownDialog Error: {ex.Message}");
        }
    }

    public static async Task<string> ShowBiosDialog()
    {
        try
        {
            var tcs = new TaskCompletionSource<string>();

            AlertDialog.Builder builder = new AlertDialog.Builder(MainActivity);
            builder.SetTitle("需要BIOS");
            builder.SetMessage("为了运行游戏，需要PS1 BIOS文件。\n\n点击确定后，请在选择正确的BIOS文件。");

            builder.SetPositiveButton("确定", async (sender, args) =>
            {
                try
                {
                    string result = await SelectFile("BIOS", "Bios Files", new[] { "*.bin" });
                    tcs.SetResult(result);
                } catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            builder.SetNegativeButton("取消", (sender, args) =>
            {
                tcs.SetResult("");
            });

            builder.SetCancelable(false);

            AlertDialog? dialog = builder.Create();
            dialog?.Show();

            return await tcs.Task;
        } catch (Exception ex)
        {
            Console.WriteLine($"ShownDialog Error: {ex.Message}");
            return "";
        }
    }

    static CancellationTokenSource cts;

    public static async Task SearchBios(string dir, List<string> files)
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        await Task.Run(() => _SearchBios(dir, files, cts.Token), cts.Token);
    }

    private static void _SearchBios(string dir, List<string> files, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(dir))
            return;
        try
        {
            DirectoryInfo dirinfo = new DirectoryInfo(dir);
            foreach (var f in dirinfo.GetFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (f.Extension != ".bin" && f.Extension != ".BIN")
                    continue;
                if (f.Length > 520 * 1024)
                    continue;

                files.Add(f.FullName);
            }
            var subDirectories = dirinfo.GetDirectories();
            if (subDirectories.Length > 0)
            {
                foreach (var subDir in subDirectories)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _SearchBios(subDir.FullName, files, cancellationToken);
                }
            }
        } catch (OperationCanceledException)
        {
        }
    }

    public static async Task<string> SelectFile(string title, string filetype, string[] filetypes)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(MainView);
            if (topLevel == null)
            {
                return "";
            }
            if (topLevel.StorageProvider == null)
            {
                Console.WriteLine("Not StorageProvider");
                return "";
            }
            IStorageFolder? suggestedStartLocation = null;
            var lastPath = PSXHandler.ini.Read("main", "LastPath");
            if (!string.IsNullOrEmpty(lastPath) && Directory.Exists(lastPath))
            {
                suggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(lastPath);
            }
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType(filetype)
                    {
                        Patterns = filetypes,// new[] { "*.bin", "*.iso", "*.cue", "*.img", "*.exe" },
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                },
                SuggestedStartLocation = suggestedStartLocation
            });
            Console.WriteLine($"StorageProvider {files}");
            if (files == null || files.Count == 0)
            {
                return "";
            }
            var file = files[0];
            var filePath = file.Path.LocalPath.Replace("/document/raw:", "");
            //Console.WriteLine($"StorageProvider file {file} filepath {filePath} path {Path.GetFullPath(filePath)}");
            if (!File.Exists(filePath))
            {
                return "";
            }
            PSXHandler.ini.Write("main", "LastPath", Path.GetFullPath(filePath));
            return filePath;
        } catch (Exception ex)
        {
            Console.WriteLine($"Select Fail: {ex.Message}");
            return "";
        }
    }
}
