using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Customer;

public partial class CustomerHomePage : ContentPage
{
    private readonly CustomerHomeViewModel _vm;

    public CustomerHomePage() : this(ServiceHelper.Get<CustomerHomeViewModel>())
    {
    }

    public CustomerHomePage(CustomerHomeViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Shell.SetTabBarIsVisible(this, false);
        if (_vm.AppearingCommand.CanExecute(null))
        {
            _vm.AppearingCommand.Execute(null);
        }
    }

    protected override void OnDisappearing()
    {
        _vm.OnPageDisappearing();
        base.OnDisappearing();
    }
}
