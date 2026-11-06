using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

public class NavigationService
{
    private readonly SessionService _session;

    public NavigationService(SessionService session)
    {
        _session = session;
    }

    private static Shell? GetShell() =>
        Shell.Current ?? Application.Current?.Windows.FirstOrDefault()?.Page as Shell;

    public async Task GoToSplashAsync()
    {
        var shell = GetShell();
        if (shell != null)
        {
            await shell.GoToAsync("//splash");
        }
    }

    public async Task GoToLoginAsync()
    {
        var shell = GetShell();
        if (shell != null)
        {
            await shell.GoToAsync("//customer/account");
        }
    }

    public async Task GoToRoleHomeAsync()
    {
        var shell = GetShell();
        if (shell == null)
        {
            return;
        }

        var mode = _session.ResolveRoleMode();
        switch (mode)
        {
            case AppRoleMode.Rider:
                await shell.GoToAsync("//rider/dashboard");
                break;
            case AppRoleMode.Waiter:
                await shell.GoToAsync("//waiter/tables");
                break;
            case AppRoleMode.Admin:
                await shell.GoToAsync("//admin/home");
                break;
            case AppRoleMode.UnsupportedStaff:
                await shell.GoToAsync("//unsupported");
                break;
            case AppRoleMode.Customer:
            default:
                // MakanGes food delivery theme: standard entry leads to customer catalog/store
                await shell.GoToAsync("//customer/home");
                break;
        }
    }
}
