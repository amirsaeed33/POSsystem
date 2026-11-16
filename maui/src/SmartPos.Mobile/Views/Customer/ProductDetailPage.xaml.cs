using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Customer;

public partial class ProductDetailPage : ContentPage
{
    public ProductDetailPage() : this(ServiceHelper.Get<ProductDetailViewModel>())
    {
    }

    public ProductDetailPage(ProductDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
