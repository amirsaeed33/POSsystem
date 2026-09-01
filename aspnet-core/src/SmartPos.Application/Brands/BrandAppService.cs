using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Runtime.Caching;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Brands.Dto;

namespace SmartPos.Brands
{
    [AbpAuthorize]
    public class BrandAppService : AsyncCrudAppService<Brand, BrandDto, int, PagedBrandResultRequestDto, CreateBrandDto, BrandDto>, IBrandAppService
    {
        private const string CacheName = "BrandLookupCache";
        private readonly IBranchContext _branchContext;
        private readonly IRepository<User, long> _userRepository;
        private readonly ICacheManager _cacheManager;

        public BrandAppService(
            IRepository<Brand> repository,
            IBranchContext branchContext,
            IRepository<User, long> userRepository,
            ICacheManager cacheManager)
            : base(repository)
        {
            _branchContext = branchContext;
            _userRepository = userRepository;
            _cacheManager = cacheManager;
            CreatePermissionName = PermissionNames.Pages_Brands_Create;
            UpdatePermissionName = PermissionNames.Pages_Brands_Edit;
            DeletePermissionName = PermissionNames.Pages_Brands_Delete;
            GetPermissionName = PermissionNames.Pages_Brands;
            GetAllPermissionName = PermissionNames.Pages_Brands;
        }

        public override async Task<BrandDto> CreateAsync(CreateBrandDto input)
        {
            CheckCreatePermission();
            var branchId = RequireCurrentBranchId();
            var entity = ObjectMapper.Map<Brand>(input);
            entity.TenantId = AbpSession.TenantId;
            entity.BranchId = branchId;
            entity.IsActive = input.IsActive;
            if (string.IsNullOrWhiteSpace(entity.Description))
            {
                entity.Description = input.Name;
            }
            await Repository.InsertAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();
            await ClearLookupCacheAsync();
            return MapToEntityDto(entity);
        }

        public override async Task<BrandDto> UpdateAsync(BrandDto input)
        {
            CheckUpdatePermission();
            var entity = await Repository.GetAsync(input.Id);
            if (entity.BranchId <= 0)
            {
                entity.BranchId = RequireCurrentBranchId();
            }
            ObjectMapper.Map(input, entity);
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

        [AbpAuthorize(PermissionNames.Pages_Brands, PermissionNames.Pages_Products)]
        public async Task<ListResultDto<BrandDto>> GetLookupAsync()
        {
            var branchId = ResolveBranchId();
            var tenantId = AbpSession.TenantId ?? 0;
            var cacheKey = $"Tenant_{tenantId}_Branch_{branchId?.ToString() ?? "All"}";

            var cache = _cacheManager.GetCache<string, ListResultDto<BrandDto>>(CacheName);
            return await cache.GetAsync(cacheKey, async (key) =>
            {
                var query = Repository.GetAll().Where(x => x.IsActive);
                if (branchId.HasValue)
                {
                    query = query.Where(x => x.BranchId == branchId.Value);
                }

                var items = await query.OrderBy(x => x.Name).ToListAsync();
                return new ListResultDto<BrandDto>(ObjectMapper.Map<List<BrandDto>>(items));
            });
        }

        private async Task ClearLookupCacheAsync()
        {
            await _cacheManager.GetCache(CacheName).ClearAsync();
        }

        protected override IQueryable<Brand> CreateFilteredQuery(PagedBrandResultRequestDto input)
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
                throw new UserFriendlyException("Select a branch before managing brands.");
            }

            return branchId.Value;
        }
    }
}
