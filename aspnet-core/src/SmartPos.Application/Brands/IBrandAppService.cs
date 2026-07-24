using Abp.Application.Services;
using SmartPos.Brands.Dto;

namespace SmartPos.Brands
{
    public interface IBrandAppService : IAsyncCrudAppService<BrandDto, int, PagedBrandResultRequestDto, CreateBrandDto, BrandDto>
    {
    }
}
