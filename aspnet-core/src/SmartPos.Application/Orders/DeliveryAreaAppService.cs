using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Runtime.Caching;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.Caching;
using SmartPos.Orders.Dto;

namespace SmartPos.Orders
{
    [AbpAuthorize(PermissionNames.Pages_DeliveryAreas)]
    public class DeliveryAreaAppService :
        AsyncCrudAppService<DeliveryArea, DeliveryAreaDto, int, PagedDeliveryAreaResultRequestDto, CreateDeliveryAreaDto, DeliveryAreaDto>,
        IDeliveryAreaAppService
    {
        private readonly IRepository<Branch> _branchRepository;
        private readonly IRepository<User, long> _userRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IBranchContext _branchContext;
        private readonly ICacheManager _cacheManager;
        private readonly ISmartPosReadCacheInvalidator _readCacheInvalidator;

        public DeliveryAreaAppService(
            IRepository<DeliveryArea> repository,
            IRepository<Branch> branchRepository,
            IRepository<User, long> userRepository,
            IBranchAccessChecker branchAccessChecker,
            IBranchContext branchContext,
            ICacheManager cacheManager,
            ISmartPosReadCacheInvalidator readCacheInvalidator)
            : base(repository)
        {
            _branchRepository = branchRepository;
            _userRepository = userRepository;
            _branchAccessChecker = branchAccessChecker;
            _branchContext = branchContext;
            _cacheManager = cacheManager;
            _readCacheInvalidator = readCacheInvalidator;

            CreatePermissionName = PermissionNames.Pages_DeliveryAreas;
            UpdatePermissionName = PermissionNames.Pages_DeliveryAreas;
            DeletePermissionName = PermissionNames.Pages_DeliveryAreas;
            GetPermissionName = PermissionNames.Pages_DeliveryAreas;
            GetAllPermissionName = PermissionNames.Pages_DeliveryAreas;
        }

        public override async Task<PagedResultDto<DeliveryAreaDto>> GetAllAsync(PagedDeliveryAreaResultRequestDto input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                return await base.GetAllAsync(input);
            }
        }

        public override async Task<DeliveryAreaDto> GetAsync(EntityDto<int> input)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                return await base.GetAsync(input);
            }
        }

        protected override IQueryable<DeliveryArea> CreateFilteredQuery(PagedDeliveryAreaResultRequestDto input)
        {
            var branchId = input.BranchId ?? BranchQueryHelper.ResolveBranchIdForFilter(_branchContext, _userRepository, AbpSession, PermissionChecker);

            return Repository.GetAllIncluding(x => x.Branch)
                .WhereIf(branchId.HasValue && branchId.Value > 0, x => x.BranchId == branchId.Value)
                .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive.Value)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x =>
                    (x.Name != null && x.Name.Contains(input.Keyword))
                    || (x.CoverageDetails != null && x.CoverageDetails.Contains(input.Keyword))
                    || (x.EstimatedDeliveryTime != null && x.EstimatedDeliveryTime.Contains(input.Keyword))
                    || (x.Branch != null && x.Branch.Name.Contains(input.Keyword)));
        }

        protected override async Task<DeliveryArea> GetEntityByIdAsync(int id)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                return await AsyncQueryableExecuter.FirstOrDefaultAsync(
                    Repository.GetAllIncluding(x => x.Branch)
                        .Where(x => x.Id == id));
            }
        }

        public override async Task<DeliveryAreaDto> CreateAsync(CreateDeliveryAreaDto input)
        {
            CheckCreatePermission();

            int targetBranchId;
            if (input.BranchId.HasValue && input.BranchId.Value > 0)
            {
                await _branchAccessChecker.EnsureCanAccessBranchAsync(input.BranchId.Value);
                targetBranchId = input.BranchId.Value;
            }
            else
            {
                targetBranchId = await _branchAccessChecker.RequireEffectiveBranchIdAsync();
            }

            Branch branch;
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                branch = await _branchRepository.FirstOrDefaultAsync(x => x.Id == targetBranchId);
            }

            if (branch == null)
            {
                throw new UserFriendlyException($"Branch with id {targetBranchId} was not found.");
            }

            var nameTrimmed = input.Name.Trim();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var exists = await Repository.GetAll()
                    .AnyAsync(x => x.BranchId == targetBranchId && x.Name.ToLower() == nameTrimmed.ToLower());

                if (exists)
                {
                    throw new UserFriendlyException($"A delivery area named '{nameTrimmed}' already exists for this branch.");
                }

                var entity = new DeliveryArea
                {
                    TenantId = branch.TenantId ?? AbpSession.TenantId,
                    BranchId = targetBranchId,
                    Name = nameTrimmed,
                    DeliveryFee = Math.Max(0, input.DeliveryFee),
                    MinimumOrderAmount = Math.Max(0, input.MinimumOrderAmount),
                    EstimatedDeliveryTime = input.EstimatedDeliveryTime?.Trim(),
                    CoverageDetails = input.CoverageDetails?.Trim(),
                    IsActive = input.IsActive
                };

                await Repository.InsertAsync(entity);
                await CurrentUnitOfWork.SaveChangesAsync();
                await _readCacheInvalidator.ClearDeliveryAsync();

                return await GetAsync(new EntityDto<int>(entity.Id));
            }
        }

        public override async Task<DeliveryAreaDto> UpdateAsync(DeliveryAreaDto input)
        {
            CheckUpdatePermission();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var entity = await Repository.GetAsync(input.Id);
                if (entity == null)
                {
                    throw new UserFriendlyException("Delivery area not found.");
                }

                await _branchAccessChecker.EnsureCanAccessBranchAsync(entity.BranchId);

                var nameTrimmed = input.Name.Trim();
                var duplicate = await Repository.GetAll()
                    .AnyAsync(x => x.BranchId == entity.BranchId && x.Id != entity.Id && x.Name.ToLower() == nameTrimmed.ToLower());

                if (duplicate)
                {
                    throw new UserFriendlyException($"Another delivery area named '{nameTrimmed}' already exists for this branch.");
                }

                entity.Name = nameTrimmed;
                entity.DeliveryFee = Math.Max(0, input.DeliveryFee);
                entity.MinimumOrderAmount = Math.Max(0, input.MinimumOrderAmount);
                entity.EstimatedDeliveryTime = input.EstimatedDeliveryTime?.Trim();
                entity.CoverageDetails = input.CoverageDetails?.Trim();
                entity.IsActive = input.IsActive;

                await Repository.UpdateAsync(entity);
                await CurrentUnitOfWork.SaveChangesAsync();
                await _readCacheInvalidator.ClearDeliveryAsync();

                return await GetAsync(new EntityDto<int>(entity.Id));
            }
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var entity = await Repository.GetAsync(input.Id);
                if (entity == null)
                {
                    throw new UserFriendlyException("Delivery area not found.");
                }

                await _branchAccessChecker.EnsureCanAccessBranchAsync(entity.BranchId);

                await Repository.DeleteAsync(entity);
                await _readCacheInvalidator.ClearDeliveryAsync();
            }
        }

        public async Task<DeliveryAreaDto> ToggleActiveAsync(EntityDto<int> input)
        {
            CheckUpdatePermission();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
            {
                var entity = await Repository.GetAsync(input.Id);
                if (entity == null)
                {
                    throw new UserFriendlyException("Delivery area not found.");
                }

                await _branchAccessChecker.EnsureCanAccessBranchAsync(entity.BranchId);

                entity.IsActive = !entity.IsActive;
                await Repository.UpdateAsync(entity);
                await CurrentUnitOfWork.SaveChangesAsync();
                await _readCacheInvalidator.ClearDeliveryAsync();

                return await GetAsync(new EntityDto<int>(entity.Id));
            }
        }

        [AbpAllowAnonymous]
        public async Task<List<DeliveryAreaDto>> GetActiveAreasForBranchAsync(int branchId)
        {
            var cacheKey = SmartPosAppCaches.BranchKey(branchId);
            var cache = SmartPosAppCaches.Typed<List<DeliveryAreaDto>>(
                _cacheManager,
                SmartPosAppCaches.DeliveryAreas,
                TimeSpan.FromMinutes(15));

            return await cache.GetAsync(cacheKey, async _ =>
            {
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant))
                {
                    var areas = await Repository.GetAllIncluding(x => x.Branch)
                        .AsNoTracking()
                        .Where(x => x.BranchId == branchId && x.IsActive)
                        .OrderBy(x => x.DeliveryFee)
                        .ThenBy(x => x.Name)
                        .ToListAsync();

                    return areas.Select(MapToEntityDto).ToList();
                }
            });
        }

        protected override DeliveryAreaDto MapToEntityDto(DeliveryArea entity)
        {
            return new DeliveryAreaDto
            {
                Id = entity.Id,
                BranchId = entity.BranchId,
                BranchName = entity.Branch?.Name,
                Name = entity.Name,
                DeliveryFee = entity.DeliveryFee,
                MinimumOrderAmount = entity.MinimumOrderAmount,
                EstimatedDeliveryTime = entity.EstimatedDeliveryTime,
                CoverageDetails = entity.CoverageDetails,
                IsActive = entity.IsActive
            };
        }
    }
}
