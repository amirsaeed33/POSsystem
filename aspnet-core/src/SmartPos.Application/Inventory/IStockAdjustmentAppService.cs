using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Inventory.Dto;

namespace SmartPos.Inventory
{
    public interface IStockAdjustmentAppService : IApplicationService
    {
        Task<StockAdjustmentDto> CreateAsync(CreateStockAdjustmentDto input);

        Task<StockAdjustmentDto> GetAsync(EntityDto<int> input);

        Task<PagedResultDto<StockAdjustmentDto>> GetAllAsync(PagedStockAdjustmentResultRequestDto input);

        Task DeleteAsync(EntityDto<int> input);
    }
}
