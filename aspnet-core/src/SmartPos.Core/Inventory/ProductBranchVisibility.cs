using System.Linq;
using SmartPos.Products;

namespace SmartPos.Inventory
{
    public static class ProductBranchVisibility
    {
        /// <summary>
        /// Products visible to a branch: shared, or assigned via BranchStock.
        /// </summary>
        public static IQueryable<Product> WhereVisibleToBranch(
            this IQueryable<Product> products,
            IQueryable<BranchStock> branchStocks,
            int branchId)
        {
            return products.Where(p =>
                p.IsShared
                || branchStocks.Any(bs => bs.BranchId == branchId && bs.ProductId == p.Id));
        }
    }
}
