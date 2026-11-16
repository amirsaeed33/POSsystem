using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

public class AuthService
{
    private readonly AbpApiClient _api;
    private readonly SessionService _session;

    public AuthService(AbpApiClient api, SessionService session)
    {
        _api = api;
        _session = session;
    }

    public Task<IsTenantAvailableOutput> IsTenantAvailableAsync(string tenancyName) =>
        _api.PostAsync<IsTenantAvailableOutput>(
            "api/services/app/Account/IsTenantAvailable",
            new IsTenantAvailableInput { TenancyName = tenancyName.Trim() });

    /// <summary>
    /// Same as web login: tenancy is optional. When omitted, the API resolves tenant from the username/email.
    /// </summary>
    public async Task<AuthenticateResultModel> LoginAsync(
        string? tenancyName,
        string userName,
        string password,
        double? latitude = null,
        double? longitude = null)
    {
        // Do not send Abp.TenantId while authenticating — matches Angular (no tenancy required).
        _session.ClearTenantHeader();

        if (!string.IsNullOrWhiteSpace(tenancyName))
        {
            var tenant = await IsTenantAvailableAsync(tenancyName);
            if (!tenant.IsAvailable || tenant.TenantId == null)
            {
                throw new ApiException("Tenant not found or inactive.");
            }

            await _session.SetGuestTenantAsync(tenant.TenantId.Value, tenancyName.Trim());
        }

        var auth = await _api.PostAsync<AuthenticateResultModel>(
            "api/TokenAuth/Authenticate",
            new AuthenticateModel
            {
                TenancyName = string.IsNullOrWhiteSpace(tenancyName) ? null : tenancyName.Trim(),
                UserNameOrEmailAddress = userName.Trim(),
                Password = password,
                RememberClient = true,
                Latitude = latitude,
                Longitude = longitude
            });

        if (auth.TenantId.HasValue && !string.IsNullOrWhiteSpace(auth.TenancyName))
        {
            await _session.SetGuestTenantAsync(auth.TenantId.Value, auth.TenancyName);
        }

        await _session.SetAuthenticatedAsync(auth, null);
        var session = await GetCurrentLoginInformationsAsync();
        await _session.SetAuthenticatedAsync(auth, session.User);
        return auth;
    }

    public Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformationsAsync() =>
        _api.GetAsync<GetCurrentLoginInformationsOutput>("api/services/app/Session/GetCurrentLoginInformations");

    public Task LogoutAsync() => _session.LogoutAsync();
}
