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

        public string CarName { get; set; }

        public string CarNumber { get; set; }

        public string CarLocation { get; set; }

        public string WaiterName { get; set; }

        public DateTime PrintTime { get; set; } = DateTime.UtcNow;

        public string PrintedBy { get; set; }

        public long? PrintedByUserId { get; set; }

        public string PrintedByName { get; set; }

        public string PrintedByRole { get; set; }

        public bool HasNewItems { get; set; } = true;

        public string Message { get; set; }

        public List<KitchenSlipItemDto> NewItems { get; set; } = new List<KitchenSlipItemDto>();
    }

    public class ReserveCarServiceDto
    {
        [Required]
        public int BranchId { get; set; }

        /// <summary>Optional car bay table id — when set, bay is marked Occupied.</summary>
        public int? TableId { get; set; }

        [Required]
        public string CarName { get; set; }

        [Required]
        public string CarNumber { get; set; }

        [Required]
        public string CarLocation { get; set; }

        public string WaiterName { get; set; }

        public string Notes { get; set; }
    }

    /// <summary>Open a dine-in table or car bay session (empty order + Occupied status).</summary>
    public class OpenTableSessionDto
    {
        [Required]
        public int BranchId { get; set; }

        [Required]
        public int TableId { get; set; }

        public string WaiterName { get; set; }

        /// <summary>Required when the table is a car bay.</summary>
        public string CarNumber { get; set; }

        public string Notes { get; set; }
    }

    public class RecordPrintLogDto
    {
        [Required]
        public int BranchId { get; set; }

        public int? OrderId { get; set; }

        public string OrderNo { get; set; }

        [Required]
        public string ReceiptType { get; set; }
    }

    public class RestaurantPrintLogDto
    {
        public int Id { get; set; }

        public int BranchId { get; set; }

        public int? OrderId { get; set; }

        public string OrderNo { get; set; }

        public string ReceiptType { get; set; }

        public long PrintedByUserId { get; set; }

        public string PrintedByName { get; set; }

        public string PrintedByRole { get; set; }

        public DateTime PrintTime { get; set; }
    }

    public class UnpaidTakeawayOrderDto
    {
        public int Id { get; set; }

        public string OrderNo { get; set; }

        public DateTime CreationTime { get; set; }

        public string Status { get; set; }

        public decimal GrandTotal { get; set; }

        public string WaiterName { get; set; }

        public string CustomerName { get; set; }

        public int ItemCount { get; set; }
    }

    public class KitchenSlipItemDto
    {
        public int? LineId { get; set; }

        public string ProductName { get; set; }

        public decimal Quantity { get; set; }

        public decimal PreviousQuantity { get; set; }

        public decimal TotalQuantity { get; set; }

        public string KitchenNotes { get; set; }

        public string SelectedModifiers { get; set; }
    }

    public class ConfirmKotPrintDto
    {
        [Required]
        public int OrderId { get; set; }

        public List<int> LineIds { get; set; } = new List<int>();
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
