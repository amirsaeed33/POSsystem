using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;

namespace SmartPos.Branches
{
    public class BranchAccessChecker : IBranchAccessChecker, ITransientDependency
    {
        private readonly IRepository<Branch> _branchRepository;
        private readonly IRepository<UserBranch> _userBranchRepository;
        private readonly IAbpSession _abpSession;

        public BranchAccessChecker(
            IRepository<Branch> branchRepository,
            IRepository<UserBranch> userBranchRepository,
            IAbpSession abpSession)
        {
            _branchRepository = branchRepository;
            _userBranchRepository = userBranchRepository;
            _abpSession = abpSession;
        }

        public async Task EnsureCanAccessAsync(int branchId, bool requireActive = true)
        {
            if (branchId <= 0)
            {
                throw new UserFriendlyException("Branch is required.");
            }

            var branch = await _branchRepository.FirstOrDefaultAsync(branchId);
            if (branch == null)
            {
                throw new UserFriendlyException("Branch was not found.");
            }

            if (branch.TenantId != _abpSession.TenantId)
            {
                throw new UserFriendlyException("Branch does not belong to the current tenant.");
            }

            if (requireActive && !branch.IsActive)
            {
                throw new UserFriendlyException($"Branch '{branch.Name}' is inactive.");
            }

            if (!_abpSession.UserId.HasValue)
            {
                throw new UserFriendlyException("You do not have access to this branch.");
            }

            var hasAccess = await _userBranchRepository.GetAll()
                .AnyAsync(x => x.UserId == _abpSession.UserId.Value && x.BranchId == branchId);

            if (!hasAccess)
            {
                throw new UserFriendlyException($"You do not have access to branch '{branch.Name}'.");
            }
        }

        public async Task<bool> HasAccessAsync(int branchId)
        {
            if (branchId <= 0 || !_abpSession.UserId.HasValue)
            {
                return false;
            }

            return await _userBranchRepository.GetAll()
                .AnyAsync(x => x.UserId == _abpSession.UserId.Value && x.BranchId == branchId);
        }
    }
}
