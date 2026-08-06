using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Repositories;
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
        private readonly BranchStatusLookup _branchStatusLookup;

        public BranchAccessChecker(
            IBranchContext branchContext,
            IRepository<Branch> branchRepository,
            UserManager userManager,
            IPermissionChecker permissionChecker,
            IAbpSession abpSession,
            BranchStatusLookup branchStatusLookup)
        {
            _branchContext = branchContext;
            _branchRepository = branchRepository;
            _userManager = userManager;
            _permissionChecker = permissionChecker;
            _abpSession = abpSession;
            _branchStatusLookup = branchStatusLookup;
        }

        public async Task<bool> CanAccessBranchAsync(int branchId)
        {
            if (!_abpSession.UserId.HasValue)
            {
                return false;
            }

            var branch = await _branchRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == branchId && x.IsActive);

            if (branch == null)
            {
                return false;
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
                throw new UserFriendlyException("You do not have access to this branch.");
            }

            var branch = await _branchRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == branchId && x.IsActive);

            if (branch == null)
            {
                throw new UserFriendlyException("You do not have access to this branch.");
            }

            if (_abpSession.TenantId.HasValue && !await IsApprovedAsync(branch.StatusId))
            {
                throw new UserFriendlyException(
                    "This branch is not approved yet. Please wait for host administrator approval before using the system.");
            }

            if (await _permissionChecker.IsGrantedAsync(PermissionNames.Pages_Branches))
            {
                return;
            }

            var user = await _userManager.GetUserByIdAsync(_abpSession.UserId.Value);
            if (user.BranchId != branchId)
            {
                throw new UserFriendlyException("You do not have access to this branch.");
            }
        }

        public async Task<int?> GetEffectiveBranchIdAsync()
        {
            if (!_abpSession.UserId.HasValue)
            {
                return null;
            }

            var headerBranchId = _branchContext.BranchId;
            if (headerBranchId.HasValue)
            {
                await EnsureCanAccessBranchAsync(headerBranchId.Value);
                return headerBranchId.Value;
            }

            var user = await _userManager.GetUserByIdAsync(_abpSession.UserId.Value);
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
                throw new UserFriendlyException("No branch is assigned. Please contact your administrator.");
            }

            await EnsureCanAccessBranchAsync(branchId.Value);
            return branchId.Value;
        }

        private async Task<bool> IsApprovedAsync(int statusId)
        {
            var approvedId = await _branchStatusLookup.GetIdAsync(BranchStatuses.Approved);
            return statusId == approvedId;
        }
    }
}
