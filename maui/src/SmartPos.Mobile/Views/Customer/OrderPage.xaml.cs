using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Customer;

[QueryProperty(nameof(CategoryFilter), "category")]
public partial class OrderPage : ContentPage
{
    private readonly CustomerOrderViewModel _vm;
    private int _categoryScrollIndex;

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

    private void OnCategoryScrollLeft(object? sender, TappedEventArgs e) =>
        ScrollCategoriesBy(-2);

    private void OnCategoryScrollRight(object? sender, TappedEventArgs e) =>
        ScrollCategoriesBy(2);

    private void ScrollCategoriesBy(int delta)
    {
        var count = _vm.Categories.Count;
        if (count == 0 || CategoriesCollectionView == null)
        {
            return;
        }

        _categoryScrollIndex = Math.Clamp(_categoryScrollIndex + delta, 0, count - 1);
        CategoriesCollectionView.ScrollTo(
            _categoryScrollIndex,
            position: ScrollToPosition.MakeVisible,
            animate: true);
    }
}

