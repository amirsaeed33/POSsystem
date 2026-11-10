using System.Collections.ObjectModel;
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
    private readonly FavoritesService _favorites;

    [ObservableProperty]
    private int productId;

    [ObservableProperty]
    private OnlineProductDto? product;

    [ObservableProperty]
    private decimal quantity = 1;

    [ObservableProperty]
    private string imageUrl = string.Empty;

    [ObservableProperty]
    private bool isFavorite;

    [ObservableProperty]
    private bool hasModifiers;

    [ObservableProperty]
    private decimal totalPrice;

    [ObservableProperty]
    private string specialInstructions = string.Empty;

    [ObservableProperty]
    private int cartCount;

    [ObservableProperty]
    private string activeBranchName = string.Empty;

    public ObservableCollection<ModifierGroupItem> ModifierGroups { get; } = new();

    public string ProductName => Product?.Name ?? "Item Details";
    public string CategoryName => !string.IsNullOrWhiteSpace(Product?.CategoryName) ? Product.CategoryName : "Fresh Menu";
    public string Description => !string.IsNullOrWhiteSpace(Product?.Description) ? Product.Description : "Freshly prepared to order with authentic, high-quality ingredients.";
    public string BasePriceDisplay => Product != null ? $"Rs. {Product.Price:0}" : string.Empty;
    public string TotalPriceDisplay => $"Rs. {TotalPrice:0}";
    public string TotalButtonText => $"Add to Cart • Rs. {TotalPrice:0}";
    public bool InStock => Product?.InStock ?? true;
    public string StockBadgeText => InStock ? "In Stock" : "Out of Stock";
    public Color StockBadgeBg => InStock ? Color.FromArgb("#D1FAE5") : Color.FromArgb("#FEE2E2");
    public Color StockBadgeTextColor => InStock ? Color.FromArgb("#047857") : Color.FromArgb("#B91C1C");
    public bool HasDiscount => Product != null && Product.HasOriginalPrice;
    public string DiscountBadgeText => Product?.DealDiscountBadge ?? "DEAL";
    public string OriginalPriceDisplay => Product?.OriginalPriceDisplay ?? string.Empty;
    public string FavoriteGlyph => IsFavorite ? "❤️" : "🤍";

    public ProductDetailViewModel(
        OnlineOrderService orders,
        SessionService session,
        CartService cart,
        FavoritesService favorites)
    {
        _orders = orders;
        _session = session;
        _cart = cart;
        _favorites = favorites;
        Title = "Product Details";
        CartCount = _cart.ItemCount;

        _favorites.FavoritesChanged += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (ProductId > 0)
                {
                    IsFavorite = _favorites.IsFavorite(ProductId);
                    OnPropertyChanged(nameof(FavoriteGlyph));
                }
            });
        };

        _cart.CartChanged += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CartCount = _cart.ItemCount;
            });
        };
    }

    partial void OnProductIdChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
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
                Title = Product.Name ?? "Product Details";
                ImageUrl = !string.IsNullOrWhiteSpace(Product.ImagePath)
                    ? _orders.ResolveImageUrl(Product.ImagePath)
                    : string.Empty;

                IsFavorite = _favorites.IsFavorite(ProductId);
                ActiveBranchName = "Main Branch";

                OnPropertyChanged(nameof(ProductName));
                OnPropertyChanged(nameof(CategoryName));
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(BasePriceDisplay));
                OnPropertyChanged(nameof(InStock));
                OnPropertyChanged(nameof(StockBadgeText));
                OnPropertyChanged(nameof(StockBadgeBg));
                OnPropertyChanged(nameof(StockBadgeTextColor));
                OnPropertyChanged(nameof(HasDiscount));
                OnPropertyChanged(nameof(DiscountBadgeText));
                OnPropertyChanged(nameof(OriginalPriceDisplay));
                OnPropertyChanged(nameof(FavoriteGlyph));

                // Load modifier groups
                try
                {
                    var groups = await _orders.GetModifiersForProductAsync(ProductId);
                    groups = groups?.Where(g => g.Options is { Count: > 0 }).ToList() ?? new List<ProductModifierGroupDto>();

                    ModifierGroups.Clear();
                    foreach (var g in groups)
                    {
                        var groupItem = new ModifierGroupItem
                        {
                            Id = g.Id,
                            Name = string.IsNullOrWhiteSpace(g.Name) ? "Options" : g.Name!,
                            IsRequired = g.IsRequired || g.MinSelection > 0,
                            AllowMultiple = g.AllowMultiple || g.MaxSelection > 1,
                            MaxSelection = g.MaxSelection <= 0 ? 1 : g.MaxSelection
                        };

                        var defaultApplied = false;
                        foreach (var opt in g.Options)
                        {
                            var item = new ModifierOptionItem
                            {
                                Id = opt.Id,
                                GroupId = g.Id,
                                Name = opt.Name ?? "Option",
                                Price = opt.EffectivePrice,
                                AllowMultiple = groupItem.AllowMultiple,
                                IsRequired = groupItem.IsRequired,
                                MaxSelection = groupItem.MaxSelection,
                                IsSelected = opt.IsDefault && !defaultApplied
                            };
                            if (item.IsSelected)
                            {
                                defaultApplied = true;
                            }
                            groupItem.Options.Add(item);
                        }

                        if (groupItem.IsRequired && !groupItem.Options.Any(o => o.IsSelected) && groupItem.Options.Count > 0)
                        {
                            groupItem.Options[0].IsSelected = true;
                        }

                        ModifierGroups.Add(groupItem);
                    }

                    HasModifiers = ModifierGroups.Count > 0;
                }
                catch
                {
                    ModifierGroups.Clear();
                    HasModifiers = false;
                }

                Quantity = 1;
                RecalculateTotal();
            }
        });
    }

    [RelayCommand]
    public void Increase()
    {
        Quantity += 1;
        RecalculateTotal();
    }

    [RelayCommand]
    public void Decrease()
    {
        if (Quantity > 1)
        {
            Quantity -= 1;
            RecalculateTotal();
        }
    }

    [RelayCommand]
    public void ToggleOption(ModifierOptionItem? option)
    {
        if (option == null) return;

        var group = ModifierGroups.FirstOrDefault(g => g.Id == option.GroupId);
        if (group == null) return;

        if (!group.AllowMultiple)
        {
            foreach (var o in group.Options)
            {
                o.IsSelected = false;
            }
            option.IsSelected = true;
        }
        else
        {
            if (!option.IsSelected)
            {
                var selectedCount = group.Options.Count(o => o.IsSelected);
                if (group.MaxSelection > 0 && selectedCount >= group.MaxSelection)
                {
                    return;
                }
                option.IsSelected = true;
            }
            else
            {
                if (group.IsRequired && group.Options.Count(o => o.IsSelected) <= 1)
                {
                    return;
                }
                option.IsSelected = false;
            }
        }

        RecalculateTotal();
    }

    private void RecalculateTotal()
    {
        decimal basePrice = Product?.Price ?? 0;
        decimal extra = 0;

        foreach (var group in ModifierGroups)
        {
            foreach (var opt in group.Options)
            {
                if (opt.IsSelected && opt.Price > 0)
                {
                    extra += opt.Price;
                }
            }
        }

        TotalPrice = (basePrice + extra) * Quantity;
        OnPropertyChanged(nameof(TotalPriceDisplay));
        OnPropertyChanged(nameof(TotalButtonText));
    }

    [RelayCommand]
    public async Task AddToCartAsync()
    {
        if (Product == null) return;

        // Verify mandatory modifier groups
        foreach (var group in ModifierGroups)
        {
            if (group.IsRequired && !group.Options.Any(o => o.IsSelected))
            {
                await Shell.Current.DisplayAlert("Selection Required", $"Please select an option for '{group.Name}'.", "OK");
                return;
            }
        }

        var selectedList = new List<string>();
        foreach (var group in ModifierGroups)
        {
            var selectedOpts = group.Options.Where(o => o.IsSelected).ToList();
            if (selectedOpts.Count > 0)
            {
                var optsStr = string.Join(", ", selectedOpts.Select(o => o.Price > 0 ? $"{o.Name} (+Rs {o.Price:0})" : o.Name));
                selectedList.Add($"{group.Name}: {optsStr}");
            }
        }

        if (!string.IsNullOrWhiteSpace(SpecialInstructions))
        {
            selectedList.Add($"Note: {SpecialInstructions.Trim()}");
        }

        var modifiersString = selectedList.Count > 0 ? string.Join(" | ", selectedList) : null;
        _cart.AddOrIncrement(Product, Quantity, modifiersString);
        CartCount = _cart.ItemCount;

        if (Shell.Current != null)
        {
            await Shell.Current.DisplayAlert("Added to Cart", $"{Quantity}x {Product.Name} added to your cart.", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    [RelayCommand]
    public async Task ToggleFavoriteAsync()
    {
        if (ProductId <= 0) return;
        IsFavorite = await _favorites.ToggleFavoriteAsync(ProductId);
        OnPropertyChanged(nameof(FavoriteGlyph));
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    [RelayCommand]
    public async Task OpenCartAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//customer/cart");
        }
    }

    [RelayCommand]
    public async Task ShareAsync()
    {
        if (Product == null) return;

        try
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Title = ProductName,
                Text = $"Check out {ProductName} ({BasePriceDisplay}) on SmartPos! Order delicious food delivered hot."
            });
        }
        catch { }
    }
}
