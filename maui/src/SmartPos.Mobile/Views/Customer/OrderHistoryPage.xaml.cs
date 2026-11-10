using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Customer;

public partial class OrderHistoryPage : ContentPage
{
    private readonly OrderHistoryViewModel _vm;

    public OrderHistoryPage(OrderHistoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_vm.AppearingCommand.CanExecute(null))
        {
            await _vm.AppearingCommand.ExecuteAsync(null);
        }
    }
}

