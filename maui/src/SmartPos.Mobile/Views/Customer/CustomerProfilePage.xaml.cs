using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Customer;

public partial class CustomerProfilePage : ContentPage
{
    private readonly CustomerProfileViewModel _vm;

    public CustomerProfilePage() : this(ServiceHelper.Get<CustomerProfileViewModel>())
    {
    }

    public CustomerProfilePage(CustomerProfileViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.AppearingCommand.Execute(null);
    }
}

