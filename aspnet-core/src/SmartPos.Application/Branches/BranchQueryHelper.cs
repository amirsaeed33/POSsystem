using System.Linq;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using SmartPos.Authorization;
using SmartPos.Authorization.Users;

namespace SmartPos.Branches
{
    /// <summary>
    /// Sync helpers for LINQ filters when async access checker cannot be used.
    /// Prefer <see cref="IBranchAccessChecker"/> for creates/writes.
    /// </summary>
    public static class BranchQueryHelper
    {
        public static int? ResolveBranchIdForFilter(
            IBranchContext branchContext,
            IRepository<User, long> userRepository,
            IAbpSession abpSession,
            IPermissionChecker permissionChecker)
        {
            int? userBranchId = null;
            if (abpSession.UserId.HasValue)
            {
                userBranchId = userRepository.GetAll()
                    .Where(x => x.Id == abpSession.UserId.Value)
                    .Select(x => x.BranchId)
                    .FirstOrDefault();
            }

            if (!branchContext.BranchId.HasValue)
            {
                return userBranchId;
            }

            var headerBranchId = branchContext.BranchId.Value;
            var canSwitchAll = permissionChecker != null &&
                               permissionChecker.IsGranted(PermissionNames.Pages_Branches);

            if (canSwitchAll || userBranchId == headerBranchId)
            {
                return headerBranchId;
            }

            // Ignore unauthorized header — keep user locked to assigned branch.
            return userBranchId;
        }
    }
}
