using System.Text.Json.Serialization;

namespace SmartPos.Mobile.Models;

public class RiderDashboardStatsDto
{
    [JsonPropertyName("todayDeliveredCount")]
    public int TodayDeliveredCount { get; set; }

    [JsonPropertyName("activeAssignedCount")]
    public int ActiveAssignedCount { get; set; }

    [JsonPropertyName("todayDeliveredAmount")]
    public decimal TodayDeliveredAmount { get; set; }

    [JsonPropertyName("activePendingAmount")]
    public decimal ActivePendingAmount { get; set; }

    [JsonPropertyName("totalAssignedToday")]
    public int TotalAssignedToday { get; set; }

    [JsonPropertyName("riderStaffId")]
    public int RiderStaffId { get; set; }

    [JsonPropertyName("riderName")]
    public string? RiderName { get; set; }

    [JsonPropertyName("riderPhone")]
    public string? RiderPhone { get; set; }

    [JsonPropertyName("branchName")]
    public string? BranchName { get; set; }
}

public class RiderOrderDto : EntityDto
{
    [JsonPropertyName("orderNo")]
    public string? OrderNo { get; set; }

    [JsonPropertyName("orderDate")]
    public DateTime OrderDate { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("statusText")]
    public string? StatusText { get; set; }

    [JsonPropertyName("riderAssignedTime")]
    public DateTime? RiderAssignedTime { get; set; }

    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("customerPhone")]
    public string? CustomerPhone { get; set; }

    [JsonPropertyName("customerAddress")]
    public string? CustomerAddress { get; set; }

    [JsonPropertyName("deliveryAreaName")]
    public string? DeliveryAreaName { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("distanceKm")]
    public double? DistanceKm { get; set; }

    [JsonPropertyName("subTotal")]
    public decimal SubTotal { get; set; }

    [JsonPropertyName("deliveryFee")]
    public decimal DeliveryFee { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("paymentMethod")]
    public string? PaymentMethod { get; set; }

    [JsonPropertyName("isAcceptedByRider")]
    public bool IsAcceptedByRider { get; set; }

    [JsonPropertyName("items")]
    public List<RiderOrderItemDto> Items { get; set; } = new();

    public string TotalText => TotalAmount.ToString("0.00");
}

public class RiderOrderItemDto
{
    [JsonPropertyName("productId")]
    public int ProductId { get; set; }

    [JsonPropertyName("productName")]
    public string? ProductName { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("lineTotal")]
    public decimal LineTotal { get; set; }

    [JsonPropertyName("isDeal")]
    public bool IsDeal { get; set; }
}

public class RejectRiderOrderDto
{
    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
