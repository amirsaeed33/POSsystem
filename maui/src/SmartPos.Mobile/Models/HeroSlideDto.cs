using System.Text.Json.Serialization;

namespace SmartPos.Mobile.Models;

public class HeroSlideDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("branchId")]
    public int BranchId { get; set; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }

    [JsonPropertyName("badge")]
    public string? Badge { get; set; }

    [JsonPropertyName("badgeIcon")]
    public string? BadgeIcon { get; set; }

    [JsonPropertyName("titleMain")]
    public string? TitleMain { get; set; }

    [JsonPropertyName("titleHighlight")]
    public string? TitleHighlight { get; set; }

    [JsonPropertyName("titleEnd")]
    public string? TitleEnd { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("offerPill")]
    public string? OfferPill { get; set; }

    [JsonPropertyName("rating")]
    public string? Rating { get; set; }

    [JsonPropertyName("primaryBtnText")]
    public string? PrimaryBtnText { get; set; }

    [JsonPropertyName("secondaryBtnText")]
    public string? SecondaryBtnText { get; set; }

    [JsonPropertyName("imagePath")]
    public string? ImagePath { get; set; }

    [JsonPropertyName("bgImagePath")]
    public string? BgImagePath { get; set; }

    public string? ImageUrl { get; set; }
    public string? BgImageUrl { get; set; }

    public bool HasBgImage => !string.IsNullOrWhiteSpace(BgImageUrl);

    public string DisplayBadgeIcon =>
        string.IsNullOrWhiteSpace(BadgeIcon) ? "🔥" : BadgeIcon!;

    public string DisplayBadge =>
        string.IsNullOrWhiteSpace(Badge) ? "FEATURED" : Badge!;

    public string DisplayOffer =>
        string.IsNullOrWhiteSpace(OfferPill) ? "Special offers available" : OfferPill!;

    public string DisplayRating =>
        string.IsNullOrWhiteSpace(Rating) ? "4.9★" : Rating!;

    public string DisplayPrimaryCta =>
        string.IsNullOrWhiteSpace(PrimaryBtnText) ? "Order Now" : PrimaryBtnText!;

    public string DisplaySecondaryCta =>
        string.IsNullOrWhiteSpace(SecondaryBtnText) ? "Explore Full Menu" : SecondaryBtnText!;

    public string DisplayDescription =>
        string.IsNullOrWhiteSpace(Description)
            ? "Fresh food, cooked to order and delivered hot."
            : Description!;
}
