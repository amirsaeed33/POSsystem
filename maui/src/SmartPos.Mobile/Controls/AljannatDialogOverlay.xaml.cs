using Microsoft.Maui.Controls.Shapes;

namespace SmartPos.Mobile.Controls;

public partial class AljannatDialogOverlay : ContentView
{
    private static readonly Color Accent = Color.FromArgb("#C84B11");
    private static readonly Color Destructive = Color.FromArgb("#B83227");
    private static readonly Color SoftAccent = Color.FromArgb("#FDF1EB");
    private static readonly Color SoftDestructive = Color.FromArgb("#FDECEB");
    private static readonly Color StrokeAccent = Color.FromArgb("#F0C4AE");
    private static readonly Color StrokeDestructive = Color.FromArgb("#F5C6C2");

    public event EventHandler? CancelRequested;
    public event EventHandler? ConfirmRequested;
    public event EventHandler<string>? ActionSelected;

    public AljannatDialogOverlay()
    {
        InitializeComponent();
    }

    public string PromptText => PromptEntry?.Text?.Trim() ?? string.Empty;

    public void ConfigureAlert(string title, string message, string buttonText)
    {
        ApplyChrome(isDestructive: false);
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        MessageLabel.IsVisible = !string.IsNullOrWhiteSpace(message);
        PromptHost.IsVisible = false;
        ActionSheetHost.IsVisible = false;
        ActionSheetHost.Children.Clear();
        TwoButtonRow.IsVisible = false;
        SingleButton.IsVisible = true;
        SingleButton.BackgroundColor = Accent;
        SingleButtonLabel.Text = buttonText;
    }

    public void ConfigureConfirm(
        string title,
        string message,
        string confirmText,
        string cancelText,
        bool isDestructive)
    {
        ApplyChrome(isDestructive);
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        MessageLabel.IsVisible = !string.IsNullOrWhiteSpace(message);
        PromptHost.IsVisible = false;
        ActionSheetHost.IsVisible = false;
        ActionSheetHost.Children.Clear();
        SingleButton.IsVisible = false;
        TwoButtonRow.IsVisible = true;
        CancelLabel.Text = cancelText;
        ConfirmLabel.Text = confirmText;
        ConfirmButton.BackgroundColor = isDestructive ? Destructive : Accent;
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
        ApplyChrome(isDestructive);
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        MessageLabel.IsVisible = !string.IsNullOrWhiteSpace(message);
        PromptHost.IsVisible = true;
        PromptEntry.Placeholder = placeholder ?? string.Empty;
        PromptEntry.Text = initialValue ?? string.Empty;
        ActionSheetHost.IsVisible = false;
        ActionSheetHost.Children.Clear();
        SingleButton.IsVisible = false;
        TwoButtonRow.IsVisible = true;
        CancelLabel.Text = cancel;
        ConfirmLabel.Text = accept;
        ConfirmButton.BackgroundColor = isDestructive ? Destructive : Accent;
    }

    public void ConfigureActionSheet(string title, string cancel, string? destruction, IEnumerable<string> buttons)
    {
        ApplyChrome(isDestructive: !string.IsNullOrWhiteSpace(destruction));
        TitleLabel.Text = title;
        MessageLabel.IsVisible = false;
        PromptHost.IsVisible = false;
        SingleButton.IsVisible = false;
        TwoButtonRow.IsVisible = false;

        ActionSheetHost.Children.Clear();
        ActionSheetHost.IsVisible = true;

        foreach (var button in buttons.Where(b => !string.IsNullOrWhiteSpace(b)))
        {
            ActionSheetHost.Children.Add(BuildSheetButton(button.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(destruction))
        {
            ActionSheetHost.Children.Add(BuildSheetButton(destruction.Trim(), isDestructive: true));
        }

        ActionSheetHost.Children.Add(BuildSheetButton(cancel, isCancel: true));
    }

    private Border BuildSheetButton(string text, bool isDestructive = false, bool isCancel = false)
    {
        var border = new Border
        {
            HeightRequest = 46,
            StrokeThickness = isCancel || !isDestructive ? 1 : 0,
            Stroke = Color.FromArgb("#EAE2DA"),
            BackgroundColor = isDestructive
                ? Destructive
                : isCancel
                    ? Color.FromArgb("#F4EFEA")
                    : Color.FromArgb("#FFFFFF"),
            Padding = new Thickness(12, 0)
        };
        border.StrokeShape = new RoundRectangle { CornerRadius = 14 };

        var label = new Label
        {
            Text = text,
            FontSize = 13.5,
            FontAttributes = FontAttributes.Bold,
            TextColor = isDestructive ? Colors.White : Color.FromArgb("#1E1916"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };
        border.Content = label;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (isCancel)
            {
                CancelRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ActionSelected?.Invoke(this, text);
            }
        };
        border.GestureRecognizers.Add(tap);
        return border;
    }

    private void ApplyChrome(bool isDestructive)
    {
        IconBadge.BackgroundColor = isDestructive ? SoftDestructive : SoftAccent;
        IconBadge.Stroke = isDestructive ? StrokeDestructive : StrokeAccent;
        IconPath.Fill = isDestructive ? Destructive : Accent;
        // Material-style path data: trash (destructive) vs info (default)
        IconPath.Data = isDestructive
            ? (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(
                "M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z")
            : (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(
                "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-6h2v6zm0-8h-2V7h2v2z");
    }

    private void OnCancelTapped(object? sender, TappedEventArgs e) =>
        CancelRequested?.Invoke(this, EventArgs.Empty);

    private void OnConfirmTapped(object? sender, TappedEventArgs e) =>
        ConfirmRequested?.Invoke(this, EventArgs.Empty);
}
