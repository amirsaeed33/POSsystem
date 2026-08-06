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
using SmartPos.Brands.Dto;

namespace SmartPos.Brands
{
    [AbpAuthorize]
    public class BrandAppService : AsyncCrudAppService<Brand, BrandDto, int, PagedBrandResultRequestDto, CreateBrandDto, BrandDto>, IBrandAppService
    {
        public BrandAppService(IRepository<Brand> repository)
            : base(repository)
        {
            CreatePermissionName = PermissionNames.Pages_Brands;
            UpdatePermissionName = PermissionNames.Pages_Brands;
            DeletePermissionName = PermissionNames.Pages_Brands;
            GetPermissionName = PermissionNames.Pages_Brands;
            GetAllPermissionName = PermissionNames.Pages_Brands;
        }

        /// <summary>
        /// Dropdown lookup for product forms. Allowed for Brands or Products permission.
        /// </summary>
        [AbpAuthorize(PermissionNames.Pages_Brands, PermissionNames.Pages_Products)]
        public async Task<ListResultDto<BrandDto>> GetLookupAsync()
        {
            var items = await Repository.GetAll()
                .OrderBy(x => x.Name)
                .ToListAsync();

            return new ListResultDto<BrandDto>(ObjectMapper.Map<List<BrandDto>>(items));
        }

        protected override IQueryable<Brand> CreateFilteredQuery(PagedBrandResultRequestDto input)
        {
            return Repository.GetAll()
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || (x.Description != null && x.Description.Contains(input.Keyword)));
        }
    }
}
