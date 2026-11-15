namespace SmartPos.Mobile.Views.Customer;

public partial class SplashScreenPage : ContentPage
{
    private bool _hasStartedTransition;

    public SplashScreenPage()
    {
        InitializeComponent();
#if IOS
        Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.SetUseSafeArea(this, false);
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_hasStartedTransition)
        {
            return;
        }

        _hasStartedTransition = true;

        try
        {
            // Reset staggered entrance state
            EmblemBadge.Opacity = 0;
            EmblemBadge.Scale = 0.88;
            WordmarkLabel.Opacity = 0;
            WordmarkLabel.TranslationY = 12;
            GoldDivider.Opacity = 0;
            GoldDivider.TranslationY = 12;
            TaglineLabel.Opacity = 0;
            TaglineLabel.TranslationY = 12;
            BrandCenterStack.Opacity = 1;
            FooterStack.Opacity = 1;
            ProgressFill.WidthRequest = 36;

            // 1. Emblem fades in + scales up (~350ms)
            await Task.WhenAll(
                EmblemBadge.FadeTo(1, 350, Easing.CubicOut),
                EmblemBadge.ScaleTo(1.0, 350, Easing.CubicOut));

            // 2. Staggered wordmark / divider / tagline (~120ms apart, ~280ms each)
            var wordmarkTask = Task.WhenAll(
                WordmarkLabel.FadeTo(1, 280, Easing.CubicOut),
                WordmarkLabel.TranslateTo(0, 0, 280, Easing.CubicOut));
            await Task.Delay(120);
            var dividerTask = Task.WhenAll(
                GoldDivider.FadeTo(1, 280, Easing.CubicOut),
                GoldDivider.TranslateTo(0, 0, 280, Easing.CubicOut));
            await Task.Delay(120);
            var taglineTask = Task.WhenAll(
                TaglineLabel.FadeTo(1, 280, Easing.CubicOut),
                TaglineLabel.TranslateTo(0, 0, 280, Easing.CubicOut));

            // Progress bar fills while brand holds on screen
            var progressTcs = new TaskCompletionSource<bool>();
            var progressAnim = new Animation(v => ProgressFill.WidthRequest = v, 36, 120);
            progressAnim.Commit(
                ProgressFill,
                "SplashProgressFill",
                length: 1600,
                easing: Easing.SinInOut,
                finished: (_, _) => progressTcs.TrySetResult(true));

            // Hold so total on-brand time stays ~2.5s (entrance ~850ms + dwell)
            await Task.Delay(1500);
            await Task.WhenAll(wordmarkTask, dividerTask, taglineTask, progressTcs.Task);

            // 3. Smooth fade-out before transition
            await Task.WhenAll(
                BrandCenterStack.FadeTo(0, 400, Easing.CubicIn),
                FooterStack.FadeTo(0, 400, Easing.CubicIn),
                BrandCenterStack.ScaleTo(0.95, 400, Easing.CubicIn));

            // Cold start always opens the customer storefront (branch pick → home).
            // Staff/rider still reach their homes via login → GoToRoleHomeAsync.
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("//customer/home");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SplashScreenPage] Transition error: {ex}");
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("//customer/home");
            }
        }
    }
}
