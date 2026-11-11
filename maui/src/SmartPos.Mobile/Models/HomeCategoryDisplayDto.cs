using CommunityToolkit.Mvvm.ComponentModel;

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
        ? Color.FromArgb("#10B981")
        : (IsSelected ? Color.FromArgb("#10B981") : Color.FromArgb("#4B5563"));

    public FontAttributes LabelFontAttributes =>
        (IsMoreButton || IsSelected) ? FontAttributes.Bold : FontAttributes.None;

    public Color CardBorderColor =>
        IsSelected ? Color.FromArgb("#10B981") : Color.FromArgb("#E5E7EB");

    public double CardBorderWidth => IsSelected ? 1.5 : 1.0;

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(LabelColor));
        OnPropertyChanged(nameof(LabelFontAttributes));
        OnPropertyChanged(nameof(CardBorderColor));
        OnPropertyChanged(nameof(CardBorderWidth));
    }
}

