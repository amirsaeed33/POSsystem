using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Categories.Dto;

namespace SmartPos.Categories
{
    [AbpAuthorize]
    public class CategoryAppService : AsyncCrudAppService<Category, CategoryDto, int, PagedCategoryResultRequestDto, CreateCategoryDto, CategoryDto>, ICategoryAppService
    {
        public CategoryAppService(IRepository<Category> repository)
            : base(repository)
        {
            CreatePermissionName = PermissionNames.Pages_Categories;
            UpdatePermissionName = PermissionNames.Pages_Categories;
            DeletePermissionName = PermissionNames.Pages_Categories;
            GetPermissionName = PermissionNames.Pages_Categories;
            GetAllPermissionName = PermissionNames.Pages_Categories;
        }

        /// <summary>
        /// Dropdown lookup for product forms. Allowed for Categories or Products permission.
        /// </summary>
        [AbpAuthorize(PermissionNames.Pages_Categories, PermissionNames.Pages_Products)]
        public async Task<ListResultDto<CategoryDto>> GetLookupAsync()
        {
            var items = await Repository.GetAll()
                .OrderBy(x => x.Name)
                .ToListAsync();

            return new ListResultDto<CategoryDto>(ObjectMapper.Map<List<CategoryDto>>(items));
        }

        protected override IQueryable<Category> CreateFilteredQuery(PagedCategoryResultRequestDto input)
        {
            return Repository.GetAll()
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || (x.Description != null && x.Description.Contains(input.Keyword)));
        }
    }
}
