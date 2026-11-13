using System.Text.Json.Serialization;

namespace SmartPos.Mobile.Models;

public class RestaurantTableDto : EntityDto
{
    [JsonPropertyName("branchId")]
    public int BranchId { get; set; }

    [JsonPropertyName("tableNo")]
    public string? TableNo { get; set; }

    [JsonPropertyName("area")]
    public string? Area { get; set; }

    [JsonPropertyName("capacity")]
    public int Capacity { get; set; }

    [JsonPropertyName("isCarBay")]
    public bool IsCarBay { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("statusName")]
    public string? StatusName { get; set; }

    [JsonPropertyName("currentOrderId")]
    public int? CurrentOrderId { get; set; }

    [JsonPropertyName("currentVehicleNumber")]
    public string? CurrentVehicleNumber { get; set; }

    [JsonPropertyName("waiterId")]
    public int? WaiterId { get; set; }

    [JsonPropertyName("waiterName")]
    public string? WaiterName { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    public bool IsFree => Status == 0;
    public bool IsOccupied => Status == 1;
    public bool IsBilled => Status == 2;

    public string DisplayTitle =>
        IsCarBay
            ? (string.IsNullOrWhiteSpace(CurrentVehicleNumber) ? (TableNo ?? "Car bay") : CurrentVehicleNumber)
            : (string.IsNullOrWhiteSpace(TableNo) ? $"Table #{Id}" : TableNo);

    public string StatusDisplay =>
        !string.IsNullOrWhiteSpace(StatusName) && !string.Equals(StatusName, "Free", StringComparison.OrdinalIgnoreCase)
            ? StatusName!
            : Status switch
            {
                0 => "Available",
                1 => "Occupied",
                2 => "Billed",
                3 => "Reserved",
                _ => $"Status {Status}"
            };

    public string WaiterDisplay =>
        string.IsNullOrWhiteSpace(WaiterName) ? "Unassigned" : WaiterName!;

    public string AreaDisplay =>
        string.IsNullOrWhiteSpace(Area) ? (IsCarBay ? "Car Parking" : "Dining") : Area!;

    public string TypeLabel => IsCarBay ? "CAR" : "TABLE";

    public string CapacityLine => Capacity > 0 ? $"{Capacity} seats" : (IsCarBay ? "Vehicle bay" : "Open seating");

    public string MetaLine => $"{AreaDisplay}  ·  {CapacityLine}";
}

public class RestaurantOrderDto : EntityDto
{
    [JsonPropertyName("branchId")]
    public int BranchId { get; set; }

    [JsonPropertyName("orderNo")]
    public string? OrderNo { get; set; }

    [JsonPropertyName("tableId")]
    public int? TableId { get; set; }

    [JsonPropertyName("tableNo")]
    public string? TableNo { get; set; }

    [JsonPropertyName("orderType")]
    public int OrderType { get; set; }

    [JsonPropertyName("carName")]
    public string? CarName { get; set; }

    [JsonPropertyName("carNumber")]
    public string? CarNumber { get; set; }

    [JsonPropertyName("carLocation")]
    public string? CarLocation { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("statusName")]
    public string? StatusName { get; set; }

    [JsonPropertyName("customerId")]
    public int? CustomerId { get; set; }

    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("waiterName")]
    public string? WaiterName { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("subTotal")]
    public decimal SubTotal { get; set; }

    [JsonPropertyName("discountPercent")]
    public decimal DiscountPercent { get; set; }

    [JsonPropertyName("discountAmount")]
    public decimal DiscountAmount { get; set; }

    [JsonPropertyName("taxPercent")]
    public decimal TaxPercent { get; set; }

    [JsonPropertyName("taxAmount")]
    public decimal TaxAmount { get; set; }

    [JsonPropertyName("serviceChargePercent")]
    public decimal ServiceChargePercent { get; set; }

    [JsonPropertyName("serviceChargeAmount")]
    public decimal ServiceChargeAmount { get; set; }

    [JsonPropertyName("grandTotal")]
    public decimal GrandTotal { get; set; }

    [JsonPropertyName("saleId")]
    public int? SaleId { get; set; }

    [JsonPropertyName("creationTime")]
    public DateTime CreationTime { get; set; }

    [JsonPropertyName("lines")]
    public List<RestaurantOrderLineDto> Lines { get; set; } = new();

    public string GrandTotalText => GrandTotal.ToString("0.00");
}

public class RestaurantOrderLineDto : EntityDto
{
    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }

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

    [JsonPropertyName("kitchenNotes")]
    public string? KitchenNotes { get; set; }

    [JsonPropertyName("selectedModifiers")]
    public string? SelectedModifiers { get; set; }

    [JsonPropertyName("isSentToKitchen")]
    public bool IsSentToKitchen { get; set; }

    [JsonPropertyName("printedQuantity")]
    public decimal PrintedQuantity { get; set; }
}

public class CreateOrUpdateRestaurantOrderDto
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("branchId")]
    public int BranchId { get; set; }

    [JsonPropertyName("tableId")]
    public int? TableId { get; set; }

    [JsonPropertyName("tableNo")]
    public string? TableNo { get; set; }

    [JsonPropertyName("orderType")]
    public int OrderType { get; set; }

    [JsonPropertyName("carName")]
    public string? CarName { get; set; }

    [JsonPropertyName("carNumber")]
    public string? CarNumber { get; set; }

    [JsonPropertyName("carLocation")]
    public string? CarLocation { get; set; }

    [JsonPropertyName("customerId")]
    public int? CustomerId { get; set; }

    [JsonPropertyName("waiterName")]
    public string? WaiterName { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("discountPercent")]
    public decimal DiscountPercent { get; set; }

    [JsonPropertyName("discountAmount")]
    public decimal DiscountAmount { get; set; }

    [JsonPropertyName("taxPercent")]
    public decimal TaxPercent { get; set; }

    [JsonPropertyName("serviceChargePercent")]
    public decimal ServiceChargePercent { get; set; }

    [JsonPropertyName("lines")]
    public List<CreateOrUpdateRestaurantOrderLineDto> Lines { get; set; } = new();
}

public class CreateOrUpdateRestaurantOrderLineDto
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("productId")]
    public int ProductId { get; set; }

    [JsonPropertyName("productName")]
    public string? ProductName { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("kitchenNotes")]
    public string? KitchenNotes { get; set; }

    [JsonPropertyName("selectedModifiers")]
    public string? SelectedModifiers { get; set; }

    [JsonPropertyName("isSentToKitchen")]
    public bool IsSentToKitchen { get; set; }

    [JsonPropertyName("printedQuantity")]
    public decimal PrintedQuantity { get; set; }
}

public class OpenTableSessionDto
{
    [JsonPropertyName("branchId")]
    public int BranchId { get; set; }

    [JsonPropertyName("tableId")]
    public int TableId { get; set; }

    [JsonPropertyName("waiterName")]
    public string? WaiterName { get; set; }

    [JsonPropertyName("carNumber")]
    public string? CarNumber { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public class SendToKitchenResultDto
{
    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }

    [JsonPropertyName("orderNo")]
    public string? OrderNo { get; set; }

    [JsonPropertyName("hasNewItems")]
    public bool HasNewItems { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("newItems")]
    public List<KitchenSlipItemDto> NewItems { get; set; } = new();
}

public class KitchenSlipItemDto
{
    [JsonPropertyName("lineId")]
    public int? LineId { get; set; }

    [JsonPropertyName("productName")]
    public string? ProductName { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("kitchenNotes")]
    public string? KitchenNotes { get; set; }

    [JsonPropertyName("selectedModifiers")]
    public string? SelectedModifiers { get; set; }
}

public class ConfirmKotPrintDto
{
    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }

    [JsonPropertyName("lineIds")]
    public List<int> LineIds { get; set; } = new();
}

public class SettleRestaurantOrderDto
{
    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }

    [JsonPropertyName("customerId")]
    public int CustomerId { get; set; }

    [JsonPropertyName("paymentType")]
    public int PaymentType { get; set; } = 1;

    [JsonPropertyName("cashAmount")]
    public decimal CashAmount { get; set; }

    [JsonPropertyName("cardAmount")]
    public decimal CardAmount { get; set; }

    [JsonPropertyName("discountAmount")]
    public decimal DiscountAmount { get; set; }

    [JsonPropertyName("discountPercent")]
    public decimal DiscountPercent { get; set; }

    [JsonPropertyName("taxPercent")]
    public decimal TaxPercent { get; set; }

    [JsonPropertyName("serviceChargePercent")]
    public decimal ServiceChargePercent { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public class SplitBillDto
{
    [JsonPropertyName("sourceOrderId")]
    public int SourceOrderId { get; set; }

    [JsonPropertyName("numberOfSplits")]
    public int NumberOfSplits { get; set; } = 2;

    [JsonPropertyName("splitEqually")]
    public bool SplitEqually { get; set; } = true;

    [JsonPropertyName("moveLineIds")]
    public List<int> MoveLineIds { get; set; } = new();
}

public class AssignWaiterDto
{
    [JsonPropertyName("tableId")]
    public int TableId { get; set; }

    [JsonPropertyName("waiterId")]
    public int? WaiterId { get; set; }

    [JsonPropertyName("waiterName")]
    public string? WaiterName { get; set; }
}

public class ProductDto : EntityDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("stockQuantity")]
    public decimal StockQuantity { get; set; }

    [JsonPropertyName("categoryId")]
    public int CategoryId { get; set; }

    [JsonPropertyName("categoryName")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("imagePath")]
    public string? ImagePath { get; set; }

    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }

    /// <summary>Absolute URL for binding (filled by app after API load).</summary>
    public string? ImageUrl { get; set; }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);

    public string PriceText => Price.ToString("0.00");

    /// <summary>Qty currently in the waiter/customer cart (UI badge).</summary>
    public int CartQty { get; set; }

    public bool InCart => CartQty > 0;

    public string CartQtyText => CartQty.ToString();

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

public class ProductModifierGroupDto : EntityDto
{
    [JsonPropertyName("productId")]
    public int ProductId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("minSelection")]
    public int MinSelection { get; set; }

    [JsonPropertyName("maxSelection")]
    public int MaxSelection { get; set; } = 1;

    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; }

    [JsonPropertyName("allowMultiple")]
    public bool AllowMultiple { get; set; }

    [JsonPropertyName("options")]
    public List<ProductModifierOptionDto> Options { get; set; } = new();
}

public class ProductModifierOptionDto : EntityDto
{
    [JsonPropertyName("modifierGroupId")]
    public int ModifierGroupId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("extraPrice")]
    public decimal? ExtraPrice { get; set; }

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    public decimal EffectivePrice => ExtraPrice is > 0 ? ExtraPrice.Value : Price;
}

