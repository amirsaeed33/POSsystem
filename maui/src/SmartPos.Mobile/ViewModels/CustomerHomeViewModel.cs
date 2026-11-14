using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;
using SmartPos.Mobile.Services.Home;

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
    private const int CatalogPageSize = 30;
    private const int SearchDebounceMs = 350;
    private List<OnlineProductDto> _allProducts = new();
    private readonly List<CategoryChipItem> _allCategoryChips = new();
    private IDispatcherTimer? _heroTimer;
    private int _catalogTotalCount;
    private CancellationTokenSource? _searchDebounceCts;
    private int _filterGeneration;

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
    public ObservableCollection<HomeCategoryDisplayDto> DisplayCategories { get; } = new();
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
    public string TotalReviewsLabel => $"{Feedbacks.Count} {(Feedbacks.Count == 1 ? "Review" : "Verified Reviews")}";
    public string TotalReviewsSubtitle => $"{Feedbacks.Count} {(Feedbacks.Count == 1 ? "review" : "reviews")}";
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
        BuildDisplayCategories(new List<string>());
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
        try
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
        finally
        {
            LoadingService.Current.HideTabLoader();
        }
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
            await LoadBranchHomeDataAsync(SelectedBranch, forceRefresh: true);
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

        SelectedCategory = "All";
        SearchText = string.Empty;
        CategorySearchText = string.Empty;
        IsCategorySearchVisible = false;
        ShowProductGrid = true;

        await LoadBranchHomeDataAsync(branch, forceRefresh: false);

        // Start live delivery pin early (Deliver-to / address flow).
        _ = _deliveryLocation.CaptureAsync();
    }

    /// <summary>
    /// Loads store header, hero, first catalog page, and deals in parallel so cold start waits for
    /// the slowest call instead of the sum of every call.
    /// </summary>
    private async Task LoadBranchHomeDataAsync(OnlineBranchDto branch, bool forceRefresh)
    {
        _favorites.Initialize(branch.Id);
        CancelSearchDebounce();

        var branchId = branch.Id;
        var headerTask = _orders.GetStoreHeaderAsync(branchId, forceRefresh);
        var heroTask = LoadHeroSlidesAsync(branchId, forceRefresh);
        var catalogTask = LoadCatalogFirstPageAsync(branchId, forceRefresh);
        var dealsTask = _orders.GetActiveDealsAsync(branchId, forceRefresh);

        await Task.WhenAll(headerTask, heroTask, catalogTask, dealsTask);

        if (SelectedBranch?.Id != branchId)
        {
            return;
        }

        StoreHeader = await headerTask;
        RebuildCategories();
        await ApplyFiltersAsync();

        await LoadDealsAsync(branchId, forceRefresh, await dealsTask);
        _ = LoadFeedbacksAsync(branchId);
        _orders.PrefetchModifiersForCatalog(_allProducts);
    }

    [RelayCommand]
    private async Task OpenCartAsync() => await Shell.Current.GoToAsync("//customer/cart");

    [RelayCommand]
    private async Task OpenOrderHistoryAsync() => await Shell.Current.GoToAsync("//customer/history");

    [RelayCommand]
    private async Task OpenLoginAsync() => await Shell.Current.GoToAsync("//customer/account");
}
