using System.Collections.Concurrent;
using System.Text.Json;
using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

public class OnlineOrderService
{
    private const int ModifierPrefetchMaxConcurrency = 6;
    private const int ModifierPrefetchMaxProducts = 40;

    private readonly AbpApiClient _api;
    private readonly ProductCatalogCache _cache;
    private readonly SemaphoreSlim _modifierPrefetchGate = new(ModifierPrefetchMaxConcurrency, ModifierPrefetchMaxConcurrency);
    private readonly ConcurrentDictionary<int, byte> _modifierPrefetchInFlight = new();
    private readonly ConcurrentDictionary<string, Task<AbpPagedResult<OnlineProductDto>>> _catalogPageInFlight = new();

    public OnlineOrderService(AbpApiClient api, ProductCatalogCache cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<List<OnlineBranchDto>> GetBranchesAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cache.TryGetBranches(out var cached) && cached != null)
        {
            return cached;
        }

        var branches = await _api.GetAsync<List<OnlineBranchDto>>("api/services/app/CustomerOrder/GetOnlineBranches")
            ?? new List<OnlineBranchDto>();
        _cache.SetBranches(branches);
        return branches;
    }

    public async Task<OnlineStoreHeaderDto> GetStoreHeaderAsync(int branchId, bool forceRefresh = false)
    {
        if (!forceRefresh && _cache.TryGetStoreHeader(branchId, out var cached) && cached != null)
        {
            return cached;
        }

        var header = await _api.GetAsync<OnlineStoreHeaderDto>(
            $"api/services/app/CustomerOrder/GetOnlineStoreHeader?branchId={branchId}");
        if (header != null)
        {
            _cache.SetStoreHeader(branchId, header);
        }

        return header!;
    }

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

    public async Task<AbpPagedResult<OnlineProductDto>> GetCatalogPageAsync(
        PagedOnlineCatalogRequest request,
        bool forceRefresh = false,
        CancellationToken ct = default)
    {
        request ??= new PagedOnlineCatalogRequest();
        if (request.MaxResultCount <= 0)
        {
            request.MaxResultCount = 30;
        }

        request.MaxResultCount = Math.Min(request.MaxResultCount, 100);
        request.SkipCount = Math.Max(0, request.SkipCount);

        var cacheKey = request.CacheKey;
        if (!forceRefresh && request.SkipCount == 0 && _cache.TryGetCatalogPage(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        if (!forceRefresh && _catalogPageInFlight.TryGetValue(cacheKey, out var inFlight))
        {
            return await inFlight;
        }

        var fetchTask = FetchCatalogPageAsync(request, cacheKey, ct);
        _catalogPageInFlight[cacheKey] = fetchTask;
        try
        {
            return await fetchTask;
        }
        finally
        {
            _catalogPageInFlight.TryRemove(cacheKey, out _);
        }
    }

    private async Task<AbpPagedResult<OnlineProductDto>> FetchCatalogPageAsync(
        PagedOnlineCatalogRequest request,
        string cacheKey,
        CancellationToken ct)
    {
        var query = new List<string>
        {
            $"SkipCount={request.SkipCount}",
            $"MaxResultCount={request.MaxResultCount}"
        };

        if (request.BranchId is int bid && bid > 0)
        {
            query.Add($"BranchId={bid}");
        }

        if (request.CategoryId is int categoryId && categoryId > 0)
        {
            query.Add($"CategoryId={categoryId}");
        }

        if (!string.IsNullOrWhiteSpace(request.CategoryName))
        {
            query.Add($"CategoryName={Uri.EscapeDataString(request.CategoryName.Trim())}");
        }

        if (request.CategoryNames != null)
        {
            foreach (var name in request.CategoryNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                query.Add($"CategoryNames={Uri.EscapeDataString(name.Trim())}");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            query.Add($"Keyword={Uri.EscapeDataString(request.Keyword.Trim())}");
        }

        if (request.DealsOnly)
        {
            query.Add("DealsOnly=true");
        }

        var url = $"api/services/app/CustomerOrder/GetOnlineCatalogPaged?{string.Join("&", query)}";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var page = await _api.GetAsync<AbpPagedResult<OnlineProductDto>>(url, ct)
                   ?? new AbpPagedResult<OnlineProductDto>();
        sw.Stop();
        System.Diagnostics.Debug.WriteLine(
            $"[GetCatalogPageAsync] skip={request.SkipCount} take={request.MaxResultCount} items={page.Items?.Count ?? 0} total={page.TotalCount} in {sw.ElapsedMilliseconds}ms");

        page.Items ??= new List<OnlineProductDto>();
        if (request.SkipCount == 0)
        {
            _cache.SetCatalogPage(cacheKey, page);
        }

        return page;
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

    public async Task<List<HeroSlideDto>> GetHeroSlidesAsync(int branchId, bool forceRefresh = false)
    {
        if (!forceRefresh && _cache.TryGetHeroSlides(branchId, out var cached) && cached != null)
        {
            return cached;
        }

        var url = $"api/services/app/OnlineOrderHeroSlide/GetActiveByBranch?branchId={branchId}";
        List<HeroSlideDto> slides;
        try
        {
            // Successful AbpListResult (including empty Items) is authoritative — do not re-GET.
            var result = await _api.GetAsync<AbpListResult<HeroSlideDto>>(url);
            slides = result?.Items ?? new List<HeroSlideDto>();
            _cache.SetHeroSlides(branchId, slides);
            return slides;
        }
        catch
        {
            // Shape mismatch / transport failure: try raw List<> once.
        }

        try
        {
            slides = await _api.GetAsync<List<HeroSlideDto>>(url) ?? new List<HeroSlideDto>();
        }
        catch
        {
            slides = new List<HeroSlideDto>();
        }

        _cache.SetHeroSlides(branchId, slides);
        return slides;
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

    public async Task<List<DealDto>> GetActiveDealsAsync(int branchId, bool forceRefresh = false)
    {
        if (!forceRefresh && _cache.TryGetDeals(branchId, out var cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var paged = await _api.GetAsync<AbpPagedResult<DealDto>>($"api/services/app/Deal/GetAll?branchId={branchId}");
            var items = paged?.Items?.ToList() ?? new List<DealDto>();
            foreach (var item in items)
            {
                item.ImageUrl = _api.ResolveImageUrl(item.ImagePath);
            }

            _cache.SetDeals(branchId, items);
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
        var ids = products
            .Take(ModifierPrefetchMaxProducts)
            .Select(p => p.Id)
            .Where(id => id > 0)
            .Distinct()
            .Where(id => !_cache.TryGetModifiers(id, out _) && _modifierPrefetchInFlight.TryAdd(id, 0))
            .ToList();

        if (ids.Count == 0)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.WhenAll(ids.Select(PrefetchModifierOneAsync));
            }
            catch
            {
                // best-effort warm-up
            }
        });
    }

    private async Task PrefetchModifierOneAsync(int productId)
    {
        await _modifierPrefetchGate.WaitAsync().ConfigureAwait(false);
        try
        {
            await GetModifiersForProductAsync(productId).ConfigureAwait(false);
        }
        catch
        {
            // best-effort warm-up
        }
        finally
        {
            _modifierPrefetchGate.Release();
            _modifierPrefetchInFlight.TryRemove(productId, out _);
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
            if (result != null)
            {
                foreach (var fb in result)
                {
                    if (!string.IsNullOrWhiteSpace(fb.CustomerImagePath))
                    {
                        fb.CustomerPhotoUrl = _api.ResolveImageUrl(fb.CustomerImagePath);
                    }
                }
            }
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

    private static string GetSavedAddressCacheKey(string phone) => $"sp_saved_addresses_{phone.Trim()}";

    public List<CustomerSavedAddressDto> GetCachedSavedAddresses(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return new List<CustomerSavedAddressDto>();
        try
        {
            var json = Preferences.Default.Get(GetSavedAddressCacheKey(phone), string.Empty);
            if (string.IsNullOrWhiteSpace(json)) return new List<CustomerSavedAddressDto>();
            var list = JsonSerializer.Deserialize<List<CustomerSavedAddressDto>>(json, JsonDefaults.Options);
            return list ?? new List<CustomerSavedAddressDto>();
        }
        catch
        {
            return new List<CustomerSavedAddressDto>();
        }
    }

    public void SaveCachedSavedAddresses(string phone, List<CustomerSavedAddressDto> list)
    {
        if (string.IsNullOrWhiteSpace(phone)) return;
        try
        {
            var json = JsonSerializer.Serialize(list, JsonDefaults.Options);
            Preferences.Default.Set(GetSavedAddressCacheKey(phone), json);
        }
        catch
        {
            // best-effort local cache
        }
    }

    public async Task<List<CustomerSavedAddressDto>> GetSavedAddressesAsync(string phone, int? branchId = null, int? customerId = null)
    {
        var localCached = GetCachedSavedAddresses(phone);

        if (string.IsNullOrWhiteSpace(phone))
        {
            return localCached;
        }

        try
        {
            var query = new List<string> { $"Phone={Uri.EscapeDataString(phone.Trim())}" };
            if (branchId.HasValue && branchId.Value > 0) query.Add($"BranchId={branchId.Value}");
            if (customerId.HasValue && customerId.Value > 0) query.Add($"CustomerId={customerId.Value}");

            var queryString = string.Join("&", query);
            var result = await _api.GetAsync<List<CustomerSavedAddressDto>>($"api/services/app/CustomerOrder/GetSavedAddresses?{queryString}");
            if (result != null && result.Count > 0)
            {
                SaveCachedSavedAddresses(phone, result);
                return result;
            }
        }
        catch
        {
            // fallback to local cache
        }

        return localCached;
    }

    public async Task<CustomerSavedAddressDto?> SaveSavedAddressAsync(SaveCustomerAddressInput input)
    {
        CustomerSavedAddressDto? saved = null;
        try
        {
            saved = await _api.PostAsync<CustomerSavedAddressDto>("api/services/app/CustomerOrder/SaveSavedAddress", input);
        }
        catch
        {
            // fallback offline/local save
        }

        if (saved == null)
        {
            saved = new CustomerSavedAddressDto
            {
                Id = input.Id ?? (int)(DateTime.UtcNow.Ticks % int.MaxValue),
                Label = input.Label,
                Address = input.Address,
                Landmark = input.Landmark,
                DeliveryAreaId = input.DeliveryAreaId,
                DeliveryAreaName = input.DeliveryAreaName,
                Latitude = input.Latitude,
                Longitude = input.Longitude,
                IsDefault = input.IsDefault
            };
        }

        if (!string.IsNullOrWhiteSpace(input.Phone))
        {
            var current = GetCachedSavedAddresses(input.Phone);
            var existingIdx = current.FindIndex(a => (saved.Id > 0 && a.Id == saved.Id) || (string.Equals(a.Address, saved.Address, StringComparison.OrdinalIgnoreCase) && string.Equals(a.Label, saved.Label, StringComparison.OrdinalIgnoreCase)));
            if (existingIdx >= 0)
            {
                current[existingIdx] = saved;
            }
            else
            {
                current.Insert(0, saved);
            }
            SaveCachedSavedAddresses(input.Phone, current);
        }

        return saved;
    }

    public async Task DeleteSavedAddressAsync(int id, string? phone = null, int? branchId = null)
    {
        try
        {
            var query = new List<string> { $"Id={id}" };
            if (!string.IsNullOrWhiteSpace(phone)) query.Add($"Phone={Uri.EscapeDataString(phone.Trim())}");
            if (branchId.HasValue && branchId.Value > 0) query.Add($"BranchId={branchId.Value}");

            var queryString = string.Join("&", query);
            await _api.DeleteAsync($"api/services/app/CustomerOrder/DeleteSavedAddress?{queryString}");
        }
        catch
        {
            // best-effort
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var current = GetCachedSavedAddresses(phone);
            current.RemoveAll(a => a.Id == id);
            SaveCachedSavedAddresses(phone, current);
        }
    }

    public async Task<OnlineCustomerProfileDto?> GetOnlineCustomerProfileAsync(string phone, int? branchId = null)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        try
        {
            var url = branchId.HasValue && branchId.Value > 0
                ? $"api/services/app/CustomerOrder/GetOnlineCustomerProfile?phone={Uri.EscapeDataString(phone.Trim())}&branchId={branchId.Value}"
                : $"api/services/app/CustomerOrder/GetOnlineCustomerProfile?phone={Uri.EscapeDataString(phone.Trim())}";
            var profile = await _api.GetAsync<OnlineCustomerProfileDto>(url);
            return profile;
        }
        catch
        {
            return null;
        }
    }

    public async Task<OnlineCustomerProfileDto?> UpsertOnlineCustomerProfileAsync(UpsertOnlineCustomerProfileInput input)
    {
        try
        {
            var profile = await _api.PostAsync<OnlineCustomerProfileDto>("api/services/app/CustomerOrder/UpsertOnlineCustomerProfile", input);
            return profile;
        }
        catch
        {
            return null;
        }
    }

    public string ResolveImageUrl(string? path) => _api.ResolveImageUrl(path);
}
