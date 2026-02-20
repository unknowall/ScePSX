using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
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
    public class CommonActivity : AppCompatActivity
    {
        public static UserControl userControl;

        public FrameLayout mainLayout;
        public AvaloniaView ViewHost;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            AHelper.CommonActivity = this;

            ViewHost = new AvaloniaView(this);
            ViewHost.Content = userControl;

            mainLayout = new FrameLayout(this);
            mainLayout.AddView(ViewHost, new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent));

            SetContentView(mainLayout);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            AHelper.OnActivityResult(requestCode, (int)resultCode, data);
        }

        public void UpdateContent(UserControl newControl)
        {
            userControl = newControl;
            if (ViewHost != null)
            {
                ViewHost.Content = userControl;
            }
        }

        public override void OnBackPressed()
        {
            base.OnBackPressed();
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
