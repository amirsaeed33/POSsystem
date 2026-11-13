using CommunityToolkit.Mvvm.ComponentModel;
using SmartPos.Mobile.Helpers;

namespace SmartPos.Mobile.Services;

public partial class LoadingService : ObservableObject
{
    private static LoadingService? _instance;
    public static LoadingService Current
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

            try
            {
                _instance = ServiceHelper.Get<LoadingService>();
            }
            catch
            {
                _instance ??= new LoadingService();
            }

            return _instance;
        }
    }

    [ObservableProperty]
    private bool isTabLoading;

    private int _token;
    private DateTimeOffset _shownAt = DateTimeOffset.MinValue;
    private static readonly TimeSpan MinimumVisible = TimeSpan.FromMilliseconds(450);

    public void ShowTabLoader(int timeoutMs = 4000)
    {
        var token = Interlocked.Increment(ref _token);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _shownAt = DateTimeOffset.UtcNow;
            IsTabLoading = true;

            _ = Task.Delay(timeoutMs).ContinueWith(_ =>
            {
                if (token == Volatile.Read(ref _token))
                {
                    HideTabLoader();
                }
            }, TaskScheduler.Default);
        });
    }

    public void HideTabLoader()
    {
        var token = Volatile.Read(ref _token);
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var wait = MinimumVisible - (DateTimeOffset.UtcNow - _shownAt);
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait);
            }

            if (token != Volatile.Read(ref _token))
            {
                return;
            }

            IsTabLoading = false;
        });
    }
}

