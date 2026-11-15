using CommunityToolkit.Mvvm.ComponentModel;
using SmartPos.Mobile.Helpers;

namespace SmartPos.Mobile.Models;

/// <summary>UI choice row/card for Aljannat product detail hospitality sections.</summary>
public partial class ProductDetailChoiceItem : ObservableObject
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public decimal Price { get; set; }

    [ObservableProperty]
    private bool isSelected;

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(CardBgColor));
        OnPropertyChanged(nameof(CardBorderColor));
        OnPropertyChanged(nameof(CardStrokeThickness));
        OnPropertyChanged(nameof(TitleColor));
        OnPropertyChanged(nameof(SubtitleColor));
        OnPropertyChanged(nameof(PriceColor));
        OnPropertyChanged(nameof(RadioFillColor));
        OnPropertyChanged(nameof(RadioBorderColor));
        OnPropertyChanged(nameof(CheckBgColor));
        OnPropertyChanged(nameof(CheckGlyph));
        OnPropertyChanged(nameof(RowBgColor));
    }

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
    public string PriceLabel => Price > 0 ? $"+ Rs. {Price:0}" : "Included";

    public Color CardBgColor => IsSelected ? ThemeColors.PrimaryLight : ThemeColors.White;
    public Color CardBorderColor => IsSelected ? ThemeColors.Primary : ThemeColors.BorderSubtle;
    public double CardStrokeThickness => IsSelected ? 1.5 : 1.0;
    public Color TitleColor => IsSelected ? ThemeColors.Primary : ThemeColors.TextPrimary;
    public Color SubtitleColor => IsSelected ? ThemeColors.PrimaryDark : ThemeColors.Gray400;
    public Color PriceColor => IsSelected ? ThemeColors.Primary : ThemeColors.TextSecondary;

    public Color RadioFillColor => IsSelected ? ThemeColors.Primary : Colors.Transparent;
    public Color RadioBorderColor => IsSelected ? ThemeColors.Primary : ThemeColors.BorderSubtle;

    public Color CheckBgColor => IsSelected ? ThemeColors.Primary : Colors.Transparent;
    public string CheckGlyph => IsSelected ? "✓" : string.Empty;
    public Color RowBgColor => IsSelected ? ThemeColors.PrimaryLight : ThemeColors.White;
}
