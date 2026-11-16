using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Runtime.Caching;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Departments.Dto;
using SmartPos.Validation;

namespace SmartPos.Departments
{
    [AbpAuthorize]
    public class DepartmentAppService : AsyncCrudAppService<Department, DepartmentDto, int, PagedDepartmentResultRequestDto, CreateDepartmentDto, DepartmentDto>, IDepartmentAppService
    {
        private const string CacheName = "DepartmentLookupCache";
        private readonly IBranchContext _branchContext;
        private readonly IRepository<User, long> _userRepository;
        private readonly ICacheManager _cacheManager;

        public DepartmentAppService(
            IRepository<Department> repository,
            IBranchContext branchContext,
            IRepository<User, long> userRepository,
            ICacheManager cacheManager)
            : base(repository)
        {
            _branchContext = branchContext;
            _userRepository = userRepository;
            _cacheManager = cacheManager;
            CreatePermissionName = PermissionNames.Pages_Departments_Create;
            UpdatePermissionName = PermissionNames.Pages_Departments_Edit;
            DeletePermissionName = PermissionNames.Pages_Departments_Delete;
            GetPermissionName = PermissionNames.Pages_Departments;
            GetAllPermissionName = PermissionNames.Pages_Departments;
        }

        public override async Task<DepartmentDto> CreateAsync(CreateDepartmentDto input)
        {
            CheckCreatePermission();
            var branchId = RequireCurrentBranchId();
            var name = EntityNameValidation.EnsureValid(input.Name, "Department");

            var existing = await Repository.FirstOrDefaultAsync(d =>
                d.BranchId == branchId &&
                d.Name.ToLower() == name.ToLower());
            if (existing != null)
            {
                throw new UserFriendlyException($"Department '{name}' already exists in this location.");
            }

            var entity = ObjectMapper.Map<Department>(input);
            entity.Name = name;
            entity.TenantId = AbpSession.TenantId;
            entity.BranchId = branchId;
            entity.IsActive = input.IsActive;
            if (string.IsNullOrWhiteSpace(entity.Description))
            {
                entity.Description = name;
            }
            await Repository.InsertAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();
            await ClearLookupCacheAsync();
            return MapToEntityDto(entity);
        }

        public override async Task<DepartmentDto> UpdateAsync(DepartmentDto input)
        {
            CheckUpdatePermission();
            var entity = await Repository.GetAsync(input.Id);
            if (entity.BranchId <= 0)
            {
                entity.BranchId = RequireCurrentBranchId();
            }

            var name = EntityNameValidation.EnsureValid(input.Name, "Department");

            var existing = await Repository.FirstOrDefaultAsync(d =>
                d.Id != input.Id &&
                d.BranchId == entity.BranchId &&
                d.Name.ToLower() == name.ToLower());
            if (existing != null)
            {
                throw new UserFriendlyException($"Department '{name}' already exists in this location.");
            }

            ObjectMapper.Map(input, entity);
            entity.Name = name;
            if (entity.BranchId <= 0)
            {
                entity.BranchId = RequireCurrentBranchId();
            }
            await Repository.UpdateAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();
            await ClearLookupCacheAsync();
            return MapToEntityDto(entity);
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();
            await base.DeleteAsync(input);
            await ClearLookupCacheAsync();
        }

        [AbpAuthorize(PermissionNames.Pages_Departments, PermissionNames.Pages_Categories, PermissionNames.Pages_Products)]
        public async Task<ListResultDto<DepartmentDto>> GetLookupAsync()
        {
            var branchId = ResolveBranchId();
            var tenantId = AbpSession.TenantId ?? 0;
            var cacheKey = $"Tenant_{tenantId}_Branch_{branchId?.ToString() ?? "All"}";

            var cache = _cacheManager.GetCache<string, ListResultDto<DepartmentDto>>(CacheName);
            return await cache.GetAsync(cacheKey, async (key) =>
            {
                var query = Repository.GetAll().Where(x => x.IsActive);
                List<Department> items = new List<Department>();

                if (branchId.HasValue)
                {
                    items = await query.Where(x => x.BranchId == branchId.Value).OrderBy(x => x.Name).ToListAsync();
                }

                if (items.Count == 0)
                {
                    items = await query.OrderBy(x => x.Name).ToListAsync();
                }

                if (items.Count == 0 && AbpSession.TenantId == null)
                {
                    using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
                    {
                        items = await Repository.GetAll()
                            .Where(x => x.IsActive)
                            .OrderBy(x => x.Name)
                            .ToListAsync();
                    }
                }

                var distinctItems = items
                    .GroupBy(x => x.Name.Trim(), System.StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .OrderBy(x => x.Name)
                    .ToList();

                return new ListResultDto<DepartmentDto>(ObjectMapper.Map<List<DepartmentDto>>(distinctItems));
            });
        }

        private async Task ClearLookupCacheAsync()
        {
            await _cacheManager.GetCache(CacheName).ClearAsync();
        }

        protected override IQueryable<Department> CreateFilteredQuery(PagedDepartmentResultRequestDto input)
        {
            var branchId = ResolveBranchId();
            return Repository.GetAll()
                .WhereIf(branchId.HasValue, x => x.BranchId == branchId.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || (x.Description != null && x.Description.Contains(input.Keyword)));
        }

        private int? ResolveBranchId()
        {
            return BranchQueryHelper.ResolveBranchIdForFilter(
                _branchContext,
                _userRepository,
                AbpSession,
                PermissionChecker);
        }

        private int RequireCurrentBranchId()
        {
            var branchId = ResolveBranchId();
            if (!branchId.HasValue)
            {
                throw new UserFriendlyException("Select a branch before managing departments.");
            }

            return branchId.Value;
        }
    }
}

