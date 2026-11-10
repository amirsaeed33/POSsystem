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

    public async Task<DealDto?> GetDealByProductIdAsync(int productId)
    {
        try
        {
            var deal = await _api.GetAsync<DealDto>($"api/services/app/Deal/GetByProductId?productId={productId}");
            if (deal != null)
            {
                deal.ImageUrl = _api.ResolveImageUrl(deal.ImagePath);
            }
            return deal;
        }
        catch
        {
            return null;
        }
    }

    public async Task<DealDto?> GetDealByIdAsync(int dealId)
    {
        try
        {
            var deal = await _api.GetAsync<DealDto>($"api/services/app/Deal/Get?Id={dealId}");
            if (deal != null)
            {
                deal.ImageUrl = _api.ResolveImageUrl(deal.ImagePath);
            }
            return deal;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<DealDto>> GetActiveDealsAsync(int branchId)
    {
        try
        {
            var paged = await _api.GetAsync<AbpPagedResult<DealDto>>($"api/services/app/Deal/GetAll?branchId={branchId}");
            var items = paged?.Items?.ToList() ?? new List<DealDto>();
            foreach (var item in items)
            {
                item.ImageUrl = _api.ResolveImageUrl(item.ImagePath);
            }
            return items;
        }
        catch
        {
            return new List<DealDto>();
        }
    }

    public Task<CustomerOrderDto> CreateOnlineOrderAsync(CreateCustomerOrderDto input) =>
        _api.PostAsync<CustomerOrderDto>("api/services/app/CustomerOrder/CreateOnlineOrder", input);

    /// <summary>
    /// Fetches past customer orders matching the phone number (matching website /online-order/history).
    /// </summary>
    public async Task<List<CustomerOrderDto>> GetCustomerOrdersAsync(string phoneNumber, int? branchId = null, int maxResultCount = 30)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return new List<CustomerOrderDto>();
        }

        var query = new List<string>
        {
            $"Phone={Uri.EscapeDataString(phoneNumber.Trim())}",
            $"MaxResultCount={maxResultCount}"
        };

        if (branchId.HasValue && branchId.Value > 0)
        {
            query.Add($"BranchId={branchId.Value}");
        }

        var url = $"api/services/app/CustomerOrder/GetOrdersByPhone?{string.Join("&", query)}";
        var orders = await _api.GetAsync<List<CustomerOrderDto>>(url);
        return orders ?? new List<CustomerOrderDto>();
    }

    /// <summary>
    /// ABP maps TrackOrderAsync to HTTP POST (not Get*), matching the Angular client.
    /// </summary>
    public Task<CustomerOrderDto> TrackOrderAsync(string orderNo) =>
        _api.PostAsync<CustomerOrderDto>(
            $"api/services/app/CustomerOrder/TrackOrder?orderNo={Uri.EscapeDataString(orderNo)}",
            null);

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

    public async Task<List<int>> GetFavoriteProductIdsAsync(int? branchId, string? phone = null, int? customerId = null)
    {
        try
        {
            var query = new List<string>();
            if (branchId.HasValue && branchId.Value > 0) query.Add($"BranchId={branchId.Value}");
            if (!string.IsNullOrWhiteSpace(phone)) query.Add($"Phone={Uri.EscapeDataString(phone.Trim())}");
            if (customerId.HasValue && customerId.Value > 0) query.Add($"CustomerId={customerId.Value}");

            var queryString = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
            var result = await _api.GetAsync<List<int>>($"api/services/app/CustomerOrder/GetFavoriteProductIds{queryString}");
            return result ?? new List<int>();
        }
        catch
        {
            return new List<int>();
        }
    }

    public async Task<ToggleFavoriteResultDto?> ToggleFavoriteAsync(int productId, int? branchId, string? phone = null, int? customerId = null)
    {
        try
        {
            var input = new
            {
                productId,
                branchId = branchId.HasValue && branchId.Value > 0 ? branchId : null,
                phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                customerId = customerId.HasValue && customerId.Value > 0 ? customerId : null
            };
            return await _api.PostAsync<ToggleFavoriteResultDto>("api/services/app/CustomerOrder/ToggleFavorite", input);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<CustomerFeedbackDto>> GetBranchFeedbacksAsync(int branchId, int maxResultCount = 10)
    {
        try
        {
            var result = await _api.GetAsync<List<CustomerFeedbackDto>>(
                $"api/services/app/CustomerOrder/GetBranchFeedbacks?branchId={branchId}&maxResultCount={maxResultCount}");
            return result ?? new List<CustomerFeedbackDto>();
        }
        catch
        {
            return new List<CustomerFeedbackDto>();
        }
    }

    public async Task<CustomerOrderDto?> RateOrderAsync(string orderNo, int rating, string? comment)
    {
        try
        {
            var input = new RateCustomerOrderInput
            {
                OrderNo = orderNo,
                Rating = rating,
                Comment = comment
            };
            return await _api.PostAsync<CustomerOrderDto>("api/services/app/CustomerOrder/RateOrder", input);
        }
        catch
        {
            return null;
        }
    }
}
