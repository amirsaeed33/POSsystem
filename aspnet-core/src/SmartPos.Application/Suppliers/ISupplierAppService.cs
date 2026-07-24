using Abp.Application.Services;
using SmartPos.Suppliers.Dto;

namespace SmartPos.Suppliers
{
    public interface ISupplierAppService : IAsyncCrudAppService<SupplierDto, int, PagedSupplierResultRequestDto, CreateSupplierDto, SupplierDto>
    {
    }
}
