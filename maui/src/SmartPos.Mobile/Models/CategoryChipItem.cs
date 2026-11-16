using CommunityToolkit.Mvvm.ComponentModel;

using SmartPos.Mobile.Helpers;

namespace SmartPos.Mobile.Models;

public partial class CategoryChipItem : ObservableObject
{
    public string Name { get; init; } = string.Empty;

    private int count;
    public int Count
    {
        get => count;
        set
        {
            if (SetProperty(ref count, value))
            {
                OnPropertyChanged(nameof(CountLabel));
            }
        }
    }

    public string? ImageUrl { get; init; }

    public string CountLabel => Count.ToString();
    public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
    public bool IsAll => string.Equals(Name, "All", StringComparison.OrdinalIgnoreCase);
    public bool IsFavorites => string.Equals(Name, "❤️ Favorites", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(Name, "Favorites", StringComparison.OrdinalIgnoreCase);

    public string Initials
    {
        get
        {
            var name = (Name ?? "C").Trim();
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
            }

            return name.Length >= 2 ? name[..2].ToUpperInvariant() : name.ToUpperInvariant();
        }
    }

    [ObservableProperty]
    private bool isSelected;

    public bool IsSelectedAndNotAll => IsSelected && !IsAll;
    public string CheckmarkGlyph => IsSelected && !IsAll ? "✓ " : string.Empty;

    public Color ChipBorderColor =>
        IsSelected ? ThemeColors.Primary : ThemeColors.BorderSubtle;

    public Color ChipBackgroundColor =>
        IsSelected ? ThemeColors.Primary : ThemeColors.White;

    public Color ChipTextColor =>
        IsSelected ? Colors.White : ThemeColors.TextPrimary;

    public Color ChipCountTextColor =>
        IsSelected ? ThemeColors.PrimaryLight : ThemeColors.TextSecondary;

    public Color BackgroundColor => ChipBackgroundColor;

    public Color TitleColor => ChipTextColor;

    public Color CountColor => ChipCountTextColor;

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(ChipBorderColor));
        OnPropertyChanged(nameof(ChipBackgroundColor));
        OnPropertyChanged(nameof(ChipTextColor));
        OnPropertyChanged(nameof(ChipCountTextColor));
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(TitleColor));
        OnPropertyChanged(nameof(CountColor));
        OnPropertyChanged(nameof(IsSelectedAndNotAll));
        OnPropertyChanged(nameof(CheckmarkGlyph));
    }
}
