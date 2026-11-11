using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Customer;

public partial class TrackOrderPage : ContentPage
{
    private readonly TrackOrderViewModel _vm;

    public TrackOrderPage() : this(ServiceHelper.Get<TrackOrderViewModel>())
    {
    }

    public TrackOrderPage(TrackOrderViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Shell.SetTabBarIsVisible(this, false);
        await _vm.AppearingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Disappearing();
    }
}
