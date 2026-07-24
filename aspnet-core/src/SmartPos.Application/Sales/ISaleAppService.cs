using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Sales.Dto;

namespace SmartPos.Sales
{
    public interface ISaleAppService : IAsyncCrudAppService<SaleDto, int, PagedSaleResultRequestDto, CreateSaleDto, SaleDto>
    {
    }
}
