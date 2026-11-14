using System.Collections.Concurrent;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services.Home;

namespace SmartPos.Mobile.Services;

/// <summary>
/// In-memory + on-disk cache for online-order browse data so tab switches and cold starts stay snappy.
/// </summary>
public sealed class ProductCatalogCache
{
    private static readonly TimeSpan CatalogTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan StoreMetaTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ModifierTtl = TimeSpan.FromMinutes(30);

    private readonly CatalogDiskStore _disk = new();
    private readonly ConcurrentDictionary<int, CacheEntry<List<OnlineProductDto>>> _catalogByBranch = new();
    private readonly ConcurrentDictionary<string, CacheEntry<AbpPagedResult<OnlineProductDto>>> _catalogPages = new();
    private readonly ConcurrentDictionary<int, CacheEntry<OnlineProductDto>> _productById = new();
    private readonly ConcurrentDictionary<int, CacheEntry<List<ProductModifierGroupDto>>> _modifiersByProduct = new();
    private readonly ConcurrentDictionary<int, CacheEntry<OnlineStoreHeaderDto>> _storeHeaderByBranch = new();
    private readonly ConcurrentDictionary<int, CacheEntry<List<HeroSlideDto>>> _heroByBranch = new();
    private readonly ConcurrentDictionary<int, CacheEntry<List<DealDto>>> _dealsByBranch = new();
    private CacheEntry<List<OnlineBranchDto>>? _branches;

    public bool TryGetCatalog(int branchId, out List<OnlineProductDto>? products)
    {
        products = null;
        if (_catalogByBranch.TryGetValue(branchId, out var entry) && !entry.IsExpired)
        {
            products = entry.Value.Select(CloneProduct).ToList();
            return true;
        }

        if (!_disk.TryRead(CatalogFile(branchId), out List<OnlineProductDto>? diskValue, out var expiresAt) ||
            diskValue == null)
        {
            return false;
        }

        var snapshot = diskValue.Select(CloneProduct).ToList();
        _catalogByBranch[branchId] = CacheEntry<List<OnlineProductDto>>.FromDisk(snapshot, expiresAt, CatalogTtl);
        foreach (var p in snapshot)
        {
            _productById[p.Id] = CacheEntry<OnlineProductDto>.FromDisk(CloneProduct(p), expiresAt, CatalogTtl);
        }

        products = snapshot.Select(CloneProduct).ToList();
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

        _disk.WriteAsync(CatalogFile(branchId), snapshot.Select(CloneProduct).ToList(), CatalogDiskStore.DiskTtl);
    }

    public bool TryGetCatalogPage(string pageKey, out AbpPagedResult<OnlineProductDto>? page)
    {
        page = null;
        if (string.IsNullOrWhiteSpace(pageKey))
        {
            return false;
        }

        if (_catalogPages.TryGetValue(pageKey, out var entry) && !entry.IsExpired)
        {
            page = ClonePage(entry.Value);
            return true;
        }

        if (!_disk.TryRead(PageFile(pageKey), out AbpPagedResult<OnlineProductDto>? diskPage, out var expiresAt) ||
            diskPage == null)
        {
            return false;
        }

        var snapshot = ClonePage(diskPage);
        _catalogPages[pageKey] = CacheEntry<AbpPagedResult<OnlineProductDto>>.FromDisk(snapshot, expiresAt, CatalogTtl);
        foreach (var p in snapshot.Items)
        {
            _productById[p.Id] = CacheEntry<OnlineProductDto>.FromDisk(CloneProduct(p), expiresAt, CatalogTtl);
        }

        page = ClonePage(snapshot);
        return true;
    }

    public void SetCatalogPage(string pageKey, AbpPagedResult<OnlineProductDto> page)
    {
        if (string.IsNullOrWhiteSpace(pageKey) || page == null)
        {
            return;
        }

        var snapshot = ClonePage(page);
        _catalogPages[pageKey] = new CacheEntry<AbpPagedResult<OnlineProductDto>>(snapshot, CatalogTtl);

        foreach (var p in snapshot.Items)
        {
            _productById[p.Id] = new CacheEntry<OnlineProductDto>(CloneProduct(p), CatalogTtl);
        }

        _disk.WriteAsync(PageFile(pageKey), ClonePage(snapshot), CatalogDiskStore.DiskTtl);
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
        if (_storeHeaderByBranch.TryGetValue(branchId, out var entry) && !entry.IsExpired)
        {
            header = CloneStoreHeader(entry.Value);
            return true;
        }

        if (!_disk.TryRead(HeaderFile(branchId), out OnlineStoreHeaderDto? diskHeader, out var expiresAt) ||
            diskHeader == null)
        {
            return false;
        }

        var snapshot = CloneStoreHeader(diskHeader);
        _storeHeaderByBranch[branchId] = CacheEntry<OnlineStoreHeaderDto>.FromDisk(snapshot, expiresAt, StoreMetaTtl);
        header = CloneStoreHeader(snapshot);
        return true;
    }

    public void SetStoreHeader(int branchId, OnlineStoreHeaderDto header)
    {
        var snapshot = CloneStoreHeader(header);
        _storeHeaderByBranch[branchId] = new CacheEntry<OnlineStoreHeaderDto>(snapshot, StoreMetaTtl);
        _disk.WriteAsync(HeaderFile(branchId), CloneStoreHeader(snapshot), CatalogDiskStore.DiskTtl);
    }

    public bool TryGetBranches(out List<OnlineBranchDto>? branches)
    {
        branches = null;
        if (_branches != null && !_branches.IsExpired)
        {
            branches = _branches.Value.ToList();
            return true;
        }

        if (!_disk.TryRead(BranchesFile(), out List<OnlineBranchDto>? diskBranches, out var expiresAt) ||
            diskBranches == null)
        {
            return false;
        }

        var snapshot = diskBranches.ToList();
        _branches = CacheEntry<List<OnlineBranchDto>>.FromDisk(snapshot, expiresAt, StoreMetaTtl);
        branches = snapshot.ToList();
        return true;
    }

    public void SetBranches(IReadOnlyList<OnlineBranchDto> branches)
    {
        var snapshot = branches.ToList();
        _branches = new CacheEntry<List<OnlineBranchDto>>(snapshot, StoreMetaTtl);
        _disk.WriteAsync(BranchesFile(), snapshot.ToList(), CatalogDiskStore.DiskTtl);
    }

    public bool TryGetHeroSlides(int branchId, out List<HeroSlideDto>? slides)
    {
        slides = null;
        if (_heroByBranch.TryGetValue(branchId, out var entry) && !entry.IsExpired)
        {
            slides = entry.Value.ToList();
            return true;
        }

        if (!_disk.TryRead(HeroFile(branchId), out List<HeroSlideDto>? diskSlides, out var expiresAt) ||
            diskSlides == null)
        {
            return false;
        }

        var snapshot = diskSlides.ToList();
        _heroByBranch[branchId] = CacheEntry<List<HeroSlideDto>>.FromDisk(snapshot, expiresAt, StoreMetaTtl);
        slides = snapshot.ToList();
        return true;
    }

    public void SetHeroSlides(int branchId, IReadOnlyList<HeroSlideDto> slides)
    {
        var snapshot = slides.ToList();
        _heroByBranch[branchId] = new CacheEntry<List<HeroSlideDto>>(snapshot, StoreMetaTtl);
        _disk.WriteAsync(HeroFile(branchId), snapshot.ToList(), CatalogDiskStore.DiskTtl);
    }

    public bool TryGetDeals(int branchId, out List<DealDto>? deals)
    {
        deals = null;
        if (_dealsByBranch.TryGetValue(branchId, out var entry) && !entry.IsExpired)
        {
            deals = entry.Value.ToList();
            return true;
        }

        if (!_disk.TryRead(DealsFile(branchId), out List<DealDto>? diskDeals, out var expiresAt) ||
            diskDeals == null)
        {
            return false;
        }

        var snapshot = diskDeals.ToList();
        _dealsByBranch[branchId] = CacheEntry<List<DealDto>>.FromDisk(snapshot, expiresAt, StoreMetaTtl);
        deals = snapshot.ToList();
        return true;
    }

    public void SetDeals(int branchId, IReadOnlyList<DealDto> deals)
    {
        var snapshot = deals.ToList();
        _dealsByBranch[branchId] = new CacheEntry<List<DealDto>>(snapshot, StoreMetaTtl);
        _disk.WriteAsync(DealsFile(branchId), snapshot.ToList(), CatalogDiskStore.DiskTtl);
    }

    public void InvalidateBranch(int branchId)
    {
        _catalogByBranch.TryRemove(branchId, out _);
        _storeHeaderByBranch.TryRemove(branchId, out _);
        _heroByBranch.TryRemove(branchId, out _);
        _dealsByBranch.TryRemove(branchId, out _);

        var prefix = $"{branchId}|";
        foreach (var key in _catalogPages.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
        {
            _catalogPages.TryRemove(key, out _);
            _disk.Delete(PageFile(key));
        }

        _disk.Delete(CatalogFile(branchId));
        _disk.Delete(HeaderFile(branchId));
        _disk.Delete(HeroFile(branchId));
        _disk.Delete(DealsFile(branchId));
        _disk.DeleteByPrefix($"page_{branchId}_");
    }

    public void Clear()
    {
        _catalogByBranch.Clear();
        _catalogPages.Clear();
        _productById.Clear();
        _modifiersByProduct.Clear();
        _storeHeaderByBranch.Clear();
        _heroByBranch.Clear();
        _dealsByBranch.Clear();
        _branches = null;
        _disk.DeleteAll();
    }

    private static string CatalogFile(int branchId) => $"catalog_{branchId}.json";
    private static string HeaderFile(int branchId) => $"header_{branchId}.json";
    private static string HeroFile(int branchId) => $"hero_{branchId}.json";
    private static string DealsFile(int branchId) => $"deals_{branchId}.json";
    private static string BranchesFile() => "branches.json";
    private static string PageFile(string pageKey) => $"page_{CatalogDiskStore.SanitizeFileName(pageKey)}.json";

    private static AbpPagedResult<OnlineProductDto> ClonePage(AbpPagedResult<OnlineProductDto> page) => new()
    {
        TotalCount = page.TotalCount,
        Items = (page.Items ?? new List<OnlineProductDto>()).Select(CloneProduct).ToList()
    };

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

        private CacheEntry(T value, DateTimeOffset expiresAt)
        {
            Value = value;
            ExpiresAt = expiresAt;
        }

        public static CacheEntry<T> FromDisk(T value, DateTimeOffset diskExpiresAt, TimeSpan memoryCap)
        {
            var remaining = diskExpiresAt - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                remaining = TimeSpan.FromSeconds(1);
            }

            var ttl = remaining < memoryCap ? remaining : memoryCap;
            return new CacheEntry<T>(value, DateTimeOffset.UtcNow.Add(ttl));
        }

        public T Value { get; }
        public DateTimeOffset ExpiresAt { get; }
        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    }
}
