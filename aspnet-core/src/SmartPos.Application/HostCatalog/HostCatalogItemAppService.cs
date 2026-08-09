using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.HostCatalog.Dto;

namespace SmartPos.HostCatalog
{
    [AbpAuthorize]
    public class HostCatalogItemAppService :
        AsyncCrudAppService<HostCatalogItem, HostCatalogItemDto, int, PagedHostCatalogItemResultRequestDto, CreateHostCatalogItemDto, HostCatalogItemDto>,
        IHostCatalogItemAppService
    {
        public HostCatalogItemAppService(IRepository<HostCatalogItem> repository)
            : base(repository)
        {
            CreatePermissionName = PermissionNames.Pages_HostCatalog;
            UpdatePermissionName = PermissionNames.Pages_HostCatalog;
            DeletePermissionName = PermissionNames.Pages_HostCatalog;
            GetPermissionName = PermissionNames.Pages_HostCatalog;
            GetAllPermissionName = PermissionNames.Pages_HostCatalog;
        }

        [AbpAuthorize(PermissionNames.Pages_HostCatalog)]
        public override async Task<HostCatalogItemDto> CreateAsync(CreateHostCatalogItemDto input)
        {
            Normalize(input);
            await ValidateAsync(input.Type, input.CompanyTypeId, input.Name, null);
            var dto = await base.CreateAsync(input);
            return await FillCompanyTypeNameAsync(dto);
        }

        [AbpAuthorize(PermissionNames.Pages_HostCatalog)]
        public override async Task<HostCatalogItemDto> UpdateAsync(HostCatalogItemDto input)
        {
            Normalize(input);
            await ValidateAsync(input.Type, input.CompanyTypeId, input.Name, input.Id);
            var dto = await base.UpdateAsync(input);
            return await FillCompanyTypeNameAsync(dto);
        }

        [AbpAuthorize(PermissionNames.Pages_HostCatalog)]
        public override async Task<HostCatalogItemDto> GetAsync(EntityDto<int> input)
        {
            return await FillCompanyTypeNameAsync(await base.GetAsync(input));
        }

        [AbpAuthorize(PermissionNames.Pages_HostCatalog)]
        public override async Task<PagedResultDto<HostCatalogItemDto>> GetAllAsync(PagedHostCatalogItemResultRequestDto input)
        {
            var result = await base.GetAllAsync(input);
            await FillCompanyTypeNamesAsync(result.Items);
            return result;
        }

        /// <summary>
        /// Active company types for branch-create / signup picker.
        /// Anonymous so public signup can load options before authentication.
        /// </summary>
        [AbpAllowAnonymous]
        public async Task<ListResultDto<HostCatalogItemDto>> GetCompanyTypesForSeedAsync()
        {
            var items = await Repository.GetAll()
                .Where(x => x.Type == HostCatalogItemTypes.CompanyType && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return new ListResultDto<HostCatalogItemDto>(
                ObjectMapper.Map<List<HostCatalogItemDto>>(items));
        }

        /// <summary>
        /// Active categories/units/brands under a company type for branch-create / signup picker.
        /// Anonymous so public signup can load options before authentication.
        /// </summary>
        [AbpAllowAnonymous]
        public async Task<HostCatalogByCompanyTypeDto> GetCatalogByCompanyTypeAsync(EntityDto<int> input)
        {
            var companyType = await Repository.FirstOrDefaultAsync(x =>
                x.Id == input.Id
                && x.Type == HostCatalogItemTypes.CompanyType
                && x.IsActive);

            if (companyType == null)
            {
                throw new UserFriendlyException("Company type not found.");
            }

            var children = await Repository.GetAll()
                .Where(x => x.CompanyTypeId == companyType.Id && x.IsActive)
                .OrderBy(x => x.Type)
                .ThenBy(x => x.Name)
                .ToListAsync();

            var result = new HostCatalogByCompanyTypeDto
            {
                CompanyType = ObjectMapper.Map<HostCatalogItemDto>(companyType)
            };

            foreach (var child in children)
            {
                var dto = ObjectMapper.Map<HostCatalogItemDto>(child);
                dto.CompanyTypeName = companyType.Name;
                if (string.Equals(child.Type, HostCatalogItemTypes.Category, StringComparison.OrdinalIgnoreCase))
                {
                    result.Categories.Add(dto);
                }
                else if (string.Equals(child.Type, HostCatalogItemTypes.Unit, StringComparison.OrdinalIgnoreCase))
                {
                    result.Units.Add(dto);
                }
                else if (string.Equals(child.Type, HostCatalogItemTypes.Brand, StringComparison.OrdinalIgnoreCase))
                {
                    result.Brands.Add(dto);
                }
            }

            return result;
        }

        protected override IQueryable<HostCatalogItem> CreateFilteredQuery(PagedHostCatalogItemResultRequestDto input)
        {
            var type = input.Type.IsNullOrWhiteSpace() ? null : input.Type.Trim();

            return Repository.GetAll()
                .WhereIf(!type.IsNullOrWhiteSpace(), x => x.Type == type)
                .WhereIf(input.CompanyTypeId.HasValue, x => x.CompanyTypeId == input.CompanyTypeId.Value)
                .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || x.Type.Contains(input.Keyword)
                         || (x.Symbol != null && x.Symbol.Contains(input.Keyword)));
        }

        protected override IQueryable<HostCatalogItem> ApplySorting(
            IQueryable<HostCatalogItem> query,
            PagedHostCatalogItemResultRequestDto input)
        {
            return query.OrderBy(x => x.Type).ThenBy(x => x.Name);
        }

        private async Task ValidateAsync(string type, int? companyTypeId, string name, int? excludeId)
        {
            if (!HostCatalogItemTypes.IsValid(type))
            {
                throw new UserFriendlyException("Invalid catalog item type.");
            }

            if (HostCatalogItemTypes.IsCompanyType(type))
            {
                if (companyTypeId.HasValue)
                {
                    throw new UserFriendlyException("Company type rows cannot have a parent company type.");
                }
            }
            else
            {
                if (!companyTypeId.HasValue || companyTypeId.Value <= 0)
                {
                    throw new UserFriendlyException("Company type is required for categories, units, and brands.");
                }

                var parent = await Repository.FirstOrDefaultAsync(x =>
                    x.Id == companyTypeId.Value && x.Type == HostCatalogItemTypes.CompanyType);
                if (parent == null)
                {
                    throw new UserFriendlyException("Parent company type not found.");
                }
            }

            if (string.Equals(type, HostCatalogItemTypes.Unit, StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(/* symbol optional */ name))
            {
                // name already required by DTO
            }

            var exists = await Repository.GetAll()
                .AnyAsync(x =>
                    x.Type == type
                    && x.CompanyTypeId == companyTypeId
                    && x.Name == name
                    && (!excludeId.HasValue || x.Id != excludeId.Value));

            if (exists)
            {
                throw new UserFriendlyException(
                    $"An item named \"{name}\" already exists for this type.");
            }
        }

        private static void Normalize(CreateHostCatalogItemDto input)
        {
            input.Type = input.Type?.Trim();
            input.Name = input.Name?.Trim();
            input.Symbol = input.Symbol?.Trim();
            if (HostCatalogItemTypes.IsCompanyType(input.Type))
            {
                input.CompanyTypeId = null;
                input.Symbol = null;
            }
            else if (!string.Equals(input.Type, HostCatalogItemTypes.Unit, StringComparison.OrdinalIgnoreCase))
            {
                input.Symbol = null;
            }
        }

        private static void Normalize(HostCatalogItemDto input)
        {
            input.Type = input.Type?.Trim();
            input.Name = input.Name?.Trim();
            input.Symbol = input.Symbol?.Trim();
            if (HostCatalogItemTypes.IsCompanyType(input.Type))
            {
                input.CompanyTypeId = null;
                input.Symbol = null;
            }
            else if (!string.Equals(input.Type, HostCatalogItemTypes.Unit, StringComparison.OrdinalIgnoreCase))
            {
                input.Symbol = null;
            }
        }

        private async Task<HostCatalogItemDto> FillCompanyTypeNameAsync(HostCatalogItemDto dto)
        {
            if (dto?.CompanyTypeId == null)
            {
                return dto;
            }

            var parent = await Repository.FirstOrDefaultAsync(dto.CompanyTypeId.Value);
            dto.CompanyTypeName = parent?.Name;
            return dto;
        }

        private async Task FillCompanyTypeNamesAsync(IReadOnlyList<HostCatalogItemDto> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            var ids = items.Where(x => x.CompanyTypeId.HasValue)
                .Select(x => x.CompanyTypeId.Value)
                .Distinct()
                .ToList();
            if (ids.Count == 0)
            {
                return;
            }

            var names = await Repository.GetAll()
                .Where(x => ids.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            foreach (var item in items)
            {
                if (item.CompanyTypeId.HasValue
                    && names.TryGetValue(item.CompanyTypeId.Value, out var name))
                {
                    item.CompanyTypeName = name;
                }
            }
        }
    }
}
