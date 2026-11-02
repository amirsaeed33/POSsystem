using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Customer;

public partial class CartPage : ContentPage
{
    private readonly CartCheckoutViewModel _vm;

    public CartPage() : this(ServiceHelper.Get<CartCheckoutViewModel>())
    {
    }

    public CartPage(CartCheckoutViewModel vm)
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
