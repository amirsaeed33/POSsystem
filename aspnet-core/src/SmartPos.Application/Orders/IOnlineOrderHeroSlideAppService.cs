using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Orders.Dto;

namespace SmartPos.Orders
{
    public interface IOnlineOrderHeroSlideAppService : IApplicationService
    {
        Task<ListResultDto<OnlineOrderHeroSlideDto>> GetByBranchAsync(int branchId);

        Task<ListResultDto<OnlineOrderHeroSlideDto>> GetActiveByBranchAsync(int branchId);

        Task<OnlineOrderHeroSlideDto> CreateOrUpdateAsync(CreateOrUpdateOnlineOrderHeroSlideDto input);

        Task DeleteAsync(EntityDto input);
    }
}
