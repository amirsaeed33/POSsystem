using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public partial class CustomerHomeViewModel : BaseViewModel
{
    private readonly OnlineOrderService _orders;
    private readonly SessionService _session;
    private readonly CartService _cart;
    private readonly AbpApiClient _api;
    private readonly AppSettings _settings;
    private List<OnlineProductDto> _allProducts = new();
    private IDispatcherTimer? _heroTimer;

    [ObservableProperty]
    private OnlineStoreHeaderDto? storeHeader;

    [ObservableProperty]
    private OnlineBranchDto? selectedBranch;

    [ObservableProperty]
    private string selectedCategory = "All";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool needsBranchSelection = true;

    [ObservableProperty]
    private int heroPosition;

    [ObservableProperty]
    private bool isDeliveryMode = true;

    [ObservableProperty]
    private bool showProductGrid;

    public ObservableCollection<OnlineBranchDto> Branches { get; } = new();
    public ObservableCollection<CategoryChipItem> Categories { get; } = new();
    public ObservableCollection<CategoryChipItem> ExploreCategories { get; } = new();
    public ObservableCollection<OnlineProductDto> Products { get; } = new();
    public ObservableCollection<HeroSlideDto> HeroSlides { get; } = new();

    public int CartCount => _cart.ItemCount;
    public bool HasCartItems => CartCount > 0;
    public bool HasMenu => !NeedsBranchSelection && SelectedBranch != null;
    public bool HasHeroSlides => HeroSlides.Count > 0;
    public bool IsPickupMode => !IsDeliveryMode;
    public string DeliverToLabel =>
        SelectedBranch?.Name
        ?? StoreHeader?.BranchName
        ?? "Select branch";
    public string SelectedCategoryTitle =>
        string.Equals(SelectedCategory, "All", StringComparison.OrdinalIgnoreCase)
            ? "All Menu"
            : SelectedCategory;
    public string FilteredItemsLabel => $"({Products.Count})";

    public CustomerHomeViewModel(
        OnlineOrderService orders,
        SessionService session,
        CartService cart,
        AbpApiClient api,
        AppSettings settings)
    {
        _orders = orders;
        _session = session;
        _cart = cart;
        _api = api;
        _settings = settings;
        Title = "Online Order";
        _cart.CartChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CartCount));
            OnPropertyChanged(nameof(HasCartItems));
            SyncCartQtyBadges();
        };
    }

    partial void OnNeedsBranchSelectionChanged(bool value) => OnPropertyChanged(nameof(HasMenu));
    partial void OnSelectedBranchChanged(OnlineBranchDto? value)
    {
        OnPropertyChanged(nameof(HasMenu));
        OnPropertyChanged(nameof(DeliverToLabel));
    }
    partial void OnIsDeliveryModeChanged(bool value) => OnPropertyChanged(nameof(IsPickupMode));

    [RelayCommand]
    private async Task AppearingAsync()
    {
        // Same as website public storefront: no tenancy cookie required.
        _session.ClearTenantHeader();

        await RunBusyAsync(async () =>
        {
            await LoadBranchesAsync();

            // Resume last branch if user already picked one (like ?branchId= on web).
            if (_session.BranchId is int savedBranchId)
            {
                var saved = Branches.FirstOrDefault(b => b.Id == savedBranchId);
                if (saved != null)
                {
                    await OpenBranchAsync(saved);
                    return;
                }
            }

            // Optional default from config (mirrors /online-order?branchId=2 for local testing).
            if (_settings.DefaultBranchId is int defaultBranchId and > 0)
            {
                var def = Branches.FirstOrDefault(b => b.Id == defaultBranchId);
                if (def != null)
                {
                    await OpenBranchAsync(def);
                    return;
                }
            }

            NeedsBranchSelection = true;
            SelectedBranch = null;
            StoreHeader = null;
            Products.Clear();
            Categories.Clear();
            ClearHeroSlides();
            _allProducts.Clear();
        });
    }

    private async Task LoadBranchesAsync()
    {
        Branches.Clear();
        var branches = await _orders.GetBranchesAsync();
        foreach (var b in branches.OrderBy(x => x.Name))
        {
            Branches.Add(b);
        }
    }

    [RelayCommand]
    private async Task SelectBranchAsync(OnlineBranchDto? branch)
    {
        if (branch == null)
        {
            return;
        }

        await RunBusyAsync(async () => await OpenBranchAsync(branch));
    }

    private async Task OpenBranchAsync(OnlineBranchDto branch)
    {
        SelectedBranch = branch;
        NeedsBranchSelection = false;
        await _session.SetBranchAsync(branch.Id);
        _cart.Clear();

        StoreHeader = await _orders.GetStoreHeaderAsync(branch.Id);

        await LoadHeroSlidesAsync(branch.Id);

        _allProducts = await _orders.GetCatalogAsync(branch.Id);
        foreach (var p in _allProducts)
        {
            p.ImageUrl = _api.ResolveImageUrl(p.ImagePath);
        }

        SelectedCategory = "All";
        SearchText = string.Empty;
        ShowProductGrid = true;
        RebuildCategories();
        ApplyFilters();
    }

    private void RebuildCategories()
    {
        var names = new List<string>();
        foreach (var c in StoreHeader?.Categories ?? new List<string>())
        {
            if (!string.IsNullOrWhiteSpace(c) &&
                !names.Contains(c, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(c!);
            }
        }

        foreach (var c in _allProducts
                     .Select(p => p.CategoryName)
                     .Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (!names.Contains(c!, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(c!);
            }
        }

        Categories.Clear();
        ExploreCategories.Clear();

        Categories.Add(new CategoryChipItem
        {
            Name = "All",
            Count = _allProducts.Count,
            IsSelected = string.Equals(SelectedCategory, "All", StringComparison.OrdinalIgnoreCase)
        });

        foreach (var name in names)
        {
            var categoryProducts = _allProducts
                .Where(p => string.Equals(p.CategoryName, name, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var cover = categoryProducts.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.ImageUrl))
                        ?? categoryProducts.FirstOrDefault();

            var chip = new CategoryChipItem
            {
                Name = name,
                Count = categoryProducts.Count,
                ImageUrl = cover?.ImageUrl,
                IsSelected = string.Equals(SelectedCategory, name, StringComparison.OrdinalIgnoreCase)
            };
            Categories.Add(chip);
            ExploreCategories.Add(chip);
        }

        OnPropertyChanged(nameof(DeliverToLabel));
    }

    private async Task LoadHeroSlidesAsync(int branchId)
    {
        ClearHeroSlides();

        var slides = await _orders.GetHeroSlidesAsync(branchId);
        if (slides.Count == 0)
        {
            slides = CreateDefaultHeroSlides();
        }

        foreach (var slide in slides.OrderBy(s => s.SortOrder).ThenBy(s => s.Id))
        {
            if (string.IsNullOrWhiteSpace(slide.ImageUrl))
            {
                slide.ImageUrl = _api.ResolveImageUrl(slide.ImagePath);
            }

            if (string.IsNullOrWhiteSpace(slide.BgImageUrl))
            {
                var bgPath = !string.IsNullOrWhiteSpace(slide.BgImagePath)
                    ? slide.BgImagePath
                    : slide.ImagePath;
                slide.BgImageUrl = _api.ResolveImageUrl(bgPath);
            }

            if (string.IsNullOrWhiteSpace(slide.BgImageUrl) && !string.IsNullOrWhiteSpace(slide.ImageUrl))
            {
                slide.BgImageUrl = slide.ImageUrl;
            }

            HeroSlides.Add(slide);
        }

        HeroPosition = 0;
        OnPropertyChanged(nameof(HasHeroSlides));
        StartHeroAutoAdvance();
    }

    private void ClearHeroSlides()
    {
        StopHeroAutoAdvance();
        HeroSlides.Clear();
        HeroPosition = 0;
        OnPropertyChanged(nameof(HasHeroSlides));
    }

    private void StartHeroAutoAdvance()
    {
        StopHeroAutoAdvance();
        if (HeroSlides.Count <= 1 || Application.Current?.Dispatcher is null)
        {
            return;
        }

        _heroTimer = Application.Current.Dispatcher.CreateTimer();
        _heroTimer.Interval = TimeSpan.FromMilliseconds(4500);
        _heroTimer.Tick += OnHeroTimerTick;
        _heroTimer.Start();
    }

    private void OnHeroTimerTick(object? sender, EventArgs e)
    {
        if (HeroSlides.Count == 0)
        {
            return;
        }

        HeroPosition = (HeroPosition + 1) % HeroSlides.Count;
    }

    private void StopHeroAutoAdvance()
    {
        if (_heroTimer is null)
        {
            return;
        }

        _heroTimer.Tick -= OnHeroTimerTick;
        _heroTimer.Stop();
        _heroTimer = null;
    }

    public void OnPageDisappearing() => StopHeroAutoAdvance();

    private static List<HeroSlideDto> CreateDefaultHeroSlides() =>
    [
        new HeroSlideDto
        {
            Id = 1,
            SortOrder = 1,
            Badge = "FRESH FROM OVEN • 30-45 MIN DELIVERY",
            BadgeIcon = "🔥",
            TitleMain = "Craving Something",
            TitleHighlight = "Hot, Cheesy",
            TitleEnd = "& Delicious?",
            Description = "Explore handcrafted pizzas loaded with 100% mozzarella, sizzling juicy burgers, crispy wings, and mouth-watering family deals. Cooked fresh on order!",
            OfferPill = "FLAT 25% OFF ON LARGE PIZZAS",
            Rating = "4.9★",
            PrimaryBtnText = "Order Deals Now",
            SecondaryBtnText = "Explore Full Menu",
            BgImageUrl = "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=1920&q=85",
            ImageUrl = "https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=800&h=600&q=80"
        },
        new HeroSlideDto
        {
            Id = 2,
            SortOrder = 2,
            Badge = "SMASHED & GRILLED TO PERFECTION",
            BadgeIcon = "🍔",
            TitleMain = "Bite Into Pure",
            TitleHighlight = "Juicy & Crispy",
            TitleEnd = "Gourmet Burgers",
            Description = "Double smashed beef patties, crispy tender chicken fillets, secret spicy mayo, and melted golden cheddar packed inside freshly toasted brioche buns.",
            OfferPill = "FREE CRISPY FRIES + DIP WITH COMBOS",
            Rating = "4.8★",
            PrimaryBtnText = "Grab Burger Deal",
            SecondaryBtnText = "View All Burgers",
            BgImageUrl = "https://images.unsplash.com/photo-1572802419224-296b0aeee0d9?auto=format&fit=crop&w=1920&q=85",
            ImageUrl = "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=800&h=600&q=80"
        },
        new HeroSlideDto
        {
            Id = 3,
            SortOrder = 3,
            Badge = "SMOKY CHARCOAL & AUTHENTIC TASTE",
            BadgeIcon = "🍢",
            TitleMain = "Sizzling Live",
            TitleHighlight = "Smoky BBQ",
            TitleEnd = "& Kebab Platters",
            Description = "Tender chicken boti, melt-in-mouth seekh kebabs, and grilled reshmi tikka marinated in rich spices and served with piping-hot kulchas and mint raita.",
            OfferPill = "FAMILY PLATTER SPECIAL SAVE PKR 800",
            Rating = "4.9★",
            PrimaryBtnText = "Taste The BBQ",
            SecondaryBtnText = "View BBQ Platters",
            BgImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?auto=format&fit=crop&w=1920&q=85",
            ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?auto=format&fit=crop&w=800&h=600&q=80"
        },
        new HeroSlideDto
        {
            Id = 4,
            SortOrder = 4,
            Badge = "MEGA VALUE PACKS & CELEBRATIONS",
            BadgeIcon = "🎉",
            TitleMain = "Gather Round For",
            TitleHighlight = "Mega Family",
            TitleEnd = "Feast Combos",
            Description = "The ultimate party solution! 2 Large Specialty Pizzas, 10 pcs Crispy Wings, Cheesy Garlic Bread, and a chilled 1.5L drink for the whole squad.",
            OfferPill = "MEGA SAVINGS UP TO PKR 1,200",
            Rating = "5.0★",
            PrimaryBtnText = "Get Family Feast",
            SecondaryBtnText = "See All Combos",
            BgImageUrl = "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?auto=format&fit=crop&w=1920&q=85",
            ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=800&h=600&q=80"
        },
        new HeroSlideDto
        {
            Id = 5,
            SortOrder = 5,
            Badge = "PIPING HOT DELIVERY GUARANTEE",
            BadgeIcon = "⚡",
            TitleMain = "Delivered In",
            TitleHighlight = "30-45 Mins",
            TitleEnd = "Right To Your Door",
            Description = "Experience lightning-fast express delivery with insulated heat bags. Live rider GPS tracking keeps you informed every step of the way from grill to table.",
            OfferPill = "FREE EXPRESS DELIVERY TODAY",
            Rating = "4.9★",
            PrimaryBtnText = "Order Online Now",
            SecondaryBtnText = "Track Active Order",
            BgImageUrl = "https://images.unsplash.com/photo-1617347454431-f49d7ff5c3b1?auto=format&fit=crop&w=1920&q=85",
            ImageUrl = "https://images.unsplash.com/photo-1617347454431-f49d7ff5c3b1?auto=format&fit=crop&w=800&h=600&q=80"
        }
    ];

    [RelayCommand]
    private async Task ChangeBranchAsync()
    {
        _cart.Clear();
        SelectedBranch = null;
        NeedsBranchSelection = true;
        StoreHeader = null;
        Products.Clear();
        Categories.Clear();
        ClearHeroSlides();
        _allProducts.Clear();
        await _session.ClearBranchAsync();
        _session.ClearTenantHeader();
        await RunBusyAsync(LoadBranchesAsync);
    }

    [RelayCommand]
    private void SelectCategory(CategoryChipItem? category)
    {
        SelectedCategory = string.IsNullOrWhiteSpace(category?.Name) ? "All" : category!.Name;
        foreach (var chip in Categories)
        {
            chip.IsSelected = string.Equals(chip.Name, SelectedCategory, StringComparison.OrdinalIgnoreCase);
        }

        ShowProductGrid = true;
        ApplyFilters();
    }

    [RelayCommand]
    private void ViewAllMenu()
    {
        SelectedCategory = "All";
        foreach (var chip in Categories)
        {
            chip.IsSelected = chip.IsAll;
        }

        ShowProductGrid = true;
        ApplyFilters();
    }

    [RelayCommand]
    private void SetDeliveryMode() => IsDeliveryMode = true;

    [RelayCommand]
    private void SetPickupMode() => IsDeliveryMode = false;

    partial void OnSearchTextChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            ShowProductGrid = true;
        }

        ApplyFilters();
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        OnPropertyChanged(nameof(SelectedCategoryTitle));
    }

    private void ApplyFilters()
    {
        Products.Clear();
        IEnumerable<OnlineProductDto> query = _allProducts;
        if (!string.Equals(SelectedCategory, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => string.Equals(p.CategoryName, SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(p =>
                (p.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        foreach (var p in query)
        {
            p.CartQty = _cart.GetQty(p.Id);
            Products.Add(p);
        }

        OnPropertyChanged(nameof(FilteredItemsLabel));
        OnPropertyChanged(nameof(SelectedCategoryTitle));
    }

    private void SyncCartQtyBadges()
    {
        foreach (var p in _allProducts)
        {
            p.CartQty = _cart.GetQty(p.Id);
        }

        ApplyFilters();
    }

    [RelayCommand]
    private void IncreaseCard(OnlineProductDto? product)
    {
        if (product == null || !product.InStock)
        {
            return;
        }

        _cart.AddOrIncrement(product, 1);
    }

    [RelayCommand]
    private void DecreaseCard(OnlineProductDto? product)
    {
        if (product == null || product.CartQty <= 0)
        {
            return;
        }

        _cart.DecrementProduct(product.Id);
    }

    [RelayCommand]
    private async Task OpenProductAsync(OnlineProductDto? product)
    {
        if (product == null)
        {
            return;
        }

        await Shell.Current.GoToAsync($"productdetail?productId={product.Id}");
    }

    [RelayCommand]
    private async Task OpenCartAsync() => await Shell.Current.GoToAsync("//customer/cart");

    [RelayCommand]
    private async Task OpenLoginAsync() => await Shell.Current.GoToAsync("//customer/account");
}
