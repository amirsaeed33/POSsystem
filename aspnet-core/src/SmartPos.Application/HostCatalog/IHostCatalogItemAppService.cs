using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.HostCatalog.Dto;

namespace SmartPos.HostCatalog
{
    public interface IHostCatalogItemAppService :
        IAsyncCrudAppService<HostCatalogItemDto, int, PagedHostCatalogItemResultRequestDto, CreateHostCatalogItemDto, HostCatalogItemDto>
    {
        Task<ListResultDto<HostCatalogItemDto>> GetCompanyTypesForSeedAsync();

        Task<HostCatalogByCompanyTypeDto> GetCatalogByCompanyTypeAsync(EntityDto<int> input);
    }
}
