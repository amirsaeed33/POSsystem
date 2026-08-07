using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using SmartPos.Authorization;
using SmartPos.Authorization.Users;

namespace SmartPos.Branches
{
    public class BranchAccessChecker : IBranchAccessChecker, ITransientDependency
    {
        private readonly IBranchContext _branchContext;
        private readonly IRepository<Branch> _branchRepository;
        private readonly UserManager _userManager;
        private readonly IPermissionChecker _permissionChecker;
        private readonly IAbpSession _abpSession;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly BranchStatusLookup _branchStatusLookup;

        public BranchAccessChecker(
            IBranchContext branchContext,
            IRepository<Branch> branchRepository,
            UserManager userManager,
            IPermissionChecker permissionChecker,
            IAbpSession abpSession,
            IUnitOfWorkManager unitOfWorkManager,
            BranchStatusLookup branchStatusLookup)
        {
            _branchContext = branchContext;
            _branchRepository = branchRepository;
            _userManager = userManager;
            _permissionChecker = permissionChecker;
            _abpSession = abpSession;
            _unitOfWorkManager = unitOfWorkManager;
            _branchStatusLookup = branchStatusLookup;
        }

        public async Task<bool> CanAccessBranchAsync(int branchId)
        {
            if (!_abpSession.UserId.HasValue)
            {
                return false;
            }

            var branch = await FindActiveBranchAsync(branchId);
            if (branch == null)
            {
                return false;
            }

            if (await IsHostLocationAdminAsync())
            {
                return branch.TenantId.HasValue;
            }

            if (_abpSession.TenantId.HasValue && !await IsApprovedAsync(branch.StatusId))
            {
                return false;
            }

            if (await _permissionChecker.IsGrantedAsync(PermissionNames.Pages_Branches))
            {
                return true;
            }

            var user = await _userManager.GetUserByIdAsync(_abpSession.UserId.Value);
            return user.BranchId == branchId;
        }

        public async Task EnsureCanAccessBranchAsync(int branchId)
        {
            if (!_abpSession.UserId.HasValue)
            {
                throw new UserFriendlyException("You do not have access to this location.");
            }

            var branch = await FindActiveBranchAsync(branchId);
            if (branch == null)
            {
                throw new UserFriendlyException("You do not have access to this location.");
            }

            if (await IsHostLocationAdminAsync())
            {
                if (!branch.TenantId.HasValue)
                {
                    throw new UserFriendlyException(
                        "Host-level locations are not used. Select a business location.");
                }

                return;
            }

            if (_abpSession.TenantId.HasValue && !await IsApprovedAsync(branch.StatusId))
            {
                throw new UserFriendlyException(
                    "This location is not approved yet. Please wait for host administrator approval before using the system.");
            }

            if (await _permissionChecker.IsGrantedAsync(PermissionNames.Pages_Branches))
            {
                return;
            }

            var user = await _userManager.GetUserByIdAsync(_abpSession.UserId.Value);
            if (user.BranchId != branchId)
            {
                throw new UserFriendlyException("You do not have access to this location.");
            }
        }

        public async Task<int?> GetEffectiveBranchIdAsync()
        {
            if (!_abpSession.UserId.HasValue)
            {
                return null;
            }

            var user = await _userManager.GetUserByIdAsync(_abpSession.UserId.Value);
            var headerBranchId = _branchContext.BranchId;

            // Location staff (no Pages.Branches): always locked to assigned branch.
            var canSwitchLocations =
                await _permissionChecker.IsGrantedAsync(PermissionNames.Pages_Branches)
                || await IsHostLocationAdminAsync();

            if (!canSwitchLocations)
            {
                if (user.BranchId.HasValue)
                {
                    await EnsureCanAccessBranchAsync(user.BranchId.Value);
                }

                return user.BranchId;
            }

            if (headerBranchId.HasValue)
            {
                await EnsureCanAccessBranchAsync(headerBranchId.Value);
                return headerBranchId.Value;
            }

            // Host admin must pick a location from the topbar.
            if (await IsHostLocationAdminAsync())
            {
                return null;
            }

            if (user.BranchId.HasValue)
            {
                await EnsureCanAccessBranchAsync(user.BranchId.Value);
            }

            return user.BranchId;
        }

        public async Task<int> RequireEffectiveBranchIdAsync()
        {
            var branchId = await GetEffectiveBranchIdAsync();
            if (!branchId.HasValue)
            {
                throw new UserFriendlyException(
                    await IsHostLocationAdminAsync()
                        ? "Select a branch location from the top header to continue."
                        : "No location is assigned. Please contact your administrator.");
            }

            await EnsureCanAccessBranchAsync(branchId.Value);
            return branchId.Value;
        }

        private async Task<Branch> FindActiveBranchAsync(int branchId)
        {
            if (await IsHostLocationAdminAsync())
            {
                using (_unitOfWorkManager.Current.DisableFilter(AbpDataFilters.MayHaveTenant))
                {
                    return await _branchRepository.GetAll()
                        .FirstOrDefaultAsync(x => x.Id == branchId && x.IsActive);
                }
            }

            return await _branchRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == branchId && x.IsActive);
        }

        private async Task<bool> IsHostLocationAdminAsync()
        {
            if (await _permissionChecker.IsGrantedAsync(PermissionNames.Pages_Branches_Approve)
                || await _permissionChecker.IsGrantedAsync(PermissionNames.Pages_Tenants))
            {
                return true;
            }

            // Host user entity has no TenantId even when Abp.TenantId cookie is set for a location.
            if (_abpSession.UserId.HasValue)
            {
                var user = await _userManager.GetUserByIdAsync(_abpSession.UserId.Value);
                if (user != null && !user.TenantId.HasValue)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<bool> IsApprovedAsync(int statusId)
        {
            var approvedId = await _branchStatusLookup.GetIdAsync(BranchStatuses.Approved);
            return statusId == approvedId;
        }
    }
}
