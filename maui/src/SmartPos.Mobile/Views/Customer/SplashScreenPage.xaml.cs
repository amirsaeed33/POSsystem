using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.Views.Customer;

public partial class SplashScreenPage : ContentPage
{
    private readonly NavigationService _navigation;
    private bool _hasStartedTransition;

    public SplashScreenPage() : this(ServiceHelper.Get<NavigationService>())
    {
    }

    public SplashScreenPage(NavigationService navigation)
    {
        InitializeComponent();
        _navigation = navigation;
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
            // Initial animation state
            BrandContainer.Opacity = 0;
            BrandContainer.Scale = 0.90;

            // 1. Smooth Fade-in & subtle scale-up
            await Task.WhenAll(
                BrandContainer.FadeTo(1, 600, Easing.CubicOut),
                BrandContainer.ScaleTo(1.0, 600, Easing.CubicOut)
            );

            // 2. Async delay (total brand screen duration ~2.5s)
            await Task.Delay(1500);

            // 3. Smooth Fade-out before transition
            await Task.WhenAll(
                BrandContainer.FadeTo(0, 400, Easing.CubicIn),
                BrandContainer.ScaleTo(0.95, 400, Easing.CubicIn)
            );

            // 4. Navigate to destination
            if (Shell.Current != null)
            {
                await _navigation.GoToRoleHomeAsync();
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

