using System.Text.Json.Serialization;

namespace SmartPos.Mobile.Models;

public class CustomerFeedbackDto
{
    [JsonPropertyName("orderId")]
    public long OrderId { get; set; }

    [JsonPropertyName("customerId")]
    public int? CustomerId { get; set; }

    [JsonPropertyName("orderNo")]
    public string? OrderNo { get; set; }

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = "Valued Customer";

    [JsonPropertyName("customerImagePath")]
    public string? CustomerImagePath { get; set; }

    [JsonPropertyName("customerPhotoUrl")]
    public string? CustomerPhotoUrl { get; set; }

    public bool HasPhoto => !string.IsNullOrWhiteSpace(CustomerPhotoUrl) || !string.IsNullOrWhiteSpace(CustomerImagePath);

    public string? OrderNumber
    {
        get => OrderNo;
        set => OrderNo = value;
    }

    public string? OrderedItemsSummary
    {
        get => ItemsSummary;
        set => ItemsSummary = value;
    }

    [JsonPropertyName("rating")]
    public int Rating { get; set; } = 5;

    [JsonPropertyName("orderDate")]
    public DateTime OrderDate { get; set; }

    [JsonPropertyName("branchName")]
    public string? BranchName { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("itemNames")]
    public List<string>? ItemNames { get; set; }

    [JsonPropertyName("itemsSummary")]
    public string? ItemsSummary { get; set; }

    public string FormattedDate =>
        OrderDate != default ? OrderDate.ToString("MMM dd, yyyy") : "Verified Order";

    public string DisplayComment =>
        !string.IsNullOrWhiteSpace(Comment)
            ? Comment
            : (!string.IsNullOrWhiteSpace(OrderNo)
                ? $"Rated order #{OrderNo} • Delicious fresh taste & super fast delivery!"
                : "Excellent food, generous portions, and wonderful service!");

    public string StarRatingDisplay
    {
        get
        {
            var stars = Math.Clamp(Rating, 1, 5);
            return new string('⭐', stars);
        }
    }

    public string Initials
    {
        get
        {
            var name = (CustomerName ?? "C").Trim();
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
            }

            return name.Length >= 2 ? name[..2].ToUpperInvariant() : name.ToUpperInvariant();
        }
    }

    public Color AvatarBgColor
    {
        get
        {
            var colors = new[]
            {
                Color.FromArgb("#0F6B4C"),
                Color.FromArgb("#0284C7"),
                Color.FromArgb("#D97706"),
                Color.FromArgb("#7C3AED"),
                Color.FromArgb("#E11D48"),
                Color.FromArgb("#059669")
            };
            var index = (int)(Math.Abs(OrderId) % colors.Length);
            return colors[index];
        }
    }

    public bool HasItemsSummary => !string.IsNullOrWhiteSpace(ItemsSummary);
    public string DisplayItemsSummary => $"Ordered: {ItemsSummary}";

    public string DisplayFoodPill =>
        !string.IsNullOrWhiteSpace(ItemsSummary)
            ? $"🍔 {ItemsSummary}"
            : "🍔 Delicious Customer Favorite";

    public Color Star1Color => Rating >= 1 ? Color.FromArgb("#F59E0B") : Color.FromArgb("#E5E7EB");
    public Color Star2Color => Rating >= 2 ? Color.FromArgb("#F59E0B") : Color.FromArgb("#E5E7EB");
    public Color Star3Color => Rating >= 3 ? Color.FromArgb("#F59E0B") : Color.FromArgb("#E5E7EB");
    public Color Star4Color => Rating >= 4 ? Color.FromArgb("#F59E0B") : Color.FromArgb("#E5E7EB");
    public Color Star5Color => Rating >= 5 ? Color.FromArgb("#F59E0B") : Color.FromArgb("#E5E7EB");

    public string StarGlyphs => new string('★', Math.Clamp(Rating, 1, 5));
    public string RatingText => $"{Math.Clamp(Rating, 1, 5)}.0";
}

