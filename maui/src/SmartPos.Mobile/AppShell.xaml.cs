using SmartPos.Mobile.Services;
using SmartPos.Mobile.Views.Customer;

namespace SmartPos.Mobile;

public partial class AppShell : Shell
{
    private readonly SessionService _session;
    private readonly NavigationService _navigation;

    public AppShell(SessionService session, NavigationService navigation)
    {
        InitializeComponent();
        _session = session;
        _navigation = navigation;
        Routing.RegisterRoute("productdetail", typeof(ProductDetailPage));

        Loaded += OnShellLoaded;
    }

    private async void OnShellLoaded(object? sender, EventArgs e)
    {
        Loaded -= OnShellLoaded;
        await InitializeNavigationAsync();
    }

    public async Task InitializeNavigationAsync()
    {
        try
        {
            await _session.LoadAsync();
            await _navigation.GoToRoleHomeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppShell] Navigation failed: {ex}");
        }
    }
}
