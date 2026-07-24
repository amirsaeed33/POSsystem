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
    }
}
