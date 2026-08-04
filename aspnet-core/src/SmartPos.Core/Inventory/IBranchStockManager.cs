using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartPos.Inventory
{
    public interface IBranchStockManager
    {
        Task<decimal> GetQuantityAsync(int branchId, int productId);

        Task<Dictionary<int, decimal>> GetQuantitiesAsync(int branchId, IEnumerable<int> productIds);

        Task<Dictionary<int, BranchProductInfo>> GetBranchProductInfoAsync(
            int branchId,
            IEnumerable<int> productIds);

        Task IncreaseAsync(int branchId, int productId, decimal quantity);

        Task DecreaseAsync(int branchId, int productId, decimal quantity, string productName = null);

        Task SetAsync(int branchId, int productId, decimal quantity);

        Task SetPricesAsync(
            int branchId,
            int productId,
            decimal price,
            decimal wholesalePrice,
            decimal costPrice);

        Task UpsertStockAndPricesAsync(
            int branchId,
            int productId,
            decimal quantity,
            decimal price,
            decimal wholesalePrice,
            decimal costPrice);
    }
}
