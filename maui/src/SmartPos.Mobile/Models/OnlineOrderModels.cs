using System.Text.Json.Serialization;

namespace SmartPos.Mobile.Models;

public class OnlineBranchDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("isRestaurant")]
    public bool IsRestaurant { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }
}

public class OnlineStoreHeaderDto
{
    [JsonPropertyName("branchId")]
    public int BranchId { get; set; }

    [JsonPropertyName("branchName")]
    public string? BranchName { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("currencyFormat")]
    public string? CurrencyFormat { get; set; }

    [JsonPropertyName("baseDeliveryFee")]
    public decimal BaseDeliveryFee { get; set; }

    [JsonPropertyName("isDeliveryEnabled")]
    public bool IsDeliveryEnabled { get; set; }

    [JsonPropertyName("freeDeliveryThreshold")]
    public decimal FreeDeliveryThreshold { get; set; }

    [JsonPropertyName("deliveryAreaNote")]
    public string? DeliveryAreaNote { get; set; }

    [JsonPropertyName("isRestaurant")]
    public bool IsRestaurant { get; set; }

    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = new();
}

public class OnlineProductDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("categoryName")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("categoryId")]
    public int? CategoryId { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("currencyFormat")]
    public string? CurrencyFormat { get; set; }

    [JsonPropertyName("imagePath")]
    public string? ImagePath { get; set; }

    [JsonPropertyName("inStock")]
    public bool InStock { get; set; }

    [JsonPropertyName("stockQuantity")]
    public decimal StockQuantity { get; set; }

    [JsonPropertyName("isFreeDelivery")]
    public bool IsFreeDelivery { get; set; }

    [JsonPropertyName("isDeal")]
    public bool IsDeal { get; set; }

    [JsonPropertyName("cardTag")]
    public string? CardTag { get; set; }

    public string? ImageUrl { get; set; }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);

    public string PriceText => Price.ToString("0.00");

    public string PriceDisplay => $"Rs {Price:0}";

    public int CartQty { get; set; }

    public bool InCart => CartQty > 0;

    public string CartQtyText => CartQty.ToString();

    public string DisplayTag
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(CardTag))
            {
                return CardTag!;
            }

            if (IsDeal)
            {
                return "DEAL";
            }

            if (IsFreeDelivery)
            {
                return "FREE DELIVERY";
            }

            return string.Empty;
        }
    }

    public bool HasTag => !string.IsNullOrWhiteSpace(DisplayTag);

    public string Initials
    {
        get
        {
            var name = (Name ?? "P").Trim();
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
            }

            return name.Length >= 2 ? name[..2].ToUpperInvariant() : name.ToUpperInvariant();
        }
    }
}

public class DeliveryAreaDto : EntityDto
{
    [JsonPropertyName("branchId")]
    public int BranchId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("deliveryFee")]
    public decimal DeliveryFee { get; set; }

    [JsonPropertyName("minimumOrderAmount")]
    public decimal MinimumOrderAmount { get; set; }

    [JsonPropertyName("estimatedDeliveryTime")]
    public string? EstimatedDeliveryTime { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}

public class CreateCustomerOrderDto
{
    [JsonPropertyName("customerId")]
    public int CustomerId { get; set; }

    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("customerMobile")]
    public string? CustomerMobile { get; set; }

    [JsonPropertyName("branchId")]
    public int? BranchId { get; set; }

    [JsonPropertyName("orderDate")]
    public DateTime OrderDate { get; set; } = DateTime.Now;

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("deliveryAddress")]
    public string? DeliveryAddress { get; set; }

    [JsonPropertyName("deliveryLandmark")]
    public string? DeliveryLandmark { get; set; }

    [JsonPropertyName("deliveryAreaId")]
    public int? DeliveryAreaId { get; set; }

    [JsonPropertyName("deliveryArea")]
    public string? DeliveryArea { get; set; }

    [JsonPropertyName("subtotal")]
    public decimal? Subtotal { get; set; }

    [JsonPropertyName("deliveryCharges")]
    public decimal? DeliveryCharges { get; set; }

    [JsonPropertyName("grandTotal")]
    public decimal? GrandTotal { get; set; }

    [JsonPropertyName("lines")]
    public List<CreateCustomerOrderLineDto> Lines { get; set; } = new();
}

public class CreateCustomerOrderLineDto
{
    [JsonPropertyName("productId")]
    public int ProductId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }
}

public class CustomerOrderDto : EntityDto
{
    [JsonPropertyName("branchId")]
    public int BranchId { get; set; }

    [JsonPropertyName("branchName")]
    public string? BranchName { get; set; }

    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("customerPhone")]
    public string? CustomerPhone { get; set; }

    [JsonPropertyName("orderDate")]
    public DateTime OrderDate { get; set; }

    [JsonPropertyName("orderNo")]
    public string? OrderNo { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("statusName")]
    public string? StatusName { get; set; }

    [JsonPropertyName("subTotal")]
    public decimal SubTotal { get; set; }

    [JsonPropertyName("deliveryFee")]
    public decimal DeliveryFee { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("deliveryAreaName")]
    public string? DeliveryAreaName { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("riderName")]
    public string? RiderName { get; set; }

    [JsonPropertyName("lines")]
    public List<CustomerOrderLineDto> Lines { get; set; } = new();
}

public class CustomerOrderLineDto : EntityDto
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
}

public class CartLine
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; } = 1;
    public string? ImagePath { get; set; }
    public string? SelectedModifiers { get; set; }
    public string? KitchenNotes { get; set; }
    public int? LineId { get; set; }
    public bool IsSentToKitchen { get; set; }
    public decimal PrintedQuantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
    public string LineTotalText => LineTotal.ToString("0.00");
    public string UnitPriceText => UnitPrice.ToString("0.00");
}
