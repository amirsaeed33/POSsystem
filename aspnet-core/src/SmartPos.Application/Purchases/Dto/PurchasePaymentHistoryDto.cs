using System;

namespace SmartPos.Purchases.Dto
{
    public class PurchasePaymentHistoryDto
    {
        public int Id { get; set; }

        public DateTime TransactionDate { get; set; }

        public decimal Amount { get; set; }

        public int? PaymentAccountId { get; set; }

        public string PaymentAccountName { get; set; }

        public string Description { get; set; }
    }
}
