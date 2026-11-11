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

    private CancellationTokenSource? _timeoutCts;

    public void ShowTabLoader(int timeoutMs = 3500)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _timeoutCts?.Cancel();
            _timeoutCts = new CancellationTokenSource();
            var token = _timeoutCts.Token;

            IsTabLoading = true;

            // Safety timeout: automatically hide after timeoutMs if not already hidden
            _ = Task.Delay(timeoutMs, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    HideTabLoader();
                }
            }, TaskScheduler.Default);
        });
    }

    public void HideTabLoader()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _timeoutCts?.Cancel();
            _timeoutCts = null;
            IsTabLoading = false;
        });
    }
}

