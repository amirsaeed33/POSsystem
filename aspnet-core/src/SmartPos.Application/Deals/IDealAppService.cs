using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Deals.Dto;

namespace SmartPos.Deals
{
    public interface IDealAppService : IApplicationService
    {
        Task<DealDto> CreateAsync(CreateDealDto input);

        Task<DealDto> GetAsync(EntityDto<int> input);

        Task<DealDto> GetByProductIdAsync(int productId);

        Task<PagedResultDto<DealDto>> GetAllAsync(PagedDealResultRequestDto input);

        Task DeleteAsync(EntityDto<int> input);
    }
}
