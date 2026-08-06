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
using SmartPos.Units.Dto;

namespace SmartPos.Units
{
    [AbpAuthorize]
    public class UnitAppService : AsyncCrudAppService<Unit, UnitDto, int, PagedUnitResultRequestDto, CreateUnitDto, UnitDto>, IUnitAppService
    {
        public UnitAppService(IRepository<Unit> repository)
            : base(repository)
        {
            CreatePermissionName = PermissionNames.Pages_Units;
            UpdatePermissionName = PermissionNames.Pages_Units;
            DeletePermissionName = PermissionNames.Pages_Units;
            GetPermissionName = PermissionNames.Pages_Units;
            GetAllPermissionName = PermissionNames.Pages_Units;
        }

        /// <summary>
        /// Dropdown lookup for product forms. Allowed for Units or Products permission.
        /// </summary>
        [AbpAuthorize(PermissionNames.Pages_Units, PermissionNames.Pages_Products)]
        public async Task<ListResultDto<UnitDto>> GetLookupAsync()
        {
            var items = await Repository.GetAll()
                .OrderBy(x => x.Name)
                .ToListAsync();

            return new ListResultDto<UnitDto>(ObjectMapper.Map<List<UnitDto>>(items));
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
