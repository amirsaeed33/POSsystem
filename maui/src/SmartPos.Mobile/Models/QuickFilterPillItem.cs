using CommunityToolkit.Mvvm.ComponentModel;

using SmartPos.Mobile.Helpers;

namespace SmartPos.Mobile.Models;

public partial class QuickFilterPillItem : ObservableObject
{
    public string Id { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;

    [ObservableProperty]
    private bool isSelected;

    public Color BackgroundColor =>
        IsSelected ? ThemeColors.AuthTitleText : ThemeColors.Gray100;

    public Color StrokeColor =>
        IsSelected ? ThemeColors.AuthTitleText : ThemeColors.AuthInputBorder;

    public Color TextColor =>
        IsSelected ? Colors.White : ThemeColors.Slate600;

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(StrokeColor));
        OnPropertyChanged(nameof(TextColor));
    }
}
