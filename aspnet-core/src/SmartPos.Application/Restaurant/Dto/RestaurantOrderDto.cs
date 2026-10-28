using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using SmartPos.Restaurant;

namespace SmartPos.Restaurant.Dto
{
    public class RestaurantOrderDto : EntityDto
    {
        public int BranchId { get; set; }

        public string OrderNo { get; set; }

        public int? TableId { get; set; }

        public string TableNo { get; set; }

        public OrderType OrderType { get; set; }

        public string OrderTypeName => OrderType.ToString();

        public RestaurantOrderStatus Status { get; set; }

        public string StatusName => Status.ToString();

        public int? CustomerId { get; set; }

        public string CustomerName { get; set; }

        public string WaiterName { get; set; }

        public string Notes { get; set; }

        public decimal SubTotal { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxPercent { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal ServiceChargePercent { get; set; }

        public decimal ServiceChargeAmount { get; set; }

        public decimal GrandTotal { get; set; }

        public int? SaleId { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime? BilledTime { get; set; }

        public DateTime? SettledTime { get; set; }

        public string LastPrintedBy { get; set; }

        public DateTime? LastPrintedTime { get; set; }

        public List<RestaurantOrderLineDto> Lines { get; set; } = new List<RestaurantOrderLineDto>();
    }

    public class RestaurantOrderLineDto : EntityDto
    {
        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal LineTotal { get; set; }

        public string KitchenNotes { get; set; }

        public string SelectedModifiers { get; set; }

        public bool IsSentToKitchen { get; set; }

        public decimal PrintedQuantity { get; set; } = 0;

        public DateTime? SentToKitchenTime { get; set; }
    }
}
