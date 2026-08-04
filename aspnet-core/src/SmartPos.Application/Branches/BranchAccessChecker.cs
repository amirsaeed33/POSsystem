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

        public BranchAccessChecker(
            IBranchContext branchContext,
            IRepository<Branch> branchRepository,
            UserManager userManager,
            IPermissionChecker permissionChecker,
            IAbpSession abpSession)
        {
            _branchContext = branchContext;
            _branchRepository = branchRepository;
            _userManager = userManager;
            _permissionChecker = permissionChecker;
            _abpSession = abpSession;
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

            if (await _permissionChecker.IsGrantedAsync(PermissionNames.Pages_Branches))
            {
                return true;
            }

            var user = await _userManager.GetUserByIdAsync(_abpSession.UserId.Value);
            return user.BranchId == branchId;
        }

        public async Task EnsureCanAccessBranchAsync(int branchId)
        {
            if (!await CanAccessBranchAsync(branchId))
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
                if (await CanAccessBranchAsync(headerBranchId.Value))
                {
                    return headerBranchId.Value;
                }

                throw new UserFriendlyException("You do not have access to this branch.");
            }

            var user = await _userManager.GetUserByIdAsync(_abpSession.UserId.Value);
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
    }
}
