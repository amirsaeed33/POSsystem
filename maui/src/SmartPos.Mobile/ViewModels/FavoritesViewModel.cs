using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public partial class FavoritesViewModel : BaseViewModel
{
    private readonly FavoritesService _favorites;
    private readonly OnlineOrderService _orders;
    private readonly SessionService _session;
    private readonly CartService _cart;

    [ObservableProperty]
    private int favoriteCount;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private bool isEmptyState;

    [ObservableProperty]
    private int cartCount;

    public ObservableCollection<OnlineProductDto> FavoriteProducts { get; } = new();

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

        _favorites.FavoritesChanged += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                FavoriteCount = _favorites.Count;
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
                SyncCartQuantities();
            });
        };
    }

    [RelayCommand]
    public async Task AppearingAsync()
    {
        FavoriteCount = _favorites.Count;
        CartCount = _cart.ItemCount;
        await LoadFavoritesAsync();
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
                FavoriteCount = 0;
                IsEmptyState = true;
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

            FavoriteCount = FavoriteProducts.Count;
            IsEmptyState = FavoriteProducts.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            IsEmptyState = FavoriteProducts.Count == 0;
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
    }

    [RelayCommand]
    public async Task RemoveFavoriteAsync(OnlineProductDto? product)
    {
        if (product == null) return;

        await _favorites.ToggleFavoriteAsync(product.Id);
        FavoriteProducts.Remove(product);
        FavoriteCount = FavoriteProducts.Count;
        IsEmptyState = FavoriteProducts.Count == 0;
    }

    [RelayCommand]
    public async Task AddToCartAsync(OnlineProductDto? product)
    {
        if (product == null) return;

        try
        {
            var groups = await _orders.GetModifiersForProductAsync(product.Id);
            if (groups != null && groups.Any(g => g.Options != null && g.Options.Count > 0))
            {
                await Shell.Current.GoToAsync($"productdetail?productId={product.Id}");
                return;
            }
        }
        catch { }

        _cart.AddOrIncrement(product, 1);
        product.CartQty = _cart.GetQty(product.Id);
        CartCount = _cart.ItemCount;

        if (Shell.Current != null)
        {
            await Shell.Current.DisplayAlert("Added to Cart", $"{product.Name} has been added to your cart.", "OK");
        }
    }

    [RelayCommand]
    public async Task OpenProductDetailAsync(OnlineProductDto? product)
    {
        if (product == null) return;
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
}
