using CommunityToolkit.Mvvm.ComponentModel;

using SmartPos.Mobile.Helpers;

namespace SmartPos.Mobile.Models;

public partial class ModifierOptionItem : ObservableObject
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool AllowMultiple { get; set; }
    public bool IsRequired { get; set; }
    public int MaxSelection { get; set; } = 1;

    [ObservableProperty]
    private bool isSelected;

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(CardBorderColor));
        OnPropertyChanged(nameof(CardBgColor));
        OnPropertyChanged(nameof(CardStrokeThickness));
        OnPropertyChanged(nameof(CheckmarkGlyph));
        OnPropertyChanged(nameof(CheckmarkBg));
        OnPropertyChanged(nameof(CheckmarkColor));
        OnPropertyChanged(nameof(RadioBorderColor));
        OnPropertyChanged(nameof(TextColor));
        OnPropertyChanged(nameof(PriceTextColor));
        OnPropertyChanged(nameof(PillBgColor));
        OnPropertyChanged(nameof(PillTextColor));
        OnPropertyChanged(nameof(PillBorderColor));
    }

    public string PriceLabel => Price > 0 ? $"+Rs. {Price:0}" : "Included";
    public bool HasPrice => Price > 0;

    /// <summary>Zero-price multi-select options render as warm pills.</summary>
    public bool IsPillStyle => AllowMultiple && Price <= 0;

    public Color CardBorderColor => IsSelected ? ThemeColors.Primary : ThemeColors.BorderSubtle;
    public Color CardBgColor => IsSelected ? ThemeColors.PrimaryLight : ThemeColors.PageBackground;
    public double CardStrokeThickness => IsSelected ? 1.5 : 1.0;
    public string CheckmarkGlyph => IsSelected ? (AllowMultiple ? "✓" : string.Empty) : string.Empty;
    public Color CheckmarkBg => IsSelected ? ThemeColors.Primary : Colors.Transparent;
    public Color CheckmarkColor => IsSelected ? Colors.White : Colors.Transparent;
    public Color RadioBorderColor => IsSelected ? ThemeColors.Primary : ThemeColors.BorderSubtle;
    public Color TextColor => IsSelected ? ThemeColors.Primary : ThemeColors.TextPrimary;
    public Color PriceTextColor => IsSelected ? ThemeColors.Primary : ThemeColors.TextSecondary;

    public Color PillBgColor => IsSelected ? ThemeColors.Primary : ThemeColors.PageBackground;
    public Color PillTextColor => IsSelected ? Colors.White : ThemeColors.TextSecondary;
    public Color PillBorderColor => IsSelected ? ThemeColors.Primary : ThemeColors.BorderSubtle;
}
