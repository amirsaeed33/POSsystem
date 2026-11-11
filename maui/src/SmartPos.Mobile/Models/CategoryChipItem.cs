using CommunityToolkit.Mvvm.ComponentModel;

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
        IsSelected ? Color.FromArgb("#10B981") : Color.FromArgb("#E5E7EB");

    public Color ChipBackgroundColor =>
        IsSelected ? Color.FromArgb("#10B981") : Color.FromArgb("#FFFFFF");

    public Color ChipTextColor =>
        IsSelected ? Colors.White : Color.FromArgb("#111827");

    public Color ChipCountTextColor =>
        IsSelected ? Color.FromArgb("#D1FAE5") : Color.FromArgb("#6B7280");

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
