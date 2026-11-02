using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartPos.Mobile.Models;

public partial class ModifierGroupItem : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = "Options";
    public bool IsRequired { get; set; }
    public bool AllowMultiple { get; set; }
    public int MaxSelection { get; set; } = 1;

    public string RequirementLabel => IsRequired ? "(Required)" : "(Optional)";

    public ObservableCollection<ModifierOptionItem> Options { get; } = new();
}
