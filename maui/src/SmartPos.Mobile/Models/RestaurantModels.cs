using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace SmartPos.Mobile.Models;

public class RestaurantTableDto : EntityDto, INotifyPropertyChanged
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

    [JsonPropertyName("orderStartedAt")]
    public DateTime? OrderStartedAt { get; set; }

    public string ElapsedText { get; private set; } = string.Empty;

    public bool HasElapsed => !string.IsNullOrEmpty(ElapsedText);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshElapsed()
    {
        if (IsFree || OrderStartedAt is not DateTime started || started == default)
        {
            SetElapsed(string.Empty);
            return;
        }

        if (started.Kind == DateTimeKind.Utc)
        {
            started = started.ToLocalTime();
        }
        else if (started.Kind == DateTimeKind.Unspecified)
        {
            started = DateTime.SpecifyKind(started, DateTimeKind.Utc).ToLocalTime();
        }

        var elapsed = DateTime.Now - started;
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        var totalSeconds = (int)elapsed.TotalSeconds;
        SetElapsed($"{totalSeconds / 60:00}:{totalSeconds % 60:00}");
        OnPropertyChanged(nameof(ElapsedFloorText));
        OnPropertyChanged(nameof(ElapsedFloorColor));
        OnPropertyChanged(nameof(CardElapsedDisplay));
    }

    private void SetElapsed(string value)
    {
        if (ElapsedText == value)
        {
            return;
        }

        ElapsedText = value;
        OnPropertyChanged(nameof(ElapsedText));
        OnPropertyChanged(nameof(HasElapsed));
        OnPropertyChanged(nameof(ElapsedFloorText));
        OnPropertyChanged(nameof(ElapsedFloorColor));
        OnPropertyChanged(nameof(CardElapsedDisplay));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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

    public string FloorStatusLabel => Status switch
    {
        0 => "FREE",
        1 => "BUSY",
        2 => "BILLED",
        3 => "RESERVED",
        _ => "FREE"
    };

    public string TypeEyebrow
    {
        get
        {
            if (IsCarBay)
            {
                return "CAR BAY";
            }

            var area = AreaDisplay.Trim();
            if (area.Contains("Lounge", StringComparison.OrdinalIgnoreCase))
            {
                return "MUGHAL LOUNGE";
            }

            if (area.Contains("Family", StringComparison.OrdinalIgnoreCase))
            {
                return "FAMILY BOOTH";
            }

            if (area.Contains("Majlis", StringComparison.OrdinalIgnoreCase))
            {
                return "VIP MAJLIS";
            }

            if (area.Contains("Court", StringComparison.OrdinalIgnoreCase))
            {
                return "COURTYARD";
            }

            return string.IsNullOrWhiteSpace(area) ? "DINING TABLE" : area.ToUpperInvariant();
        }
    }

    public string CapacityMeta
    {
        get
        {
            var seats = Capacity > 0 ? $"{Capacity} Guests" : "Open seating";
            var zone = AreaDisplay;
            if (zone.Contains("Lounge", StringComparison.OrdinalIgnoreCase))
            {
                return $"{seats}  •  Window";
            }

            return $"{seats}  •  {zone}";
        }
    }

    public string CapacityShort => Capacity > 0 ? $"{Capacity}p" : "—";

    public string CardTrailingValue => HasRunningBill ? RunningBillText : CapacityShort;

    public string CardTrailingColor => HasRunningBill ? "#C84B11" : "#8C7E74";

    public string CardElapsedDisplay =>
        IsFree ? "Ready to seat" : (string.IsNullOrWhiteSpace(ElapsedFloorText) || ElapsedFloorText == "Available now"
            ? "Just seated"
            : $"{ElapsedFloorText} seated");

    public string ElapsedFloorText
    {
        get
        {
            if (IsFree || string.IsNullOrWhiteSpace(ElapsedText))
            {
                return "Available now";
            }

            if (ElapsedText.Contains(':'))
            {
                var parts = ElapsedText.Split(':');
                if (parts.Length >= 2 && int.TryParse(parts[0], out var mins))
                {
                    return $"{mins} min";
                }
            }

            return ElapsedText;
        }
    }

    public string ElapsedFloorColor => IsFree ? "#8C7E74" : "#C84B11";

    private string? _assignedWaiterLabel;

    public string AssignedWaiterLabel
    {
        get => _assignedWaiterLabel ?? WaiterFloorDisplayDefault;
        set
        {
            if (_assignedWaiterLabel == value)
            {
                return;
            }

            _assignedWaiterLabel = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(WaiterFloorDisplay));
        }
    }

    private string WaiterFloorDisplayDefault =>
        string.IsNullOrWhiteSpace(WaiterName) ? "Unassigned" : WaiterName!;

    public string WaiterFloorDisplay => AssignedWaiterLabel;

    private decimal _runningBillAmount;

    public decimal RunningBillAmount
    {
        get => _runningBillAmount;
        set
        {
            if (_runningBillAmount == value)
            {
                return;
            }

            _runningBillAmount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RunningBillText));
            OnPropertyChanged(nameof(HasRunningBill));
            OnPropertyChanged(nameof(CardTrailingValue));
            OnPropertyChanged(nameof(CardTrailingColor));
        }
    }

    public bool HasRunningBill => !IsFree && RunningBillAmount > 0;

    public string RunningBillText => HasRunningBill
        ? $"Rs. {RunningBillAmount:N0}"
        : string.Empty;

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }
}

public class WaiterKotLineItem
{
    public string QuantityLabel { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string LineTotalText { get; set; } = string.Empty;
    public string DisplayLine => $"{QuantityLabel} {ProductName} — {LineTotalText}";
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

    [JsonPropertyName("customerId")]
    public int? CustomerId { get; set; }

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

public class ChangeTableStatusDto
{
    [JsonPropertyName("tableId")]
    public int TableId { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("currentOrderId")]
    public int? CurrentOrderId { get; set; }
}

public class PosCustomerDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("customerType")]
    public int CustomerType { get; set; }

    public string Display =>
        string.IsNullOrWhiteSpace(Phone) ? (Name ?? "Customer") : $"{Name}  ·  {Phone}";
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

    public string PriceDisplayRs => $"Rs. {Price:N0}";

    public string ItemCodeText =>
        !string.IsNullOrWhiteSpace(Barcode)
            ? Barcode.Trim().ToUpperInvariant()
            : $"ITM-{Id:D2}";

    public string PortionMetaText =>
        !string.IsNullOrWhiteSpace(Description)
            ? Description.Trim()
            : (!string.IsNullOrWhiteSpace(CategoryName) ? CategoryName!.Trim() : "Chef's special");

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

