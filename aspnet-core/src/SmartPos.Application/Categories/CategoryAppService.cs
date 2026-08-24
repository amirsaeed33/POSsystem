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
using SmartPos.Categories.Dto;

namespace SmartPos.Categories
{
    [AbpAuthorize]
    public class CategoryAppService : AsyncCrudAppService<Category, CategoryDto, int, PagedCategoryResultRequestDto, CreateCategoryDto, CategoryDto>, ICategoryAppService
    {
        private const string CacheName = "CategoryLookupCache";
        private readonly IBranchContext _branchContext;
        private readonly IRepository<User, long> _userRepository;
        private readonly ICacheManager _cacheManager;

        public CategoryAppService(
            IRepository<Category> repository,
            IBranchContext branchContext,
            IRepository<User, long> userRepository,
            ICacheManager cacheManager)
            : base(repository)
        {
            _branchContext = branchContext;
            _userRepository = userRepository;
            _cacheManager = cacheManager;
            CreatePermissionName = PermissionNames.Pages_Categories;
            UpdatePermissionName = PermissionNames.Pages_Categories;
            DeletePermissionName = PermissionNames.Pages_Categories;
            GetPermissionName = PermissionNames.Pages_Categories;
            GetAllPermissionName = PermissionNames.Pages_Categories;
        }

        public override async Task<CategoryDto> CreateAsync(CreateCategoryDto input)
        {
            CheckCreatePermission();
            var branchId = RequireCurrentBranchId();
            var entity = ObjectMapper.Map<Category>(input);
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

        public override async Task<CategoryDto> UpdateAsync(CategoryDto input)
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

        [AbpAuthorize(PermissionNames.Pages_Categories, PermissionNames.Pages_Products)]
        public async Task<ListResultDto<CategoryDto>> GetLookupAsync()
        {
            var branchId = ResolveBranchId();
            var tenantId = AbpSession.TenantId ?? 0;
            var cacheKey = $"Tenant_{tenantId}_Branch_{branchId?.ToString() ?? "All"}";

            var cache = _cacheManager.GetCache<string, ListResultDto<CategoryDto>>(CacheName);
            return await cache.GetAsync(cacheKey, async (key) =>
            {
                var query = Repository.GetAll().Where(x => x.IsActive);
                if (branchId.HasValue)
                {
                    query = query.Where(x => x.BranchId == branchId.Value);
                }

                var items = await query.OrderBy(x => x.Name).ToListAsync();
                return new ListResultDto<CategoryDto>(ObjectMapper.Map<List<CategoryDto>>(items));
            });
        }

        private async Task ClearLookupCacheAsync()
        {
            await _cacheManager.GetCache(CacheName).ClearAsync();
        }

        protected override IQueryable<Category> CreateFilteredQuery(PagedCategoryResultRequestDto input)
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
                throw new UserFriendlyException("Select a branch before managing categories.");
            }

            return branchId.Value;
        }
    }
}
