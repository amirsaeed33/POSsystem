using Abp.Application.Services;
using SmartPos.Categories.Dto;

namespace SmartPos.Categories
{
    public interface ICategoryAppService : IAsyncCrudAppService<CategoryDto, int, PagedCategoryResultRequestDto, CreateCategoryDto, CategoryDto>
    {
    }
}
