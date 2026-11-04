namespace SmartPos.Mobile;

public partial class App : Application
{
    private readonly AppShell _shell;

    public App(AppShell shell)
    {
        InitializeComponent();
        // Keep light UI so labels stay readable on our light page backgrounds.
        UserAppTheme = AppTheme.Light;
        _shell = shell;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_shell)
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
