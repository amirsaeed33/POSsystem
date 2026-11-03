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

        if (Window is null)
        {
            return;
        }

        // Respect system insets so the status bar (battery / signal / clock) stays visible,
        // matching apps like WhatsApp. Content starts below the status bar.
        WindowCompat.SetDecorFitsSystemWindows(Window, true);

        var brandGreen = Android.Graphics.Color.ParseColor("#0F6B4C");
        Window.SetStatusBarColor(brandGreen);
        Window.SetNavigationBarColor(Android.Graphics.Color.White);

        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            Window.NavigationBarDividerColor = Android.Graphics.Color.ParseColor("#E4EBE6");
        }

        // Light icons on the green status bar.
        var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
        if (controller != null)
        {
            controller.AppearanceLightStatusBars = false;
            controller.AppearanceLightNavigationBars = true;
        }

        // Do not use LayoutFullscreen — that draws under the status bar and hides battery/signal.
        Window.DecorView.SystemUiFlags = SystemUiFlags.Visible;
    }
}
