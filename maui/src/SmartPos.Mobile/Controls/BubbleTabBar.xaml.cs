namespace SmartPos.Mobile.Controls;

public enum BubbleTabMode
{
    Customer,
    Rider,
    Waiter,
    Admin
}

public partial class BubbleTabBar : ContentView
{
    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(
            nameof(SelectedIndex),
            typeof(int),
            typeof(BubbleTabBar),
            0,
            propertyChanged: OnConfigChanged);

    public static readonly BindableProperty ModeProperty =
        BindableProperty.Create(
            nameof(Mode),
            typeof(BubbleTabMode),
            typeof(BubbleTabBar),
            BubbleTabMode.Customer,
            propertyChanged: OnConfigChanged);

    private static readonly Color Accent = Color.FromArgb("#F15A29");
    private static readonly Color Inactive = Color.FromArgb("#9CA3AF");

    private string[] _routes = [];
    private string[] _activeIcons = [];
    private string[] _inactiveIcons = [];
    private string[] _labels = [];
    private int _tabCount = 4;

    public BubbleTabBar()
    {
        InitializeComponent();
        // Never leave residual TranslationY from older AbsoluteLayout docking.
        TranslationY = 0;
        SizeChanged += (_, _) => PositionBubble();
        TabsGrid.SizeChanged += (_, _) => PositionBubble();
        Loaded += (_, _) =>
        {
            TranslationY = 0;
            ApplyMode();
            Dispatcher.Dispatch(PositionBubble);
        };
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public BubbleTabMode Mode
    {
        get => (BubbleTabMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    private static void OnConfigChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is BubbleTabBar bar)
        {
            bar.ApplyMode();
        }
    }

    private void ApplyMode()
    {
        switch (Mode)
        {
            case BubbleTabMode.Rider:
                _tabCount = 3;
                _labels = ["Home", "History", "Profile"];
                _routes = ["//rider/dashboard", "//rider/history", "//rider/account"];
                _activeIcons = ["tab_home_active.png", "tab_history_active.png", "tab_account_active.png"];
                _inactiveIcons = ["tab_home_idle.png", "tab_history_idle.png", "tab_account_idle.png"];
                break;
            case BubbleTabMode.Waiter:
                _tabCount = 4;
                _labels = ["Tables", "Order", "Orders", "Profile"];
                _routes = ["//waiter/tables", "//waiter/order", "//waiter/myorders", "//waiter/account"];
                _activeIcons = ["tab_tables_active.png", "tab_order_active.png", "tab_myorders_active.png", "tab_account_active.png"];
                _inactiveIcons = ["tab_tables_idle.png", "tab_order_idle.png", "tab_myorders_idle.png", "tab_account_idle.png"];
                break;
            case BubbleTabMode.Admin:
                _tabCount = 2;
                _labels = ["Dashboard", "Profile"];
                _routes = ["//admin/home", "//admin/account"];
                _activeIcons = ["tab_home_active.png", "tab_account_active.png"];
                _inactiveIcons = ["tab_home_idle.png", "tab_account_idle.png"];
                break;
            default:
                _tabCount = 4;
                _labels = ["Home", "Cart", "Track", "Profile"];
                _routes = ["//customer/home", "//customer/cart", "//customer/track", "//customer/account"];
                _activeIcons = ["tab_home_active.png", "tab_cart_active.png", "tab_track_active.png", "tab_account_active.png"];
                _inactiveIcons = ["tab_home_idle.png", "tab_cart_idle.png", "tab_track_idle.png", "tab_account_idle.png"];
                break;
        }

        Label0.Text = _labels.ElementAtOrDefault(0) ?? string.Empty;
        Label1.Text = _labels.ElementAtOrDefault(1) ?? string.Empty;
        Label2.Text = _labels.ElementAtOrDefault(2) ?? string.Empty;
        Label3.Text = _labels.ElementAtOrDefault(3) ?? string.Empty;

        Tab2.IsVisible = _tabCount >= 3;
        Tab3.IsVisible = _tabCount >= 4;

        TabsGrid.ColumnDefinitions.Clear();
        for (var i = 0; i < _tabCount; i++)
        {
            TabsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        var icons = new[] { Icon0, Icon1, Icon2, Icon3 };
        var labels = new[] { Label0, Label1, Label2, Label3 };
        var tabs = new[] { Tab0, Tab1, Tab2, Tab3 };

        for (var i = 0; i < 4; i++)
        {
            if (i >= _tabCount)
            {
                tabs[i].IsVisible = false;
                continue;
            }

            tabs[i].IsVisible = true;
            var active = i == SelectedIndex;
            icons[i].Source = active ? _activeIcons[i] : _inactiveIcons[i];
            labels[i].TextColor = active ? Accent : Inactive;
            icons[i].TranslationY = active ? -8 : 0;
            labels[i].TranslationY = active ? -2 : 0;
        }

        PositionBubble();
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(16), PositionBubble);
    }

    private void PositionBubble()
    {
        var tabs = new[] { Tab0, Tab1, Tab2, Tab3 };
        var icons = new[] { Icon0, Icon1, Icon2, Icon3 };
        var index = Math.Clamp(SelectedIndex, 0, Math.Max(0, _tabCount - 1));
        var tab = tabs[index];
        var icon = icons[index];
        var bubbleSize = ActiveBubble.WidthRequest;

        if (tab.Width > 0 && TabsGrid.Width > 0 && icon.Width > 0)
        {
            var iconCenterX = tab.X + icon.X + (icon.Width / 2.0);
            ActiveBubble.TranslationX = Math.Max(0, iconCenterX - (bubbleSize / 2.0));
            ActiveBubble.TranslationY = -8;
            return;
        }

        if (tab.Width > 0 && TabsGrid.Width > 0)
        {
            var centerInPill = tab.X + (tab.Width / 2.0);
            ActiveBubble.TranslationX = Math.Max(0, centerInPill - (bubbleSize / 2.0));
            ActiveBubble.TranslationY = -8;
            return;
        }

        var hostWidth = PillHost.Width > 0 ? PillHost.Width : Math.Max(0, Width - 8);
        if (hostWidth <= 0 || _tabCount <= 0)
        {
            return;
        }

        var slot = hostWidth / _tabCount;
        ActiveBubble.TranslationX = Math.Max(0, (slot * index) + ((slot - bubbleSize) / 2.0));
        ActiveBubble.TranslationY = -8;
    }

    private async void OnTab0Tapped(object? sender, TappedEventArgs e) => await GoAsync(0);
    private async void OnTab1Tapped(object? sender, TappedEventArgs e) => await GoAsync(1);
    private async void OnTab2Tapped(object? sender, TappedEventArgs e) => await GoAsync(2);
    private async void OnTab3Tapped(object? sender, TappedEventArgs e) => await GoAsync(3);

    private async Task GoAsync(int index)
    {
        if (index >= _tabCount)
        {
            return;
        }

        if (index == SelectedIndex)
        {
            return;
        }

        SelectedIndex = index;
        try
        {
            await Shell.Current.GoToAsync(_routes[index]);
        }
        catch
        {
            // ignore navigation races
        }
    }
}
