using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

public class OnlineOrderService
{
    private readonly AbpApiClient _api;

    public OnlineOrderService(AbpApiClient api)
    {
        _api = api;
    }

    public Task<List<OnlineBranchDto>> GetBranchesAsync() =>
        _api.GetAsync<List<OnlineBranchDto>>("api/services/app/CustomerOrder/GetOnlineBranches");

    public Task<OnlineStoreHeaderDto> GetStoreHeaderAsync(int branchId) =>
        _api.GetAsync<OnlineStoreHeaderDto>($"api/services/app/CustomerOrder/GetOnlineStoreHeader?branchId={branchId}");

    public Task<List<OnlineProductDto>> GetCatalogAsync(int? branchId) =>
        _api.GetAsync<List<OnlineProductDto>>(
            branchId.HasValue
                ? $"api/services/app/CustomerOrder/GetOnlineCatalog?branchId={branchId}"
                : "api/services/app/CustomerOrder/GetOnlineCatalog");

    public Task<OnlineProductDto> GetProductAsync(int id, int? branchId) =>
        _api.GetAsync<OnlineProductDto>(
            branchId.HasValue
                ? $"api/services/app/CustomerOrder/GetOnlineProduct?id={id}&branchId={branchId}"
                : $"api/services/app/CustomerOrder/GetOnlineProduct?id={id}");

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
}
