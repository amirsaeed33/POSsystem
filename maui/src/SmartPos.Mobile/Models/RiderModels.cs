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

    public string TotalText => $"Rs {TotalAmount:0.##}";

    public bool IsNewOrder => !IsAcceptedByRider;

    public string BadgeText => IsNewOrder ? "NEW" : (StatusText ?? "Active");

    public string PhoneDisplay => string.IsNullOrWhiteSpace(CustomerPhone) ? "No phone" : CustomerPhone.Trim();

    public bool HasPhone => NormalizePhone(CustomerPhone) != null;

    public string AreaLine => string.IsNullOrWhiteSpace(DeliveryAreaName) ? "Delivery" : DeliveryAreaName.Trim();

    public string ItemsSummary
    {
        get
        {
            if (Items == null || Items.Count == 0)
            {
                return "No items listed";
            }

            var names = Items
                .Where(i => !string.IsNullOrWhiteSpace(i.ProductName))
                .Take(3)
                .Select(i => $"{i.Quantity:0.#}× {i.ProductName}");
            var text = string.Join(" · ", names);
            return Items.Count > 3 ? $"{text} +{Items.Count - 3}" : text;
        }
    }

    public string? WhatsAppNumber => NormalizePhone(CustomerPhone);

    public static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        if (digits.StartsWith('0') && digits.Length >= 10)
        {
            digits = "92" + digits[1..];
        }

        return digits.Length >= 10 ? digits : null;
    }
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
