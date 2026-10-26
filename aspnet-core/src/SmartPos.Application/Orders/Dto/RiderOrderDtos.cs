using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using SmartPos.Orders;

namespace SmartPos.Orders.Dto
{
    public class RiderDashboardStatsDto
    {
        public int TodayDeliveredCount { get; set; }
        public int ActiveAssignedCount { get; set; }
        public decimal TodayDeliveredAmount { get; set; }
        public decimal ActivePendingAmount { get; set; }
        public int TotalAssignedToday { get; set; }
        public int RiderStaffId { get; set; }
        public string RiderName { get; set; }
        public string RiderPhone { get; set; }
        public string BranchName { get; set; }
    }

    public class RiderOrderDto : EntityDto<int>
    {
        public string OrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        public CustomerOrderStatus Status { get; set; }
        public string StatusText { get; set; }
        public DateTime? RiderAssignedTime { get; set; }
        public string RiderAssignedTimeText { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerAddress { get; set; }
        public string DeliveryAreaName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? DistanceKm { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string Notes { get; set; }
        public string PaymentMethod { get; set; }
        public bool IsAcceptedByRider { get; set; }
        public List<RiderOrderItemDto> Items { get; set; } = new List<RiderOrderItemDto>();
    }

    public class RiderOrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public bool IsDeal { get; set; }
        public List<string> DealItemDescriptions { get; set; } = new List<string>();
    }

    public class RiderOrderHistoryInput : PagedResultRequestDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Keyword { get; set; }
        public CustomerOrderStatus? Status { get; set; }
    }

    public class RejectRiderOrderDto
    {
        public int OrderId { get; set; }
        public string Reason { get; set; }
    }
}
