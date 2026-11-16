using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.Views.Shared;

namespace SmartPos.Mobile.Services;

public sealed class AljannatDialogService : IAljannatDialogService
{
    private static IAljannatDialogService? _fallback;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public static IAljannatDialogService Current
    {
        get
        {
            try
            {
                return ServiceHelper.Get<IAljannatDialogService>();
            }
            catch
            {
                return _fallback ??= new AljannatDialogService();
            }
        }
    }

    public AljannatDialogService()
    {
        _fallback ??= this;
    }

    public async Task ShowAlertAsync(string title, string message, string buttonText = "OK")
    {
        await ShowCoreAsync(page =>
        {
            page.ConfigureAlert(title, message, buttonText);
            return Task.CompletedTask;
        });
    }

    public async Task<bool> ShowConfirmAsync(
        string title,
        string message,
        string confirmText = "Confirm",
        string cancelText = "Cancel",
        bool isDestructive = false)
    {
        var result = await ShowCoreAsync(page =>
        {
            page.ConfigureConfirm(title, message, confirmText, cancelText, isDestructive);
            return Task.CompletedTask;
        });
        return result.IsConfirmed;
    }

    public async Task<string?> ShowPromptAsync(
        string title,
        string message,
        string accept = "OK",
        string cancel = "Cancel",
        string? placeholder = null,
        string initialValue = "",
        bool isDestructive = false)
    {
        var result = await ShowCoreAsync(page =>
        {
            page.ConfigurePrompt(title, message, accept, cancel, placeholder, initialValue, isDestructive);
            return Task.CompletedTask;
        });
        return result.IsConfirmed ? (result.Text ?? string.Empty) : null;
    }

    public async Task<string?> ShowActionSheetAsync(
        string title,
        string cancel,
        string? destruction,
        params string[] buttons)
    {
        var result = await ShowCoreAsync(page =>
        {
            page.ConfigureActionSheet(title, cancel, destruction, buttons ?? Array.Empty<string>());
            return Task.CompletedTask;
        });

        if (!result.IsConfirmed)
        {
            return null;
        }

        return result.Action ?? result.Text;
    }

    private async Task<DialogResult> ShowCoreAsync(Func<AljannatDialogPage, Task> configure)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = new AljannatDialogPage();
                await configure(page);

                var nav = GetNavigation();
                if (nav == null)
                {
                    return DialogResult.Cancelled();
                }

                await nav.PushModalAsync(page, animated: false);
                var result = await page.Result;

                try
                {
                    if (nav.ModalStack.Contains(page))
                    {
                        await nav.PopModalAsync(animated: false);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AljannatDialogService] Pop failed: {ex.Message}");
                }

                return result;
            });
        }
        finally
        {
            _gate.Release();
        }
    }

    private static INavigation? GetNavigation()
    {
        if (Shell.Current?.Navigation != null)
        {
            return Shell.Current.Navigation;
        }

        return Application.Current?.Windows?.FirstOrDefault()?.Page?.Navigation
               ?? Application.Current?.MainPage?.Navigation;
    }
}
