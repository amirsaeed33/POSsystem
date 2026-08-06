namespace SmartPos.Lookups
{
    /// <summary>
    /// Well-known lookup type codes. Catalog of types lives in AppLookUps
    /// where Type = <see cref="LookUpType"/>.
    /// </summary>
    public static class LookUpTypes
    {
        /// <summary>Meta-type: each Name is an allowed lookup Type.</summary>
        public const string LookUpType = "LookUpType";

        public const string PaymentMethod = "PaymentMethod";
        public const string DiscountType = "DiscountType";
        public const string StockAdjustmentReason = "StockAdjustmentReason";
        public const string Gender = "Gender";
        public const string BranchStatus = "BranchStatus";
    }
}
