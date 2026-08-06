using SmartPos.Branches;

namespace SmartPos.Lookups
{
    public static class LookUpSeedData
    {
        public static readonly LookUpSeedItem[] Items =
        {
            // Catalog of lookup types (Type dropdown source)
            new(LookUpTypes.LookUpType, LookUpTypes.LookUpType, "Lookup Type", 1),
            new(LookUpTypes.LookUpType, LookUpTypes.PaymentMethod, "Payment Method", 10),
            new(LookUpTypes.LookUpType, LookUpTypes.DiscountType, "Discount Type", 20),
            new(LookUpTypes.LookUpType, LookUpTypes.StockAdjustmentReason, "Stock Adjustment Reason", 30),
            new(LookUpTypes.LookUpType, LookUpTypes.Gender, "Gender", 40),
            new(LookUpTypes.LookUpType, LookUpTypes.BranchStatus, "Branch Status", 50),

            new(LookUpTypes.PaymentMethod, "Cash", "Cash", 10),
            new(LookUpTypes.PaymentMethod, "Card", "Card", 20),
            new(LookUpTypes.PaymentMethod, "BankTransfer", "Bank Transfer", 30),
            new(LookUpTypes.PaymentMethod, "Credit", "Credit", 40),
            new(LookUpTypes.PaymentMethod, "Mixed", "Mixed", 50),
            new(LookUpTypes.PaymentMethod, "Other", "Other", 60),

            new(LookUpTypes.DiscountType, "None", "None", 10),
            new(LookUpTypes.DiscountType, "Percentage", "Percentage", 20),
            new(LookUpTypes.DiscountType, "FixedAmount", "Fixed Amount", 30),

            new(LookUpTypes.StockAdjustmentReason, "CountCorrection", "Count Correction", 10),
            new(LookUpTypes.StockAdjustmentReason, "Damage", "Damage", 20),
            new(LookUpTypes.StockAdjustmentReason, "Expired", "Expired", 30),
            new(LookUpTypes.StockAdjustmentReason, "Theft", "Theft", 40),
            new(LookUpTypes.StockAdjustmentReason, "Transfer", "Transfer", 50),
            new(LookUpTypes.StockAdjustmentReason, "Other", "Other", 60),

            new(LookUpTypes.Gender, "Male", "Male", 10),
            new(LookUpTypes.Gender, "Female", "Female", 20),
            new(LookUpTypes.Gender, "Other", "Other", 30),

            new(LookUpTypes.BranchStatus, BranchStatuses.Pending, "Pending", 10),
            new(LookUpTypes.BranchStatus, BranchStatuses.Approved, "Approved", 20),
            new(LookUpTypes.BranchStatus, BranchStatuses.Rejected, "Rejected", 30),
        };
    }

    public sealed class LookUpSeedItem
    {
        public LookUpSeedItem(string type, string name, string displayName, int sortOrder)
        {
            Type = type;
            Name = name;
            DisplayName = displayName;
            SortOrder = sortOrder;
        }

        public string Type { get; }
        public string Name { get; }
        public string DisplayName { get; }
        public int SortOrder { get; }
    }
}
