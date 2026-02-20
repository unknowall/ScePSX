using Android.App;
using Android.Content;
using Android.Content.PM;
using Avalonia.Android;

#pragma warning disable CS8602

namespace ScePSX;

[Activity(
    Label = "ScePSX",
    Theme = "@style/SplashTheme",
    Icon = "@drawable/icon",
    MainLauncher = true,
    Exported = true,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetTheme(Resource.Style.Theme_AppCompat_NoActionBar);
        AHelper.MainActivity = this;
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        AHelper.OnActivityResult(requestCode, (int)resultCode, data);
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
