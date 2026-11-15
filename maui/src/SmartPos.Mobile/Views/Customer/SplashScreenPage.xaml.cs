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
            // Show Aljannat brand immediately (native splash is only a matching solid color).
            EmblemBadge.Opacity = 1;
            EmblemBadge.Scale = 1.0;
            WordmarkLabel.Opacity = 1;
            WordmarkLabel.TranslationY = 0;
            GoldDivider.Opacity = 1;
            GoldDivider.TranslationY = 0;
            TaglineLabel.Opacity = 1;
            TaglineLabel.TranslationY = 0;
            BrandCenterStack.Opacity = 1;
            FooterStack.Opacity = 1;
            ProgressFill.WidthRequest = 36;

            // Light polish after first paint
            await EmblemBadge.ScaleTo(1.04, 220, Easing.CubicOut);
            await EmblemBadge.ScaleTo(1.0, 180, Easing.CubicIn);

            var wordmarkTask = Task.CompletedTask;
            var dividerTask = Task.CompletedTask;
            var taglineTask = Task.CompletedTask;

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
