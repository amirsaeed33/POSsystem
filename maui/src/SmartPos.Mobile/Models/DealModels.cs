using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartPos.Mobile.Models;

public class DealDto : EntityDto
{
    [JsonPropertyName("branchId")]
    public int BranchId { get; set; }

    [JsonPropertyName("branchName")]
    public string? BranchName { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("dealPrice")]
    public decimal DealPrice { get; set; }

    public decimal Price
    {
        get => DealPrice;
        set => DealPrice = value;
    }

    [JsonPropertyName("expiryDate")]
    public DateTime? ExpiryDate { get; set; }

    [JsonPropertyName("isExpired")]
    public bool IsExpired { get; set; }

    [JsonPropertyName("categoryType")]
    public string? CategoryType { get; set; }

    [JsonPropertyName("categoryId")]
    public int? CategoryId { get; set; }

    [JsonPropertyName("categoryName")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("productId")]
    public int? ProductId { get; set; }

    [JsonPropertyName("imagePath")]
    public string? ImagePath { get; set; }

    [JsonPropertyName("items")]
    public List<DealDetailDto> Items { get; set; } = new();

    [JsonPropertyName("totalOriginalPrice")]
    public decimal TotalOriginalPrice { get; set; }

    public string? ImageUrl { get; set; }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
    public string DealPriceDisplay => $"Rs {DealPrice:0}";
    public string OriginalPriceDisplay => TotalOriginalPrice > DealPrice ? $"Rs {TotalOriginalPrice:0}" : string.Empty;
    public bool HasDiscount => TotalOriginalPrice > DealPrice;
    public string DiscountBadge
    {
        get
        {
            if (TotalOriginalPrice > DealPrice && TotalOriginalPrice > 0)
            {
                var pct = (int)Math.Round((TotalOriginalPrice - DealPrice) / TotalOriginalPrice * 100);
                if (pct > 0) return $"SAVE {pct}%";
            }
            return "HOT DEAL";
        }
    }
}

public class DealDetailDto : EntityDto
{
    [JsonPropertyName("dealId")]
    public int DealId { get; set; }

    [JsonPropertyName("productId")]
    public int ProductId { get; set; }

    [JsonPropertyName("productName")]
    public string? ProductName { get; set; }

    [JsonPropertyName("productBarcode")]
    public string? ProductBarcode { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 1;

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("subTotal")]
    public decimal SubTotal { get; set; }
}

public partial class DealCustomiseItem : ObservableObject
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public ObservableCollection<ModifierGroupItem> ModifierGroups { get; } = new();

    public string HeaderTitle => Quantity > 1 ? $"{Quantity}x {ProductName}" : ProductName;
    public bool HasModifierGroups => ModifierGroups.Count > 0;
}
