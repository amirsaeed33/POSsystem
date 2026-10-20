using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Authorization.Users;
using SmartPos.Branches;
using SmartPos.GroceryPurchases.Dto;
using SmartPos.HostCatalog;

namespace SmartPos.GroceryPurchases
{
    [AbpAuthorize(PermissionNames.Pages_GroceryPurchases, PermissionNames.Pages_Purchases)]
    public class GroceryPurchaseAppService : AsyncCrudAppService<GroceryPurchase, GroceryPurchaseDto, int, PagedGroceryPurchaseResultRequestDto, CreateGroceryPurchaseDto, UpdateGroceryPurchaseDto>, IGroceryPurchaseAppService
    {
        private readonly IRepository<Branch> _branchRepository;
        private readonly IRepository<BranchSeedRequest> _branchSeedRequestRepository;
        private readonly IBranchAccessChecker _branchAccessChecker;
        private readonly IRepository<User, long> _userRepository;

        public GroceryPurchaseAppService(
            IRepository<GroceryPurchase> repository,
            IRepository<Branch> branchRepository,
            IRepository<BranchSeedRequest> branchSeedRequestRepository,
            IBranchAccessChecker branchAccessChecker,
            IRepository<User, long> userRepository)
            : base(repository)
        {
            _branchRepository = branchRepository;
            _branchSeedRequestRepository = branchSeedRequestRepository;
            _branchAccessChecker = branchAccessChecker;
            _userRepository = userRepository;

            CreatePermissionName = PermissionNames.Pages_GroceryPurchases_Create;
            UpdatePermissionName = PermissionNames.Pages_GroceryPurchases_Edit;
            DeletePermissionName = PermissionNames.Pages_GroceryPurchases_Delete;
            GetPermissionName = PermissionNames.Pages_GroceryPurchases;
            GetAllPermissionName = PermissionNames.Pages_GroceryPurchases;
        }

        protected override void CheckPermission(string permissionName)
        {
            if (string.IsNullOrEmpty(permissionName))
            {
                return;
            }

            if (PermissionChecker.IsGranted(permissionName))
            {
                return;
            }

            if (permissionName == PermissionNames.Pages_GroceryPurchases && PermissionChecker.IsGranted(PermissionNames.Pages_Purchases))
            {
                return;
            }
            if (permissionName == PermissionNames.Pages_GroceryPurchases_Create && PermissionChecker.IsGranted(PermissionNames.Pages_Purchases_Create))
            {
                return;
            }
            if (permissionName == PermissionNames.Pages_GroceryPurchases_Edit && PermissionChecker.IsGranted(PermissionNames.Pages_Purchases_Edit))
            {
                return;
            }
            if (permissionName == PermissionNames.Pages_GroceryPurchases_Delete && PermissionChecker.IsGranted(PermissionNames.Pages_Purchases_Delete))
            {
                return;
            }

            throw new AbpAuthorizationException(L("AllOfThesePermissionsMustBeGranted", permissionName));
        }

        public override async Task<GroceryPurchaseDto> CreateAsync(CreateGroceryPurchaseDto input)
        {
            CheckCreatePermission();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var effectiveBranchId = await _branchAccessChecker.GetEffectiveBranchIdAsync();
                var branchId = input.BranchId ?? effectiveBranchId;

                if (!branchId.HasValue)
                {
                    throw new UserFriendlyException("Please select a branch to record grocery purchases.");
                }

                var branch = await _branchRepository.FirstOrDefaultAsync(branchId.Value);
                if (branch == null)
                {
                    throw new UserFriendlyException("Selected branch does not exist.");
                }

                var isRestaurant = await IsRestaurantBranchAsync(branch);
                if (!isRestaurant)
                {
                    throw new UserFriendlyException("Grocery purchases are only available for Restaurant branches.");
                }

                var entity = ObjectMapper.Map<GroceryPurchase>(input);
                entity.BranchId = branchId.Value;
                entity.TenantId = branch.TenantId;
                entity.ProductName = input.ProductName?.Trim();
                entity.Unit = input.Unit?.Trim();

                await Repository.InsertAsync(entity);
                await CurrentUnitOfWork.SaveChangesAsync();

                var dto = MapToEntityDto(entity);
                dto.BranchName = branch.Name;
                return dto;
            }
        }

        public override async Task<GroceryPurchaseDto> UpdateAsync(UpdateGroceryPurchaseDto input)
        {
            CheckUpdatePermission();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var entity = await Repository.GetAllIncluding(x => x.Branch).FirstOrDefaultAsync(x => x.Id == input.Id);
                if (entity == null)
                {
                    throw new UserFriendlyException("Grocery purchase not found.");
                }

                var effectiveBranchId = await _branchAccessChecker.GetEffectiveBranchIdAsync();
                if (effectiveBranchId.HasValue && entity.BranchId != effectiveBranchId.Value)
                {
                    throw new UserFriendlyException("You do not have permission to modify grocery purchases from another location.");
                }

                ObjectMapper.Map(input, entity);
                entity.ProductName = input.ProductName?.Trim();
                entity.Unit = input.Unit?.Trim();

                await Repository.UpdateAsync(entity);
                await CurrentUnitOfWork.SaveChangesAsync();

                return MapToEntityDto(entity);
            }
        }

        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var entity = await Repository.FirstOrDefaultAsync(input.Id);
                if (entity == null)
                {
                    return;
                }

                var effectiveBranchId = await _branchAccessChecker.GetEffectiveBranchIdAsync();
                if (effectiveBranchId.HasValue && entity.BranchId != effectiveBranchId.Value)
                {
                    throw new UserFriendlyException("You do not have permission to delete grocery purchases from another location.");
                }

                await Repository.DeleteAsync(entity);
                await CurrentUnitOfWork.SaveChangesAsync();
            }
        }

        public override async Task<GroceryPurchaseDto> GetAsync(EntityDto<int> input)
        {
            CheckGetPermission();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var entity = await Repository.GetAllIncluding(x => x.Branch).FirstOrDefaultAsync(x => x.Id == input.Id);
                if (entity == null)
                {
                    throw new UserFriendlyException("Grocery purchase not found.");
                }

                return MapToEntityDto(entity);
            }
        }

        public override async Task<PagedResultDto<GroceryPurchaseDto>> GetAllAsync(PagedGroceryPurchaseResultRequestDto input)
        {
            CheckGetAllPermission();

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var query = CreateFilteredQuery(input);
                var totalCount = await AsyncQueryableExecuter.CountAsync(query);

                query = ApplySorting(query, input);
                query = ApplyPaging(query, input);

                var entities = await AsyncQueryableExecuter.ToListAsync(query);
                var dtos = entities.Select(MapToEntityDto).ToList();

                var creatorUserIds = entities.Where(x => x.CreatorUserId.HasValue).Select(x => x.CreatorUserId.Value).Distinct().ToList();
                if (creatorUserIds.Count > 0)
                {
                    var userNames = await _userRepository.GetAll()
                        .Where(u => creatorUserIds.Contains(u.Id))
                        .Select(u => new { u.Id, u.Name, u.Surname, u.UserName })
                        .ToDictionaryAsync(u => u.Id, u => string.IsNullOrWhiteSpace(u.Name) ? u.UserName : $"{u.Name} {u.Surname}".Trim());

                    foreach (var dto in dtos)
                    {
                        var entity = entities.FirstOrDefault(e => e.Id == dto.Id);
                        if (entity?.CreatorUserId.HasValue == true && userNames.TryGetValue(entity.CreatorUserId.Value, out var name))
                        {
                            dto.CreatedByName = name;
                        }
                    }
                }

                return new PagedResultDto<GroceryPurchaseDto>(totalCount, dtos);
            }
        }

        protected override IQueryable<GroceryPurchase> CreateFilteredQuery(PagedGroceryPurchaseResultRequestDto input)
        {
            var query = Repository.GetAllIncluding(x => x.Branch);

            if (input.BranchId.HasValue)
            {
                query = query.Where(x => x.BranchId == input.BranchId.Value);
            }
            else
            {
                var effectiveBranchId = _branchAccessChecker.GetEffectiveBranchIdAsync().GetAwaiter().GetResult();
                if (effectiveBranchId.HasValue)
                {
                    query = query.Where(x => x.BranchId == effectiveBranchId.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(input.Keyword))
            {
                var kw = input.Keyword.Trim();
                query = query.Where(x => x.ProductName.Contains(kw) || x.Unit.Contains(kw));
            }

            if (input.StartDate.HasValue)
            {
                query = query.Where(x => x.PurchaseDate >= input.StartDate.Value);
            }

            if (input.EndDate.HasValue)
            {
                query = query.Where(x => x.PurchaseDate <= input.EndDate.Value);
            }

            return query;
        }

        protected override IQueryable<GroceryPurchase> ApplySorting(IQueryable<GroceryPurchase> query, PagedGroceryPurchaseResultRequestDto input)
        {
            return query.OrderByDescending(x => x.PurchaseDate).ThenByDescending(x => x.Id);
        }

        private static bool IsRestaurantTypeName(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return false;
            var t = typeName.Trim().ToLowerInvariant();
            return t == "restaurant" || t == "resturent" || t.Contains("restaurant") || t.Contains("resturent");
        }

        private async Task<bool> IsRestaurantBranchAsync(Branch branch)
        {
            if (branch == null) return false;

            if (!string.IsNullOrWhiteSpace(branch.BranchType))
            {
                return IsRestaurantTypeName(branch.BranchType);
            }

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var reqType = await _branchSeedRequestRepository.GetAll()
                    .Where(x => x.BranchId == branch.Id)
                    .Select(x => x.CompanyType.Name)
                    .FirstOrDefaultAsync();

                if (IsRestaurantTypeName(reqType))
                {
                    branch.BranchType = "Restaurant";
                    await _branchRepository.UpdateAsync(branch);
                    return true;
                }
            }

            return false;
        }
    }
}
