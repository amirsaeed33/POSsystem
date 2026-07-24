using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Purchases.Dto;

namespace SmartPos.Purchases
{
    public interface IPurchaseReturnAppService : IApplicationService
    {
        Task<PurchaseReturnableDto> GetReturnablePurchaseAsync(EntityDto<int> input);

        Task<PurchaseReturnDto> CreateAsync(CreatePurchaseReturnDto input);

        Task DeleteAsync(EntityDto<int> input);

        Task<PagedResultDto<PurchaseReturnDto>> GetAllAsync(PagedPurchaseReturnResultRequestDto input);

        Task<PurchaseReturnDto> GetAsync(EntityDto<int> input);
    }
}
