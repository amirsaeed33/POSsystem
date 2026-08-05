using System;
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
using SmartPos.Lookups.Dto;

namespace SmartPos.Lookups
{
    [AbpAuthorize(PermissionNames.Pages_LookUps)]
    public class LookUpAppService :
        AsyncCrudAppService<LookUp, LookUpDto, int, PagedLookUpResultRequestDto, CreateLookUpDto, LookUpDto>,
        ILookUpAppService
    {
        public LookUpAppService(IRepository<LookUp> repository)
            : base(repository)
        {
        }

        public override async Task<LookUpDto> CreateAsync(CreateLookUpDto input)
        {
            Normalize(input);
            input.Type = await EnsureValidTypeAsync(input.Type);
            await EnsureUniqueNameAsync(input.Type, input.Name, null);
            return await base.CreateAsync(input);
        }

        public override async Task<LookUpDto> UpdateAsync(LookUpDto input)
        {
            Normalize(input);
            input.Type = await EnsureValidTypeAsync(input.Type);
            await EnsureUniqueNameAsync(input.Type, input.Name, input.Id);
            return await base.UpdateAsync(input);
        }

        /// <summary>Active lookups for a type — available to any authenticated user for dropdowns.</summary>
        [AbpAuthorize]
        public async Task<ListResultDto<LookUpDto>> GetByTypeAsync(string type)
        {
            if (type.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException("Lookup type is required.");
            }

            var normalizedType = type.Trim();
            var items = await Repository.GetAll()
                .Where(x => x.Type == normalizedType && x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.DisplayName)
                .ToListAsync();

            return new ListResultDto<LookUpDto>(items.Select(MapToEntityDto).ToList());
        }

        protected override IQueryable<LookUp> CreateFilteredQuery(PagedLookUpResultRequestDto input)
        {
            var type = input.Type.IsNullOrWhiteSpace() ? null : input.Type.Trim();

            return Repository.GetAll()
                .WhereIf(!type.IsNullOrWhiteSpace(), x => x.Type == type)
                .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || x.DisplayName.Contains(input.Keyword)
                         || x.Type.Contains(input.Keyword));
        }

        protected override IQueryable<LookUp> ApplySorting(IQueryable<LookUp> query, PagedLookUpResultRequestDto input)
        {
            return query.OrderBy(x => x.Type).ThenBy(x => x.SortOrder).ThenBy(x => x.DisplayName);
        }

        private async Task EnsureUniqueNameAsync(string type, string name, int? excludeId)
        {
            var exists = await Repository.GetAll()
                .AnyAsync(x =>
                    x.Type == type
                    && x.Name == name
                    && (!excludeId.HasValue || x.Id != excludeId.Value));

            if (exists)
            {
                throw new UserFriendlyException(
                    $"A lookup with name \"{name}\" already exists for type \"{type}\".");
            }
        }

        /// <summary>
        /// Type must exist as an active LookUpType catalog entry.
        /// Bootstrap: Type = LookUpType is always allowed so new types can be registered.
        /// </summary>
        private async Task<string> EnsureValidTypeAsync(string type)
        {
            var value = type?.Trim();
            if (value.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException("Lookup type is required.");
            }

            if (string.Equals(value, LookUpTypes.LookUpType, StringComparison.OrdinalIgnoreCase))
            {
                return LookUpTypes.LookUpType;
            }

            var lookUp = await Repository.FirstOrDefaultAsync(x =>
                x.Type == LookUpTypes.LookUpType
                && x.Name == value
                && x.IsActive);

            if (lookUp == null)
            {
                var catalog = await Repository.GetAll()
                    .Where(x => x.Type == LookUpTypes.LookUpType && x.IsActive)
                    .ToListAsync();
                lookUp = catalog.FirstOrDefault(x =>
                    string.Equals(x.Name, value, StringComparison.OrdinalIgnoreCase));
            }

            if (lookUp == null)
            {
                throw new UserFriendlyException(
                    $"Invalid lookup type \"{value}\". Add it under Lookups (Lookup Type) first.");
            }

            return lookUp.Name;
        }

        private static void Normalize(CreateLookUpDto input)
        {
            input.Type = input.Type?.Trim();
            input.Name = input.Name?.Trim();
            input.DisplayName = input.DisplayName?.Trim();
        }

        private static void Normalize(LookUpDto input)
        {
            input.Type = input.Type?.Trim();
            input.Name = input.Name?.Trim();
            input.DisplayName = input.DisplayName?.Trim();
        }
    }
}
