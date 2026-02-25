using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Widget;
using AndroidX.AppCompat.App;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

#pragma warning disable CS8602
#pragma warning disable CS8604
#pragma warning disable CS8618

namespace ScePSX;

public static class AHelper
{
    public static MainActivity MainActivity;
    public static GameActivity GameActivity;
    public static CommonActivity CommonActivity;

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

        foreach (string dir in new[] { "SaveState", "Save", "Icons", "Cheats", "Shaders", "BIOS" })
        {
            Directory.CreateDirectory(Path.Combine(RootPath, dir));
        }

        string[] files = new[] {
                "ScePSX.ini",
                "lang.xml",
                "lang-pt.xml",
                "icon.png",
                "gamedb.yaml",
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
            builder.SetTitle("");
            builder.SetMessage(Translations.GetText("txtaccess"));

            builder.SetPositiveButton(Translations.GetText("txtok"), (sender, args) =>
            {
                NavToPermission();
            });

            builder.SetNegativeButton(Translations.GetText("txtcancel"), (sender, args) =>
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
            //Toast.MakeText(MainActivity, "请手动开启「存储] 或 [所有文件访问」权限", ToastLength.Long).Show();
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
            builder.SetTitle("BIOS");
            builder.SetMessage(Translations.GetText("txtbios"));

            builder.SetPositiveButton(Translations.GetText("txtok"), async (sender, args) =>
            {
                try
                {
                    string result = await SelectFile("BIOS");
                    tcs.SetResult(result);
                } catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            builder.SetNegativeButton(Translations.GetText("txtcancel"), (sender, args) =>
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

    public static async Task<int> ShowDialog(string title, string text, AppCompatActivity? activity = null)
    {
        if (activity == null)
            activity = MainActivity;
        try
        {
            var tcs = new TaskCompletionSource<int>();

            AlertDialog.Builder builder = new AlertDialog.Builder(activity);
            builder.SetTitle(title);
            builder.SetMessage(text);

            builder.SetPositiveButton(Translations.GetText("txtok"), (sender, args) =>
            {
                tcs.SetResult(0);
            });

            builder.SetNegativeButton(Translations.GetText("txtcancel"), (sender, args) =>
            {
                tcs.SetResult(1);
            });

            builder.SetCancelable(false);

            AlertDialog? dialog = builder.Create();
            dialog?.Show();

            return await tcs.Task;
        } catch (Exception ex)
        {
            Console.WriteLine($"ShownDialog Error: {ex.Message}");
            return -1;
        }
    }

    static CancellationTokenSource searchcts;

    public static async Task SearchBios(string dir, List<string> files)
    {
        searchcts?.Cancel();
        searchcts = new CancellationTokenSource();
        await Task.Run(() => _SearchBios(dir, files, searchcts.Token), searchcts.Token);
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
        } catch (System.OperationCanceledException)
        {
        }
    }

    public static async Task<string> LoadFile(string title, string filetype, string[] filetypes, TopLevel? topLevel = null)
    {
        if (topLevel == null)
            topLevel = TopLevel.GetTopLevel(MainView);
        try
        {
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
            }).ConfigureAwait(false);
            if (files == null || files.Count == 0)
            {
                return "";
            }
            var file = files[0];
            var filePath = GetFilePath(file.Path.LocalPath);
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

    public static async Task<string> SaveFile(string title, string SuggestedFileName, string DefaultExtension, string[] filetypes)
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
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = SuggestedFileName,
                DefaultExtension = DefaultExtension,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType(title) { Patterns = filetypes }
                }
            }).ConfigureAwait(false);
            var filePath = GetFilePath(file.Path.LocalPath);
            if (!File.Exists(filePath))
            {
                return "";
            }
            PSXHandler.ini.Write("main", "LastPath", Path.GetFullPath(filePath));
            return filePath;
        } catch (Exception ex)
        {
            Console.WriteLine($"SaveFile Fail: {ex.Message}");
            return "";
        }
    }

    public static TaskCompletionSource<string> selecttcs;

    public static Android.Net.Uri? GetLastPathUri()
    {
        var lastPath = PSXHandler.ini.Read("main", "LastPath");
        if (!string.IsNullOrEmpty(lastPath) && File.Exists(lastPath))
        {
            var file = new Java.IO.File(lastPath);
            if (file.Exists())
            {
                return Android.Net.Uri.FromFile(file);
            }
        }
        return null;
    }

    public static string GetFilePath(string filePath)
    {
        if (filePath.Contains("primary:"))
        {
            var subPath = filePath.Substring(filePath.IndexOf("primary:") + "primary:".Length);
            filePath = Path.Combine("/storage/emulated/0/", subPath);
            return filePath;
        } else if (filePath.Contains(":") && filePath.Contains("/document/"))
        {
            var idPart = filePath.Replace("/document/", "");
            var splitIdx = idPart.IndexOf(':');
            if (splitIdx > 0)
            {
                var volume = idPart.Substring(0, splitIdx);
                var subPath = idPart.Substring(splitIdx + 1);
                filePath = Path.Combine("/storage/", volume, subPath);
                return filePath;
            }
        }
        filePath = filePath.Replace("/document/raw:", "");
        return filePath;
    }

    public static async Task<string> SelectFile(string title, string suggestedName = "", bool isOpen = true, AppCompatActivity? activity = null)
    {
        selecttcs = new TaskCompletionSource<string>();

        if (activity == null)
        {
            activity = AHelper.MainActivity;
        }
        try
        {
            Intent intent;
            if (isOpen)
            {
                intent = new Intent(Intent.ActionOpenDocument);
            } else
            {
                intent = new Intent(Intent.ActionCreateDocument);
                if (!string.IsNullOrEmpty(suggestedName))
                {
                    intent.PutExtra(Intent.ExtraTitle, suggestedName);
                }
            }
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");

            intent.PutExtra(Intent.ExtraAllowMultiple, false);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                intent.PutExtra(DocumentsContract.ExtraInitialUri, GetLastPathUri());
            }

            activity.StartActivityForResult(Intent.CreateChooser(intent, title), 10001);

            var result = await selecttcs.Task.WaitAsync(TimeSpan.FromMinutes(2));

            if (!string.IsNullOrEmpty(result))
            {
                PSXHandler.ini.Write("main", "LastPath", Path.GetFullPath(result));
            }

            return result;
        } catch (Exception ex)
        {
            Console.WriteLine($"SelectFile: {ex.Message}");
            return "";
        }
    }

    public static void OnActivityResult(int requestCode, int resultCode, Intent? data)
    {
        if (requestCode != 10001 || selecttcs == null)
            return;
        try
        {
            if (resultCode == -1 && data?.Data != null)
            {
                var uri = data.Data;
                var filePath = GetFilePath(uri.Path);
                //Console.WriteLine($"filePath: {filePath}");
                selecttcs.TrySetResult(filePath ?? "");
            } else
            {
                selecttcs.TrySetResult("");
            }
        } catch (Exception ex)
        {
            Console.WriteLine($"OnActivityResult: {ex.Message}");
            selecttcs.TrySetResult("");
        }
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
                _currentToast = Toast.MakeText(Android.App.Application.Context, message, duration);
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
