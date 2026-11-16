namespace SmartPos.Mobile.Services;

public class AppSettings
{
    public string ApiBaseUrl { get; set; } = "https://aljannatapp.com";
    public string DefaultTenancyName { get; set; } = string.Empty;
    /// <summary>Optional. When set, customer storefront opens this branch like /online-order?branchId=.</summary>
    public int? DefaultBranchId { get; set; }
    public string AppName { get; set; } = "SmartPos";
}
