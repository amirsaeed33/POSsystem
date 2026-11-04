using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartPos.Mobile.Models;

public partial class QuickFilterPillItem : ObservableObject
{
    public string Id { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;

    [ObservableProperty]
    private bool isSelected;

    public Color BackgroundColor =>
        IsSelected ? Color.FromArgb("#0F172A") : Color.FromArgb("#F1F5F9");

    public Color StrokeColor =>
        IsSelected ? Color.FromArgb("#0F172A") : Color.FromArgb("#E2E8F0");

    public Color TextColor =>
        IsSelected ? Colors.White : Color.FromArgb("#475569");

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(StrokeColor));
        OnPropertyChanged(nameof(TextColor));
    }
}
