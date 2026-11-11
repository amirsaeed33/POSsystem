using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Customer;

[QueryProperty(nameof(CategoryFilter), "category")]
public partial class OrderPage : ContentPage
{
    private readonly CustomerOrderViewModel _vm;

    public string? CategoryFilter
    {
        get => _vm.CategoryFilter;
        set => _vm.CategoryFilter = value;
    }

    public OrderPage() : this(ServiceHelper.Get<CustomerOrderViewModel>())
    {
    }

    public OrderPage(CustomerOrderViewModel vm)
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
}

