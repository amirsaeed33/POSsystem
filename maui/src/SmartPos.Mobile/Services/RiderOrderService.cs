using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

public class RiderOrderService
{
    private readonly AbpApiClient _api;

    public RiderOrderService(AbpApiClient api)
    {
        _api = api;
    }

    public Task<RiderDashboardStatsDto> GetDashboardStatsAsync() =>
        _api.GetAsync<RiderDashboardStatsDto>("api/services/app/RiderOrder/GetDashboardStats");

    public Task<List<RiderOrderDto>> GetAssignedOrdersAsync() =>
        _api.GetAsync<List<RiderOrderDto>>("api/services/app/RiderOrder/GetAssignedOrders");

    public Task<AbpPagedResult<RiderOrderDto>> GetOrderHistoryAsync(int skipCount = 0, int maxResultCount = 20, string? keyword = null)
    {
        var url = $"api/services/app/RiderOrder/GetOrderHistory?SkipCount={skipCount}&MaxResultCount={maxResultCount}";
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            url += $"&Keyword={Uri.EscapeDataString(keyword)}";
        }

        return _api.GetAsync<AbpPagedResult<RiderOrderDto>>(url);
    }

    public Task<RiderOrderDto> AcceptOrderAsync(int orderId) =>
        _api.PostAsync<RiderOrderDto>("api/services/app/RiderOrder/AcceptOrder", new EntityDto { Id = orderId });

    public Task<RiderOrderDto> RejectOrderAsync(int orderId, string reason) =>
        _api.PostAsync<RiderOrderDto>("api/services/app/RiderOrder/RejectOrder", new RejectRiderOrderDto
        {
            OrderId = orderId,
            Reason = reason
        });

    public Task<RiderOrderDto> CompleteDeliveryAsync(int orderId) =>
        _api.PostAsync<RiderOrderDto>("api/services/app/RiderOrder/CompleteDelivery", new EntityDto { Id = orderId });
}
