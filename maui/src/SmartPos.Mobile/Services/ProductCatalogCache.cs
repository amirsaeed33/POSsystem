using System.Collections.Concurrent;
using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

/// <summary>
/// In-memory cache for online-order catalog and product modifiers so browse/add-to-cart stays snappy.
/// </summary>
public sealed class ProductCatalogCache
{
    private static readonly TimeSpan CatalogTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ModifierTtl = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<int, CacheEntry<List<OnlineProductDto>>> _catalogByBranch = new();
    private readonly ConcurrentDictionary<int, CacheEntry<OnlineProductDto>> _productById = new();
    private readonly ConcurrentDictionary<int, CacheEntry<List<ProductModifierGroupDto>>> _modifiersByProduct = new();

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

    public void InvalidateBranch(int branchId)
    {
        _catalogByBranch.TryRemove(branchId, out _);
    }

    public void Clear()
    {
        _catalogByBranch.Clear();
        _productById.Clear();
        _modifiersByProduct.Clear();
    }

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
