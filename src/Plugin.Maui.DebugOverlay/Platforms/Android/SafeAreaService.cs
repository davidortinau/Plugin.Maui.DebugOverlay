using Android.OS;
using Android.Views;

namespace Plugin.Maui.DebugOverlay.Platforms
{
    public static class SafeAreaService
    { 
        public static float GetTopSafeAreaInset()
        {
            float topInsetDp = 0f;
            var activity = Platform.CurrentActivity;
            var decorView = activity?.Window?.DecorView;

            if (decorView == null)
                return 0f;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R) // Android 11+
            {
                var insets = decorView.RootWindowInsets;
                if (insets != null)
                {
                    var statusInsets = insets.GetInsetsIgnoringVisibility(WindowInsets.Type.StatusBars());
                    topInsetDp = statusInsets.Top / decorView.Resources.DisplayMetrics.Density;
                }
            }
            else if (Build.VERSION.SdkInt >= BuildVersionCodes.M) // Android 6–10
            {
                var insets = decorView.RootWindowInsets;
                if (insets != null)
                {
                    topInsetDp = insets.StableInsetTop / decorView.Resources.DisplayMetrics.Density;
                }
            }
            else
            {
#pragma warning disable CS0618
                // fallback : approx 24dp for status bar
                topInsetDp = 24f;
#pragma warning restore CS0618
            }

            return topInsetDp;
        }


    }
} 