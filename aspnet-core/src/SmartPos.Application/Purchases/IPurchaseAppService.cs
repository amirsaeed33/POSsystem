using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Purchases.Dto;

namespace SmartPos.Purchases
{
    public interface IPurchaseAppService : IAsyncCrudAppService<PurchaseDto, int, PagedPurchaseResultRequestDto, CreatePurchaseDto, PurchaseDto>
    {
    }
}
