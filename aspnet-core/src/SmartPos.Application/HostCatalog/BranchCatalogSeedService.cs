using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Branches;
using SmartPos.Brands;
using SmartPos.Categories;
using SmartPos.Units;

namespace SmartPos.HostCatalog
{
    public class BranchCatalogSeedService : ITransientDependency
    {
        private readonly IRepository<BranchSeedRequest> _seedRequestRepository;
        private readonly IRepository<BranchSeedRequestItem> _seedRequestItemRepository;
        private readonly IRepository<HostCatalogItem> _hostCatalogRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Brand> _brandRepository;
        private readonly IRepository<Unit> _unitRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public BranchCatalogSeedService(
            IRepository<BranchSeedRequest> seedRequestRepository,
            IRepository<BranchSeedRequestItem> seedRequestItemRepository,
            IRepository<HostCatalogItem> hostCatalogRepository,
            IRepository<Category> categoryRepository,
            IRepository<Brand> brandRepository,
            IRepository<Unit> unitRepository,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _seedRequestRepository = seedRequestRepository;
            _seedRequestItemRepository = seedRequestItemRepository;
            _hostCatalogRepository = hostCatalogRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _unitRepository = unitRepository;
            _unitOfWorkManager = unitOfWorkManager;
        }

        public async Task ApproveAndCopyAsync(Branch branch, long? approvedByUserId)
        {
            if (branch == null || !branch.TenantId.HasValue)
            {
                return;
            }

            using (_unitOfWorkManager.Current.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var request = await _seedRequestRepository.GetAll()
                    .Include(x => x.Items)
                    .Where(x =>
                        x.BranchId == branch.Id
                        && x.Status == BranchSeedRequestStatuses.Pending)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                if (request == null)
                {
                    return;
                }

                request.Status = BranchSeedRequestStatuses.Approved;
                request.ApprovedByUserId = approvedByUserId;
                request.ApprovedDate = DateTime.UtcNow;

                var hostItemIds = request.Items?.Select(x => x.HostItemId).Distinct().ToList()
                                  ?? new System.Collections.Generic.List<int>();
                if (hostItemIds.Count == 0)
                {
                    await _unitOfWorkManager.Current.SaveChangesAsync();
                    return;
                }

                var hostItems = await _hostCatalogRepository.GetAll()
                    .Where(x => hostItemIds.Contains(x.Id) && x.IsActive)
                    .ToListAsync();

                foreach (var hostItem in hostItems)
                {
                    await CopyHostItemAsync(branch, hostItem);
                }

                await _unitOfWorkManager.Current.SaveChangesAsync();
            }
        }

        public async Task RejectAsync(int branchId, long? approvedByUserId)
        {
            using (_unitOfWorkManager.Current.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var request = await _seedRequestRepository.GetAll()
                    .Where(x =>
                        x.BranchId == branchId
                        && x.Status == BranchSeedRequestStatuses.Pending)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                if (request == null)
                {
                    return;
                }

                request.Status = BranchSeedRequestStatuses.Rejected;
                request.ApprovedByUserId = approvedByUserId;
                request.ApprovedDate = DateTime.UtcNow;
                await _unitOfWorkManager.Current.SaveChangesAsync();
            }
        }

        public async Task CreateRequestAsync(
            Branch branch,
            int companyTypeId,
            System.Collections.Generic.IList<int> hostItemIds,
            long requestedByUserId)
        {
            if (branch == null || !branch.TenantId.HasValue)
            {
                throw new UserFriendlyException("Branch tenant is required for seed request.");
            }

            var companyType = await _hostCatalogRepository.FirstOrDefaultAsync(x =>
                x.Id == companyTypeId
                && x.Type == HostCatalogItemTypes.CompanyType
                && x.IsActive);

            if (companyType == null)
            {
                throw new UserFriendlyException("Company type is required.");
            }

            var distinctIds = (hostItemIds ?? new System.Collections.Generic.List<int>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (distinctIds.Count == 0)
            {
                throw new UserFriendlyException(
                    "Select at least one category, unit, or brand to seed for this location.");
            }

            // Inline type checks so EF can translate the filter (static helpers are not translated).
            var hostItems = await _hostCatalogRepository.GetAll()
                .Where(x =>
                    distinctIds.Contains(x.Id)
                    && x.CompanyTypeId == companyTypeId
                    && x.IsActive
                    && (x.Type == HostCatalogItemTypes.Category
                        || x.Type == HostCatalogItemTypes.Unit
                        || x.Type == HostCatalogItemTypes.Brand))
                .ToListAsync();

            if (hostItems.Count == 0)
            {
                throw new UserFriendlyException("No valid seed items were selected for this company type.");
            }

            var request = new BranchSeedRequest
            {
                TenantId = branch.TenantId.Value,
                BranchId = branch.Id,
                RequestedByUserId = requestedByUserId,
                CompanyTypeId = companyTypeId,
                Status = BranchSeedRequestStatuses.Pending
            };

            await _seedRequestRepository.InsertAsync(request);
            await _unitOfWorkManager.Current.SaveChangesAsync();

            foreach (var hostItem in hostItems)
            {
                await _seedRequestItemRepository.InsertAsync(new BranchSeedRequestItem
                {
                    BranchSeedRequestId = request.Id,
                    HostItemId = hostItem.Id
                });
            }

            await _unitOfWorkManager.Current.SaveChangesAsync();
        }

        private async Task CopyHostItemAsync(Branch branch, HostCatalogItem hostItem)
        {
            var tenantId = branch.TenantId;
            var branchId = branch.Id;

            if (string.Equals(hostItem.Type, HostCatalogItemTypes.Category, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _categoryRepository.GetAll()
                    .AnyAsync(x =>
                        x.TenantId == tenantId
                        && x.BranchId == branchId
                        && x.HostSourceId == hostItem.Id);
                if (exists)
                {
                    return;
                }

                await _categoryRepository.InsertAsync(new Category
                {
                    TenantId = tenantId,
                    BranchId = branchId,
                    Name = hostItem.Name,
                    IsActive = true,
                    HostSourceId = hostItem.Id
                });
                return;
            }

            if (string.Equals(hostItem.Type, HostCatalogItemTypes.Brand, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _brandRepository.GetAll()
                    .AnyAsync(x =>
                        x.TenantId == tenantId
                        && x.BranchId == branchId
                        && x.HostSourceId == hostItem.Id);
                if (exists)
                {
                    return;
                }

                await _brandRepository.InsertAsync(new Brand
                {
                    TenantId = tenantId,
                    BranchId = branchId,
                    Name = hostItem.Name,
                    IsActive = true,
                    HostSourceId = hostItem.Id
                });
                return;
            }

            if (string.Equals(hostItem.Type, HostCatalogItemTypes.Unit, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _unitRepository.GetAll()
                    .AnyAsync(x =>
                        x.TenantId == tenantId
                        && x.BranchId == branchId
                        && x.HostSourceId == hostItem.Id);
                if (exists)
                {
                    return;
                }

                await _unitRepository.InsertAsync(new Unit
                {
                    TenantId = tenantId,
                    BranchId = branchId,
                    Name = hostItem.Name,
                    Symbol = hostItem.Symbol,
                    IsActive = true,
                    HostSourceId = hostItem.Id
                });
            }
        }
    }
}