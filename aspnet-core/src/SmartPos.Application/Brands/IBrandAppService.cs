using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using SmartPos.Brands.Dto;

namespace SmartPos.Brands
{
    public interface IBrandAppService : IAsyncCrudAppService<BrandDto, int, PagedBrandResultRequestDto, CreateBrandDto, BrandDto>
    {
        Task<ListResultDto<BrandDto>> GetLookupAsync();
    }
}
