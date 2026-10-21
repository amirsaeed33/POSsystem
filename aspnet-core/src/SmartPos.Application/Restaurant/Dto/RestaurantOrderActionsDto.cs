using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartPos.Restaurant.Dto
{
    public class SendToKitchenResultDto
    {
        public int OrderId { get; set; }

        public string OrderNo { get; set; }

        public string TableNo { get; set; }

        public string WaiterName { get; set; }

        public DateTime PrintTime { get; set; } = DateTime.UtcNow;

        public List<KitchenSlipItemDto> NewItems { get; set; } = new List<KitchenSlipItemDto>();
    }

    public class KitchenSlipItemDto
    {
        public string ProductName { get; set; }

        public decimal Quantity { get; set; }

        public string KitchenNotes { get; set; }

        public string SelectedModifiers { get; set; }
    }

    public class SettleRestaurantOrderDto
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        public int PaymentType { get; set; } = 1;

        public decimal CashAmount { get; set; }

        public decimal CardAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal TaxPercent { get; set; }

        public decimal ServiceChargePercent { get; set; }

        public string Notes { get; set; }
    }

    public class SplitBillDto
    {
        [Required]
        public int SourceOrderId { get; set; }

        public int NumberOfSplits { get; set; } = 2;

        public bool SplitEqually { get; set; } = true;

        public List<int> MoveLineIds { get; set; } = new List<int>();
    }
}
