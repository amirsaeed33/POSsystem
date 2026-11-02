using SmartPos.Mobile.Controls;
using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Auth;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _vm;

    public LoginPage() : this(ServiceHelper.Get<LoginViewModel>())
    {
    }

    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Shell.SetTabBarIsVisible(this, false);
        _vm.RefreshSessionUi();
        ConfigureBubbleNavForCurrentRole();
        await PlayEnterAnimationsAsync();
    }

    private void ConfigureBubbleNavForCurrentRole()
    {
        var loc = Shell.Current?.CurrentState?.Location?.OriginalString ?? string.Empty;
        if (loc.Contains("/rider/", StringComparison.OrdinalIgnoreCase))
        {
            BottomNav.Mode = BubbleTabMode.Rider;
            BottomNav.SelectedIndex = 2;
        }
        else if (loc.Contains("/waiter/", StringComparison.OrdinalIgnoreCase))
        {
            BottomNav.Mode = BubbleTabMode.Waiter;
            BottomNav.SelectedIndex = 3;
        }
        else if (loc.Contains("/admin/", StringComparison.OrdinalIgnoreCase))
        {
            BottomNav.Mode = BubbleTabMode.Admin;
            BottomNav.SelectedIndex = 1;
        }
        else
        {
            BottomNav.Mode = BubbleTabMode.Customer;
            BottomNav.SelectedIndex = 3;
        }
    }

    private async Task PlayEnterAnimationsAsync()
    {
        BrandHeader.Opacity = 0;
        BrandHeader.TranslationY = -16;
        LoginCard.Opacity = 0;
        LoginCard.TranslationY = 28;

        var brand = BrandHeader.FadeTo(1, 420, Easing.CubicOut);
        var brandSlide = BrandHeader.TranslateTo(0, 0, 420, Easing.CubicOut);
        await Task.WhenAll(brand, brandSlide);

        var card = LoginCard.FadeTo(1, 480, Easing.CubicOut);
        var cardSlide = LoginCard.TranslateTo(0, 0, 480, Easing.CubicOut);
        await Task.WhenAll(card, cardSlide);
    }
}
