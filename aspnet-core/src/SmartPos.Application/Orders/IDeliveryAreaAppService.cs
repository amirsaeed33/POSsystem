using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Orders.Dto;

namespace SmartPos.Orders
{
    public interface IDeliveryAreaAppService : IAsyncCrudAppService<
        DeliveryAreaDto,
        int,
        PagedDeliveryAreaResultRequestDto,
        CreateDeliveryAreaDto,
        DeliveryAreaDto>
    {
        Task<List<DeliveryAreaDto>> GetActiveAreasForBranchAsync(int branchId);

        Task<DeliveryAreaDto> ToggleActiveAsync(EntityDto<int> input);
    }
}
