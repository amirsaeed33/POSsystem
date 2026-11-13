using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Waiter;

public partial class WaiterOrderPage : ContentPage
{
    private readonly WaiterOrderViewModel _vm;

    public WaiterOrderPage() : this(ServiceHelper.Get<WaiterOrderViewModel>())
    {
    }

    public WaiterOrderPage(WaiterOrderViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_vm.AppearingCommand.CanExecute(null))
        {
            _vm.AppearingCommand.Execute(null);
        }
    }

    private void OnCustomerChanged(object? sender, EventArgs e)
    {
        if (sender is Picker picker && picker.SelectedItem is PosCustomerDto customer)
        {
            _vm.ChooseCustomer(customer);
        }
    }
}
