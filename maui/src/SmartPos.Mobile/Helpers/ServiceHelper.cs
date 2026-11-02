namespace SmartPos.Mobile.Helpers;

public static class ServiceHelper
{
    public static T GetRequiredService<T>() where T : notnull
    {
        var services = IPlatformApplication.Current?.Services
            ?? throw new InvalidOperationException("DI services are not available yet.");
        return services.GetRequiredService<T>();
    }

    public static T Get<T>() where T : class => GetRequiredService<T>();
}
