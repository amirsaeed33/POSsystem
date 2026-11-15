using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

[QueryProperty(nameof(CategoryFilter), "category")]
public partial class CustomerOrderViewModel : BaseViewModel
{
    private readonly OnlineOrderService _orders;
    private readonly SessionService _session;
    private readonly CartService _cart;
    private readonly AbpApiClient _api;
    private readonly FavoritesService _favorites;

    private const int PageSize = 30;
    private readonly List<OnlineProductDto> _allProducts = new();
    private int? _loadedBranchId;
    private int _catalogTotalCount;
    private bool _isLoadingMore;
    private bool _isReloadingCatalog;
    private CancellationTokenSource? _catalogLoadCts;

    public HashSet<string> SelectedCategories { get; } = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    private string? categoryFilter;

    partial void OnCategoryFilterChanged(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var decoded = Uri.UnescapeDataString(value);
            ApplyCategoryFilterByName(decoded);
        }
    }

    public void ApplyCategoryFilterByName(string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName) || string.Equals(categoryName, "All", StringComparison.OrdinalIgnoreCase))
        {
            ClearFilters();
            return;
        }

        var trimmed = categoryName.Trim();
        SelectedCategories.Clear();
        SelectedCategories.Add(trimmed);
        IsAllSelected = false;

        foreach (var c in Categories)
        {
            c.IsSelected = string.Equals(c.Name?.Trim(), trimmed, StringComparison.OrdinalIgnoreCase);
        }

        UpdateSelectedCategoryTitle();
        _ = ReloadFirstPageAsync();
    }

    [ObservableProperty]
    private string selectedCategoryTitle = "All Menu";

    [ObservableProperty]
    private bool isAllSelected = true;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isPullRefreshing;

    [ObservableProperty]
    private OnlineStoreHeaderDto? storeHeader;

    [ObservableProperty]
    private bool isLoadingMore;

    public ObservableCollection<OnlineProductDto> Products { get; } = new();
    public ObservableCollection<CategoryChipItem> Categories { get; } = new();

    public int CartCount => _cart.ItemCount;
    public bool HasCartItems => CartCount > 0;
    public decimal CartSubtotal => _cart.Subtotal;
    public string CartSubtotalDisplay => $"Rs {CartSubtotal:0}";
    public string DishesCountLabel
    {
        get
        {
            var total = _catalogTotalCount > 0 ? _catalogTotalCount : Products.Count;
            return $"{total} {(total == 1 ? "dish" : "dishes")}";
        }
    }
    public bool HasProducts => Products.Count > 0;
    public bool CanLoadMore => Products.Count < _catalogTotalCount;
    public string DeliverToLabel => StoreHeader?.BranchName ?? "Al meezan";
    public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);
    public bool HasActiveFilters => !IsAllSelected || HasSearchText;
    public string ActiveFilterBadgeText => SelectedCategories.Count > 0 ? $"{SelectedCategories.Count} active" : string.Empty;

    public ObservableCollection<CartLine> CartLines => _cart.Lines;

    [ObservableProperty]
    private bool isTraySheetVisible;

    public CustomerOrderViewModel(
        OnlineOrderService orders,
        SessionService session,
        CartService cart,
        AbpApiClient api,
        FavoritesService favorites)
    {
        _orders = orders;
        _session = session;
        _cart = cart;
        _api = api;
        _favorites = favorites;
        Title = "Order Menu";

        _cart.CartChanged += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(CartCount));
                OnPropertyChanged(nameof(HasCartItems));
                OnPropertyChanged(nameof(CartSubtotal));
                OnPropertyChanged(nameof(CartSubtotalDisplay));
                SyncCartQtyBadges();
                if (!HasCartItems)
                {
                    IsTraySheetVisible = false;
                }
            });
        };

        _favorites.FavoritesChanged += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(SyncFavorites);
        };

        _session.SessionChanged += async (_, _) =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Only reload when the active branch changes; reuse in-memory/cache otherwise.
                if (_loadedBranchId.HasValue &&
                    _session.BranchId.HasValue &&
                    _loadedBranchId.Value == _session.BranchId.Value &&
                    _allProducts.Count > 0)
                {
                    SyncCartQtyBadges();
                    SyncFavorites();
                    return;
                }

                await LoadMenuAsync(forceRefresh: false);
            });
        };
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        try
        {
            await _session.LoadAsync();
            if (_loadedBranchId.HasValue &&
                _session.BranchId.HasValue &&
                _loadedBranchId.Value == _session.BranchId.Value &&
                _allProducts.Count > 0)
            {
                // Instant tab switch: keep last menu on screen, only sync badges.
                SyncCartQtyBadges();
                SyncFavorites();

                if (!string.IsNullOrWhiteSpace(CategoryFilter))
                {
                    ApplyCategoryFilterByName(Uri.UnescapeDataString(CategoryFilter));
                }
                return;
            }

            await LoadMenuAsync(forceRefresh: false);

            if (!string.IsNullOrWhiteSpace(CategoryFilter))
            {
                ApplyCategoryFilterByName(Uri.UnescapeDataString(CategoryFilter));
            }
        }
        finally
        {
            LoadingService.Current.HideTabLoader();
        }
    }

    [RelayCommand]
    private async Task RefreshMenuAsync()
    {
        // RefreshView already sets IsPullRefreshing=true via two-way binding.
        // Do not set it true again here — that can leave the Android spinner stuck.
        try
        {
            await LoadMenuAsync(forceRefresh: true);
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsPullRefreshing = false;
                LoadingService.Current.HideTabLoader();
            });
        }
    }

    private async Task<int> ResolveActiveBranchIdAsync()
    {
        await _session.LoadAsync();
        if (_session.BranchId.HasValue && _session.BranchId.Value > 0)
        {
            return _session.BranchId.Value;
        }

        try
        {
            var branches = await _orders.GetBranchesAsync();
            if (branches != null && branches.Count > 0)
            {
                var preferred = branches.FirstOrDefault(b => b.IsRestaurant) ?? branches[0];
                await _session.SetBranchAsync(preferred.Id);
                return preferred.Id;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CustomerOrderViewModel] Branch lookup failed: {ex.Message}");
        }

        return 2;
    }

    private static string ResolveProductImageUrl(AbpApiClient api, string? imagePath, string? name, string? categoryName)
    {
        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            var resolved = api.ResolveImageUrl(imagePath);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return resolved;
            }
        }

        var n = (name ?? string.Empty).ToLowerInvariant();
        var c = (categoryName ?? string.Empty).ToLowerInvariant();

        if (n.Contains("burger") || c.Contains("burger"))
            return "burger_photo.png";
        if (n.Contains("pizza") || c.Contains("pizza"))
            return "pizza_photo.png";

        return "burger_photo.png";
    }

    public async Task LoadMenuAsync(bool forceRefresh = false)
    {
        // Pull-to-refresh must not no-op while a quiet load is in flight.
        if (IsBusy && !forceRefresh)
        {
            return;
        }

        if (IsBusy && forceRefresh)
        {
            var spins = 0;
            while (IsBusy && spins++ < 100)
            {
                await Task.Delay(50);
            }

            if (IsBusy)
            {
                return;
            }
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var branchId = await ResolveActiveBranchIdAsync();

            var headerTask = _orders.GetStoreHeaderAsync(branchId, forceRefresh);
            try
            {
                StoreHeader = await headerTask;
                OnPropertyChanged(nameof(DeliverToLabel));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomerOrderViewModel] Store header failed: {ex.Message}");
            }

            _loadedBranchId = branchId;
            RebuildCategories();
            await ReloadFirstPageAsync(forceRefresh);
            _orders.PrefetchModifiersForCatalog(_allProducts);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CustomerOrderViewModel] LoadMenu failed: {ex}");
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private PagedOnlineCatalogRequest BuildCatalogRequest(int skipCount) => new()
    {
        BranchId = _loadedBranchId ?? _session.BranchId,
        SkipCount = skipCount,
        MaxResultCount = PageSize,
        Keyword = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
        CategoryNames = !IsAllSelected && SelectedCategories.Count > 0
            ? SelectedCategories.ToList()
            : null,
        CategoryName = !IsAllSelected && SelectedCategories.Count == 1
            ? SelectedCategories.First()
            : null
    };

    private async Task ReloadFirstPageAsync(bool forceRefresh = false)
    {
        if (_isReloadingCatalog && !forceRefresh)
        {
            return;
        }

        _catalogLoadCts?.Cancel();
        _catalogLoadCts = new CancellationTokenSource();
        var ct = _catalogLoadCts.Token;

        _isReloadingCatalog = true;
        try
        {
            var request = BuildCatalogRequest(0);
            var page = await _orders.GetCatalogPageAsync(request, forceRefresh, ct);
            if (ct.IsCancellationRequested)
            {
                return;
            }

            _allProducts.Clear();
            _catalogTotalCount = page.TotalCount;
            foreach (var p in page.Items ?? new List<OnlineProductDto>())
            {
                EnrichProduct(p);
                _allProducts.Add(p);
            }

            BindProductsFromLoaded();
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CustomerOrderViewModel] ReloadFirstPage failed: {ex.Message}");
            ErrorMessage = ex.Message;
        }
        finally
        {
            _isReloadingCatalog = false;
        }
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (_isLoadingMore || _isReloadingCatalog || !CanLoadMore)
        {
            return;
        }

        _isLoadingMore = true;
        IsLoadingMore = true;
        try
        {
            var request = BuildCatalogRequest(_allProducts.Count);
            var page = await _orders.GetCatalogPageAsync(request, forceRefresh: false);
            _catalogTotalCount = page.TotalCount;

            var existing = _allProducts.Select(p => p.Id).ToHashSet();
            foreach (var p in page.Items ?? new List<OnlineProductDto>())
            {
                if (!existing.Add(p.Id))
                {
                    continue;
                }

                EnrichProduct(p);
                _allProducts.Add(p);
                Products.Add(p);
            }

            OnPropertyChanged(nameof(DishesCountLabel));
            OnPropertyChanged(nameof(HasProducts));
            OnPropertyChanged(nameof(CanLoadMore));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CustomerOrderViewModel] LoadMore failed: {ex.Message}");
        }
        finally
        {
            _isLoadingMore = false;
            IsLoadingMore = false;
        }
    }

    private void EnrichProduct(OnlineProductDto p)
    {
        p.ImageUrl = ResolveProductImageUrl(_api, p.ImagePath, p.Name, p.CategoryName);
        p.IsFavorite = _favorites.IsFavorite(p.Id);
        p.CartQty = _cart.GetQty(p.Id);
    }

    private void BindProductsFromLoaded()
    {
        Products.Clear();
        foreach (var p in _allProducts)
        {
            p.CartQty = _cart.GetQty(p.Id);
            p.IsFavorite = _favorites.IsFavorite(p.Id);
            Products.Add(p);
        }

        OnPropertyChanged(nameof(DishesCountLabel));
        OnPropertyChanged(nameof(HasProducts));
        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(HasSearchText));
        OnPropertyChanged(nameof(CanLoadMore));
        RebuildCategories();
    }

    private void RebuildCategories()
    {
        var names = new List<string> { "All" };

        foreach (var c in StoreHeader?.Categories ?? new List<string>())
        {
            if (!string.IsNullOrWhiteSpace(c) && !names.Contains(c.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                names.Add(c.Trim());
            }
        }

        foreach (var c in _allProducts
                     .Select(p => p.CategoryName)
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Select(x => x!.Trim()))
        {
            if (!names.Contains(c, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(c);
            }
        }

        Categories.Clear();
        foreach (var name in names)
        {
            var isAll = string.Equals(name, "All", StringComparison.OrdinalIgnoreCase);
            var count = isAll
                ? (_catalogTotalCount > 0 ? _catalogTotalCount : _allProducts.Count)
                : _allProducts.Count(p =>
                    string.Equals(p.CategoryName?.Trim(), name, StringComparison.OrdinalIgnoreCase));

            // Hide store-header categories that have no products in the loaded catalog.
            if (!isAll && count == 0)
            {
                continue;
            }

            var isSel = isAll
                ? (IsAllSelected || SelectedCategories.Count == 0)
                : SelectedCategories.Contains(name);

            Categories.Add(new CategoryChipItem
            {
                Name = name,
                Count = count,
                IsSelected = isSel
            });
        }

        // Drop selection for categories that were hidden (0 products).
        var visible = Categories
            .Where(c => !c.IsAll)
            .Select(c => c.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removed = SelectedCategories.Where(s => !visible.Contains(s)).ToList();
        foreach (var s in removed)
        {
            SelectedCategories.Remove(s);
        }

        if (SelectedCategories.Count == 0)
        {
            IsAllSelected = true;
            var allChip = Categories.FirstOrDefault(c => c.IsAll);
            if (allChip != null)
            {
                allChip.IsSelected = true;
            }
        }

        UpdateSelectedCategoryTitle();
    }

    [RelayCommand]
    public void ToggleCategory(CategoryChipItem? chip)
    {
        if (chip == null) return;

        LoadingService.Current.ShowTabLoader();
        try
        {
        if (chip.IsAll)
        {
            SelectedCategories.Clear();
            IsAllSelected = true;
            foreach (var c in Categories)
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

            var allChip = Categories.FirstOrDefault(c => c.IsAll);
            if (SelectedCategories.Count > 0)
            {
                if (allChip != null) allChip.IsSelected = false;
                IsAllSelected = false;
            }
            else
            {
                if (allChip != null) allChip.IsSelected = true;
                IsAllSelected = true;
            }
        }

        UpdateSelectedCategoryTitle();
        _ = ReloadFirstPageAsync();
        }
        finally
        {
            LoadingService.Current.HideTabLoader();
        }
    }

    [RelayCommand]
    public void SelectCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category) || string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
        {
            var allChip = Categories.FirstOrDefault(c => c.IsAll);
            ToggleCategory(allChip);
        }
        else
        {
            var chip = Categories.FirstOrDefault(c => string.Equals(c.Name, category, StringComparison.OrdinalIgnoreCase));
            if (chip != null)
            {
                ToggleCategory(chip);
            }
        }
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchText = string.Empty;
        _ = ReloadFirstPageAsync();
    }

    [RelayCommand]
    public void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedCategories.Clear();
        foreach (var c in Categories)
        {
            c.IsSelected = c.IsAll;
        }
        IsAllSelected = true;
        UpdateSelectedCategoryTitle();
        _ = ReloadFirstPageAsync();
    }

    private void UpdateSelectedCategoryTitle()
    {
        if (IsAllSelected || SelectedCategories.Count == 0)
        {
            SelectedCategoryTitle = "All Menu";
        }
        else if (SelectedCategories.Count == 1)
        {
            SelectedCategoryTitle = $"{SelectedCategories.First()} Menu";
        }
        else
        {
            var firstTwo = string.Join(", ", SelectedCategories.Take(2));
            SelectedCategoryTitle = SelectedCategories.Count > 2
                ? $"{firstTwo} +{SelectedCategories.Count - 2}"
                : $"{firstTwo} Menu";
        }

        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(ActiveFilterBadgeText));
    }

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasSearchText));
        OnPropertyChanged(nameof(HasActiveFilters));
        _ = ReloadFirstPageAsync();
    }

    private void ApplyFilters() => BindProductsFromLoaded();

    private void SyncCartQtyBadges()
    {
        foreach (var p in Products)
        {
            p.CartQty = _cart.GetQty(p.Id);
        }
    }

    private void SyncFavorites()
    {
        foreach (var p in Products)
        {
            p.IsFavorite = _favorites.IsFavorite(p.Id);
        }
    }

    [RelayCommand]
    private async Task IncreaseCardAsync(OnlineProductDto? product)
    {
        if (product == null || !product.InStock)
        {
            return;
        }

        if (product.IsDeal || product.DealId.HasValue || (product.CategoryName != null && product.CategoryName.Contains("deal", StringComparison.OrdinalIgnoreCase)))
        {
            if (Shell.Current != null)
            {
                ProductDetailLaunch.SetPrefill(product);
                await Shell.Current.GoToAsync($"productdetail?productId={product.Id}");
            }
            return;
        }

        _cart.AddOrIncrement(product, 1);
        product.CartQty = _cart.GetQty(product.Id);
    }

    [RelayCommand]
    private void DecreaseCard(OnlineProductDto? product)
    {
        if (product == null || product.CartQty <= 0)
        {
            return;
        }

        _cart.DecrementProduct(product.Id);
        product.CartQty = _cart.GetQty(product.Id);
    }

    [RelayCommand]
    private async Task OpenProductAsync(OnlineProductDto? product)
    {
        if (product == null || Shell.Current == null)
        {
            return;
        }

        ProductDetailLaunch.SetPrefill(product);
        await Shell.Current.GoToAsync($"productdetail?productId={product.Id}");
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(OnlineProductDto? product)
    {
        if (product == null)
        {
            return;
        }

        await _favorites.ToggleFavoriteAsync(product.Id);
        product.IsFavorite = _favorites.IsFavorite(product.Id);
    }

    [RelayCommand]
    private async Task OpenCartAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("cart");
        }
    }

    [RelayCommand]
    private void OpenTraySheet()
    {
        if (!HasCartItems)
        {
            return;
        }

        IsTraySheetVisible = true;
    }

    [RelayCommand]
    private void CloseTraySheet() => IsTraySheetVisible = false;

    [RelayCommand]
    private async Task ProceedToCheckoutAsync()
    {
        IsTraySheetVisible = false;
        await OpenCartAsync();
    }

    [RelayCommand]
    private void IncreaseTrayLine(CartLine? line)
    {
        if (line == null)
        {
            return;
        }

        _cart.SetQuantity(line, line.Quantity + 1);
    }

    [RelayCommand]
    private void DecreaseTrayLine(CartLine? line)
    {
        if (line == null)
        {
            return;
        }

        _cart.SetQuantity(line, line.Quantity - 1);
    }
}
