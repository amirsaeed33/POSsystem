using CommunityToolkit.Mvvm.ComponentModel;

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

    public Color CardBorderColor => IsSelected ? Color.FromArgb("#10B981") : Color.FromArgb("#E5E7EB");
    public Color CardBgColor => IsSelected ? Color.FromArgb("#ECFDF5") : Colors.White;
    public double CardStrokeThickness => IsSelected ? 1.5 : 1.0;
    public string CheckmarkGlyph => IsSelected ? "✓" : string.Empty;
    public Color CheckmarkBg => IsSelected ? Color.FromArgb("#10B981") : Colors.Transparent;
    public Color CheckmarkColor => IsSelected ? Colors.White : Colors.Transparent;
    public Color RadioBorderColor => IsSelected ? Color.FromArgb("#10B981") : Color.FromArgb("#D1D5DB");
    public Color TextColor => IsSelected ? Color.FromArgb("#065F46") : Color.FromArgb("#111827");
}
