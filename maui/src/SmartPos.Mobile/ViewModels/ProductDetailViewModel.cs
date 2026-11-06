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
    private string imageUrl = "burger_photo.png";

    [ObservableProperty]
    private string selectedTab = "Burger";

    [ObservableProperty]
    private bool isCouponClaimed;

    [ObservableProperty]
    private bool isFavorite;

    public bool IsBurgerTabSelected => string.Equals(SelectedTab, "Burger", StringComparison.OrdinalIgnoreCase);
    public bool IsComboTabSelected => string.Equals(SelectedTab, "Anzay's Combo", StringComparison.OrdinalIgnoreCase);

    public string RestaurantTitle => Product?.Name ?? "Burger Anzay";
    public string RestaurantSubtitle => "American, Fast food, Burger";
    public string LocationAddress => "W. Gray St, Utica, Pennsylvania";
    public string RatingScore => "4.3";
    public string ReviewCount => "1K+";
    public string DeliveryEstimate => "15min";

    public string CouponButtonText => IsCouponClaimed ? "Claimed ✓" : "Claim Now";
    public string FavoriteIcon => IsFavorite ? "❤️" : "🤍";

    public ProductDetailViewModel(OnlineOrderService orders, SessionService session, CartService cart, AbpApiClient api)
    {
        _orders = orders;
        _session = session;
        _cart = cart;
        _api = api;
        Title = "Burger Anzay";
    }

    partial void OnProductIdChanged(int value) => _ = LoadAsync();

    partial void OnSelectedTabChanged(string value)
    {
        OnPropertyChanged(nameof(IsBurgerTabSelected));
        OnPropertyChanged(nameof(IsComboTabSelected));
    }

    partial void OnIsCouponClaimedChanged(bool value) => OnPropertyChanged(nameof(CouponButtonText));
    partial void OnIsFavoriteChanged(bool value) => OnPropertyChanged(nameof(FavoriteIcon));

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (ProductId <= 0)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            var p = await _orders.GetProductAsync(ProductId, _session.BranchId);
            if (p != null)
            {
                Product = p;
                Title = Product.Name ?? "Burger Anzay";
                if (!string.IsNullOrWhiteSpace(Product.ImagePath))
                {
                    ImageUrl = _api.ResolveImageUrl(Product.ImagePath);
                }
                OnPropertyChanged(nameof(RestaurantTitle));
            }
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
    private async Task GoBackAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        try
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Title = RestaurantTitle,
                Text = $"Check out {RestaurantTitle} on MakanGes! Order fresh burgers and fast delivery."
            });
        }
        catch
        {
        }
    }

    [RelayCommand]
    private void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
    }

    [RelayCommand]
    private async Task ClaimCouponAsync()
    {
        if (IsCouponClaimed)
        {
            return;
        }

        IsCouponClaimed = true;
        if (Shell.Current != null)
        {
            await Shell.Current.DisplayAlert("Coupon Claimed!", "Free cheese coupon has been applied to your order.", "OK");
        }
    }

    [RelayCommand]
    private void SelectMenuTab(string tabName)
    {
        if (!string.IsNullOrWhiteSpace(tabName))
        {
            SelectedTab = tabName;
        }
    }

    [RelayCommand]
    private async Task AddToCartAsync()
    {
        var targetProduct = Product ?? new OnlineProductDto
        {
            Id = ProductId > 0 ? ProductId : 1,
            Name = "Cheese Burger",
            Price = 15m,
            Description = "Made with a juicy beef patty, melted cheddar cheese, crisp lettuce, and signature Anzay sauce."
        };

        _cart.AddOrIncrement(targetProduct, Quantity);
        if (Shell.Current != null)
        {
            await Shell.Current.DisplayAlert("Cart", $"{Quantity}x {targetProduct.Name} added to cart.", "OK");
            await Shell.Current.GoToAsync("//customer/cart");
        }
    }

    [RelayCommand]
    private void AddSpecificItemToCart(string itemName)
    {
        decimal price = 15m;
        if (string.Equals(itemName, "Double Beef Burger", StringComparison.OrdinalIgnoreCase))
        {
            price = 18m;
        }
        else if (string.Equals(itemName, "Crispy Chicken Burger", StringComparison.OrdinalIgnoreCase))
        {
            price = 14m;
        }
        else if (string.Equals(itemName, "Anzay Ultimate Combo", StringComparison.OrdinalIgnoreCase))
        {
            price = 22m;
        }

        var item = new OnlineProductDto
        {
            Id = ProductId > 0 ? ProductId : 1,
            Name = itemName,
            Price = price
        };

        _cart.AddOrIncrement(item, 1);
        if (Shell.Current != null)
        {
            Shell.Current.DisplayAlert("Added to Cart", $"{itemName} added to your order.", "OK");
        }
    }
}
