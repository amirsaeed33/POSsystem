using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Waiter;

public partial class WaiterCartPage : ContentPage
{
    private readonly WaiterOrderViewModel _vm;

    public WaiterCartPage() : this(ServiceHelper.Get<WaiterOrderViewModel>())
    {
    }

    public WaiterCartPage(WaiterOrderViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.RefreshCartDisplay();
    }
}
