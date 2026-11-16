using System.Windows.Input;

namespace SmartPos.Mobile.Controls;

public partial class AppButton : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(AppButton), string.Empty);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(AppButton));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(AppButton));

    public static readonly BindableProperty VariantProperty =
        BindableProperty.Create(
            nameof(Variant),
            typeof(AppButtonVariant),
            typeof(AppButton),
            AppButtonVariant.Primary,
            propertyChanged: OnVariantChanged);

    public static readonly BindableProperty ImageSourceProperty =
        BindableProperty.Create(nameof(ImageSource), typeof(ImageSource), typeof(AppButton));

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(
            nameof(CornerRadius),
            typeof(int?),
            typeof(AppButton),
            null,
            propertyChanged: OnLayoutOverrideChanged);

    public static readonly BindableProperty ButtonHeightProperty =
        BindableProperty.Create(
            nameof(ButtonHeight),
            typeof(double?),
            typeof(AppButton),
            null,
            propertyChanged: OnLayoutOverrideChanged);

    public event EventHandler? Clicked;

    public AppButton()
    {
        InitializeComponent();
        ApplyVariant();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public AppButtonVariant Variant
    {
        get => (AppButtonVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public ImageSource? ImageSource
    {
        get => (ImageSource?)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public int? CornerRadius
    {
        get => (int?)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public double? ButtonHeight
    {
        get => (double?)GetValue(ButtonHeightProperty);
        set => SetValue(ButtonHeightProperty, value);
    }

    private static void OnVariantChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AppButton button)
        {
            button.ApplyVariant();
        }
    }

    private static void OnLayoutOverrideChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AppButton button)
        {
            button.ApplyLayoutOverrides();
        }
    }

    private void ApplyVariant()
    {
        if (InnerButton == null)
        {
            return;
        }

        var styleKey = Variant switch
        {
            AppButtonVariant.Secondary => "SecondaryButton",
            AppButtonVariant.Danger => "DangerButton",
            AppButtonVariant.Outline => "OutlineButton",
            AppButtonVariant.StaffPrimary => "StaffPrimaryButton",
            AppButtonVariant.AuthPrimary => "AuthPrimaryButton",
            AppButtonVariant.QtyDecrease => "QtyDecreaseButton",
            AppButtonVariant.QtyIncrease => "QtyIncreaseButton",
            _ => "PrimaryButton"
        };

        if (Application.Current?.Resources.TryGetValue(styleKey, out var resource) == true
            && resource is Style style)
        {
            InnerButton.Style = style;
        }

        ApplyLayoutOverrides();
    }

    private void ApplyLayoutOverrides()
    {
        if (InnerButton == null)
        {
            return;
        }

        if (CornerRadius.HasValue)
        {
            InnerButton.CornerRadius = CornerRadius.Value;
        }

        if (ButtonHeight.HasValue)
        {
            InnerButton.HeightRequest = ButtonHeight.Value;
        }
    }

    private void OnInnerClicked(object? sender, EventArgs e) => Clicked?.Invoke(this, e);
}
