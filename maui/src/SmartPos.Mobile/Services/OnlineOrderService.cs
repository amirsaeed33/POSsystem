using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

public class OnlineOrderService
{
    private readonly AbpApiClient _api;
    private readonly ProductCatalogCache _cache;

    public OnlineOrderService(AbpApiClient api, ProductCatalogCache cache)
    {
        _api = api;
        _cache = cache;
    }

    public Task<List<OnlineBranchDto>> GetBranchesAsync() =>
        _api.GetAsync<List<OnlineBranchDto>>("api/services/app/CustomerOrder/GetOnlineBranches");

    public Task<OnlineStoreHeaderDto> GetStoreHeaderAsync(int branchId) =>
        _api.GetAsync<OnlineStoreHeaderDto>($"api/services/app/CustomerOrder/GetOnlineStoreHeader?branchId={branchId}");

    public async Task<List<OnlineProductDto>> GetCatalogAsync(int? branchId, bool forceRefresh = false)
    {
        if (branchId is int bid && !forceRefresh && _cache.TryGetCatalog(bid, out var cached) && cached != null)
        {
            return cached;
        }

        var products = await _api.GetAsync<List<OnlineProductDto>>(
            branchId.HasValue
                ? $"api/services/app/CustomerOrder/GetOnlineCatalog?branchId={branchId}"
                : "api/services/app/CustomerOrder/GetOnlineCatalog")
            ?? new List<OnlineProductDto>();

        if (branchId is int branchKey)
        {
            _cache.SetCatalog(branchKey, products);
        }

        return products;
    }

    public async Task<OnlineProductDto> GetProductAsync(int id, int? branchId, bool forceRefresh = false)
    {
        if (!forceRefresh && _cache.TryGetProduct(id, out var cached) && cached != null)
        {
            return cached;
        }

        var product = await _api.GetAsync<OnlineProductDto>(
            branchId.HasValue
                ? $"api/services/app/CustomerOrder/GetOnlineProduct?id={id}&branchId={branchId}"
                : $"api/services/app/CustomerOrder/GetOnlineProduct?id={id}");

        _cache.SetProduct(product);
        return product;
    }

    public Task<List<string>> GetCategoriesAsync(int? branchId) =>
        _api.GetAsync<List<string>>(
            branchId.HasValue
                ? $"api/services/app/CustomerOrder/GetOnlineCategories?branchId={branchId}"
                : "api/services/app/CustomerOrder/GetOnlineCategories");

    public Task<List<DeliveryAreaDto>> GetDeliveryAreasAsync(int branchId) =>
        _api.GetAsync<List<DeliveryAreaDto>>($"api/services/app/DeliveryArea/GetActiveAreasForBranch?branchId={branchId}");

    public async Task<List<HeroSlideDto>> GetHeroSlidesAsync(int branchId)
    {
        var url = $"api/services/app/OnlineOrderHeroSlide/GetActiveByBranch?branchId={branchId}";
        try
        {
            var result = await _api.GetAsync<AbpListResult<HeroSlideDto>>(url);
            if (result.Items?.Count > 0)
            {
                return result.Items;
            }
        }
        catch
        {
            // fall through
        }

        try
        {
            return await _api.GetAsync<List<HeroSlideDto>>(url) ?? new List<HeroSlideDto>();
        }
        catch
        {
            return new List<HeroSlideDto>();
        }
    }

    public Task<CustomerOrderDto> CreateOnlineOrderAsync(CreateCustomerOrderDto input) =>
        _api.PostAsync<CustomerOrderDto>("api/services/app/CustomerOrder/CreateOnlineOrder", input);

    public Task<CustomerOrderDto> TrackOrderAsync(string orderNo) =>
        _api.GetAsync<CustomerOrderDto>($"api/services/app/CustomerOrder/TrackOrder?orderNo={Uri.EscapeDataString(orderNo)}");

    public async Task<List<ProductModifierGroupDto>> GetModifiersForProductAsync(int productId, bool forceRefresh = false)
    {
        if (!forceRefresh && _cache.TryGetModifiers(productId, out var cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var groups = await _api.GetAsync<List<ProductModifierGroupDto>>(
                $"api/services/app/ProductModifier/GetByProduct?productId={productId}")
                ?? new List<ProductModifierGroupDto>();
            _cache.SetModifiers(productId, groups);
            return groups;
        }
        catch
        {
            return new List<ProductModifierGroupDto>();
        }
    }

    /// <summary>Warm modifier cache in the background so size popups open instantly.</summary>
    public void PrefetchModifiersForCatalog(IEnumerable<OnlineProductDto> products)
    {
        foreach (var product in products.Take(40))
        {
            var id = product.Id;
            _ = Task.Run(async () =>
            {
                try
                {
                    await GetModifiersForProductAsync(id);
                }
                catch
                {
                    // best-effort warm-up
                }
            });
        }
    }

    public void InvalidateCatalog(int branchId) => _cache.InvalidateBranch(branchId);
}
