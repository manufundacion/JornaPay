using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Util;
using AndroidX.AppCompat.App;

namespace JornaPay
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                               ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {

        public override Resources Resources
        {
            get
            {
                var res = base.Resources;
                var config = new Configuration(res.Configuration);

                if (config.FontScale != 1.0f)
                {
                    config.FontScale = 1.0f;
                }

                var metrics = res.DisplayMetrics;

                float targetDensity = 3.0f;
                metrics.Density = targetDensity;
                metrics.ScaledDensity = targetDensity;

                // Conversión explícita al enum
                metrics.DensityDpi = (DisplayMetricsDensity)(int)(targetDensity * 160);

                Context context = CreateConfigurationContext(config);
                context.Resources.DisplayMetrics.SetTo(metrics);

                return context.Resources;
            }
        }

    }
}
