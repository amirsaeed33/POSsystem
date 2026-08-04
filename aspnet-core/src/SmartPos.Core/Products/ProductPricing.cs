using System;
using SmartPos.Customers;

namespace SmartPos.Products
{
    public static class ProductPricing
    {
        /// <summary>
        /// Resolves unit sale price from customer type. Falls back to retail price when wholesale is unset.
        /// </summary>
        public static decimal GetSaleUnitPrice(Product product, int customerType)
        {
            if (product == null)
            {
                return 0;
            }

            if (CustomerTypes.IsWholesaler(customerType))
            {
                return product.WholesalePrice > 0 ? product.WholesalePrice : product.Price;
            }

            return product.Price;
        }

        public static decimal ProfitPerUnit(decimal price, decimal costPrice)
        {
            return price - costPrice;
        }

        public static decimal? ProfitMarginPercent(decimal price, decimal costPrice)
        {
            if (price <= 0)
            {
                return null;
            }

            return Math.Round((price - costPrice) / price * 100m, 2);
        }

        public static decimal StockProfit(decimal price, decimal costPrice, decimal stockQuantity)
        {
            return ProfitPerUnit(price, costPrice) * stockQuantity;
        }

        /// <summary>
        /// Calculates weighted-average cost after a purchase against existing stock.
        /// Call with quantity/cost before the purchase quantity is applied.
        /// </summary>
        public static decimal CalculateAverageCost(
            decimal existingQuantity,
            decimal existingCostPrice,
            decimal purchaseQuantity,
            decimal unitCost)
        {
            if (purchaseQuantity <= 0)
            {
                return existingCostPrice;
            }

            if (existingQuantity <= 0)
            {
                return unitCost;
            }

            var totalCost = (existingQuantity * existingCostPrice) + (purchaseQuantity * unitCost);
            var newQty = existingQuantity + purchaseQuantity;
            return Math.Round(totalCost / newQty, 4);
        }

        /// <summary>
        /// Updates average cost on the product catalog default. Prefer branch-aware
        /// <see cref="CalculateAverageCost"/> for multi-branch stock.
        /// </summary>
        public static void ApplyPurchaseCost(Product product, decimal purchaseQuantity, decimal unitCost)
        {
            if (product == null || purchaseQuantity <= 0)
            {
                return;
            }

            product.CostPrice = CalculateAverageCost(
                product.StockQuantity,
                product.CostPrice,
                purchaseQuantity,
                unitCost);
        }
    }
}
