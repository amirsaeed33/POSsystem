using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

using SmartPos.Mobile.Helpers;

namespace SmartPos.Mobile.Models;

public partial class ModifierGroupItem : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = "Options";
    public bool IsRequired { get; set; }
    public bool AllowMultiple { get; set; }
    public int MaxSelection { get; set; } = 1;

    public string RequirementLabel => IsRequired ? "(Required)" : "(Optional)";

    public string RequirementBadgeText => IsRequired ? "REQUIRED" : "OPTIONAL";
    public Color RequirementTextColor => IsRequired ? ThemeColors.Primary : ThemeColors.TextSecondary;
    public Color RequirementBgColor => IsRequired ? ThemeColors.PrimaryLight : ThemeColors.Gray100;
    public Color RequirementBorderColor => IsRequired ? Color.FromArgb("#F0C4AE") : ThemeColors.BorderSubtle;

    public string SelectionRuleHint => AllowMultiple
        ? (MaxSelection > 1 ? $"Choose up to {MaxSelection}" : "Multiple choices")
        : "Choose 1";

    public bool IsSatisfied => !IsRequired || Options.Any(o => o.IsSelected);

    /// <summary>Zero-price option groups (spice levels, etc.) render as warm pills.</summary>
    public bool UsePillLayout => Options.Count > 0 && Options.All(o => o.Price <= 0);

    public bool UseCardLayout => !UsePillLayout;

    public ObservableCollection<ModifierOptionItem> Options { get; } = new();
}
