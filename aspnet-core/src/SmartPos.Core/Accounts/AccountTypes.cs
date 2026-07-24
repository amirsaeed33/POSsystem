namespace SmartPos.Accounts
{
    public static class AccountTypes
    {
        public const string Cash = "Cash";
        public const string Bank = "Bank";
        public const string CreditCard = "Credit Card";
        public const string Other = "Other";
        public const string Customer = "Customer";
        public const string Supplier = "Supplier";
        public const string Purchase = "Purchase";
        public const string Sale = "Sale";
        public const string Expense = "Expense";
    }

    public static class SystemAccountCodes
    {
        public const string Cash = "SYS-CASH";
        public const string Bank = "SYS-BANK";
        public const string Purchase = "SYS-PURCHASE";
        public const string Sale = "SYS-SALE";
        public const string Expense = "SYS-EXPENSE";
    }

    public static class VoucherTypes
    {
        public const string Invoice = "Invoice";
        public const string Payment = "Payment";
        public const string Journal = "Journal";
        public const string OpeningBalance = "OpeningBalance";
        public const string Adjustment = "Adjustment";
        public const string Expense = "Expense";
        public const string Sale = "Sale";
        public const string SaleReturn = "SaleReturn";
        public const string PurchaseReturn = "PurchaseReturn";
    }
}
