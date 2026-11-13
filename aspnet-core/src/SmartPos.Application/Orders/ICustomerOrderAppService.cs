using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Orders.Dto;
using SmartPos.Sales.Dto;

namespace SmartPos.Orders
{
    public interface ICustomerOrderAppService : IAsyncCrudAppService<
        CustomerOrderDto,
        int,
        PagedCustomerOrderResultRequestDto,
        CreateCustomerOrderDto,
        CustomerOrderDto>
    {
        Task<SaleDto> ApproveAsync(EntityDto<int> input);

        Task RejectAsync(EntityDto<int> input);

        Task<CustomerOrderDto> CreateOnlineOrderAsync(CreateCustomerOrderDto input);

        Task<List<OnlineProductDto>> GetOnlineCatalogAsync(int? branchId);

        [AbpAllowAnonymous]
        Task<PagedResultDto<OnlineProductDto>> GetOnlineCatalogPagedAsync(PagedOnlineCatalogResultRequestDto input);

        Task<OnlineProductDto> GetOnlineProductAsync(int id, int? branchId);

        Task<OnlineStoreHeaderDto> GetOnlineStoreHeaderAsync(int branchId);
    }
}
