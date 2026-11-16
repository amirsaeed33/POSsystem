namespace SmartPos.Mobile.Controls;

public partial class TransparentBorderLoader : ContentView
{
    public static readonly BindableProperty IsRunningProperty =
        BindableProperty.Create(
            nameof(IsRunning),
            typeof(bool),
            typeof(TransparentBorderLoader),
            false,
            propertyChanged: OnIsRunningChanged);

    private CancellationTokenSource? _spinCts;

    public TransparentBorderLoader()
    {
        InitializeComponent();
    }

    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    private static void OnIsRunningChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TransparentBorderLoader loader)
        {
            loader.UpdateRunning((bool)newValue);
        }
    }

    private void UpdateRunning(bool running)
    {
        IsVisible = running;
        InputTransparent = !running;

        _spinCts?.Cancel();
        _spinCts = null;
        SpinRing.CancelAnimations();
        SpinRing.Rotation = 0;

        if (!running)
        {
            return;
        }

        _spinCts = new CancellationTokenSource();
        _ = SpinAsync(_spinCts.Token);
    }

    private async Task SpinAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await SpinRing.RotateTo(360, 750, Easing.Linear);
                SpinRing.Rotation = 0;
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
