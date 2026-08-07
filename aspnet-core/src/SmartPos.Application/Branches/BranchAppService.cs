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
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Authorization.Users;
using SmartPos.Branches.Dto;
using SmartPos.Inventory;
using SmartPos.Lookups;
using SmartPos.MultiTenancy;
using SmartPos.Products;

namespace SmartPos.Branches
{
    [AbpAuthorize]
    public class BranchAppService : AsyncCrudAppService<Branch, BranchDto, int, PagedBranchResultRequestDto, CreateBranchDto, BranchDto>, IBranchAppService
    {
        public UserManager UserManager { get; set; }

        private readonly IRepository<Product> _productRepository;
        private readonly IBranchStockManager _branchStockManager;
        private readonly IRepository<Tenant> _tenantRepository;
        private readonly IRepository<LookUp> _lookUpRepository;
        private readonly BranchStatusLookup _branchStatusLookup;

        public BranchAppService(
            IRepository<Branch> repository,
            IRepository<Product> productRepository,
            IBranchStockManager branchStockManager,
            IRepository<Tenant> tenantRepository,
            IRepository<LookUp> lookUpRepository,
            BranchStatusLookup branchStatusLookup)
            : base(repository)
        {
            _productRepository = productRepository;
            _branchStockManager = branchStockManager;
            _tenantRepository = tenantRepository;
            _lookUpRepository = lookUpRepository;
            _branchStatusLookup = branchStatusLookup;
            CreatePermissionName = PermissionNames.Pages_Branches;
            UpdatePermissionName = PermissionNames.Pages_Branches;
            DeletePermissionName = PermissionNames.Pages_Branches;
            GetPermissionName = PermissionNames.Pages_Branches;
            GetAllPermissionName = PermissionNames.Pages_Branches;
        }

        [AbpAuthorize(PermissionNames.Pages_Branches)]
        public override async Task<BranchDto> CreateAsync(CreateBranchDto input)
        {
            CheckCreatePermission();

            if (!AbpSession.TenantId.HasValue)
            {
                throw new UserFriendlyException(
                    "Host administrators cannot create locations. Each business creates its own locations.");
            }

            var branch = ObjectMapper.Map<Branch>(input);
            branch.TenantId = AbpSession.TenantId;
            // New branches always start as Pending until host admin approves.
            branch.StatusId = await _branchStatusLookup.GetIdAsync(BranchStatuses.Pending);
            branch.ImagePath = BranchImageStore.SaveBase64Image(input.ImageBase64);

            await Repository.InsertAsync(branch);
            await CurrentUnitOfWork.SaveChangesAsync();

            await SeedSharedProductsAsync(branch.Id);

            return await GetAsync(new EntityDto<int>(branch.Id));
        }

        [AbpAuthorize(PermissionNames.Pages_Branches)]
        public override async Task<BranchDto> UpdateAsync(BranchDto input)
        {
            CheckUpdatePermission();

            var branch = await GetEntityByIdAsync(input.Id);

            branch.Name = input.Name;
            branch.Code = input.Code;
            branch.IsActive = input.IsActive;
            branch.IsDefault = input.IsDefault;
            branch.InvoiceAddress = input.InvoiceAddress;
            branch.InvoiceContactEmail = input.InvoiceContactEmail;
            branch.InvoiceContactPhone = input.InvoiceContactPhone;
            branch.TaxNumber = input.TaxNumber;
            branch.Website = input.Website;
            branch.InvoiceFooter = input.InvoiceFooter;

            // Only host admin with approve permission may change StatusId.
            if (AbpSession.TenantId == null
                && input.StatusId > 0
                && input.StatusId != branch.StatusId
                && await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Branches_Approve))
            {
                await _branchStatusLookup.EnsureValidStatusIdAsync(input.StatusId);
                branch.StatusId = input.StatusId;
            }

            if (BranchImageStore.IsNewImagePayload(input.ImageBase64))
            {
                BranchImageStore.DeleteIfExists(branch.ImagePath);
                branch.ImagePath = BranchImageStore.SaveBase64Image(input.ImageBase64);
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            return await GetAsync(new EntityDto<int>(branch.Id));
        }

        [AbpAuthorize(PermissionNames.Pages_Branches)]
        public override async Task DeleteAsync(EntityDto<int> input)
        {
            CheckDeletePermission();

            var branch = await GetEntityByIdAsync(input.Id);
            BranchImageStore.DeleteIfExists(branch.ImagePath);
            await Repository.DeleteAsync(branch);
        }

        [AbpAuthorize(PermissionNames.Pages_Branches)]
        public override async Task<BranchDto> GetAsync(EntityDto<int> input)
        {
            BranchDto dto;
            if (AbpSession.TenantId == null)
            {
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
                {
                    var entity = await Repository.GetAsync(input.Id);
                    dto = MapToEntityDto(entity);
                }
            }
            else
            {
                dto = await base.GetAsync(input);
            }

            await FillStatusNamesAsync(new[] { dto });
            return dto;
        }

        protected override async Task<Branch> GetEntityByIdAsync(int id)
        {
            if (AbpSession.TenantId == null)
            {
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
                {
                    return await Repository.GetAsync(id);
                }
            }

            return await base.GetEntityByIdAsync(id);
        }

        /// <summary>
        /// Host admin sees every business location; tenant users see only their own.
        /// </summary>
        [AbpAuthorize(PermissionNames.Pages_Branches)]
        public override async Task<PagedResultDto<BranchDto>> GetAllAsync(PagedBranchResultRequestDto input)
        {
            CheckGetAllPermission();

            PagedResultDto<BranchDto> result;
            if (await CanBrowseAllLocationsAsync())
            {
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
                {
                    var query = CreateFilteredQuery(input).Where(x => x.TenantId != null);
                    var totalCount = await AsyncQueryableExecuter.CountAsync(query);
                    query = ApplySorting(query, input);
                    query = ApplyPaging(query, input);
                    var entities = await AsyncQueryableExecuter.ToListAsync(query);
                    result = new PagedResultDto<BranchDto>(
                        totalCount,
                        entities.Select(MapToEntityDto).ToList());
                    await FillTenancyNamesAsync(result.Items);
                }
            }
            else if (AbpSession.TenantId.HasValue)
            {
                result = await base.GetAllAsync(input);
            }
            else
            {
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
                {
                    result = await base.GetAllAsync(input);
                    await FillTenancyNamesAsync(result.Items);
                }
            }

            await FillStatusNamesAsync(result.Items);
            return result;
        }

        public async Task<ListResultDto<BranchDto>> GetLookupAsync()
        {
            // Host admin: every business location across tenants.
            if (await CanBrowseAllLocationsAsync())
            {
                using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
                {
                    var allLocations = await Repository.GetAll()
                        .Where(x => x.IsActive && x.TenantId != null)
                        .OrderBy(x => x.Name)
                        .ToListAsync();
                    var allDtos = ObjectMapper.Map<List<BranchDto>>(allLocations);
                    for (var i = 0; i < allLocations.Count; i++)
                    {
                        allDtos[i].TenantId = allLocations[i].TenantId;
                    }
                    await FillTenancyNamesAsync(allDtos);
                    await FillStatusNamesAsync(allDtos);
                    return new ListResultDto<BranchDto>(allDtos);
                }
            }

            IQueryable<Branch> query = Repository.GetAll().Where(x => x.IsActive);

            if (!await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Branches))
            {
                if (!AbpSession.UserId.HasValue)
                {
                    return new ListResultDto<BranchDto>(new List<BranchDto>());
                }

                var currentUser = await UserManager.GetUserByIdAsync(AbpSession.UserId.Value);
                if (!currentUser.BranchId.HasValue)
                {
                    return new ListResultDto<BranchDto>(new List<BranchDto>());
                }

                query = query.Where(x => x.Id == currentUser.BranchId.Value);
            }

            var branches = await query.OrderBy(x => x.Name).ToListAsync();
            var dtos = ObjectMapper.Map<List<BranchDto>>(branches);
            for (var i = 0; i < branches.Count; i++)
            {
                dtos[i].TenantId = branches[i].TenantId;
            }
            await FillStatusNamesAsync(dtos);
            return new ListResultDto<BranchDto>(dtos);
        }

        public async Task<BranchDto> GetInvoiceInfoAsync()
        {
            Branch branch = null;

            if (AbpSession.UserId.HasValue)
            {
                var currentUser = await UserManager.FindByIdAsync(AbpSession.UserId.Value.ToString());
                if (currentUser?.BranchId != null)
                {
                    branch = await Repository.FirstOrDefaultAsync(currentUser.BranchId.Value);
                }
            }

            if (branch == null)
            {
                branch = await Repository.GetAll()
                    .OrderByDescending(x => x.IsDefault)
                    .ThenBy(x => x.Id)
                    .FirstOrDefaultAsync();
            }

            if (branch == null)
            {
                return null;
            }

            var dto = ObjectMapper.Map<BranchDto>(branch);
            await FillStatusNamesAsync(new[] { dto });
            return dto;
        }

        [AbpAuthorize(PermissionNames.Pages_Branches_Approve)]
        public async Task<ListResultDto<BranchDto>> GetPendingApprovalsAsync()
        {
            var pendingStatusId = await _branchStatusLookup.GetIdAsync(BranchStatuses.Pending);

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var branches = await Repository.GetAll()
                    .Where(x => x.TenantId != null && x.StatusId == pendingStatusId)
                    .OrderByDescending(x => x.CreationTime)
                    .ToListAsync();

                var dtos = ObjectMapper.Map<List<BranchDto>>(branches);
                await FillTenancyNamesAsync(dtos);
                await FillStatusNamesAsync(dtos);
                return new ListResultDto<BranchDto>(dtos);
            }
        }

        [AbpAuthorize(PermissionNames.Pages_Branches_Approve)]
        public async Task<BranchDto> ChangeStatusAsync(ChangeBranchStatusDto input)
        {
            // Host may set Pending / Approved / Rejected at any time (including after a prior approval).
            var status = await _branchStatusLookup.GetAsync(input.StatusId);

            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var branch = await Repository.GetAll()
                    .FirstOrDefaultAsync(x => x.Id == input.Id);

                if (branch == null)
                {
                    throw new UserFriendlyException("Branch not found.");
                }

                if (!branch.TenantId.HasValue)
                {
                    throw new UserFriendlyException("Host branches do not require approval.");
                }

                branch.StatusId = status.Id;
                await CurrentUnitOfWork.SaveChangesAsync();

                var dto = ObjectMapper.Map<BranchDto>(branch);
                var tenant = await _tenantRepository.FirstOrDefaultAsync(branch.TenantId.Value);
                dto.TenancyName = tenant?.TenancyName;
                dto.Status = status.Name;
                dto.StatusDisplayName = status.DisplayName;
                return dto;
            }
        }

        protected override IQueryable<Branch> CreateFilteredQuery(PagedBranchResultRequestDto input)
        {
            return Repository.GetAll()
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(),
                    x => x.Name.Contains(input.Keyword)
                         || x.Code.Contains(input.Keyword)
                         || (x.InvoiceAddress != null && x.InvoiceAddress.Contains(input.Keyword))
                         || (x.InvoiceContactEmail != null && x.InvoiceContactEmail.Contains(input.Keyword))
                         || (x.InvoiceContactPhone != null && x.InvoiceContactPhone.Contains(input.Keyword))
                         || (x.TaxNumber != null && x.TaxNumber.Contains(input.Keyword)))
                .WhereIf(input.StatusId.HasValue, x => x.StatusId == input.StatusId.Value);
        }

        protected override BranchDto MapToEntityDto(Branch entity)
        {
            var dto = base.MapToEntityDto(entity);
            dto.TenantId = entity.TenantId;
            dto.StatusId = entity.StatusId;
            return dto;
        }

        private async Task<bool> CanBrowseAllLocationsAsync()
        {
            if (await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Branches_Approve)
                || await PermissionChecker.IsGrantedAsync(PermissionNames.Pages_Tenants))
            {
                return true;
            }

            // Host user with a location cookie still needs the full location list.
            // Host-only permissions may not apply once Abp.TenantId is set.
            if (AbpSession.UserId.HasValue)
            {
                var user = await UserManager.GetUserByIdAsync(AbpSession.UserId.Value);
                if (user != null && !user.TenantId.HasValue)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task FillTenancyNamesAsync(IReadOnlyList<BranchDto> branches)
        {
            if (branches == null || branches.Count == 0)
            {
                return;
            }

            var tenantIds = branches
                .Where(x => x.TenantId.HasValue)
                .Select(x => x.TenantId.Value)
                .Distinct()
                .ToList();

            if (tenantIds.Count == 0)
            {
                return;
            }

            var tenants = await _tenantRepository.GetAll()
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.TenancyName);

            foreach (var dto in branches)
            {
                if (dto.TenantId.HasValue && tenants.TryGetValue(dto.TenantId.Value, out var tenancyName))
                {
                    dto.TenancyName = tenancyName;
                }
            }
        }

        private async Task FillStatusNamesAsync(IReadOnlyList<BranchDto> branches)
        {
            if (branches == null || branches.Count == 0)
            {
                return;
            }

            var statusIds = branches.Select(x => x.StatusId).Where(x => x > 0).Distinct().ToList();
            if (statusIds.Count == 0)
            {
                return;
            }

            Dictionary<int, LookUp> statuses;
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                statuses = await _lookUpRepository.GetAll()
                    .Where(x => statusIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id);
            }

            foreach (var dto in branches)
            {
                if (statuses.TryGetValue(dto.StatusId, out var lookUp))
                {
                    dto.Status = lookUp.Name;
                    dto.StatusDisplayName = lookUp.DisplayName;
                }
            }
        }

        private async Task SeedSharedProductsAsync(int branchId)
        {
            // Tenant-level products with no BranchStock rows are already visible everywhere.
            // Seed products that were assigned to every other active branch (legacy "all locations").
            var otherBranchIds = await Repository.GetAll()
                .Where(x => x.IsActive && x.Id != branchId)
                .Select(x => x.Id)
                .ToListAsync();

            if (!otherBranchIds.Any())
            {
                return;
            }

            var products = await _productRepository.GetAll()
                .Select(x => new
                {
                    x.Id,
                    x.Price,
                    x.WholesalePrice,
                    x.CostPrice
                })
                .ToListAsync();

            foreach (var product in products)
            {
                var assigned = await _branchStockManager.GetAssignedBranchIdsAsync(product.Id);
                if (!assigned.Any())
                {
                    continue;
                }

                if (!otherBranchIds.All(id => assigned.Contains(id)))
                {
                    continue;
                }

                await _branchStockManager.UpsertStockAndPricesAsync(
                    branchId,
                    product.Id,
                    0,
                    product.Price,
                    product.WholesalePrice,
                    product.CostPrice);
            }
        }
    }
}
