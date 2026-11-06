namespace SmartPos.Mobile;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        // Keep light UI so labels stay readable on our light page backgrounds.
        UserAppTheme = AppTheme.Light;
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = _services.GetRequiredService<AppShell>();
        var window = new Window(shell)
        {
            // Phone-like window when debugging on Windows.
            Width = 390,
            Height = 844,
            X = 80,
            Y = 40
        };
        return window;
    }
}
