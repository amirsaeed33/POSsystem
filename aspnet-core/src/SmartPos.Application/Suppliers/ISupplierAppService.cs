using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Suppliers.Dto;

namespace SmartPos.Suppliers
{
    public interface ISupplierAppService : IAsyncCrudAppService<SupplierDto, int, PagedSupplierResultRequestDto, CreateSupplierDto, SupplierDto>
    {
        Task<List<int>> GetAssignedProductIdsAsync(EntityDto input);

        Task SetAssignedProductsAsync(SetSupplierProductsDto input);
    }
}
