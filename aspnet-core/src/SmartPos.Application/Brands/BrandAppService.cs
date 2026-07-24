using System.Linq;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using SmartPos.Authorization;
using SmartPos.Brands.Dto;

namespace SmartPos.Brands
{
    [AbpAuthorize(PermissionNames.Pages_Brands)]
    public class BrandAppService : AsyncCrudAppService<Brand, BrandDto, int, PagedBrandResultRequestDto, CreateBrandDto, BrandDto>, IBrandAppService
    {
        public BrandAppService(IRepository<Brand> repository)
            : base(repository)
        {
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
