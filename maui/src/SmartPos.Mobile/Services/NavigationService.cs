using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

public class NavigationService
{
    private readonly SessionService _session;

    public NavigationService(SessionService session)
    {
        _session = session;
    }

    public async Task GoToRoleHomeAsync()
    {
        var mode = _session.ResolveRoleMode();
        switch (mode)
        {
            case AppRoleMode.Rider:
                await Shell.Current.GoToAsync("//rider/dashboard");
                break;
            case AppRoleMode.Waiter:
                await Shell.Current.GoToAsync("//waiter/tables");
                break;
            case AppRoleMode.UnsupportedStaff:
                await Shell.Current.GoToAsync("//unsupported");
                break;
            default:
                await Shell.Current.GoToAsync("//customer/home");
                break;
        }
    }
}
