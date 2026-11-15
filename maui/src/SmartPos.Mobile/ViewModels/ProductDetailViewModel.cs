using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

[QueryProperty(nameof(ProductId), "productId")]
public partial class ProductDetailViewModel : BaseViewModel
{
    private const string FallbackHeroImageUrl =
        "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?auto=format&fit=crop&w=1200&h=900&q=85";

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

    /// <summary>Aljannat hospitality sections (always shown on detail).</summary>
    public ObservableCollection<ProductDetailChoiceItem> PortionOptions { get; } = new();
    public ObservableCollection<ProductDetailChoiceItem> SpiceOptions { get; } = new();
    public ObservableCollection<ProductDetailChoiceItem> AddonOptions { get; } = new();

    public string ProductName => Product?.Name ?? "Item Details";
    public string CategoryName => !string.IsNullOrWhiteSpace(Product?.CategoryName) ? Product.CategoryName : "Fresh Menu";
    public string CategoryBadgeText => CategoryName.Trim().ToUpperInvariant();
    public string Description => !string.IsNullOrWhiteSpace(Product?.Description) ? Product.Description : "Freshly prepared to order with authentic, high-quality ingredients.";
    public string BasePriceDisplay => Product != null ? $"Rs. {Product.Price:0}" : string.Empty;
    public string TotalPriceDisplay => $"Rs. {TotalPrice:0}";
    public string TotalButtonText => IsSoldOut ? "Sold Out" : $"Add to Cart  •  Rs. {TotalPrice:0}";
    public bool InStock => Product?.InStock ?? true;
    public bool IsSoldOut => !InStock;
    public bool IsLowStock => InStock && Product != null && Product.StockQuantity > 0 && Product.StockQuantity <= 5;
    public bool CanAddToCart => !IsSoldOut;

    public string StockBadgeText =>
        IsSoldOut ? "● SOLD OUT"
        : IsLowStock ? "● LOW STOCK"
        : "● IN STOCK  •  FRESH BATCH";

    public Color StockBadgeBg =>
        IsSoldOut ? ThemeColors.DangerBackground
        : IsLowStock ? ThemeColors.PrimaryLight
        : ThemeColors.SuccessBackground;

    public Color StockBadgeStroke =>
        IsSoldOut ? Color.FromArgb("#F5C6C2")
        : IsLowStock ? Color.FromArgb("#F0C4AE")
        : Color.FromArgb("#F3D6B3");

    public Color StockBadgeTextColor =>
        IsSoldOut ? ThemeColors.DangerText
        : IsLowStock ? ThemeColors.Primary
        : ThemeColors.Tertiary;

    public bool HasDiscount => Product != null && Product.HasOriginalPrice;
    public string DiscountBadgeText => Product?.DealDiscountBadge ?? "DEAL";
    public string OriginalPriceDisplay =>
        Product?.OriginalPrice is decimal original
            ? $"Rs. {original:0}"
            : string.Empty;
    public string SavingsDisplay =>
        HasDiscount && Product?.OriginalPrice is decimal original
            ? $"SAVE Rs. {(original - Product.Price):0}"
            : string.Empty;

    public string RatingDisplay => "★ 4.9";
    public string RatingMetaDisplay => "(280+ Diners)";
    public string PrepTimeDisplay => "25–30 min";

    /// <summary>Alias for hero binding; always a non-empty absolute image URL.</summary>
    public string ProductImage => ImageUrl;

    public string QuantityText => Quantity.ToString("0");

    public Color FavoriteFillColor => IsFavorite ? ThemeColors.Primary : Colors.Transparent;
    public Color FavoriteStrokeColor => IsFavorite ? ThemeColors.Primary : ThemeColors.TextPrimary;
    public string FavoriteGlyph => "♥";
    public Color FavoriteGlyphColor => ThemeColors.Primary;

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
                    OnPropertyChanged(nameof(FavoriteFillColor));
                    OnPropertyChanged(nameof(FavoriteStrokeColor));
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
        if (ProductId == 0)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            OnlineProductDto? p = null;
            if (ProductId > 0)
            {
                p = await _orders.GetProductAsync(ProductId, _session.BranchId);
            }

            // Deal/combo cards (incl. local fallbacks) can prefill when API id is missing.
            p ??= ProductDetailLaunch.TakePrefill(ProductId);

            if (p == null)
            {
                return;
            }

            Product = p;
            Title = Product.Name ?? "Product Details";
            ImageUrl = ResolveHeroImageUrl(Product);
            OnPropertyChanged(nameof(ProductImage));

            IsFavorite = ProductId > 0 && _favorites.IsFavorite(ProductId);
            ActiveBranchName = "Main Branch";

            OnPropertyChanged(nameof(ProductName));
            OnPropertyChanged(nameof(CategoryName));
            OnPropertyChanged(nameof(CategoryBadgeText));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(BasePriceDisplay));
            OnPropertyChanged(nameof(InStock));
            OnPropertyChanged(nameof(IsSoldOut));
            OnPropertyChanged(nameof(IsLowStock));
            OnPropertyChanged(nameof(CanAddToCart));
            OnPropertyChanged(nameof(StockBadgeText));
            OnPropertyChanged(nameof(StockBadgeBg));
            OnPropertyChanged(nameof(StockBadgeStroke));
            OnPropertyChanged(nameof(StockBadgeTextColor));
            OnPropertyChanged(nameof(HasDiscount));
            OnPropertyChanged(nameof(DiscountBadgeText));
            OnPropertyChanged(nameof(OriginalPriceDisplay));
            OnPropertyChanged(nameof(SavingsDisplay));
            OnPropertyChanged(nameof(FavoriteFillColor));
            OnPropertyChanged(nameof(FavoriteStrokeColor));
            OnPropertyChanged(nameof(TotalButtonText));

            // Keep API modifiers for cart parity when present; Aljannat UI uses hospitality choices.
            ModifierGroups.Clear();
            HasModifiers = false;
            if (ProductId > 0)
            {
                try
                {
                    var groups = await _orders.GetModifiersForProductAsync(ProductId);
                    groups = groups?.Where(g => g.Options is { Count: > 0 }).ToList() ?? new List<ProductModifierGroupDto>();

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
            }

            SeedAljannatHospitalityChoices();
            Quantity = 1;
            OnPropertyChanged(nameof(QuantityText));
            RecalculateTotal();
        });
    }

    partial void OnQuantityChanged(decimal value) => OnPropertyChanged(nameof(QuantityText));

    private string ResolveHeroImageUrl(OnlineProductDto product)
    {
        _orders.ResolveProductImageUrl(product);
        if (!string.IsNullOrWhiteSpace(product.ImageUrl) &&
            Uri.TryCreate(product.ImageUrl, UriKind.Absolute, out _))
        {
            return product.ImageUrl!;
        }

        return FallbackHeroImageUrl;
    }

    private void SeedAljannatHospitalityChoices()
    {
        PortionOptions.Clear();
        PortionOptions.Add(new ProductDetailChoiceItem
        {
            Key = "portion-regular",
            Name = "Single Regular",
            Subtitle = "Classic plate, ready for the dastarkhwan",
            Price = 0,
            IsSelected = true
        });
        PortionOptions.Add(new ProductDetailChoiceItem
        {
            Key = "portion-combo",
            Name = "Make it a Combo (Fries + Drink)",
            Subtitle = "Upgrade with sides for a complete meal",
            Price = 250,
            IsSelected = false
        });

        SpiceOptions.Clear();
        SpiceOptions.Add(new ProductDetailChoiceItem
        {
            Key = "spice-mild",
            Name = "Mild & Aromatic",
            Subtitle = "Gentle Pepper",
            Price = 0,
            IsSelected = false
        });
        SpiceOptions.Add(new ProductDetailChoiceItem
        {
            Key = "spice-med",
            Name = "Traditional Med",
            Subtitle = "Chef Choice",
            Price = 0,
            IsSelected = true
        });
        SpiceOptions.Add(new ProductDetailChoiceItem
        {
            Key = "spice-fiery",
            Name = "Fiery Peshawari",
            Subtitle = "Extra Chili",
            Price = 0,
            IsSelected = false
        });

        AddonOptions.Clear();
        AddonOptions.Add(new ProductDetailChoiceItem
        {
            Key = "addon-naan",
            Name = "2× Roghni Tandoori Naan",
            Price = 180
        });
        AddonOptions.Add(new ProductDetailChoiceItem
        {
            Key = "addon-raita",
            Name = "Smoky Mint & Zeera Raita",
            Price = 120
        });
        AddonOptions.Add(new ProductDetailChoiceItem
        {
            Key = "addon-ghee",
            Name = "Extra Desi Ghee / Cheese",
            Price = 250
        });
        AddonOptions.Add(new ProductDetailChoiceItem
        {
            Key = "addon-salad",
            Name = "Kachumber Kachori Salad",
            Price = 150
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

    [RelayCommand]
    private void SelectPortion(ProductDetailChoiceItem? choice)
    {
        if (choice == null)
        {
            return;
        }

        foreach (var item in PortionOptions)
        {
            item.IsSelected = ReferenceEquals(item, choice);
        }

        RecalculateTotal();
    }

    [RelayCommand]
    private void SelectSpice(ProductDetailChoiceItem? choice)
    {
        if (choice == null)
        {
            return;
        }

        foreach (var item in SpiceOptions)
        {
            item.IsSelected = ReferenceEquals(item, choice);
        }

        RecalculateTotal();
    }

    [RelayCommand]
    private void ToggleAddon(ProductDetailChoiceItem? choice)
    {
        if (choice == null)
        {
            return;
        }

        choice.IsSelected = !choice.IsSelected;
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

        extra += PortionOptions.Where(o => o.IsSelected).Sum(o => o.Price);
        extra += AddonOptions.Where(o => o.IsSelected).Sum(o => o.Price);

        TotalPrice = (basePrice + extra) * Quantity;
        OnPropertyChanged(nameof(TotalPriceDisplay));
        OnPropertyChanged(nameof(TotalButtonText));
    }

    [RelayCommand]
    public async Task AddToCartAsync()
    {
        if (Product == null || IsSoldOut)
        {
            return;
        }

        foreach (var group in ModifierGroups)
        {
            if (group.IsRequired && !group.Options.Any(o => o.IsSelected))
            {
                await AljannatDialogService.Current.ShowAlertAsync("Selection Required", $"Please select an option for '{group.Name}'.", "OK");
                return;
            }
        }

        if (!PortionOptions.Any(o => o.IsSelected))
        {
            await AljannatDialogService.Current.ShowAlertAsync("Selection Required", "Please choose a Portion / Meal Option.", "OK");
            return;
        }

        if (!SpiceOptions.Any(o => o.IsSelected))
        {
            await AljannatDialogService.Current.ShowAlertAsync("Selection Required", "Please choose a Spice & Char Profile.", "OK");
            return;
        }

        var selectedList = new List<string>();
        foreach (var group in ModifierGroups)
        {
            var selectedOpts = group.Options.Where(o => o.IsSelected).ToList();
            if (selectedOpts.Count > 0)
            {
                var optsStr = string.Join(", ", selectedOpts.Select(o => o.Price > 0 ? $"{o.Name} (+Rs. {o.Price:0})" : o.Name));
                selectedList.Add($"{group.Name}: {optsStr}");
            }
        }

        var portion = PortionOptions.FirstOrDefault(o => o.IsSelected);
        if (portion != null)
        {
            selectedList.Add(portion.Price > 0
                ? $"Portion: {portion.Name} (+Rs. {portion.Price:0})"
                : $"Portion: {portion.Name}");
        }

        var spice = SpiceOptions.FirstOrDefault(o => o.IsSelected);
        if (spice != null)
        {
            selectedList.Add($"Spice: {spice.Name}");
        }

        var addons = AddonOptions.Where(o => o.IsSelected).ToList();
        if (addons.Count > 0)
        {
            selectedList.Add("Add-Ons: " + string.Join(", ",
                addons.Select(a => a.Price > 0 ? $"{a.Name} (+Rs. {a.Price:0})" : a.Name)));
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
            await AljannatDialogService.Current.ShowAlertAsync("Added to Cart", $"{Quantity}x {Product.Name} added to your cart.", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    [RelayCommand]
    public async Task ToggleFavoriteAsync()
    {
        if (ProductId <= 0) return;
        IsFavorite = await _favorites.ToggleFavoriteAsync(ProductId);
        OnPropertyChanged(nameof(FavoriteFillColor));
        OnPropertyChanged(nameof(FavoriteStrokeColor));
    }

    [RelayCommand]
    public void AppendInstruction(string? phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return;
        }

        var clean = phrase.Trim().TrimStart('+').Trim();
        if (string.IsNullOrWhiteSpace(SpecialInstructions))
        {
            SpecialInstructions = clean;
            return;
        }

        if (SpecialInstructions.Contains(clean, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        SpecialInstructions = $"{SpecialInstructions.Trim().TrimEnd(',')}, {clean}";
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
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProductDetailViewModel.ShareAsync] Share failed: {ex.Message}");
        }
    }
}
