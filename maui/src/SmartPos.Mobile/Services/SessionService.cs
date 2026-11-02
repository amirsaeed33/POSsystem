using System.Text.Json;
using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

public class SessionService
{
    private const string TokenKey = "sp.accessToken";
    private const string TenantIdKey = "sp.tenantId";
    private const string TenancyNameKey = "sp.tenancyName";
    private const string BranchIdKey = "sp.branchId";
    private const string UserJsonKey = "sp.userJson";
    private const string GuestTenantIdKey = "sp.guestTenantId";
    private const string GuestTenancyNameKey = "sp.guestTenancyName";
    private const string GuestBranchIdKey = "sp.guestBranchId";

    public string? AccessToken { get; private set; }
    public int? TenantId { get; private set; }
    public string? TenancyName { get; private set; }
    public int? BranchId { get; private set; }
    public UserLoginInfoDto? User { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

    public event EventHandler? SessionChanged;

    public async Task LoadAsync()
    {
        AccessToken = await SecureStorage.Default.GetAsync(TokenKey);
        var tenant = await SecureStorage.Default.GetAsync(TenantIdKey);
        TenantId = int.TryParse(tenant, out var tid) ? tid : null;
        TenancyName = await SecureStorage.Default.GetAsync(TenancyNameKey);
        var branch = await SecureStorage.Default.GetAsync(BranchIdKey);
        BranchId = int.TryParse(branch, out var bid) ? bid : null;
        var userJson = await SecureStorage.Default.GetAsync(UserJsonKey);
        if (!string.IsNullOrWhiteSpace(userJson))
        {
            User = JsonSerializer.Deserialize<UserLoginInfoDto>(userJson, JsonDefaults.Options);
        }

        if (TenantId == null)
        {
            var guestTenant = await SecureStorage.Default.GetAsync(GuestTenantIdKey);
            TenantId = int.TryParse(guestTenant, out var gtid) ? gtid : null;
            TenancyName = await SecureStorage.Default.GetAsync(GuestTenancyNameKey) ?? TenancyName;
        }

        if (BranchId == null)
        {
            var guestBranch = await SecureStorage.Default.GetAsync(GuestBranchIdKey);
            BranchId = int.TryParse(guestBranch, out var gbid) ? gbid : null;
        }
    }

    /// <summary>
    /// Clears in-memory tenant headers only (used before login so the API can resolve tenant from username, same as web).
    /// </summary>
    public void ClearTenantHeader()
    {
        TenantId = null;
        TenancyName = null;
    }

    public async Task SetGuestTenantAsync(int tenantId, string tenancyName)
    {
        TenantId = tenantId;
        TenancyName = tenancyName;
        await SecureStorage.Default.SetAsync(GuestTenantIdKey, tenantId.ToString());
        await SecureStorage.Default.SetAsync(GuestTenancyNameKey, tenancyName);
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SetBranchAsync(int branchId)
    {
        BranchId = branchId;
        if (IsAuthenticated)
        {
            await SecureStorage.Default.SetAsync(BranchIdKey, branchId.ToString());
        }
        else
        {
            await SecureStorage.Default.SetAsync(GuestBranchIdKey, branchId.ToString());
        }

        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public Task ClearBranchAsync()
    {
        BranchId = null;
        SecureStorage.Default.Remove(BranchIdKey);
        SecureStorage.Default.Remove(GuestBranchIdKey);
        SessionChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public async Task SetAuthenticatedAsync(AuthenticateResultModel auth, UserLoginInfoDto? user)
    {
        AccessToken = auth.AccessToken;
        TenantId = auth.TenantId ?? TenantId;
        TenancyName = auth.TenancyName ?? TenancyName;
        User = user;
        if (user?.BranchId is int bid)
        {
            BranchId = bid;
        }

        if (!string.IsNullOrWhiteSpace(AccessToken))
        {
            await SecureStorage.Default.SetAsync(TokenKey, AccessToken);
        }

        if (TenantId.HasValue)
        {
            await SecureStorage.Default.SetAsync(TenantIdKey, TenantId.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(TenancyName))
        {
            await SecureStorage.Default.SetAsync(TenancyNameKey, TenancyName);
        }

        if (BranchId.HasValue)
        {
            await SecureStorage.Default.SetAsync(BranchIdKey, BranchId.Value.ToString());
        }

        if (user != null)
        {
            await SecureStorage.Default.SetAsync(UserJsonKey, JsonSerializer.Serialize(user, JsonDefaults.Options));
        }

        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task UpdateUserAsync(UserLoginInfoDto user)
    {
        User = user;
        if (user.BranchId.HasValue)
        {
            BranchId = user.BranchId;
            await SecureStorage.Default.SetAsync(BranchIdKey, user.BranchId.Value.ToString());
        }

        await SecureStorage.Default.SetAsync(UserJsonKey, JsonSerializer.Serialize(user, JsonDefaults.Options));
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task LogoutAsync(bool keepGuestTenant = true)
    {
        AccessToken = null;
        User = null;
        SecureStorage.Default.Remove(TokenKey);
        SecureStorage.Default.Remove(UserJsonKey);
        SecureStorage.Default.Remove(BranchIdKey);

        if (!keepGuestTenant)
        {
            TenantId = null;
            TenancyName = null;
            BranchId = null;
            SecureStorage.Default.Remove(TenantIdKey);
            SecureStorage.Default.Remove(TenancyNameKey);
            SecureStorage.Default.Remove(GuestTenantIdKey);
            SecureStorage.Default.Remove(GuestTenancyNameKey);
            SecureStorage.Default.Remove(GuestBranchIdKey);
        }
        else
        {
            var guestTenant = await SecureStorage.Default.GetAsync(GuestTenantIdKey);
            TenantId = int.TryParse(guestTenant, out var gtid) ? gtid : TenantId;
            TenancyName = await SecureStorage.Default.GetAsync(GuestTenancyNameKey) ?? TenancyName;
            var guestBranch = await SecureStorage.Default.GetAsync(GuestBranchIdKey);
            BranchId = int.TryParse(guestBranch, out var gbid) ? gbid : null;
        }

        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public AppRoleMode ResolveRoleMode()
    {
        if (!IsAuthenticated || User == null)
        {
            return AppRoleMode.GuestCustomer;
        }

        if (IsRider(User))
        {
            return AppRoleMode.Rider;
        }

        if (IsWaiter(User))
        {
            return AppRoleMode.Waiter;
        }

        var designation = User.Designation?.ToLowerInvariant() ?? string.Empty;
        if (designation.Contains("customer") || User.Roles.Any(r => r.Contains("customer", StringComparison.OrdinalIgnoreCase)))
        {
            return AppRoleMode.Customer;
        }

        // Back-office roles open the mobile admin dashboard.
        if (IsAdmin(User))
        {
            return AppRoleMode.Admin;
        }

        return AppRoleMode.Customer;
    }

    public static bool IsRider(UserLoginInfoDto user)
    {
        if (user.IsDeliveryRider)
        {
            return true;
        }

        if (user.Roles.Any(r => r.Contains("rider", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var d = user.Designation?.ToLowerInvariant() ?? string.Empty;
        return d.Contains("rider") || d.Contains("delivery");
    }

    public static bool IsWaiter(UserLoginInfoDto user)
    {
        if (user.IsWaiter)
        {
            return true;
        }

        if (user.Roles.Any(r => r.Contains("waiter", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var d = user.Designation?.ToLowerInvariant() ?? string.Empty;
        return d.Contains("waiter") || d.Contains("server");
    }

    public static bool IsAdmin(UserLoginInfoDto user)
    {
        if (user.Roles.Any(r =>
                r.Contains("admin", StringComparison.OrdinalIgnoreCase) ||
                r.Contains("manager", StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                r.Contains("cashier", StringComparison.OrdinalIgnoreCase) ||
                r.Contains("owner", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var d = user.Designation?.ToLowerInvariant() ?? string.Empty;
        return d.Contains("admin") || d.Contains("manager") || d.Contains("owner");
    }
}

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
