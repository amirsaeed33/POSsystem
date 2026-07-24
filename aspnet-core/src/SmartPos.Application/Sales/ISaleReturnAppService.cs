using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Sales.Dto;

namespace SmartPos.Sales
{
    public interface ISaleReturnAppService : IApplicationService
    {
        Task<SaleReturnableDto> GetReturnableSaleAsync(EntityDto<int> input);

        Task<SaleReturnDto> CreateAsync(CreateSaleReturnDto input);

        Task DeleteAsync(EntityDto<int> input);

        Task<PagedResultDto<SaleReturnDto>> GetAllAsync(PagedSaleReturnResultRequestDto input);

        Task<SaleReturnDto> GetAsync(EntityDto<int> input);
    }
}
