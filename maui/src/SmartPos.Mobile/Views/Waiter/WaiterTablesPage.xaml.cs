using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Waiter;

public partial class WaiterTablesPage : ContentPage
{
    private readonly WaiterTablesViewModel _vm;

    public WaiterTablesPage() : this(ServiceHelper.Get<WaiterTablesViewModel>())
    {
    }

    public WaiterTablesPage(WaiterTablesViewModel vm)
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
