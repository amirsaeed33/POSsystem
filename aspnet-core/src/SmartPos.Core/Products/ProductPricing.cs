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
        /// Updates average cost from a purchase. Call before increasing <see cref="Product.StockQuantity"/>.
        /// </summary>
        public static void ApplyPurchaseCost(Product product, decimal purchaseQuantity, decimal unitCost)
        {
            if (product == null || purchaseQuantity <= 0)
            {
                return;
            }

            var oldQty = product.StockQuantity;
            if (oldQty <= 0)
            {
                product.CostPrice = unitCost;
                return;
            }

            var totalCost = (oldQty * product.CostPrice) + (purchaseQuantity * unitCost);
            var newQty = oldQty + purchaseQuantity;
            product.CostPrice = Math.Round(totalCost / newQty, 4);
        }
    }
}
