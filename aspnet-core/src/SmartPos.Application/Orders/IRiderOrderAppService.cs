using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Orders.Dto;

namespace SmartPos.Orders
{
    public interface IRiderOrderAppService : IApplicationService
    {
        Task<RiderDashboardStatsDto> GetDashboardStatsAsync();
        Task<List<RiderOrderDto>> GetAssignedOrdersAsync();
        Task<PagedResultDto<RiderOrderDto>> GetOrderHistoryAsync(RiderOrderHistoryInput input);
        Task<RiderOrderDto> AcceptOrderAsync(EntityDto<int> input);
        Task<RiderOrderDto> RejectOrderAsync(RejectRiderOrderDto input);
        Task<RiderOrderDto> CompleteDeliveryAsync(EntityDto<int> input);
    }
}
