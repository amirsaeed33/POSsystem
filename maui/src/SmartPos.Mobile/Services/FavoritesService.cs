using System.Text.Json;
using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

public class FavoritesService
{
    private readonly OnlineOrderService _orders;
    private readonly SessionService _session;
    private readonly HashSet<int> _favoriteIds = new();
    private int _currentBranchId;
    private string? _customerPhone;
    private int? _customerId;

    public event EventHandler? FavoritesChanged;

    public FavoritesService(OnlineOrderService orders, SessionService session)
    {
        _orders = orders;
        _session = session;
    }

    public bool IsFavorite(int productId) => _favoriteIds.Contains(productId);

    public IReadOnlyCollection<int> GetFavoriteIds() => _favoriteIds;

    public int Count => _favoriteIds.Count;

    public void Initialize(int branchId, string? phone = null, int? customerId = null)
    {
        _currentBranchId = branchId;
        _customerPhone = !string.IsNullOrWhiteSpace(phone)
            ? phone
            : Preferences.Default.Get("sp.customerPhone", string.Empty);

        _customerId = customerId ?? (int?)(_session.User?.Id);

        LoadFromLocal(branchId);
        _ = SyncWithServerAsync();
    }

    public void UpdateCustomerPhone(string phone)
    {
        if (!string.IsNullOrWhiteSpace(phone))
        {
            _customerPhone = phone.Trim();
            Preferences.Default.Set("sp.customerPhone", _customerPhone);
            _ = SyncWithServerAsync();
        }
    }

    private static string GetStorageKey(int branchId) =>
        branchId > 0 ? $"smartpos_store_favorites_{branchId}" : "smartpos_store_favorites";

    public void LoadFromLocal(int branchId)
    {
        _favoriteIds.Clear();
        var key = GetStorageKey(branchId);
        var json = Preferences.Default.Get(key, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            json = Preferences.Default.Get("smartpos_store_favorites", string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<int>>(json);
                if (list != null)
                {
                    foreach (var id in list.Where(x => x > 0))
                    {
                        _favoriteIds.Add(id);
                    }
                }
            }
            catch
            {
                // ignore corrupted local json
            }
        }

        FavoritesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SaveToLocal()
    {
        try
        {
            var json = JsonSerializer.Serialize(_favoriteIds.ToList());
            var key = GetStorageKey(_currentBranchId);
            Preferences.Default.Set(key, json);
            Preferences.Default.Set("smartpos_store_favorites", json);
        }
        catch
        {
            // preferences fallback
        }
    }

    public async Task<bool> ToggleFavoriteAsync(int productId)
    {
        if (productId <= 0)
        {
            return false;
        }

        bool isFavNow;
        if (_favoriteIds.Contains(productId))
        {
            _favoriteIds.Remove(productId);
            isFavNow = false;
        }
        else
        {
            _favoriteIds.Add(productId);
            isFavNow = true;
        }

        // Instant offline-first persistence
        SaveToLocal();
        FavoritesChanged?.Invoke(this, EventArgs.Empty);

        // Background server sync with backend wishlist API
        _ = Task.Run(async () =>
        {
            try
            {
                var res = await _orders.ToggleFavoriteAsync(productId, _currentBranchId, _customerPhone, _customerId);
                if (res != null && res.IsFavorite != isFavNow)
                {
                    // Server returned authoritative state differing from local
                    if (res.IsFavorite)
                    {
                        _favoriteIds.Add(productId);
                    }
                    else
                    {
                        _favoriteIds.Remove(productId);
                    }

                    SaveToLocal();
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        FavoritesChanged?.Invoke(this, EventArgs.Empty);
                    });
                }
            }
            catch
            {
                // Offline or unauthenticated guest: local state persists cleanly
            }
        });

        return isFavNow;
    }

    public async Task SyncWithServerAsync()
    {
        if (string.IsNullOrWhiteSpace(_customerPhone) && !_customerId.HasValue)
        {
            return;
        }

        try
        {
            var serverIds = await _orders.GetFavoriteProductIdsAsync(_currentBranchId, _customerPhone, _customerId);
            if (serverIds != null)
            {
                // Push local favorites to server
                var localOnly = _favoriteIds.Where(id => !serverIds.Contains(id)).ToList();
                foreach (var id in localOnly)
                {
                    try
                    {
                        await _orders.ToggleFavoriteAsync(id, _currentBranchId, _customerPhone, _customerId);
                    }
                    catch
                    {
                    }
                }

                // Add server favorites to local
                foreach (var id in serverIds)
                {
                    _favoriteIds.Add(id);
                }

                SaveToLocal();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    FavoritesChanged?.Invoke(this, EventArgs.Empty);
                });
            }
        }
        catch
        {
        }
    }
}

