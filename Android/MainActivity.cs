using Android.App;
using Android.Content.PM;
using Android.Views;
using Avalonia.Android;
using Avalonia.Controls;

namespace ScePSX;

[Activity(
    Label = "ScePSX",
    Theme = "@style/SplashTheme",
    Icon = "@drawable/icon",
    MainLauncher = true,
    Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        SetTheme(Resource.Style.Theme_AppCompat_NoActionBar);

        base.OnCreate(savedInstanceState);

        AHelper.MainActivity = this;
    }

    public override void OnBackPressed()
    {
        base.OnBackPressed();
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        if (requestCode == 1001)
        {
            bool isGranted = grantResults.Length > 0 && grantResults[0] == Android.Content.PM.Permission.Granted;
            if (AHelper.MainView != null)
                AHelper.HasPermission = isGranted;
        }
    }
}
