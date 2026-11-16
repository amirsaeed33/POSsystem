using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

public class DashboardService
{
    private readonly AbpApiClient _api;
    private readonly SessionService _session;

    public DashboardService(AbpApiClient api, SessionService session)
    {
        _api = api;
        _session = session;
    }

    public Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default) =>
        _api.GetAsync<DashboardDto>("api/services/app/Dashboard/Get", ct);

    public Task<OnlineOrderPipelineDto> GetOnlineOrderPipelineAsync(CancellationToken ct = default)
    {
        var qs = _session.BranchId is > 0 ? $"?BranchId={_session.BranchId}" : string.Empty;
        return _api.GetAsync<OnlineOrderPipelineDto>(
            $"api/services/app/Dashboard/GetOnlineOrderPipeline{qs}",
            ct);
    }
}
