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
        base.OnCreate(savedInstanceState);

        SetTheme(Resource.Style.Theme_AppCompat_NoActionBar);

        if (Window != null)
        {
            Window.AddFlags(Android.Views.WindowManagerFlags.Fullscreen);
#pragma warning disable CS0618
            Window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)(
                Android.Views.SystemUiFlags.Fullscreen |
                Android.Views.SystemUiFlags.HideNavigation |
                Android.Views.SystemUiFlags.ImmersiveSticky);
#pragma warning restore CS0618
        }
    }

    public override void OnBackPressed()
    {
        base.OnBackPressed();
    }
}
