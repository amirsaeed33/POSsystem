using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;

namespace SmartPos.Mobile;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges =
        ConfigChanges.ScreenSize |
        ConfigChanges.Orientation |
        ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize |
        ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Draw edge-to-edge so the floating BubbleTabBar can sit on the physical bottom bezel.
        // MAUI 9 Android otherwise reserves the system navigation / gesture inset as empty gap.
        if (Window is null)
        {
            return;
        }

        WindowCompat.SetDecorFitsSystemWindows(Window, false);
        Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
        Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);

        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            Window.NavigationBarDividerColor = Android.Graphics.Color.Transparent;
        }

        var decor = Window.DecorView;
        decor.SystemUiFlags =
            SystemUiFlags.LayoutStable |
            SystemUiFlags.LayoutFullscreen |
            SystemUiFlags.LayoutHideNavigation;
    }
}
