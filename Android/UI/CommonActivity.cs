using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Avalonia.Android;
using Avalonia.Controls;

#pragma warning disable CS8602
#pragma warning disable CS8618

namespace ScePSX
{
    [Activity(Label = "ScePSX.CommonActivity",
              Theme = "@style/Theme.AppCompat.NoActionBar",
              LaunchMode = LaunchMode.SingleTop,
              ScreenOrientation = ScreenOrientation.Unspecified,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class CommonActivity : Activity
    {
        public static UserControl userControl;

        private FrameLayout mainLayout;
        private AvaloniaView ViewHost;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            ViewHost = new AvaloniaView(this);
            ViewHost.Content = userControl;

            mainLayout = new FrameLayout(this);
            mainLayout.AddView(ViewHost, new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent));

            SetContentView(mainLayout);
        }

        public void UpdateContent(UserControl newControl)
        {
            userControl = newControl;
            if (ViewHost != null)
            {
                ViewHost.Content = userControl;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnResume()
        {
            base.OnResume();
        }
    }
}
