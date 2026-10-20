using Abp.Application.Services;
using SmartPos.GroceryPurchases.Dto;

namespace SmartPos.GroceryPurchases
{
    public interface IGroceryPurchaseAppService : IAsyncCrudAppService<GroceryPurchaseDto, int, PagedGroceryPurchaseResultRequestDto, CreateGroceryPurchaseDto, UpdateGroceryPurchaseDto>
    {
    }
}
