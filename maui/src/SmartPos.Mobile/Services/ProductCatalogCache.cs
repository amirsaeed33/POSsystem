using System.Collections.Concurrent;
using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

/// <summary>
/// In-memory cache for online-order browse data so tab/menu switches stay snappy.
/// </summary>
public sealed class ProductCatalogCache
{
    private static readonly TimeSpan CatalogTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan StoreMetaTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ModifierTtl = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<int, CacheEntry<List<OnlineProductDto>>> _catalogByBranch = new();
    private readonly ConcurrentDictionary<int, CacheEntry<OnlineProductDto>> _productById = new();
    private readonly ConcurrentDictionary<int, CacheEntry<List<ProductModifierGroupDto>>> _modifiersByProduct = new();
    private readonly ConcurrentDictionary<int, CacheEntry<OnlineStoreHeaderDto>> _storeHeaderByBranch = new();
    private readonly ConcurrentDictionary<int, CacheEntry<List<HeroSlideDto>>> _heroByBranch = new();
    private readonly ConcurrentDictionary<int, CacheEntry<List<DealDto>>> _dealsByBranch = new();
    private CacheEntry<List<OnlineBranchDto>>? _branches;

    public bool TryGetCatalog(int branchId, out List<OnlineProductDto>? products)
    {
        products = null;
        if (!_catalogByBranch.TryGetValue(branchId, out var entry) || entry.IsExpired)
        {
            return false;
        }

        // Return shallow copies of list so callers can mutate ImageUrl/CartQty safely.
        products = entry.Value.Select(CloneProduct).ToList();
        return true;
    }

    public void SetCatalog(int branchId, IReadOnlyList<OnlineProductDto> products)
    {
        var snapshot = products.Select(CloneProduct).ToList();
        _catalogByBranch[branchId] = new CacheEntry<List<OnlineProductDto>>(snapshot, CatalogTtl);

        foreach (var p in snapshot)
        {
            _productById[p.Id] = new CacheEntry<OnlineProductDto>(CloneProduct(p), CatalogTtl);
        }
    }

    public bool TryGetProduct(int productId, out OnlineProductDto? product)
    {
        product = null;
        if (!_productById.TryGetValue(productId, out var entry) || entry.IsExpired)
        {
            return false;
        }

        product = CloneProduct(entry.Value);
        return true;
    }

    public void SetProduct(OnlineProductDto product)
    {
        _productById[product.Id] = new CacheEntry<OnlineProductDto>(CloneProduct(product), CatalogTtl);
    }

    public bool TryGetModifiers(int productId, out List<ProductModifierGroupDto>? groups)
    {
        groups = null;
        if (!_modifiersByProduct.TryGetValue(productId, out var entry) || entry.IsExpired)
        {
            return false;
        }

        groups = entry.Value;
        return true;
    }

    public void SetModifiers(int productId, List<ProductModifierGroupDto> groups)
    {
        _modifiersByProduct[productId] = new CacheEntry<List<ProductModifierGroupDto>>(groups, ModifierTtl);
    }

    public bool TryGetStoreHeader(int branchId, out OnlineStoreHeaderDto? header)
    {
        header = null;
        if (!_storeHeaderByBranch.TryGetValue(branchId, out var entry) || entry.IsExpired)
        {
            return false;
        }

        header = CloneStoreHeader(entry.Value);
        return true;
    }

    public void SetStoreHeader(int branchId, OnlineStoreHeaderDto header)
    {
        _storeHeaderByBranch[branchId] = new CacheEntry<OnlineStoreHeaderDto>(CloneStoreHeader(header), StoreMetaTtl);
    }

    public bool TryGetBranches(out List<OnlineBranchDto>? branches)
    {
        branches = null;
        if (_branches == null || _branches.IsExpired)
        {
            return false;
        }

        branches = _branches.Value.ToList();
        return true;
    }

    public void SetBranches(IReadOnlyList<OnlineBranchDto> branches)
    {
        _branches = new CacheEntry<List<OnlineBranchDto>>(branches.ToList(), StoreMetaTtl);
    }

    public bool TryGetHeroSlides(int branchId, out List<HeroSlideDto>? slides)
    {
        slides = null;
        if (!_heroByBranch.TryGetValue(branchId, out var entry) || entry.IsExpired)
        {
            return false;
        }

        slides = entry.Value.ToList();
        return true;
    }

    public void SetHeroSlides(int branchId, IReadOnlyList<HeroSlideDto> slides)
    {
        _heroByBranch[branchId] = new CacheEntry<List<HeroSlideDto>>(slides.ToList(), StoreMetaTtl);
    }

    public bool TryGetDeals(int branchId, out List<DealDto>? deals)
    {
        deals = null;
        if (!_dealsByBranch.TryGetValue(branchId, out var entry) || entry.IsExpired)
        {
            return false;
        }

        deals = entry.Value.ToList();
        return true;
    }

    public void SetDeals(int branchId, IReadOnlyList<DealDto> deals)
    {
        _dealsByBranch[branchId] = new CacheEntry<List<DealDto>>(deals.ToList(), StoreMetaTtl);
    }

    public void InvalidateBranch(int branchId)
    {
        _catalogByBranch.TryRemove(branchId, out _);
        _storeHeaderByBranch.TryRemove(branchId, out _);
        _heroByBranch.TryRemove(branchId, out _);
        _dealsByBranch.TryRemove(branchId, out _);
    }

    public void Clear()
    {
        _catalogByBranch.Clear();
        _productById.Clear();
        _modifiersByProduct.Clear();
        _storeHeaderByBranch.Clear();
        _heroByBranch.Clear();
        _dealsByBranch.Clear();
        _branches = null;
    }

    private static OnlineStoreHeaderDto CloneStoreHeader(OnlineStoreHeaderDto h) => new()
    {
        BranchId = h.BranchId,
        BranchName = h.BranchName,
        Address = h.Address,
        Phone = h.Phone,
        CurrencyFormat = h.CurrencyFormat,
        BaseDeliveryFee = h.BaseDeliveryFee,
        IsDeliveryEnabled = h.IsDeliveryEnabled,
        FreeDeliveryThreshold = h.FreeDeliveryThreshold,
        DeliveryAreaNote = h.DeliveryAreaNote,
        IsRestaurant = h.IsRestaurant,
        Categories = h.Categories?.ToList() ?? new List<string>()
    };

    private static OnlineProductDto CloneProduct(OnlineProductDto p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        CategoryName = p.CategoryName,
        CategoryId = p.CategoryId,
        Price = p.Price,
        CurrencyFormat = p.CurrencyFormat,
        ImagePath = p.ImagePath,
        ImageUrl = p.ImageUrl,
        InStock = p.InStock,
        StockQuantity = p.StockQuantity,
        IsFreeDelivery = p.IsFreeDelivery,
        IsDeal = p.IsDeal,
        CardTag = p.CardTag,
        CartQty = p.CartQty,
        DealId = p.DealId,
        OriginalPrice = p.OriginalPrice,
        DealItemsSummary = p.DealItemsSummary
    };

    private sealed class CacheEntry<T>
    {
        public CacheEntry(T value, TimeSpan ttl)
        {
            Value = value;
            ExpiresAt = DateTimeOffset.UtcNow.Add(ttl);
        }

        public T Value { get; }
        public DateTimeOffset ExpiresAt { get; }
        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    }
}
