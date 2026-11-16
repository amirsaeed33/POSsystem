using CommunityToolkit.Mvvm.ComponentModel;

using SmartPos.Mobile.Helpers;

namespace SmartPos.Mobile.Models;

public partial class HomeCategoryDisplayDto : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string IconOrEmoji { get; set; } = "🍽️";
    public bool IsMoreButton { get; set; }
    public int GridRow { get; set; }
    public int GridColumn { get; set; }

    [ObservableProperty]
    private bool isSelected;

    public bool HasImageUrl => !IsMoreButton && !string.IsNullOrWhiteSpace(ImageUrl);
    public bool ShowEmoji => !IsMoreButton;

    public Color LabelColor => IsMoreButton
        ? ThemeColors.Primary
        : (IsSelected ? ThemeColors.Primary : ThemeColors.Gray600);

    public FontAttributes LabelFontAttributes =>
        (IsMoreButton || IsSelected) ? FontAttributes.Bold : FontAttributes.None;

    public Color CardBorderColor =>
        IsSelected ? ThemeColors.Primary : ThemeColors.BorderSubtle;

    public double CardBorderWidth => IsSelected ? 1.5 : 1.0;

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(LabelColor));
        OnPropertyChanged(nameof(LabelFontAttributes));
        OnPropertyChanged(nameof(CardBorderColor));
        OnPropertyChanged(nameof(CardBorderWidth));
    }
}

