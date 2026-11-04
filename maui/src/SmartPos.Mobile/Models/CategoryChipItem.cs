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

    public Color BackgroundColor =>
        IsSelected ? Color.FromArgb("#0F6B4C") : Color.FromArgb("#F3F6F4");

    public Color TitleColor =>
        IsSelected ? Colors.White : Color.FromArgb("#1F2A24");

    public Color CountColor =>
        IsSelected ? Color.FromArgb("#D7EDE4") : Color.FromArgb("#6B7A72");

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(TitleColor));
        OnPropertyChanged(nameof(CountColor));
    }
}
