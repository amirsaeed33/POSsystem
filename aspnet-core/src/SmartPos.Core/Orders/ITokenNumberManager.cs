using System.Threading.Tasks;
using Abp.Domain.Services;

namespace SmartPos.Orders
{
    public interface ITokenNumberManager : IDomainService
    {
        /// <summary>
        /// Allocates the next daily queue/token number for the branch.
        /// Resets to 0 at 12:00 AM (midnight) every day and counts 1, 2, 3...
        /// across Dine-In, Takeaway, Online Delivery, and Car Service orders.
        /// </summary>
        Task<int> AllocateNextTokenNoAsync(int branchId);
    }
}

