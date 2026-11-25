using System.Linq;
using SmartPos.Products;

namespace SmartPos.Inventory
{
    public static class ProductBranchVisibility
    {
        /// <summary>
        /// Products visible to a branch:
        /// - assigned via a BranchStock row for that branch, or
        /// - owned by that branch (Product.BranchId) when no BranchStock rows exist yet.
        /// Products are never treated as tenant-wide / visible on every branch by default.
        /// </summary>
        public static IQueryable<Product> WhereVisibleToBranch(
            this IQueryable<Product> products,
            IQueryable<BranchStock> branchStocks,
            int branchId)
        {
            return products.Where(p =>
                branchStocks.Any(bs => bs.ProductId == p.Id)
                    ? branchStocks.Any(bs => bs.BranchId == branchId && bs.ProductId == p.Id)
                    : p.BranchId == branchId);
        }
    }
}
