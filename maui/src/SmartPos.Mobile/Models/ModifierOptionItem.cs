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
    }

    public string PriceLabel => Price > 0 ? $"+Rs {Price:0}" : "Included";
    public bool HasPrice => Price > 0;

    public Color CardBorderColor => IsSelected ? ThemeColors.Primary : ThemeColors.BorderSubtle;
    public Color CardBgColor => IsSelected ? ThemeColors.SuccessBackground : Colors.White;
    public double CardStrokeThickness => IsSelected ? 1.5 : 1.0;
    public string CheckmarkGlyph => IsSelected ? "✓" : string.Empty;
    public Color CheckmarkBg => IsSelected ? ThemeColors.Primary : Colors.Transparent;
    public Color CheckmarkColor => IsSelected ? Colors.White : Colors.Transparent;
    public Color RadioBorderColor => IsSelected ? ThemeColors.Primary : ThemeColors.Gray300;
    public Color TextColor => IsSelected ? ThemeColors.SuccessText : ThemeColors.TextPrimary;
}
