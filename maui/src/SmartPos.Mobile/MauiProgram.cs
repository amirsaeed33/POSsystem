using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartPos.Mobile.Services;
using SmartPos.Mobile.ViewModels;
using SmartPos.Mobile.Views.Admin;
using SmartPos.Mobile.Views.Auth;
using SmartPos.Mobile.Views.Customer;
using SmartPos.Mobile.Views.Rider;
using SmartPos.Mobile.Views.Waiter;

namespace SmartPos.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                handlers.AddHandler(typeof(Shell), typeof(SmartPos.Mobile.Platforms.Android.ShellTabBarCollapse));
#endif
            });

        using var settingsStream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("SmartPos.Mobile.appsettings.json");
        var configBuilder = new ConfigurationBuilder();
        if (settingsStream != null)
        {
            configBuilder.AddJsonStream(settingsStream);
        }

        var config = configBuilder.Build();
        var settings = new AppSettings();
        config.Bind(settings);
        if (string.IsNullOrWhiteSpace(settings.ApiBaseUrl))
        {
            settings.ApiBaseUrl = "http://aljannatapi.runasp.net";
        }

        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<CartService>();
        builder.Services.AddSingleton<ProductCatalogCache>();
        builder.Services.AddSingleton<DeliveryLocationService>();
        builder.Services.AddSingleton<NavigationService>();
        builder.Services.AddSingleton(_ =>
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(settings.ApiBaseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(60)
            };
            return client;
        });
        builder.Services.AddSingleton<AbpApiClient>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<OnlineOrderService>();
        builder.Services.AddSingleton<RiderOrderService>();
        builder.Services.AddSingleton<RestaurantService>();
        builder.Services.AddSingleton<DashboardService>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<UnsupportedStaffViewModel>();
        builder.Services.AddTransient<AdminDashboardViewModel>();
        builder.Services.AddTransient<CustomerHomeViewModel>();
        builder.Services.AddTransient<ProductDetailViewModel>();
        builder.Services.AddTransient<CartCheckoutViewModel>();
        builder.Services.AddTransient<TrackOrderViewModel>();
        builder.Services.AddTransient<RiderDashboardViewModel>();
        builder.Services.AddTransient<RiderHistoryViewModel>();
        builder.Services.AddTransient<WaiterTablesViewModel>();
        builder.Services.AddTransient<WaiterOrderViewModel>();
        builder.Services.AddTransient<WaiterMyOrdersViewModel>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<UnsupportedStaffPage>();
        builder.Services.AddTransient<AdminDashboardPage>();
        builder.Services.AddTransient<CustomerHomePage>();
        builder.Services.AddTransient<ProductDetailPage>();
        builder.Services.AddTransient<CartPage>();
        builder.Services.AddTransient<TrackOrderPage>();
        builder.Services.AddTransient<RiderDashboardPage>();
        builder.Services.AddTransient<RiderHistoryPage>();
        builder.Services.AddTransient<WaiterTablesPage>();
        builder.Services.AddTransient<WaiterOrderPage>();
        builder.Services.AddTransient<WaiterMyOrdersPage>();

        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
