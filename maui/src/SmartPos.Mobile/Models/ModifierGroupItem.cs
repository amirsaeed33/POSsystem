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

    public string RequirementBadgeText => IsRequired ? "Required" : "Optional";
    public Color RequirementTextColor => IsRequired ? ThemeColors.WarningMuted : ThemeColors.TextSecondary;
    public Color RequirementBgColor => IsRequired ? ThemeColors.WarningBackground : ThemeColors.Gray100;

    public string SelectionRuleHint => AllowMultiple
        ? (MaxSelection > 1 ? $"Choose up to {MaxSelection}" : "Multiple choices")
        : "Choose 1";

    public bool IsSatisfied => !IsRequired || Options.Any(o => o.IsSelected);

    public ObservableCollection<ModifierOptionItem> Options { get; } = new();
}
