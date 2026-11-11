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

    private List<OnlineProductDto> _allProducts = new();
    private int? _loadedBranchId;

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
        ApplyFilters();
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

    public ObservableCollection<OnlineProductDto> Products { get; } = new();
    public ObservableCollection<CategoryChipItem> Categories { get; } = new();

    public int CartCount => _cart.ItemCount;
    public bool HasCartItems => CartCount > 0;
    public decimal CartSubtotal => _cart.Subtotal;
    public string CartSubtotalDisplay => $"Rs {CartSubtotal:0}";
    public string DishesCountLabel => $"{Products.Count} {(Products.Count == 1 ? "dish" : "dishes")}";
    public bool HasProducts => Products.Count > 0;
    public string DeliverToLabel => StoreHeader?.BranchName ?? "Al meezan";
    public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);
    public bool HasActiveFilters => !IsAllSelected || HasSearchText;
    public string ActiveFilterBadgeText => SelectedCategories.Count > 0 ? $"{SelectedCategories.Count} active" : string.Empty;

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
            OnPropertyChanged(nameof(CartCount));
            OnPropertyChanged(nameof(HasCartItems));
            OnPropertyChanged(nameof(CartSubtotal));
            OnPropertyChanged(nameof(CartSubtotalDisplay));
            SyncCartQtyBadges();
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
        IsPullRefreshing = true;
        try
        {
            await LoadMenuAsync(forceRefresh: true);
        }
        finally
        {
            IsPullRefreshing = false;
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
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var branchId = await ResolveActiveBranchIdAsync();

            // Load header + catalog together; both are cached so warm visits are near-instant.
            var headerTask = _orders.GetStoreHeaderAsync(branchId, forceRefresh);
            var catalogTask = _orders.GetCatalogAsync(branchId, forceRefresh);

            try
            {
                StoreHeader = await headerTask;
                OnPropertyChanged(nameof(DeliverToLabel));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomerOrderViewModel] Store header failed: {ex.Message}");
            }

            _allProducts = await catalogTask;
            foreach (var p in _allProducts)
            {
                p.ImageUrl = ResolveProductImageUrl(_api, p.ImagePath, p.Name, p.CategoryName);
                p.IsFavorite = _favorites.IsFavorite(p.Id);
                p.CartQty = _cart.GetQty(p.Id);
            }

            _loadedBranchId = branchId;
            RebuildCategories();
            ApplyFilters();
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

    private void RebuildCategories()
    {
        var names = new List<string> { "All" };

        var activeProductCategories = _allProducts
            .Select(p => p.CategoryName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var c in activeProductCategories)
        {
            if (!names.Contains(c, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(c);
            }
        }

        foreach (var c in StoreHeader?.Categories ?? new List<string>())
        {
            if (!string.IsNullOrWhiteSpace(c) && !names.Contains(c.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                names.Add(c.Trim());
            }
        }

        Categories.Clear();
        foreach (var name in names)
        {
            var count = string.Equals(name, "All", StringComparison.OrdinalIgnoreCase)
                ? _allProducts.Count
                : _allProducts.Count(p => string.Equals(p.CategoryName, name, StringComparison.OrdinalIgnoreCase));

            var isSel = string.Equals(name, "All", StringComparison.OrdinalIgnoreCase)
                ? (IsAllSelected || SelectedCategories.Count == 0)
                : SelectedCategories.Contains(name);

            Categories.Add(new CategoryChipItem
            {
                Name = name,
                Count = count,
                IsSelected = isSel
            });
        }

        UpdateSelectedCategoryTitle();
    }

    [RelayCommand]
    public void ToggleCategory(CategoryChipItem? chip)
    {
        if (chip == null) return;

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
        ApplyFilters();
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
        ApplyFilters();
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
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        IEnumerable<OnlineProductDto> query = _allProducts;

        if (!IsAllSelected && SelectedCategories.Count > 0)
        {
            query = query.Where(p =>
                !string.IsNullOrWhiteSpace(p.CategoryName) &&
                SelectedCategories.Contains(p.CategoryName.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var st = SearchText.Trim();
            query = query.Where(p =>
                (p.Name != null && p.Name.Contains(st, StringComparison.OrdinalIgnoreCase)) ||
                (p.CategoryName != null && p.CategoryName.Contains(st, StringComparison.OrdinalIgnoreCase)) ||
                (p.Description != null && p.Description.Contains(st, StringComparison.OrdinalIgnoreCase)));
        }

        Products.Clear();
        foreach (var item in query)
        {
            item.CartQty = _cart.GetQty(item.Id);
            item.IsFavorite = _favorites.IsFavorite(item.Id);
            Products.Add(item);
        }

        OnPropertyChanged(nameof(DishesCountLabel));
        OnPropertyChanged(nameof(HasProducts));
        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(HasSearchText));
    }

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
            var homeVm = ServiceHelper.Get<CustomerHomeViewModel>();
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("//customer/home");
            }
            await homeVm.OpenDealModalAsync(product);
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

        if (product.IsDeal || product.DealId.HasValue || (product.CategoryName != null && product.CategoryName.Contains("deal", StringComparison.OrdinalIgnoreCase)))
        {
            var homeVm = ServiceHelper.Get<CustomerHomeViewModel>();
            await Shell.Current.GoToAsync("//customer/home");
            await homeVm.OpenDealModalAsync(product);
            return;
        }

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
}
