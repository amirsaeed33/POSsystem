using System.Linq;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using SmartPos.Authorization;
using SmartPos.Units.Dto;

namespace SmartPos.Units
{
    [AbpAuthorize(PermissionNames.Pages_Units)]
    public class UnitAppService : AsyncCrudAppService<Unit, UnitDto, int, PagedUnitResultRequestDto, CreateUnitDto, UnitDto>, IUnitAppService
    {
        public UnitAppService(IRepository<Unit> repository)
            : base(repository)
        {
        }

        protected override IQueryable<Unit> CreateFilteredQuery(PagedUnitResultRequestDto input)
        {
            return Repository.GetAll()
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || (x.Description != null && x.Description.Contains(input.Keyword)));
        }
    }
}
