using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Waiter;

public partial class WaiterMyOrdersPage : ContentPage
{
    private readonly WaiterMyOrdersViewModel _vm;

    public WaiterMyOrdersPage() : this(ServiceHelper.Get<WaiterMyOrdersViewModel>())
    {
    }

    public WaiterMyOrdersPage(WaiterMyOrdersViewModel vm)
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
}
