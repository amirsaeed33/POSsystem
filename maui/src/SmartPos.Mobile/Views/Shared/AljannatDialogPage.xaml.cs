using SmartPos.Mobile.Controls;

namespace SmartPos.Mobile.Views.Shared;

public partial class AljannatDialogPage : ContentPage
{
    private readonly TaskCompletionSource<DialogResult> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _allowScrimCancel;
    private bool _completed;

    public AljannatDialogPage()
    {
        InitializeComponent();
        Dialog.CancelRequested += (_, _) => Complete(DialogResult.Cancelled());
        Dialog.ConfirmRequested += (_, _) => Complete(DialogResult.Confirmed(Dialog.PromptText));
        Dialog.ActionSelected += (_, action) => Complete(DialogResult.FromAction(action));
    }

    public Task<DialogResult> Result => _tcs.Task;

    public void ConfigureAlert(string title, string message, string buttonText)
    {
        _allowScrimCancel = false;
        Dialog.ConfigureAlert(title, message, buttonText);
    }

    public void ConfigureConfirm(
        string title,
        string message,
        string confirmText,
        string cancelText,
        bool isDestructive)
    {
        _allowScrimCancel = true;
        Dialog.ConfigureConfirm(title, message, confirmText, cancelText, isDestructive);
    }

    public void ConfigurePrompt(
        string title,
        string message,
        string accept,
        string cancel,
        string? placeholder,
        string initialValue,
        bool isDestructive)
    {
        _allowScrimCancel = true;
        Dialog.ConfigurePrompt(title, message, accept, cancel, placeholder, initialValue, isDestructive);
    }

    public void ConfigureActionSheet(string title, string cancel, string? destruction, IEnumerable<string> buttons)
    {
        _allowScrimCancel = true;
        Dialog.ConfigureActionSheet(title, cancel, destruction, buttons);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            Dialog.Opacity = 0;
            Dialog.Scale = 0.92;
            await Task.WhenAll(
                Dialog.FadeTo(1, 180, Easing.CubicOut),
                Dialog.ScaleTo(1, 180, Easing.CubicOut));
        }
        catch
        {
            Dialog.Opacity = 1;
            Dialog.Scale = 1;
        }
    }

    protected override bool OnBackButtonPressed()
    {
        if (_allowScrimCancel)
        {
            Complete(DialogResult.Cancelled());
            return true;
        }

        return true;
    }

    private void OnScrimTapped(object? sender, TappedEventArgs e)
    {
        if (_allowScrimCancel)
        {
            Complete(DialogResult.Cancelled());
        }
    }

    private void Complete(DialogResult result)
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        _tcs.TrySetResult(result);
    }
}

public sealed class DialogResult
{
    public bool IsConfirmed { get; init; }
    public string? Text { get; init; }
    public string? Action { get; init; }

    public static DialogResult Confirmed(string? text = null) => new()
    {
        IsConfirmed = true,
        Text = text
    };

    public static DialogResult Cancelled() => new()
    {
        IsConfirmed = false
    };

    public static DialogResult FromAction(string action) => new()
    {
        IsConfirmed = true,
        Action = action,
        Text = action
    };
}
