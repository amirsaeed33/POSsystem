using Abp.Application.Services;
using SmartPos.Customers.Dto;

namespace SmartPos.Customers
{
    public interface ICustomerAppService : IAsyncCrudAppService<CustomerDto, int, PagedCustomerResultRequestDto, CreateCustomerDto, CustomerDto>
    {
    }
}
