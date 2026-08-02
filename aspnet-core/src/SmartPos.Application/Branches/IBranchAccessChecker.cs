using System.Threading.Tasks;

namespace SmartPos.Branches
{
    public interface IBranchAccessChecker
    {
        Task EnsureCanAccessAsync(int branchId, bool requireActive = true);

        Task<bool> HasAccessAsync(int branchId);
    }
}
