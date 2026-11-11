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
    private readonly DeliveryLocationService _deliveryLocation;
    private readonly FavoritesService _favorites;
    private List<OnlineProductDto> _allProducts = new();
    private readonly List<CategoryChipItem> _allCategoryChips = new();
    private IDispatcherTimer? _heroTimer;

    [ObservableProperty]
    private OnlineStoreHeaderDto? storeHeader;

    [ObservableProperty]
    private OnlineBranchDto? selectedBranch;

    [ObservableProperty]
    private string selectedCategory = "All";

    public HashSet<string> SelectedCategories { get; } = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    private bool isAllCategoriesSelected = true;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string categorySearchText = string.Empty;

    [ObservableProperty]
    private bool isCategorySearchVisible;

    [ObservableProperty]
    private bool needsBranchSelection = true;

    [ObservableProperty]
    private int heroPosition;

    [ObservableProperty]
    private bool isDeliveryMode = true;

    [ObservableProperty]
    private bool showProductGrid;

    [ObservableProperty]
    private bool isProductModalVisible;

    [ObservableProperty]
    private OnlineProductDto? modalProduct;

    [ObservableProperty]
    private bool isLoadingModifiers;

    // === DEALS & COMBO CUSTOMIZATION STATE ===
    [ObservableProperty]
    private bool isLoadingDeals;

    [ObservableProperty]
    private bool isComboModalVisible;

    [ObservableProperty]
    private bool isLoadingComboDeal;

    [ObservableProperty]
    private OnlineProductDto? comboProduct;

    [ObservableProperty]
    private int comboQuantity = 1;

    [ObservableProperty]
    private string comboTotalLabel = "PKR 0";

    [ObservableProperty]
    private bool isComboValidationPassed = true;

    [ObservableProperty]
    private string comboValidationMessage = string.Empty;

    public bool CanAddComboToCart => IsComboValidationPassed && !IsLoadingComboDeal && ComboProduct != null;
    public bool HasComboValidationMessage => !string.IsNullOrWhiteSpace(ComboValidationMessage);

    /// <summary>Pull-to-refresh only. Must stay separate from <see cref="BaseViewModel.IsBusy"/> or RefreshView traps forever.</summary>
    [ObservableProperty]
    private bool isPullRefreshing;

    [ObservableProperty]
    private int detailQuantity = 1;

    [ObservableProperty]
    private string modalTotalLabel = "PKR 0";

    public ObservableCollection<OnlineBranchDto> Branches { get; } = new();
    public ObservableCollection<CategoryChipItem> Categories { get; } = new();
    public ObservableCollection<CategoryChipItem> ExploreCategories { get; } = new();
    public ObservableCollection<OnlineProductDto> Products { get; } = new();
    public ObservableCollection<HeroSlideDto> HeroSlides { get; } = new();
    public ObservableCollection<ModifierGroupItem> ModalModifierGroups { get; } = new();

    // Dedicated Hot Deals & Combo Items
    public ObservableCollection<OnlineProductDto> DealsList { get; } = new();
    public ObservableCollection<DealCustomiseItem> DealCustomiseItems { get; } = new();
    public ObservableCollection<QuickFilterPillItem> FilterPills { get; } = new();
    public ObservableCollection<CustomerFeedbackDto> Feedbacks { get; } = new();
    public ObservableCollection<CustomerFeedbackDto> BranchFeedbacks => Feedbacks;

    [ObservableProperty]
    private string selectedPill = string.Empty;

    [ObservableProperty]
    private bool isLoadingFeedbacks;

    [ObservableProperty]
    private int feedbackPosition;

    [ObservableProperty]
    private double averageRating = 4.9;

    [ObservableProperty]
    private string ratingScoreDisplay = "4.9";

    public double AverageRatingScore => AverageRating;
    public int TotalReviewsCount => Feedbacks.Count;
    public string TotalReviewsLabel => $"{Math.Max(Feedbacks.Count, 4)} Verified Reviews";
    public string TotalReviewsSubtitle => $"{Math.Max(Feedbacks.Count, 4)} reviews";
    public bool HasFeedbacks => Feedbacks.Count > 0;

    public int CartCount => _cart.ItemCount;
    public bool HasCartItems => CartCount > 0;
    public decimal CartSubtotal => _cart.Subtotal;
    public string CartSubtotalDisplay => $"Rs {CartSubtotal:0}";
    public string CartCountBadge => $"{CartCount} {(CartCount == 1 ? "item" : "items")}";
    public bool IsFloatingCartBarVisible => HasCartItems && !IsAnyModalVisible && !NeedsBranchSelection;
    public bool HasMenu => !NeedsBranchSelection && SelectedBranch != null;
    public bool HasHeroSlides => HeroSlides.Count > 0;
    public bool HasDeals => DealsList.Count > 0;
    public bool HasComboItems => DealCustomiseItems.Count > 0;
    public bool IsAnyModalVisible => IsProductModalVisible || IsComboModalVisible;
    public bool IsPickupMode => !IsDeliveryMode;
    public string DeliverToLabel =>
        SelectedBranch?.Name
        ?? StoreHeader?.BranchName
        ?? "Select branch";
    public string SelectedCategoryTitle
    {
        get
        {
            if (IsFavoritesCategorySelected) return "❤️ Favorites";
            if (IsAllCategoriesSelected || SelectedCategories.Count == 0) return "All Menu";
            if (SelectedCategories.Count == 1) return $"{SelectedCategories.First()} Menu";
            var firstTwo = string.Join(", ", SelectedCategories.Take(2));
            return SelectedCategories.Count > 2 ? $"{firstTwo} +{SelectedCategories.Count - 2}" : $"{firstTwo} Menu";
        }
    }
    public string FilteredItemsLabel => $"({Products.Count})";
    public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);
    public bool HasActiveFilters => !IsAllCategoriesSelected || HasSearchText || !string.IsNullOrWhiteSpace(SelectedPill);
    public bool HasProducts => Products.Count > 0;

    public bool IsFavoritesCategorySelected =>
        string.Equals(SelectedCategory, "❤️ Favorites", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(SelectedCategory, "Favorites", StringComparison.OrdinalIgnoreCase);

    public string EmptyStateIcon => IsFavoritesCategorySelected ? "❤️" : "🔍";

    public string EmptyStateTitle =>
        IsFavoritesCategorySelected
            ? "No Favorites Yet"
            : (!string.IsNullOrWhiteSpace(SelectedPill)
                ? $"No items found for '{SelectedPill}'"
                : (!string.IsNullOrWhiteSpace(SearchText) ? "No matching products" : "No products available"));

    public string EmptyStateSubtitle =>
        IsFavoritesCategorySelected
            ? "No favorite items yet. Tap ❤️ on items to add them here!"
            : (!string.IsNullOrWhiteSpace(SelectedPill)
                ? "Try tapping the active capsule again to clear the filter."
                : "Try selecting another category or adjusting your search.");

    public CustomerHomeViewModel(
        OnlineOrderService orders,
        SessionService session,
        CartService cart,
        AbpApiClient api,
        AppSettings settings,
        DeliveryLocationService deliveryLocation,
        FavoritesService favorites)
    {
        _orders = orders;
        _session = session;
        _cart = cart;
        _api = api;
        _settings = settings;
        _deliveryLocation = deliveryLocation;
        _favorites = favorites;
        Title = "Online Order";
        _cart.CartChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CartCount));
            OnPropertyChanged(nameof(HasCartItems));
            OnPropertyChanged(nameof(CartSubtotal));
            OnPropertyChanged(nameof(CartSubtotalDisplay));
            OnPropertyChanged(nameof(CartCountBadge));
            OnPropertyChanged(nameof(IsFloatingCartBarVisible));
            SyncCartQtyBadges();
        };
        _favorites.FavoritesChanged += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                SyncFavorites();
            });
        };

        InitializeFilterPills();
    }

    private void InitializeFilterPills()
    {
        FilterPills.Clear();
        FilterPills.Add(new QuickFilterPillItem { Id = "near_me", Icon = "📍", Title = "Near Me" });
        FilterPills.Add(new QuickFilterPillItem { Id = "top_deals", Icon = "🔥", Title = "Top Deals" });
        FilterPills.Add(new QuickFilterPillItem { Id = "highly_rated", Icon = "⭐", Title = "Highly Rated" });
        FilterPills.Add(new QuickFilterPillItem { Id = "fast_delivery", Icon = "⚡", Title = "Fast Delivery" });
        FilterPills.Add(new QuickFilterPillItem { Id = "budget_picks", Icon = "🏷️", Title = "Budget Picks" });
    }

    partial void OnNeedsBranchSelectionChanged(bool value)
    {
        OnPropertyChanged(nameof(HasMenu));
        OnPropertyChanged(nameof(IsFloatingCartBarVisible));
    }
    partial void OnSelectedBranchChanged(OnlineBranchDto? value)
    {
        OnPropertyChanged(nameof(HasMenu));
        OnPropertyChanged(nameof(DeliverToLabel));
    }
    partial void OnIsDeliveryModeChanged(bool value) => OnPropertyChanged(nameof(IsPickupMode));
    partial void OnIsProductModalVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsAnyModalVisible));
        OnPropertyChanged(nameof(IsFloatingCartBarVisible));
    }
    partial void OnIsComboModalVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsAnyModalVisible));
        OnPropertyChanged(nameof(IsFloatingCartBarVisible));
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        // Same as website public storefront: no tenancy cookie required.
        _session.ClearTenantHeader();

        // Already on a branch: quiet re-appear (no center loader, no pull spinner).
        if (SelectedBranch != null && !NeedsBranchSelection)
        {
            return;
        }

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
            DealsList.Clear();
            OnPropertyChanged(nameof(HasDeals));
            ClearHeroSlides();
            _allProducts.Clear();
        });
    }

    [RelayCommand]
    private async Task RefreshHomeAsync()
    {
        if (IsPullRefreshing || IsBusy)
        {
            return;
        }

        try
        {
            IsPullRefreshing = true;
            _session.ClearTenantHeader();

            if (SelectedBranch == null)
            {
                await LoadBranchesAsync();
                return;
            }

            // Soft refresh: reload header/catalog without clearing the cart.
            var branch = SelectedBranch;
            StoreHeader = await _orders.GetStoreHeaderAsync(branch.Id);
            await LoadHeroSlidesAsync(branch.Id);
            _allProducts = await _orders.GetCatalogAsync(branch.Id, forceRefresh: true);
            foreach (var p in _allProducts)
            {
                p.ImageUrl = _api.ResolveImageUrl(p.ImagePath);
                p.IsFavorite = _favorites.IsFavorite(p.Id);
            }

            RebuildCategories();
            ApplyFilters();
            await LoadDealsAsync(branch.Id);
            _ = LoadFeedbacksAsync(branch.Id);
            _orders.PrefetchModifiersForCatalog(_allProducts);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsPullRefreshing = false;
        }
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
        _favorites.Initialize(branch.Id);

        await LoadHeroSlidesAsync(branch.Id);

        // Cached catalog: first open hits API; later visits / re-appear are instant.
        _allProducts = await _orders.GetCatalogAsync(branch.Id);
        foreach (var p in _allProducts)
        {
            p.ImageUrl = _api.ResolveImageUrl(p.ImagePath);
            p.IsFavorite = _favorites.IsFavorite(p.Id);
        }

        SelectedCategory = "All";
        SearchText = string.Empty;
        CategorySearchText = string.Empty;
        IsCategorySearchVisible = false;
        ShowProductGrid = true;
        RebuildCategories();
        ApplyFilters();

        await LoadDealsAsync(branch.Id);
        _ = LoadFeedbacksAsync(branch.Id);

        // Warm size/modifier options so add-to-cart popup feels instant.
        _orders.PrefetchModifiersForCatalog(_allProducts);

        // Start live delivery pin early (Deliver-to / address flow).
        _ = _deliveryLocation.CaptureAsync();
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

        _allCategoryChips.Clear();
        ExploreCategories.Clear();

        _allCategoryChips.Add(new CategoryChipItem
        {
            Name = "All",
            Count = _allProducts.Count,
            IsSelected = string.Equals(SelectedCategory, "All", StringComparison.OrdinalIgnoreCase)
        });

        _allCategoryChips.Add(new CategoryChipItem
        {
            Name = "❤️ Favorites",
            Count = _allProducts.Count(p => p.IsFavorite),
            IsSelected = IsFavoritesCategorySelected
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
                IsSelected = SelectedCategories.Contains(name)
            };
            _allCategoryChips.Add(chip);
            ExploreCategories.Add(chip);
        }

        ApplyCategorySearchFilter();
        OnPropertyChanged(nameof(DeliverToLabel));
    }

    private void ApplyCategorySearchFilter()
    {
        Categories.Clear();
        IEnumerable<CategoryChipItem> query = _allCategoryChips;
        if (!string.IsNullOrWhiteSpace(CategorySearchText))
        {
            var q = CategorySearchText.Trim();
            query = _allCategoryChips.Where(c =>
                c.IsAll ||
                c.IsFavorites ||
                (c.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        foreach (var chip in query)
        {
            chip.IsSelected = chip.IsAll
                ? IsAllCategoriesSelected
                : (chip.IsFavorites ? IsFavoritesCategorySelected : SelectedCategories.Contains(chip.Name));
            Categories.Add(chip);
        }
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

    private IDispatcherTimer? _feedbackTimer;
    private bool _isFeedbackPaused;

    private void StartFeedbackAutoAdvance()
    {
        StopFeedbackAutoAdvance();
        if (Feedbacks.Count <= 1 || Application.Current?.Dispatcher is null)
        {
            return;
        }

        _feedbackTimer = Application.Current.Dispatcher.CreateTimer();
        _feedbackTimer.Interval = TimeSpan.FromMilliseconds(3800);
        _feedbackTimer.Tick += OnFeedbackTimerTick;
        _feedbackTimer.Start();
    }

    private void OnFeedbackTimerTick(object? sender, EventArgs e)
    {
        if (_isFeedbackPaused || Feedbacks.Count == 0)
        {
            return;
        }

        FeedbackPosition = (FeedbackPosition + 1) % Feedbacks.Count;
    }

    private void StopFeedbackAutoAdvance()
    {
        if (_feedbackTimer is null)
        {
            return;
        }

        _feedbackTimer.Tick -= OnFeedbackTimerTick;
        _feedbackTimer.Stop();
        _feedbackTimer = null;
    }

    [RelayCommand]
    public void PauseFeedbackAutoAdvance() => _isFeedbackPaused = true;

    [RelayCommand]
    public void ResumeFeedbackAutoAdvance() => _isFeedbackPaused = false;

    public void OnPageDisappearing()
    {
        StopHeroAutoAdvance();
        StopFeedbackAutoAdvance();
    }

    private async Task LoadFeedbacksAsync(int branchId)
    {
        try
        {
            IsLoadingFeedbacks = true;
            Feedbacks.Clear();
            var list = await _orders.GetBranchFeedbacksAsync(branchId);
            if (list == null || list.Count == 0)
            {
                list = CreateDefaultFeedbacks(SelectedBranch?.Name ?? "Our Kitchen");
            }

            var localAvatarFile = Path.Combine(FileSystem.AppDataDirectory, "customer_avatar.jpg");
            var localAvatarExists = File.Exists(localAvatarFile);
            var currentCustomerName = Preferences.Default.Get("sp.customerName", string.Empty).Trim();

            foreach (var item in list)
            {
                if (localAvatarExists && !string.IsNullOrWhiteSpace(currentCustomerName)
                    && (string.Equals(item.CustomerName?.Trim(), currentCustomerName, StringComparison.OrdinalIgnoreCase)
                        || (currentCustomerName.Length >= 4 && item.CustomerName != null && item.CustomerName.Contains(currentCustomerName, StringComparison.OrdinalIgnoreCase))))
                {
                    item.CustomerPhotoUrl = localAvatarFile;
                }

                Feedbacks.Add(item);
            }

            if (Feedbacks.Count > 0)
            {
                var avg = Feedbacks.Average(f => f.Rating);
                AverageRating = Math.Round(avg, 1);
                RatingScoreDisplay = AverageRating.ToString("0.0");
            }

            FeedbackPosition = 0;
            OnPropertyChanged(nameof(BranchFeedbacks));
            OnPropertyChanged(nameof(HasFeedbacks));
            OnPropertyChanged(nameof(TotalReviewsLabel));
            OnPropertyChanged(nameof(TotalReviewsCount));
            OnPropertyChanged(nameof(TotalReviewsSubtitle));
            OnPropertyChanged(nameof(AverageRatingScore));
            StartFeedbackAutoAdvance();
        }
        catch
        {
            var defaults = CreateDefaultFeedbacks(SelectedBranch?.Name ?? "Our Kitchen");
            foreach (var item in defaults)
            {
                Feedbacks.Add(item);
            }
            FeedbackPosition = 0;
            OnPropertyChanged(nameof(BranchFeedbacks));
            OnPropertyChanged(nameof(HasFeedbacks));
            OnPropertyChanged(nameof(TotalReviewsLabel));
            OnPropertyChanged(nameof(TotalReviewsCount));
            OnPropertyChanged(nameof(TotalReviewsSubtitle));
            OnPropertyChanged(nameof(AverageRatingScore));
            StartFeedbackAutoAdvance();
        }
        finally
        {
            IsLoadingFeedbacks = false;
        }
    }

    private static List<CustomerFeedbackDto> CreateDefaultFeedbacks(string branchName) =>
    [
        new CustomerFeedbackDto
        {
            OrderId = 101,
            CustomerName = "Hamza Ali",
            Rating = 5,
            OrderDate = DateTime.Now.AddDays(-1),
            BranchName = branchName,
            Comment = "Food was piping hot and delivered in just 25 minutes! Best burgers in town.",
            ItemsSummary = "Zinger Supreme Meal • Loaded Fries"
        },
        new CustomerFeedbackDto
        {
            OrderId = 102,
            CustomerName = "Ayesha Khan",
            Rating = 5,
            OrderDate = DateTime.Now.AddDays(-2),
            BranchName = branchName,
            Comment = "Pizza cheese pull was incredible! Highly recommend the family combo deal.",
            ItemsSummary = "Large Ranch Pizza • Garlic Bread"
        },
        new CustomerFeedbackDto
        {
            OrderId = 103,
            CustomerName = "Usman Tariq",
            Rating = 5,
            OrderDate = DateTime.Now.AddDays(-3),
            BranchName = branchName,
            Comment = "Everything was fresh, properly packed, and delicious. Great experience!",
            ItemsSummary = "Double Patty Beef Burger • Mint Mojito"
        },
        new CustomerFeedbackDto
        {
            OrderId = 104,
            CustomerName = "Zainab Fatima",
            Rating = 5,
            OrderDate = DateTime.Now.AddDays(-4),
            BranchName = branchName,
            Comment = "Crispy wings with honey mustard sauce are a must try. Fast delivery!",
            ItemsSummary = "10 Pcs Honey Glazed Wings • Drink"
        }
    ];

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

    // =========================================================================
    // 🔥 DEALS FETCHING & DATA ENGINE
    // =========================================================================
    public async Task LoadDealsAsync(int branchId)
    {
        try
        {
            IsLoadingDeals = true;
            DealsList.Clear();

            // 1. Check for explicit deals already present in catalog
            var catalogDeals = _allProducts
                .Where(p => p.IsDeal || (p.CategoryName != null && p.CategoryName.Contains("deal", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var d in catalogDeals)
            {
                if (string.IsNullOrWhiteSpace(d.DealItemsSummary))
                {
                    d.DealItemsSummary = d.Description ?? "Combo Deal Bundle";
                }
                DealsList.Add(d);
            }

            // 2. Fetch active deals from the dedicated DealAppService backend endpoint
            var remoteDeals = await _orders.GetActiveDealsAsync(branchId);
            foreach (var rd in remoteDeals.Where(x => x.IsActive && !x.IsExpired))
            {
                if (DealsList.Any(x => x.Id == rd.ProductId || (rd.ProductId.HasValue && x.Id == rd.ProductId.Value) || x.DealId == rd.Id))
                {
                    continue;
                }

                var summary = rd.Items != null && rd.Items.Count > 0
                    ? string.Join(" • ", rd.Items.Select(i => $"{i.Quantity}x {i.ProductName}"))
                    : (rd.Description ?? "Special Combo Deal");

                DealsList.Add(new OnlineProductDto
                {
                    Id = rd.ProductId ?? -rd.Id,
                    DealId = rd.Id,
                    Name = rd.Name,
                    Description = rd.Description,
                    CategoryName = "Deals",
                    Price = rd.DealPrice,
                    OriginalPrice = rd.TotalOriginalPrice > 0 ? rd.TotalOriginalPrice : null,
                    ImagePath = rd.ImagePath,
                    ImageUrl = rd.ImageUrl,
                    InStock = true,
                    IsDeal = true,
                    DealItemsSummary = summary
                });
            }

            // 3. Fallback: If no explicit deals in DB yet, create mouth-watering combo deals linked to catalog items
            if (DealsList.Count == 0)
            {
                var fallbackDeals = CreateDefaultComboDeals();
                foreach (var fd in fallbackDeals)
                {
                    DealsList.Add(fd);
                }
            }

            OnPropertyChanged(nameof(HasDeals));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadDealsAsync] Error: {ex.Message}");
        }
        finally
        {
            IsLoadingDeals = false;
        }
    }

    private List<OnlineProductDto> CreateDefaultComboDeals()
    {
        return
        [
            new OnlineProductDto
            {
                Id = -101,
                DealId = 101,
                Name = "🔥 Mega Crunch Feast Combo",
                Description = "Crispy golden chicken fillet zinger burger, seasoned cheese fries, and a chilled can drink.",
                CategoryName = "Deals",
                Price = 890,
                OriginalPrice = 1060,
                InStock = true,
                IsDeal = true,
                ImageUrl = "https://images.unsplash.com/photo-1550547660-d9450f859349?auto=format&fit=crop&w=720&q=80",
                DealItemsSummary = "🍔 1x Zinger Burger • 🍟 1x Regular Fries • 🥤 1x Soft Drink"
            },
            new OnlineProductDto
            {
                Id = -102,
                DealId = 102,
                Name = "👑 Royal Pizza & Wings Deal",
                Description = "Medium cheesy specialty pizza, 10 pcs spicy hot wings, and a large cold drink for pizza lovers.",
                CategoryName = "Deals",
                Price = 1490,
                OriginalPrice = 1720,
                InStock = true,
                IsDeal = true,
                ImageUrl = "https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=720&q=80",
                DealItemsSummary = "🍕 1x Medium Pizza • 🍗 10x Hot Wings • 🥤 1x Soft Drink"
            },
            new OnlineProductDto
            {
                Id = -103,
                DealId = 103,
                Name = "🍔 Duo Burger Buddy Pack",
                Description = "Two handcrafted crispy burgers, 2 golden regular fries, and 2 chilled soft drinks for you and a friend.",
                CategoryName = "Deals",
                Price = 1290,
                OriginalPrice = 1520,
                InStock = true,
                IsDeal = true,
                ImageUrl = "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=720&q=80",
                DealItemsSummary = "🍔 2x Burgers • 🍟 2x Fries • 🥤 2x Drinks"
            },
            new OnlineProductDto
            {
                Id = -104,
                DealId = 104,
                Name = "🍢 Smoky BBQ Platter Feast",
                Description = "Tender grilled chicken malai boti, authentic seekh kebabs, 2 hot roghni naans, and a chilled drink.",
                CategoryName = "Deals",
                Price = 1390,
                OriginalPrice = 1550,
                InStock = true,
                IsDeal = true,
                ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?auto=format&fit=crop&w=720&q=80",
                DealItemsSummary = "🍢 1x Malai Boti • 🍢 1x Seekh Kabab • 🫓 2x Naan • 🥤 1x Drink"
            }
        ];
    }

    [RelayCommand]
    private async Task ChangeBranchAsync()
    {
        _cart.Clear();
        SelectedBranch = null;
        NeedsBranchSelection = true;
        StoreHeader = null;
        Products.Clear();
        Categories.Clear();
        DealsList.Clear();
        OnPropertyChanged(nameof(HasDeals));
        ClearHeroSlides();
        _allProducts.Clear();
        await _session.ClearBranchAsync();
        _session.ClearTenantHeader();
        await RunBusyAsync(LoadBranchesAsync);
    }

    [RelayCommand]
    public void ToggleCategory(CategoryChipItem? chip)
    {
        if (chip == null) return;

        if (chip.IsAll)
        {
            SelectedCategories.Clear();
            IsAllCategoriesSelected = true;
            foreach (var c in _allCategoryChips)
            {
                c.IsSelected = c.IsAll;
            }
        }
        else
        {
            var targetName = chip.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(targetName)) return;

            if (SelectedCategories.Contains(targetName))
            {
                SelectedCategories.Remove(targetName);
                chip.IsSelected = false;
            }
            else
            {
                SelectedCategories.Add(targetName);
                chip.IsSelected = true;
            }

            var allChip = _allCategoryChips.FirstOrDefault(c => c.IsAll);
            if (SelectedCategories.Count > 0)
            {
                if (allChip != null) allChip.IsSelected = false;
                IsAllCategoriesSelected = false;
            }
            else
            {
                if (allChip != null) allChip.IsSelected = true;
                IsAllCategoriesSelected = true;
            }
        }

        UpdateCategorySelectionTitles();
        ApplyCategorySearchFilter();
        ShowProductGrid = true;
        ApplyFilters();
    }

    [RelayCommand]
    private void SelectCategory(CategoryChipItem? category)
    {
        ToggleCategory(category);
    }

    [RelayCommand]
    private void SelectCategoryByName(string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return;
        }

        if (string.Equals(categoryName, "More", StringComparison.OrdinalIgnoreCase))
        {
            _ = ViewAllMenuAsync();
            return;
        }

        if (string.Equals(categoryName, "Promo", StringComparison.OrdinalIgnoreCase))
        {
            SelectedPill = "Top Deals";
            ApplyFilters();
            return;
        }

        var chip = _allCategoryChips.FirstOrDefault(c => string.Equals(c.Name?.Trim(), categoryName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (chip != null)
        {
            ToggleCategory(chip);
        }
        else
        {
            var targetName = categoryName.Trim();
            if (SelectedCategories.Contains(targetName))
            {
                SelectedCategories.Remove(targetName);
            }
            else
            {
                SelectedCategories.Add(targetName);
            }

            IsAllCategoriesSelected = SelectedCategories.Count == 0;
            var allChip = _allCategoryChips.FirstOrDefault(c => c.IsAll);
            if (allChip != null) allChip.IsSelected = IsAllCategoriesSelected;

            foreach (var c in _allCategoryChips)
            {
                c.IsSelected = c.IsAll ? IsAllCategoriesSelected : SelectedCategories.Contains(c.Name?.Trim() ?? "");
            }

            UpdateCategorySelectionTitles();
            ApplyCategorySearchFilter();
            ShowProductGrid = true;
            ApplyFilters();
        }
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchText = string.Empty;
        ApplyFilters();
    }

    [RelayCommand]
    public void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedPill = string.Empty;
        foreach (var p in FilterPills) p.IsSelected = false;
        SelectedCategories.Clear();
        foreach (var c in _allCategoryChips)
        {
            c.IsSelected = c.IsAll;
        }
        IsAllCategoriesSelected = true;
        UpdateCategorySelectionTitles();
        ApplyFilters();
    }

    private void UpdateCategorySelectionTitles()
    {
        if (IsAllCategoriesSelected || SelectedCategories.Count == 0)
        {
            SelectedCategory = "All";
        }
        else if (SelectedCategories.Count == 1)
        {
            SelectedCategory = SelectedCategories.First();
        }
        else
        {
            var firstTwo = string.Join(", ", SelectedCategories.Take(2));
            SelectedCategory = SelectedCategories.Count > 2
                ? $"{firstTwo} +{SelectedCategories.Count - 2}"
                : firstTwo;
        }

        OnPropertyChanged(nameof(SelectedCategoryTitle));
        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(IsFavoritesCategorySelected));
    }

    [RelayCommand]
    private async Task NavigateToProductDetailAsync(object? param)
    {
        int productId = 0;
        if (param is int id)
        {
            productId = id;
        }
        else if (param is string s && int.TryParse(s, out var parsed))
        {
            productId = parsed;
        }
        else if (param is OnlineProductDto p)
        {
            productId = p.Id;
        }
        else
        {
            productId = Products.FirstOrDefault()?.Id ?? 1;
        }

        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync($"productdetail?productId={productId}");
        }
    }

    [RelayCommand]
    private void ToggleCategorySearch()
    {
        IsCategorySearchVisible = !IsCategorySearchVisible;
        if (!IsCategorySearchVisible)
        {
            CategorySearchText = string.Empty;
            ApplyCategorySearchFilter();
        }
    }

    [RelayCommand]
    private void ClearCategorySearch()
    {
        CategorySearchText = string.Empty;
        ApplyCategorySearchFilter();
    }

    [RelayCommand]
    private async Task ViewAllMenuAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//customer/order");
        }
    }

    [RelayCommand]
    private async Task OrderNowAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//customer/order");
        }
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

    partial void OnCategorySearchTextChanged(string value) => ApplyCategorySearchFilter();

    partial void OnSelectedCategoryChanged(string value)
    {
        OnPropertyChanged(nameof(SelectedCategoryTitle));
    }

    private void ApplyFilters()
    {
        Products.Clear();
        IEnumerable<OnlineProductDto> query = _allProducts;
        if (IsFavoritesCategorySelected)
        {
            query = query.Where(p => p.IsFavorite);
        }
        else if (!IsAllCategoriesSelected && SelectedCategories.Count > 0)
        {
            query = query.Where(p =>
                !string.IsNullOrWhiteSpace(p.CategoryName) &&
                SelectedCategories.Contains(p.CategoryName));
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var st = SearchText.Trim();
            query = query.Where(p =>
                (p.Name?.Contains(st, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Description?.Contains(st, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.CategoryName?.Contains(st, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        // 3. Smart Quick Capsule Filters matching Web logic
        if (!string.IsNullOrWhiteSpace(SelectedPill))
        {
            if (SelectedPill.Contains("Top Deals", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p =>
                    p.IsDeal ||
                    (p.CategoryName?.Contains("deal", StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Name?.Contains("deal", StringComparison.OrdinalIgnoreCase) ?? false));
            }
            else if (SelectedPill.Contains("Highly Rated", StringComparison.OrdinalIgnoreCase))
            {
                query = query
                    .Where(p => (4.7 + ((Math.Abs(p.Id) % 4) * 0.1)) >= 4.8)
                    .OrderByDescending(p => 4.7 + ((Math.Abs(p.Id) % 4) * 0.1));
            }
            else if (SelectedPill.Contains("Budget Picks", StringComparison.OrdinalIgnoreCase))
            {
                query = query
                    .Where(p => p.Price > 0)
                    .OrderBy(p => p.Price);
            }
            else if (SelectedPill.Contains("Fast Delivery", StringComparison.OrdinalIgnoreCase))
            {
                query = query
                    .Where(p => p.InStock)
                    .OrderByDescending(p => p.IsFreeDelivery);
            }
            else if (SelectedPill.Contains("Near Me", StringComparison.OrdinalIgnoreCase))
            {
                query = query
                    .Where(p => p.InStock);
            }
        }

        foreach (var p in query)
        {
            p.CartQty = _cart.GetQty(p.Id);
            Products.Add(p);
        }

        OnPropertyChanged(nameof(FilteredItemsLabel));
        OnPropertyChanged(nameof(SelectedCategoryTitle));
        OnPropertyChanged(nameof(IsFavoritesCategorySelected));
        OnPropertyChanged(nameof(EmptyStateIcon));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateSubtitle));
        OnPropertyChanged(nameof(HasProducts));
        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(HasSearchText));
    }

    [RelayCommand]
    private void SelectPill(QuickFilterPillItem? pill)
    {
        if (pill == null)
        {
            return;
        }

        if (string.Equals(SelectedPill, pill.Title, StringComparison.OrdinalIgnoreCase))
        {
            SelectedPill = string.Empty;
            pill.IsSelected = false;
        }
        else
        {
            SelectedPill = pill.Title;
            foreach (var p in FilterPills)
            {
                p.IsSelected = string.Equals(p.Title, pill.Title, StringComparison.OrdinalIgnoreCase);
            }
        }

        ApplyFilters();
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(OnlineProductDto? product)
    {
        if (product == null)
        {
            return;
        }

        var isFav = await _favorites.ToggleFavoriteAsync(product.Id);
        product.IsFavorite = isFav;

        var favChip = _allCategoryChips.FirstOrDefault(c => c.IsFavorites);
        if (favChip != null)
        {
            favChip.Count = _allProducts.Count(p => p.IsFavorite);
        }

        if (IsFavoritesCategorySelected)
        {
            ApplyFilters();
        }
    }

    private void SyncFavorites()
    {
        foreach (var p in _allProducts)
        {
            p.IsFavorite = _favorites.IsFavorite(p.Id);
        }

        var favChip = _allCategoryChips.FirstOrDefault(c => c.IsFavorites);
        if (favChip != null)
        {
            favChip.Count = _allProducts.Count(p => p.IsFavorite);
        }

        if (IsFavoritesCategorySelected)
        {
            ApplyFilters();
        }
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
    private async Task IncreaseCardAsync(OnlineProductDto? product)
    {
        if (product == null || !product.InStock)
        {
            return;
        }

        // If product is a deal/combo, open combo customization modal!
        if (product.IsDeal || (product.CategoryName != null && product.CategoryName.Contains("deal", StringComparison.OrdinalIgnoreCase)))
        {
            await OpenDealModalAsync(product);
            return;
        }

        // Open options only when the product has modifiers; otherwise add straight to cart.
        await OpenProductModalAsync(product);
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

        // If product is a deal/combo, open combo customization modal!
        if (product.IsDeal || (product.CategoryName != null && product.CategoryName.Contains("deal", StringComparison.OrdinalIgnoreCase)))
        {
            await OpenDealModalAsync(product);
            return;
        }

        await OpenProductModalAsync(product);
    }

    // =========================================================================
    // 🍔 COMBO CUSTOMIZATION MODAL (BOTTOM SHEET)
    // =========================================================================
    [RelayCommand]
    public async Task OpenDealModalAsync(OnlineProductDto? dealProduct)
    {
        if (dealProduct == null)
        {
            return;
        }

        ComboProduct = dealProduct;
        ComboQuantity = 1;
        DealCustomiseItems.Clear();
        IsLoadingComboDeal = true;
        IsComboModalVisible = true;
        RefreshComboTotal();

        try
        {
            DealDto? deal = null;
            if (dealProduct.DealId.HasValue && dealProduct.DealId.Value > 0)
            {
                deal = await _orders.GetDealByIdAsync(dealProduct.DealId.Value);
            }
            if (deal == null && dealProduct.Id > 0)
            {
                deal = await _orders.GetDealByProductIdAsync(dealProduct.Id);
            }

            var itemsToCustomise = new List<DealCustomiseItem>();

            if (deal != null && deal.Items != null && deal.Items.Count > 0)
            {
                foreach (var item in deal.Items)
                {
                    var customItem = new DealCustomiseItem
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName ?? $"Item #{item.ProductId}",
                        Quantity = item.Quantity > 0 ? item.Quantity : 1,
                        UnitPrice = item.UnitPrice
                    };

                    var groups = await _orders.GetModifiersForProductAsync(item.ProductId);
                    if (groups.Count == 0)
                    {
                        groups = CreateFallbackModifiersForName(customItem.ProductName);
                    }

                    PopulateModifierGroups(customItem, groups);
                    itemsToCustomise.Add(customItem);
                }
            }
            else
            {
                // Fallback / simulated combo items for rich immediate testing
                itemsToCustomise = CreateFallbackComboItems(dealProduct);
            }

            DealCustomiseItems.Clear();
            foreach (var itm in itemsToCustomise)
            {
                DealCustomiseItems.Add(itm);
            }

            OnPropertyChanged(nameof(HasComboItems));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpenDealModalAsync] Error: {ex.Message}");
            // Still populate fallback combo items so user is never stuck
            var fallback = CreateFallbackComboItems(dealProduct);
            DealCustomiseItems.Clear();
            foreach (var itm in fallback)
            {
                DealCustomiseItems.Add(itm);
            }
            OnPropertyChanged(nameof(HasComboItems));
        }
        finally
        {
            for (var i = 0; i < DealCustomiseItems.Count; i++)
            {
                DealCustomiseItems[i].ItemIndex = i + 1;
                DealCustomiseItems[i].TotalItemCount = DealCustomiseItems.Count;
                DealCustomiseItems[i].NotifyStateChanged();
            }

            IsLoadingComboDeal = false;
            ValidateComboSelections();
            RefreshComboTotal();
        }
    }

    private List<DealCustomiseItem> CreateFallbackComboItems(OnlineProductDto dealProduct)
    {
        var list = new List<DealCustomiseItem>();
        var name = (dealProduct.Name ?? string.Empty).ToLowerInvariant();

        if (name.Contains("pizza"))
        {
            var pizzaItem = new DealCustomiseItem { ProductId = 1160, ProductName = "Chicken Tikka Pizza (Medium)", Quantity = 1 };
            PopulateModifierGroups(pizzaItem,
            [
                new ProductModifierGroupDto
                {
                    Id = 201,
                    Name = "Crust & Cheese",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 2001, Name = "Pan Crust (Crispy)", IsDefault = true },
                        new ProductModifierOptionDto { Id = 2002, Name = "Thin Crust (Traditional)" },
                        new ProductModifierOptionDto { Id = 2003, Name = "Cheese Stuffed Crust (+PKR 180)", Price = 180 }
                    ]
                },
                new ProductModifierGroupDto
                {
                    Id = 202,
                    Name = "Special Dip Sauce",
                    IsRequired = false,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 2004, Name = "Garlic Mayo Dip (+PKR 50)", Price = 50 },
                        new ProductModifierOptionDto { Id = 2005, Name = "Spicy Ranch (+PKR 60)", Price = 60 }
                    ]
                }
            ]);
            list.Add(pizzaItem);

            var wingsItem = new DealCustomiseItem { ProductId = 1155, ProductName = "Hot Wings (10 Pcs)", Quantity = 1 };
            PopulateModifierGroups(wingsItem,
            [
                new ProductModifierGroupDto
                {
                    Id = 203,
                    Name = "Wings Flavor",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 2006, Name = "Spicy Buffalo Glaze", IsDefault = true },
                        new ProductModifierOptionDto { Id = 2007, Name = "Smoky BBQ Sauce" },
                        new ProductModifierOptionDto { Id = 2008, Name = "Crispy Fried (No Sauce)" }
                    ]
                }
            ]);
            list.Add(wingsItem);

            var drinkItem = new DealCustomiseItem { ProductId = 1206, ProductName = "Chilled Beverage (1.5L)", Quantity = 1 };
            PopulateModifierGroups(drinkItem,
            [
                new ProductModifierGroupDto
                {
                    Id = 204,
                    Name = "Drink Choice",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 2009, Name = "Pepsi 1.5L", IsDefault = true },
                        new ProductModifierOptionDto { Id = 2010, Name = "7Up 1.5L" },
                        new ProductModifierOptionDto { Id = 2011, Name = "Mirinda 1.5L" }
                    ]
                }
            ]);
            list.Add(drinkItem);
        }
        else if (name.Contains("bbq") || name.Contains("platter"))
        {
            var botiItem = new DealCustomiseItem { ProductId = 1171, ProductName = "Chicken Malai Boti", Quantity = 1 };
            PopulateModifierGroups(botiItem,
            [
                new ProductModifierGroupDto
                {
                    Id = 301,
                    Name = "Spice & Herb Level",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 3001, Name = "Creamy Mild Malai", IsDefault = true },
                        new ProductModifierOptionDto { Id = 3002, Name = "Medium Masala Boti" }
                    ]
                }
            ]);
            list.Add(botiItem);

            var kababItem = new DealCustomiseItem { ProductId = 1172, ProductName = "Chicken Seekh Kabab (4 Pcs)", Quantity = 1 };
            PopulateModifierGroups(kababItem,
            [
                new ProductModifierGroupDto
                {
                    Id = 302,
                    Name = "Chutney & Sauce",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 3003, Name = "Fresh Mint Raita", IsDefault = true },
                        new ProductModifierOptionDto { Id = 3004, Name = "Spicy Imli Chutney" }
                    ]
                }
            ]);
            list.Add(kababItem);

            var naanItem = new DealCustomiseItem { ProductId = 1202, ProductName = "Roghni Naan", Quantity = 2 };
            list.Add(naanItem);
        }
        else
        {
            // Standard Burger / Zinger / Fast Food combo
            var burgerItem = new DealCustomiseItem { ProductId = 1149, ProductName = "Zinger Burger", Quantity = name.Contains("duo") ? 2 : 1 };
            PopulateModifierGroups(burgerItem,
            [
                new ProductModifierGroupDto
                {
                    Id = 101,
                    Name = "Spice Level",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 1001, Name = "Spicy Crispy Zinger", IsDefault = true },
                        new ProductModifierOptionDto { Id = 1002, Name = "Mild / Normal Crunch" }
                    ]
                },
                new ProductModifierGroupDto
                {
                    Id = 102,
                    Name = "Extra Toppings",
                    IsRequired = false,
                    AllowMultiple = true,
                    MaxSelection = 2,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 1003, Name = "Extra Golden Cheese Slice (+PKR 60)", Price = 60 },
                        new ProductModifierOptionDto { Id = 1004, Name = "Spicy Jalapenos (+PKR 40)", Price = 40 }
                    ]
                }
            ]);
            list.Add(burgerItem);

            var friesItem = new DealCustomiseItem { ProductId = 1157, ProductName = "French Fries", Quantity = name.Contains("duo") ? 2 : 1 };
            PopulateModifierGroups(friesItem,
            [
                new ProductModifierGroupDto
                {
                    Id = 103,
                    Name = "Fries Seasoning",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 1005, Name = "Classic Salted", IsDefault = true },
                        new ProductModifierOptionDto { Id = 1006, Name = "Spicy Chatpata Masala" },
                        new ProductModifierOptionDto { Id = 1007, Name = "Melted Cheese Sauce (+PKR 70)", Price = 70 }
                    ]
                }
            ]);
            list.Add(friesItem);

            var drinkItem = new DealCustomiseItem { ProductId = 1206, ProductName = "Soft Drink (Can 250ml)", Quantity = name.Contains("duo") ? 2 : 1 };
            PopulateModifierGroups(drinkItem,
            [
                new ProductModifierGroupDto
                {
                    Id = 104,
                    Name = "Drink Choice",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 1008, Name = "Pepsi Cold Can", IsDefault = true },
                        new ProductModifierOptionDto { Id = 1009, Name = "7Up / Sprite Can" },
                        new ProductModifierOptionDto { Id = 1010, Name = "Mirinda / Fanta Can" },
                        new ProductModifierOptionDto { Id = 1011, Name = "Mountain Dew Can" }
                    ]
                }
            ]);
            list.Add(drinkItem);
        }

        return list;
    }

    private static List<ProductModifierGroupDto> CreateFallbackModifiersForName(string productName)
    {
        var lower = (productName ?? string.Empty).ToLowerInvariant();
        if (lower.Contains("burger") || lower.Contains("zinger"))
        {
            return
            [
                new ProductModifierGroupDto
                {
                    Id = 101,
                    Name = "Spice Level",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 1001, Name = "Spicy Crunch", IsDefault = true },
                        new ProductModifierOptionDto { Id = 1002, Name = "Mild Normal" }
                    ]
                }
            ];
        }

        if (lower.Contains("drink") || lower.Contains("beverage") || lower.Contains("soft"))
        {
            return
            [
                new ProductModifierGroupDto
                {
                    Id = 104,
                    Name = "Drink Flavour",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 1008, Name = "Pepsi", IsDefault = true },
                        new ProductModifierOptionDto { Id = 1009, Name = "7Up / Sprite" },
                        new ProductModifierOptionDto { Id = 1010, Name = "Mirinda" }
                    ]
                }
            ];
        }

        return new List<ProductModifierGroupDto>();
    }

    private static void PopulateModifierGroups(DealCustomiseItem item, List<ProductModifierGroupDto> groups)
    {
        item.ModifierGroups.Clear();
        foreach (var g in groups.Where(x => x.Options is { Count: > 0 }))
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
                var optionItem = new ModifierOptionItem
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

                if (optionItem.IsSelected)
                {
                    defaultApplied = true;
                }

                groupItem.Options.Add(optionItem);
            }

            if (groupItem.IsRequired && !groupItem.Options.Any(o => o.IsSelected) && groupItem.Options.Count > 0)
            {
                groupItem.Options[0].IsSelected = true;
            }

            item.ModifierGroups.Add(groupItem);
        }
    }

    [RelayCommand]
    private void SelectComboOption(ModifierOptionItem? option)
    {
        if (option == null)
        {
            return;
        }

        foreach (var item in DealCustomiseItems)
        {
            var group = item.ModifierGroups.FirstOrDefault(g => g.Id == option.GroupId);
            if (group != null)
            {
                if (group.AllowMultiple || group.MaxSelection > 1)
                {
                    if (option.IsSelected)
                    {
                        if (group.IsRequired && group.Options.Count(o => o.IsSelected) <= 1)
                        {
                            return;
                        }
                        option.IsSelected = false;
                    }
                    else
                    {
                        var selectedCount = group.Options.Count(o => o.IsSelected);
                        if (group.MaxSelection > 0 && selectedCount >= group.MaxSelection)
                        {
                            var first = group.Options.FirstOrDefault(o => o.IsSelected);
                            if (first != null) first.IsSelected = false;
                        }
                        option.IsSelected = true;
                    }
                }
                else
                {
                    foreach (var o in group.Options)
                    {
                        o.IsSelected = ReferenceEquals(o, option);
                    }
                }
                item.NotifyStateChanged();
                break;
            }
        }

        ValidateComboSelections();
        RefreshComboTotal();
    }

    private bool ValidateComboSelections()
    {
        if (DealCustomiseItems == null || DealCustomiseItems.Count == 0)
        {
            IsComboValidationPassed = true;
            ComboValidationMessage = string.Empty;
            OnPropertyChanged(nameof(CanAddComboToCart));
            OnPropertyChanged(nameof(HasComboValidationMessage));
            return true;
        }

        foreach (var item in DealCustomiseItems)
        {
            item.NotifyStateChanged();
            foreach (var group in item.ModifierGroups.Where(g => g.IsRequired))
            {
                if (!group.Options.Any(o => o.IsSelected))
                {
                    IsComboValidationPassed = false;
                    ComboValidationMessage = $"Please select {group.Name} for {item.ProductName}.";
                    OnPropertyChanged(nameof(CanAddComboToCart));
                    OnPropertyChanged(nameof(HasComboValidationMessage));
                    return false;
                }
            }
        }

        IsComboValidationPassed = true;
        ComboValidationMessage = string.Empty;
        OnPropertyChanged(nameof(CanAddComboToCart));
        OnPropertyChanged(nameof(HasComboValidationMessage));
        return true;
    }

    [RelayCommand]
    private void IncreaseComboQty()
    {
        ComboQuantity++;
        RefreshComboTotal();
    }

    [RelayCommand]
    private void DecreaseComboQty()
    {
        if (ComboQuantity <= 1)
        {
            return;
        }

        ComboQuantity--;
        RefreshComboTotal();
    }

    private void RefreshComboTotal()
    {
        if (ComboProduct == null)
        {
            ComboTotalLabel = "Rs 0";
            return;
        }

        var extra = DealCustomiseItems
            .SelectMany(i => i.ModifierGroups)
            .SelectMany(g => g.Options.Where(o => o.IsSelected))
            .Sum(o => o.Price);

        var total = (ComboProduct.Price + extra) * ComboQuantity;
        ComboTotalLabel = $"Rs {total:0}";
        OnPropertyChanged(nameof(ComboTotalLabel));
    }

    [RelayCommand]
    private void AddComboToCart()
    {
        if (ComboProduct == null)
        {
            return;
        }

        // Validate mandatory selections for every item in the combo
        if (!ValidateComboSelections())
        {
            if (Shell.Current != null && !string.IsNullOrWhiteSpace(ComboValidationMessage))
            {
                _ = Shell.Current.DisplayAlert("Choice Required", ComboValidationMessage, "OK");
            }
            return;
        }

        var itemSummaries = new List<string>();
        foreach (var item in DealCustomiseItems)
        {
            var selectedOpts = item.ModifierGroups
                .SelectMany(g => g.Options.Where(o => o.IsSelected))
                .Select(o => o.Price > 0 ? $"{o.Name} (+Rs {o.Price:0})" : o.Name)
                .ToList();

            if (selectedOpts.Count > 0)
            {
                itemSummaries.Add($"{item.HeaderTitle} ({string.Join(", ", selectedOpts)})");
            }
            else
            {
                itemSummaries.Add(item.HeaderTitle);
            }
        }

        var extra = DealCustomiseItems
            .SelectMany(i => i.ModifierGroups)
            .SelectMany(g => g.Options.Where(o => o.IsSelected))
            .Sum(o => o.Price);

        var modifiersSummary = string.Join(" • ", itemSummaries);

        var lineProduct = new OnlineProductDto
        {
            Id = ComboProduct.Id,
            Name = ComboProduct.Name,
            Price = ComboProduct.Price + extra,
            ImagePath = ComboProduct.ImagePath,
            CategoryName = ComboProduct.CategoryName ?? "Deals",
            InStock = true,
            IsDeal = true
        };

        var dealName = lineProduct.Name;
        _cart.AddOrIncrement(lineProduct, ComboQuantity, modifiersSummary);
        CloseComboModal();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (Shell.Current != null)
            {
                await Shell.Current.DisplayAlert("Deal Added", $"{dealName} added to your cart!", "OK");
            }
        });
    }

    [RelayCommand]
    private void CloseComboModal()
    {
        IsComboModalVisible = false;
        ComboProduct = null;
        ComboQuantity = 1;
        DealCustomiseItems.Clear();
        IsLoadingComboDeal = false;
    }

    // =========================================================================
    // 🍔 SINGLE PRODUCT CUSTOMIZATION MODAL
    // =========================================================================
    [RelayCommand]
    private void CloseProductModal()
    {
        IsProductModalVisible = false;
        ModalProduct = null;
        ModalModifierGroups.Clear();
        IsLoadingModifiers = false;
        DetailQuantity = 1;
    }

    [RelayCommand]
    private void SelectModalOption(ModifierOptionItem? option)
    {
        if (option == null)
        {
            return;
        }

        var group = ModalModifierGroups.FirstOrDefault(g => g.Id == option.GroupId);
        if (group == null)
        {
            return;
        }

        if (group.AllowMultiple || group.MaxSelection > 1)
        {
            if (option.IsSelected)
            {
                if (group.IsRequired && group.Options.Count(o => o.IsSelected) <= 1)
                {
                    return;
                }

                option.IsSelected = false;
            }
            else
            {
                var selectedCount = group.Options.Count(o => o.IsSelected);
                if (group.MaxSelection > 0 && selectedCount >= group.MaxSelection)
                {
                    var first = group.Options.FirstOrDefault(o => o.IsSelected);
                    if (first != null)
                    {
                        first.IsSelected = false;
                    }
                }

                option.IsSelected = true;
            }
        }
        else
        {
            foreach (var o in group.Options)
            {
                o.IsSelected = ReferenceEquals(o, option);
            }
        }

        RefreshModalTotal();
    }

    [RelayCommand]
    private void IncreaseDetailQty()
    {
        DetailQuantity++;
        RefreshModalTotal();
    }

    [RelayCommand]
    private void DecreaseDetailQty()
    {
        if (DetailQuantity <= 1)
        {
            return;
        }

        DetailQuantity--;
        RefreshModalTotal();
    }

    [RelayCommand]
    private void AddModalToCart()
    {
        if (ModalProduct == null)
        {
            return;
        }

        foreach (var group in ModalModifierGroups.Where(g => g.IsRequired))
        {
            if (!group.Options.Any(o => o.IsSelected))
            {
                _ = Shell.Current.DisplayAlert("Required", $"Please select {group.Name}.", "OK");
                return;
            }
        }

        var selected = ModalModifierGroups
            .SelectMany(g => g.Options.Where(o => o.IsSelected))
            .ToList();
        var extra = selected.Sum(o => o.Price);
        var modifiers = selected.Count == 0
            ? null
            : string.Join(", ", selected.Select(o => o.Name));

        var lineProduct = new OnlineProductDto
        {
            Id = ModalProduct.Id,
            Name = ModalProduct.Name,
            Price = ModalProduct.Price + extra,
            ImagePath = ModalProduct.ImagePath,
            CategoryName = ModalProduct.CategoryName,
            InStock = ModalProduct.InStock
        };

        _cart.AddOrIncrement(lineProduct, DetailQuantity, modifiers);
        CloseProductModal();
    }

    private async Task OpenProductModalAsync(OnlineProductDto product)
    {
        var groups = await _orders.GetModifiersForProductAsync(product.Id);
        groups = groups.Where(g => g.Options is { Count: > 0 }).ToList();

        if (groups.Count == 0 && LooksLikePizza(product))
        {
            groups =
            [
                new ProductModifierGroupDto
                {
                    Id = -1,
                    Name = "Size",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = -11, Name = "Small", IsDefault = true },
                        new ProductModifierOptionDto { Id = -12, Name = "Medium" },
                        new ProductModifierOptionDto { Id = -13, Name = "Large" },
                        new ProductModifierOptionDto { Id = -14, Name = "XL" }
                    ]
                }
            ];
        }

        // No options to choose — add one unit immediately (no popup).
        if (groups.Count == 0)
        {
            _cart.AddOrIncrement(product, 1);
            return;
        }

        ModalProduct = product;
        DetailQuantity = 1;
        ModalModifierGroups.Clear();
        IsLoadingModifiers = false;
        IsProductModalVisible = true;
        RefreshModalTotal();

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

            ModalModifierGroups.Add(groupItem);
        }

        RefreshModalTotal();
    }

    private static bool LooksLikePizza(OnlineProductDto product)
    {
        return (product.Name?.Contains("pizza", StringComparison.OrdinalIgnoreCase) ?? false)
            || (product.CategoryName?.Contains("pizza", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void RefreshModalTotal()
    {
        if (ModalProduct == null)
        {
            ModalTotalLabel = "PKR 0";
            return;
        }

        var extra = ModalModifierGroups
            .SelectMany(g => g.Options.Where(o => o.IsSelected))
            .Sum(o => o.Price);
        var total = (ModalProduct.Price + extra) * DetailQuantity;
        ModalTotalLabel = $"PKR {total:0}";
    }

    [RelayCommand]
    private async Task OpenCartAsync() => await Shell.Current.GoToAsync("//customer/cart");

    [RelayCommand]
    private async Task OpenOrderHistoryAsync() => await Shell.Current.GoToAsync("//customer/history");

    [RelayCommand]
    private async Task OpenLoginAsync() => await Shell.Current.GoToAsync("//customer/account");
}
