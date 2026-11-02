using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartPos.Mobile.Models;

public partial class ModifierOptionItem : ObservableObject
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool AllowMultiple { get; set; }
    public bool IsRequired { get; set; }
    public int MaxSelection { get; set; } = 1;

    [ObservableProperty]
    private bool isSelected;

    public string PriceLabel => Price > 0 ? $"+PKR {Price:0}" : string.Empty;
    public bool HasPrice => Price > 0;
}
