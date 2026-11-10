using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

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

    [JsonPropertyName("imagePath")]
    public string? ImagePath { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("website")]
    public string? Website { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    public string? LogoUrl { get; set; }

    public bool HasLogo => !string.IsNullOrWhiteSpace(LogoUrl);
    public bool HasAddress => !string.IsNullOrWhiteSpace(Address);
    public bool HasPhone => !string.IsNullOrWhiteSpace(Phone);
    public bool HasEmail => !string.IsNullOrWhiteSpace(Email);
    public bool HasWebsite => !string.IsNullOrWhiteSpace(Website);
    public string BusinessTypeLabel => IsRestaurant ? "Restaurant" : "Store";

    public string Initials
    {
        get
        {
            var name = (Name ?? "B").Trim();
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
            }

            return name.Length >= 2 ? name[..2].ToUpperInvariant() : name.ToUpperInvariant();
        }
    }
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

public class OnlineProductDto : ObservableObject
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

    [JsonPropertyName("dealId")]
    public int? DealId { get; set; }

    [JsonPropertyName("originalPrice")]
    public decimal? OriginalPrice { get; set; }

    [JsonPropertyName("dealItemsSummary")]
    public string? DealItemsSummary { get; set; }

    public bool HasDealSummary => !string.IsNullOrWhiteSpace(DealItemsSummary);
    public bool HasOriginalPrice => OriginalPrice.HasValue && OriginalPrice.Value > Price;
    public string OriginalPriceDisplay => OriginalPrice.HasValue ? $"Rs {OriginalPrice.Value:0}" : string.Empty;
    public string DealDiscountBadge
    {
        get
        {
            if (OriginalPrice.HasValue && OriginalPrice.Value > Price && OriginalPrice.Value > 0)
            {
                var pct = (int)Math.Round((OriginalPrice.Value - Price) / OriginalPrice.Value * 100);
                if (pct > 0) return $"SAVE {pct}%";
            }
            return "HOT DEAL";
        }
    }

    public string? ImageUrl { get; set; }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);

    public string PriceText => Price.ToString("0.00");

    public string PriceDisplay => $"Rs {Price:0}";
    public string PriceLabel => PriceDisplay;

    private int cartQty;
    public int CartQty
    {
        get => cartQty;
        set
        {
            if (SetProperty(ref cartQty, value))
            {
                OnPropertyChanged(nameof(InCart));
                OnPropertyChanged(nameof(CartQtyText));
            }
        }
    }

    public bool InCart => CartQty > 0;

    public string CartQtyText => CartQty.ToString();

    private bool isFavorite;
    public bool IsFavorite
    {
        get => isFavorite;
        set
        {
            if (SetProperty(ref isFavorite, value))
            {
                OnPropertyChanged(nameof(FavoriteFillColor));
                OnPropertyChanged(nameof(FavoriteStrokeColor));
            }
        }
    }

    public Color FavoriteFillColor => IsFavorite ? Color.FromArgb("#E11D48") : Colors.Transparent;
    public Color FavoriteStrokeColor => IsFavorite ? Color.FromArgb("#E11D48") : Color.FromArgb("#64748B");

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

    [JsonPropertyName("paymentMethod")]
    public string PaymentMethod { get; set; } = "CashOnDelivery";

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

    [JsonPropertyName("riderPhone")]
    public string? RiderPhone { get; set; }

    [JsonPropertyName("customerRating")]
    public int? CustomerRating { get; set; }

    [JsonPropertyName("customerAddress")]
    public string? CustomerAddress { get; set; }

    [JsonPropertyName("deliveryAreaId")]
    public int? DeliveryAreaId { get; set; }

    [JsonPropertyName("paymentMethod")]
    public string? PaymentMethod { get; set; }

    [JsonPropertyName("customerFeedbackComment")]
    public string? CustomerFeedbackComment { get; set; }

    [JsonPropertyName("lines")]
    public List<CustomerOrderLineDto> Lines { get; set; } = new();

    // === UI Presentation Helpers for Order History ===

    public string FormattedOrderNo => string.IsNullOrWhiteSpace(OrderNo) ? $"Order #{Id}" : OrderNo!;

    public string FormattedDate => OrderDate != default 
        ? OrderDate.ToString("dd MMM yyyy, hh:mm tt") 
        : string.Empty;

    public string FormattedTotal => $"Rs {TotalAmount:0.##}";

    public int TotalItemsCount => (int)Lines.Sum(l => l.Quantity);

    public string ItemsCountBadge => $"{TotalItemsCount} {(TotalItemsCount == 1 ? "item" : "items")}";

    public string ItemsSummary
    {
        get
        {
            if (Lines == null || Lines.Count == 0)
            {
                return "No items";
            }

            var parts = Lines
                .Where(l => !string.IsNullOrWhiteSpace(l.ProductName))
                .Select(l => $"{(int)l.Quantity}x {l.ProductName}")
                .ToList();

            return parts.Count > 0 ? string.Join(", ", parts) : $"{Lines.Count} items";
        }
    }

    public string BranchAndAreaSummary
    {
        get
        {
            var area = DeliveryAreaName?.Trim();
            var branch = BranchName?.Trim();
            if (!string.IsNullOrWhiteSpace(branch) && !string.IsNullOrWhiteSpace(area))
            {
                return $"{branch} • {area}";
            }
            return branch ?? area ?? "Direct Order";
        }
    }

    public string StatusDisplayName => Status switch
    {
        0 => "Pending",
        1 => "Accepted",
        2 => "Cancelled",
        3 => "Preparing",
        4 => "Delivered",
        5 => "Completed",
        _ => !string.IsNullOrWhiteSpace(StatusName) ? StatusName : $"Status {Status}"
    };

    public Color StatusTextColor => Status switch
    {
        0 => Color.FromArgb("#B45309"), // Dark Amber
        1 => Color.FromArgb("#1D4ED8"), // Dark Blue
        2 => Color.FromArgb("#B91C1C"), // Dark Red
        3 => Color.FromArgb("#6D28D9"), // Dark Purple
        4 or 5 => Color.FromArgb("#047857"), // Dark Emerald Green
        _ => Color.FromArgb("#374151")
    };

    public Color StatusBadgeBgColor => Status switch
    {
        0 => Color.FromArgb("#FEF3C7"), // Light Amber
        1 => Color.FromArgb("#DBEAFE"), // Light Blue
        2 => Color.FromArgb("#FEE2E2"), // Light Red
        3 => Color.FromArgb("#EDE9FE"), // Light Purple
        4 or 5 => Color.FromArgb("#D1FAE5"), // Light Emerald
        _ => Color.FromArgb("#F3F4F6")
    };

    public bool CanReorder => Lines != null && Lines.Any(l => l.ProductId > 0 && l.Quantity > 0);
}

public class RateCustomerOrderInput
{
    [JsonPropertyName("orderNo")]
    public string OrderNo { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public int Rating { get; set; } = 5;

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
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
    public bool HasModifiers => !string.IsNullOrWhiteSpace(SelectedModifiers);
}


public class ToggleFavoriteResultDto
{
    [JsonPropertyName("productId")]
    public int ProductId { get; set; }

    [JsonPropertyName("isFavorite")]
    public bool IsFavorite { get; set; }
}

public partial class CustomerSavedAddressDto : ObservableObject
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("customerId")]
    public int CustomerId { get; set; }

    [JsonPropertyName("ownerId")]
    public int OwnerId { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("landmark")]
    public string? Landmark { get; set; }

    [JsonPropertyName("deliveryAreaId")]
    public int? DeliveryAreaId { get; set; }

    [JsonPropertyName("deliveryAreaName")]
    public string? DeliveryAreaName { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    [ObservableProperty]
    [property: JsonIgnore]
    private bool isSelected;

    [JsonIgnore]
    public string DisplayIcon => (Label?.Trim().ToLowerInvariant()) switch
    {
        "home" or "ghar" => "🏠",
        "office" or "work" or "daftar" => "🏢",
        "shop" or "store" => "🏬",
        _ => "📍"
    };

    [JsonIgnore]
    public string DisplayLabel => string.IsNullOrWhiteSpace(Label) ? "Address" : Label;

    [JsonIgnore]
    public string AddressPreview
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Address)) return string.Empty;
            return Address.Length > 28 ? Address[..25] + "..." : Address;
        }
    }

    [JsonIgnore]
    public string FullAddressDisplay
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Landmark))
            {
                return $"{Address} (Near: {Landmark})";
            }
            return Address;
        }
    }

    [JsonIgnore]
    public bool HasLandmark => !string.IsNullOrWhiteSpace(Landmark);

    [JsonIgnore]
    public bool IsNotDefault => !IsDefault;
}

public class SaveCustomerAddressInput
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("customerId")]
    public int? CustomerId { get; set; }

    [JsonPropertyName("branchId")]
    public int? BranchId { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("landmark")]
    public string? Landmark { get; set; }

    [JsonPropertyName("deliveryAreaId")]
    public int? DeliveryAreaId { get; set; }

    [JsonPropertyName("deliveryAreaName")]
    public string? DeliveryAreaName { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }
}

public class DeleteCustomerSavedAddressInput
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("branchId")]
    public int? BranchId { get; set; }
}

public class OnlineCustomerProfileDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("branchId")]
    public int BranchId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("imagePath")]
    public string? ImagePath { get; set; }

    [JsonPropertyName("savedAddresses")]
    public List<CustomerSavedAddressDto> SavedAddresses { get; set; } = new();
}

public class UpsertOnlineCustomerProfileInput
{
    [JsonPropertyName("branchId")]
    public int? BranchId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("imageBase64")]
    public string? ImageBase64 { get; set; }
}
