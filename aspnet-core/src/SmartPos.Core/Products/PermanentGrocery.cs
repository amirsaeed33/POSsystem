using System;

namespace SmartPos.Products
{
    /// <summary>
    /// Restaurant inventory groceries live under this category name.
    /// Legacy single-product grocery used barcode GROCERY-* / name "Grocery".
    /// </summary>
    public static class PermanentGrocery
    {
        public const string CategoryName = "Permanent Grocery";

        public static bool IsCategoryName(string categoryName)
        {
            return !string.IsNullOrWhiteSpace(categoryName)
                   && string.Equals(categoryName.Trim(), CategoryName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsLegacyLockedProduct(string barcode, string name)
        {
            return (!string.IsNullOrWhiteSpace(barcode)
                    && barcode.StartsWith("GROCERY-", StringComparison.OrdinalIgnoreCase))
                   || string.Equals(name?.Trim(), "Grocery", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsInventoryProduct(string barcode, string name, string categoryName)
        {
            return IsLegacyLockedProduct(barcode, name) || IsCategoryName(categoryName);
        }
    }
}
