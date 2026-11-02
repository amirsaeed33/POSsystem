using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

[QueryProperty(nameof(ProductId), "productId")]
public partial class ProductDetailViewModel : BaseViewModel
{
    private readonly OnlineOrderService _orders;
    private readonly SessionService _session;
    private readonly CartService _cart;
    private readonly AbpApiClient _api;

    [ObservableProperty]
    private int productId;

    [ObservableProperty]
    private OnlineProductDto? product;

    [ObservableProperty]
    private decimal quantity = 1;

    [ObservableProperty]
    private string imageUrl = string.Empty;

    public ProductDetailViewModel(OnlineOrderService orders, SessionService session, CartService cart, AbpApiClient api)
    {
        _orders = orders;
        _session = session;
        _cart = cart;
        _api = api;
        Title = "Product";
    }

    partial void OnProductIdChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (ProductId <= 0)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            Product = await _orders.GetProductAsync(ProductId, _session.BranchId);
            Title = Product.Name ?? "Product";
            ImageUrl = _api.ResolveImageUrl(Product.ImagePath);
        });
    }

    [RelayCommand]
    private void Increase() => Quantity += 1;

    [RelayCommand]
    private void Decrease()
    {
        if (Quantity > 1)
        {
            Quantity -= 1;
        }
    }

    [RelayCommand]
    private async Task AddToCartAsync()
    {
        if (Product == null)
        {
            return;
        }

        _cart.AddOrIncrement(Product, Quantity);
        await Shell.Current.DisplayAlert("Cart", "Added to cart.", "OK");
        await Shell.Current.GoToAsync("..");
    }
}
