using System;
using System.Runtime.InteropServices;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

#pragma warning disable CS8602
#pragma warning disable CS8618

namespace ScePSX
{
    [Activity(Label = "ScePSX.GameScreenActivity",
              Theme = "@style/Theme.AppCompat.NoActionBar",
              LaunchMode = LaunchMode.SingleTop,
              ScreenOrientation = ScreenOrientation.SensorLandscape)]
    public class GameActivity : Activity
    {
        public static Action<IntPtr, int, int>? OnActivityCreated;
        public static Action<int, int>? OnActivitySizeChanged;
        public static Action? OnActivityHandleDestroyed;
        public static IntPtr AnativeWindowPtr;
        private bool _handleDelivered = false;
        public FrameLayout mainLayout;

        [DllImport("android")]
        private static extern IntPtr ANativeWindow_fromSurface(IntPtr env, IntPtr surface);

        [DllImport("android")]
        private static extern void ANativeWindow_release(IntPtr window);

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            SetTheme(Resource.Style.Theme_AppCompat_NoActionBar);

            base.OnCreate(savedInstanceState);

            Window?.AddFlags(WindowManagerFlags.KeepScreenOn);

            AHelper.GameActivity = this;

            mainLayout = new FrameLayout(this);

            var surfaceView = new SurfaceView(this);

            surfaceView.Holder.AddCallback(new SurfaceHolderCallback(this));

            mainLayout.AddView(surfaceView, new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent));

            var overlayView = new VirtualGamepadOverlay(this);

            AHelper.GamepadOverlay = overlayView;

            mainLayout.AddView(overlayView, new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent));

            AHelper.androidInput = new AndroidInputHandler(this);

            SetContentView(mainLayout);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_handleDelivered)
            {
                _handleDelivered = false;
                OnActivityHandleDestroyed?.Invoke();
                if (AnativeWindowPtr != IntPtr.Zero)
                {
                    ANativeWindow_release(AnativeWindowPtr);
                    AnativeWindowPtr = IntPtr.Zero;
                }
                AHelper.GamepadOverlay = null;
                AHelper.androidInput = null;
            }
        }

        private class SurfaceHolderCallback : Java.Lang.Object, ISurfaceHolderCallback
        {
            private readonly GameActivity _activity;

            public SurfaceHolderCallback(GameActivity activity)
            {
                _activity = activity;
            }

            public void SurfaceCreated(ISurfaceHolder holder)
            {
                _activity.RunOnUiThread(() => _activity.SetFullScreen());
            }

            public void SurfaceChanged(ISurfaceHolder holder, global::Android.Graphics.Format format, int width, int height)
            {
                IntPtr newWindowPtr = ANativeWindow_fromSurface(JNIEnv.Handle, holder.Surface.Handle);

                if (!_activity._handleDelivered)
                {
                    AnativeWindowPtr = newWindowPtr;
                    _activity._handleDelivered = true;
                    OnActivityCreated?.Invoke(AnativeWindowPtr, width, height);
                } else
                    OnActivitySizeChanged?.Invoke(width, height);
            }

            public void SurfaceDestroyed(ISurfaceHolder holder)
            {
                if (_activity._handleDelivered)
                {
                    _activity._handleDelivered = false;
                    OnActivityHandleDestroyed?.Invoke();
                    if (AnativeWindowPtr != IntPtr.Zero)
                    {
                        ANativeWindow_release(AnativeWindowPtr);
                        AnativeWindowPtr = IntPtr.Zero;
                    }
                }
            }
        }

        private void SetFullScreen()
        {
            if (Window?.DecorView == null)
                return;

            Window.DecorView.SystemUiFlags = SystemUiFlags.Fullscreen
                                    | SystemUiFlags.HideNavigation
                                    | SystemUiFlags.ImmersiveSticky
                                    | SystemUiFlags.LayoutFullscreen
                                    | SystemUiFlags.LayoutHideNavigation;
            Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
        }

        protected override void OnResume()
        {
            base.OnResume();
            SetFullScreen();
        }
    }
}
