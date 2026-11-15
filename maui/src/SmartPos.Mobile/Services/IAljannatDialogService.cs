namespace SmartPos.Mobile.Services;

public interface IAljannatDialogService
{
    Task ShowAlertAsync(string title, string message, string buttonText = "OK");

    Task<bool> ShowConfirmAsync(
        string title,
        string message,
        string confirmText = "Confirm",
        string cancelText = "Cancel",
        bool isDestructive = false);

    Task<string?> ShowPromptAsync(
        string title,
        string message,
        string accept = "OK",
        string cancel = "Cancel",
        string? placeholder = null,
        string initialValue = "",
        bool isDestructive = false);

    Task<string?> ShowActionSheetAsync(
        string title,
        string cancel,
        string? destruction,
        params string[] buttons);
}
