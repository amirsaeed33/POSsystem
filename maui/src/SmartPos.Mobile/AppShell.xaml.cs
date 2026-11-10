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
        Routing.RegisterRoute("favorites", typeof(FavoritesPage));
        Routing.RegisterRoute("track", typeof(TrackOrderPage));
        Routing.RegisterRoute("cart", typeof(CartPage));
        Routing.RegisterRoute("customer/cart", typeof(CartPage));
        Routing.RegisterRoute("order", typeof(OrderPage));
        Routing.RegisterRoute("customer/order", typeof(OrderPage));
        Routing.RegisterRoute("history", typeof(OrderHistoryPage));
        Routing.RegisterRoute("customer/history", typeof(OrderHistoryPage));

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
            // Cold start initially stays on SplashScreenPage; transition is triggered by SplashScreenPage after its delay.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppShell] Session initialization failed: {ex}");
        }
    }
}
