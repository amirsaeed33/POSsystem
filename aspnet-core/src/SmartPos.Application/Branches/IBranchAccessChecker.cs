using System.Threading.Tasks;

namespace SmartPos.Branches
{
    public interface IBranchAccessChecker
    {
        Task<bool> CanAccessBranchAsync(int branchId);

        Task EnsureCanAccessBranchAsync(int branchId);

        /// <summary>
        /// Header branch if present and allowed; otherwise the user's assigned BranchId.
        /// </summary>
        Task<int?> GetEffectiveBranchIdAsync();

        /// <summary>
        /// Same as <see cref="GetEffectiveBranchIdAsync"/> but throws when no branch can be resolved.
        /// </summary>
        Task<int> RequireEffectiveBranchIdAsync();
    }
}
