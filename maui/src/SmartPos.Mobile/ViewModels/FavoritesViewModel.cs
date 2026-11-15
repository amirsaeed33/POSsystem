using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public partial class FavoriteCategoryFilter : ObservableObject
{
    public string Name { get; set; } = "All";

    [ObservableProperty]
    private bool isSelected;
}

public partial class FavoritesViewModel : BaseViewModel
{
    private readonly FavoritesService _favorites;
    private readonly OnlineOrderService _orders;
    private readonly SessionService _session;
    private readonly CartService _cart;
    private int? _loadedBranchId;
    private string _loadedFavoriteKey = string.Empty;
    private string _selectedCategory = "All";

    [ObservableProperty]
    private int favoriteCount;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private bool isEmptyState;

    [ObservableProperty]
    private int cartCount;

    public ObservableCollection<OnlineProductDto> FavoriteProducts { get; } = new();
    public ObservableCollection<OnlineProductDto> FilteredFavoriteProducts { get; } = new();
    public ObservableCollection<FavoriteCategoryFilter> CategoryFilters { get; } = new();
    public ObservableCollection<CartLine> CartLines => _cart.Lines;

    public int SavedCount => FavoriteCount;
    public string SavedCountLabel => $"{FavoriteCount} Saved";
    public decimal CartSubtotal => _cart.Subtotal;
    public string CartSubtotalDisplay => $"Rs. {CartSubtotal:N0}";
    public bool HasCartItems => CartCount > 0;
    public bool HasFavorites => !IsEmptyState;

    [ObservableProperty]
    private bool isTraySheetVisible;

    public FavoritesViewModel(
        FavoritesService favorites,
        OnlineOrderService orders,
        SessionService session,
        CartService cart)
    {
        _favorites = favorites;
        _orders = orders;
        _session = session;
        _cart = cart;
        Title = "My Favorites";
        CartCount = _cart.ItemCount;
        EnsureDefaultFilters();

        _favorites.FavoritesChanged += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                FavoriteCount = _favorites.Count;
                OnPropertyChanged(nameof(SavedCount));
                OnPropertyChanged(nameof(SavedCountLabel));
                if (!IsBusy)
                {
                    await LoadFavoritesAsync();
                }
            });
        };

        _cart.CartChanged += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CartCount = _cart.ItemCount;
                OnPropertyChanged(nameof(CartSubtotal));
                OnPropertyChanged(nameof(CartSubtotalDisplay));
                OnPropertyChanged(nameof(HasCartItems));
                SyncCartQuantities();
                if (!HasCartItems)
                {
                    IsTraySheetVisible = false;
                }
            });
        };
    }

    [RelayCommand]
    public async Task AppearingAsync()
    {
        try
        {
            FavoriteCount = _favorites.Count;
            CartCount = _cart.ItemCount;
            OnPropertyChanged(nameof(SavedCount));
            OnPropertyChanged(nameof(SavedCountLabel));
            OnPropertyChanged(nameof(CartSubtotal));
            OnPropertyChanged(nameof(CartSubtotalDisplay));
            OnPropertyChanged(nameof(HasCartItems));

            var favoriteKey = BuildFavoriteKey(_favorites.GetFavoriteIds());
            if (_loadedBranchId.HasValue &&
                _session.BranchId == _loadedBranchId &&
                favoriteKey == _loadedFavoriteKey &&
                FavoriteProducts.Count > 0)
            {
                SyncCartQuantities();
                return;
            }

            await LoadFavoritesAsync();
        }
        finally
        {
            LoadingService.Current.HideTabLoader();
        }
    }

    [RelayCommand]
    public async Task RefreshFavoritesAsync()
    {
        IsRefreshing = true;
        try
        {
            await _favorites.SyncWithServerAsync();
            await LoadFavoritesAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task LoadFavoritesAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var favoriteIds = _favorites.GetFavoriteIds();
            if (favoriteIds.Count == 0)
            {
                FavoriteProducts.Clear();
                FilteredFavoriteProducts.Clear();
                FavoriteCount = 0;
                IsEmptyState = true;
                RebuildCategoryFilters();
                ApplyCategoryFilter();
                NotifySavedState();
                _loadedBranchId = _session.BranchId;
                _loadedFavoriteKey = string.Empty;
                return;
            }

            var branchId = _session.BranchId;
            var catalog = await _orders.GetCatalogAsync(branchId);

            var favList = new List<OnlineProductDto>();
            foreach (var id in favoriteIds)
            {
                var prod = catalog.FirstOrDefault(p => p.Id == id);
                if (prod != null)
                {
                    prod.IsFavorite = true;
                    if (string.IsNullOrWhiteSpace(prod.ImageUrl) && !string.IsNullOrWhiteSpace(prod.ImagePath))
                    {
                        prod.ImageUrl = _orders.ResolveImageUrl(prod.ImagePath);
                    }
                    prod.CartQty = _cart.GetQty(prod.Id);
                    favList.Add(prod);
                }
            }

            FavoriteProducts.Clear();
            foreach (var item in favList)
            {
                FavoriteProducts.Add(item);
            }

            _loadedBranchId = branchId;
            _loadedFavoriteKey = BuildFavoriteKey(favoriteIds);

            FavoriteCount = FavoriteProducts.Count;
            IsEmptyState = FavoriteProducts.Count == 0;
            RebuildCategoryFilters();
            ApplyCategoryFilter();
            NotifySavedState();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            IsEmptyState = FavoriteProducts.Count == 0;
            NotifySavedState();
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    private void SyncCartQuantities()
    {
        foreach (var p in FavoriteProducts)
        {
            p.CartQty = _cart.GetQty(p.Id);
        }

        foreach (var p in FilteredFavoriteProducts)
        {
            p.CartQty = _cart.GetQty(p.Id);
        }
    }

    private static string BuildFavoriteKey(IEnumerable<int> ids) =>
        string.Join(",", ids.OrderBy(x => x));

    private void EnsureDefaultFilters()
    {
        if (CategoryFilters.Count > 0)
        {
            return;
        }

        CategoryFilters.Add(new FavoriteCategoryFilter { Name = "All", IsSelected = true });
    }

    private void RebuildCategoryFilters()
    {
        var previous = _selectedCategory;
        var categories = FavoriteProducts
            .Select(p => p.CategoryName?.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        CategoryFilters.Clear();
        CategoryFilters.Add(new FavoriteCategoryFilter
        {
            Name = "All",
            IsSelected = string.Equals(previous, "All", StringComparison.OrdinalIgnoreCase) ||
                         categories.All(c => !string.Equals(c, previous, StringComparison.OrdinalIgnoreCase))
        });

        foreach (var name in categories)
        {
            CategoryFilters.Add(new FavoriteCategoryFilter
            {
                Name = name,
                IsSelected = string.Equals(previous, name, StringComparison.OrdinalIgnoreCase)
            });
        }

        var selected = CategoryFilters.FirstOrDefault(c => c.IsSelected)?.Name ?? "All";
        _selectedCategory = selected;
        foreach (var filter in CategoryFilters)
        {
            filter.IsSelected = string.Equals(filter.Name, selected, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void ApplyCategoryFilter()
    {
        FilteredFavoriteProducts.Clear();
        IEnumerable<OnlineProductDto> source = FavoriteProducts;
        if (!string.Equals(_selectedCategory, "All", StringComparison.OrdinalIgnoreCase))
        {
            source = FavoriteProducts.Where(p =>
                string.Equals(p.CategoryName?.Trim(), _selectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in source)
        {
            FilteredFavoriteProducts.Add(item);
        }
    }

    private void NotifySavedState()
    {
        OnPropertyChanged(nameof(SavedCount));
        OnPropertyChanged(nameof(SavedCountLabel));
        OnPropertyChanged(nameof(HasFavorites));
        OnPropertyChanged(nameof(HasCartItems));
        OnPropertyChanged(nameof(CartSubtotal));
        OnPropertyChanged(nameof(CartSubtotalDisplay));
    }

    [RelayCommand]
    public void SelectCategoryFilter(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            category = "All";
        }

        _selectedCategory = category.Trim();
        foreach (var filter in CategoryFilters)
        {
            filter.IsSelected = string.Equals(filter.Name, _selectedCategory, StringComparison.OrdinalIgnoreCase);
        }

        ApplyCategoryFilter();
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
    public async Task RemoveFavoriteAsync(OnlineProductDto? product)
    {
        if (product == null) return;

        await _favorites.ToggleFavoriteAsync(product.Id);
        FavoriteProducts.Remove(product);
        FavoriteCount = FavoriteProducts.Count;
        IsEmptyState = FavoriteProducts.Count == 0;
        RebuildCategoryFilters();
        ApplyCategoryFilter();
        NotifySavedState();
    }

    [RelayCommand]
    public Task AddToCartAsync(OnlineProductDto? product) => IncreaseCardAsync(product);

    [RelayCommand]
    public async Task IncreaseCardAsync(OnlineProductDto? product)
    {
        if (product == null)
        {
            return;
        }

        // Match Order page: deals need detail; otherwise add straight to tray.
        if (product.IsDeal || product.DealId.HasValue ||
            (product.CategoryName != null && product.CategoryName.Contains("deal", StringComparison.OrdinalIgnoreCase)))
        {
            await OpenProductDetailAsync(product);
            return;
        }

        if (!product.InStock && product.CartQty <= 0 && product.StockQuantity <= 0)
        {
            return;
        }

        _cart.AddOrIncrement(product, 1);
        product.CartQty = _cart.GetQty(product.Id);
        CartCount = _cart.ItemCount;
        NotifySavedState();
    }

    [RelayCommand]
    public void DecreaseCard(OnlineProductDto? product)
    {
        if (product == null || product.CartQty <= 0)
        {
            return;
        }

        _cart.DecrementProduct(product.Id);
        product.CartQty = _cart.GetQty(product.Id);
        CartCount = _cart.ItemCount;
        NotifySavedState();
    }

    [RelayCommand]
    public async Task OpenProductDetailAsync(OnlineProductDto? product)
    {
        if (product == null || Shell.Current == null)
        {
            return;
        }

        ProductDetailLaunch.SetPrefill(product);
        await Shell.Current.GoToAsync($"productdetail?productId={product.Id}");
    }

    [RelayCommand]
    public async Task BrowseMenuAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//customer/order");
        }
    }

    [RelayCommand]
    public Task NavigateToMenuAsync() => BrowseMenuAsync();

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
