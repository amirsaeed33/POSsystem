using System.Linq;
using SmartPos.Products;

namespace SmartPos.Inventory
{
    public static class ProductBranchVisibility
    {
        /// <summary>
        /// Products visible to a branch:
        /// - tenant-level (no BranchStock rows yet), or
        /// - assigned via a BranchStock row for that branch.
        /// </summary>
        public static IQueryable<Product> WhereVisibleToBranch(
            this IQueryable<Product> products,
            IQueryable<BranchStock> branchStocks,
            int branchId)
        {
            return products.Where(p =>
                p.BranchId == branchId
                || p.BranchId == 0
                || branchStocks.Any(bs => bs.BranchId == branchId && bs.ProductId == p.Id)
                || !branchStocks.Any(bs => bs.ProductId == p.Id));
        }
    }
}
