using SmartPos.Mobile.Services;

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
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is BubbleTabBar bar)
                {
                    bar.RefreshVisuals();
                }
            });

    public static readonly BindableProperty ModeProperty =
        BindableProperty.Create(
            nameof(Mode),
            typeof(BubbleTabMode),
            typeof(BubbleTabBar),
            BubbleTabMode.Customer,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is BubbleTabBar bar)
                {
                    bar.ApplyMode();
                }
            });

    public static readonly BindableProperty CartCountProperty =
        BindableProperty.Create(
            nameof(CartCount),
            typeof(int),
            typeof(BubbleTabBar),
            0,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is BubbleTabBar bar && newValue is int count)
                {
                    bar.UpdateCartBadge(count);
                }
            });

    private static readonly Color CustomerAccent = Color.FromArgb("#10B981");
    private static readonly Color StaffAccent = Color.FromArgb("#0F6B4C");
    private static readonly Color Inactive = Color.FromArgb("#94A3B8");

    private Color Accent => Mode == BubbleTabMode.Customer ? CustomerAccent : StaffAccent;

    private string[] _routes = [];
    private string[] _activeIcons = [];
    private string[] _inactiveIcons = [];
    private string[] _labels = [];
    private int _tabCount = 4;
    private bool _navigating;

    public BubbleTabBar()
    {
        InitializeComponent();
        ApplyMode();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
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

    public int CartCount
    {
        get => (int)GetValue(CartCountProperty);
        set => SetValue(CartCountProperty, value);
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        ApplyMode();
        HookShellNavigated();
        SyncActiveTab();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        UnhookShellNavigated();
    }

    private void HookShellNavigated()
    {
        if (Shell.Current != null)
        {
            Shell.Current.Navigated -= OnShellNavigated;
            Shell.Current.Navigated += OnShellNavigated;
        }
    }

    private void UnhookShellNavigated()
    {
        if (Shell.Current != null)
        {
            Shell.Current.Navigated -= OnShellNavigated;
        }
    }

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(SyncActiveTab);
    }

    public void SyncActiveTab()
    {
        try
        {
            var hostPage = GetParentPage();
            var targetIndex = -1;

            switch (Mode)
            {
                case BubbleTabMode.Customer:
                    targetIndex = ResolveCustomerTabIndex(hostPage);
                    break;
                case BubbleTabMode.Rider:
                    targetIndex = ResolveRiderTabIndex(hostPage);
                    break;
                case BubbleTabMode.Waiter:
                    targetIndex = ResolveWaiterTabIndex(hostPage);
                    break;
                case BubbleTabMode.Admin:
                    targetIndex = ResolveAdminTabIndex(hostPage);
                    break;
            }

            if (targetIndex >= 0 && targetIndex < _tabCount)
            {
                if (SelectedIndex != targetIndex)
                {
                    SelectedIndex = targetIndex;
                }
                else
                {
                    RefreshVisuals();
                }
            }
        }
        catch
        {
            // Defensive against navigation race conditions
        }
    }

    private Page? GetParentPage()
    {
        Element? current = Parent;
        while (current != null)
        {
            if (current is Page page)
            {
                return page;
            }
            current = current.Parent;
        }
        return Shell.Current?.CurrentPage;
    }

    private int ResolveCustomerTabIndex(Page? page)
    {
        if (page != null)
        {
            var pageType = page.GetType().Name;
            if (pageType.Equals("CustomerHomePage", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (pageType.Equals("OrderPage", StringComparison.OrdinalIgnoreCase) ||
                pageType.Equals("CartPage", StringComparison.OrdinalIgnoreCase) ||
                pageType.Equals("ProductDetailPage", StringComparison.OrdinalIgnoreCase) ||
                pageType.Equals("TrackOrderPage", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (pageType.Equals("FavoritesPage", StringComparison.OrdinalIgnoreCase))
                return 2;
            if (pageType.Equals("CustomerProfilePage", StringComparison.OrdinalIgnoreCase) ||
                pageType.Equals("OrderHistoryPage", StringComparison.OrdinalIgnoreCase))
                return 3;
        }

        var route = (Shell.Current?.CurrentState?.Location?.OriginalString ?? string.Empty).ToLowerInvariant();
        if (route.Contains("favorite"))
            return 2;
        if (route.Contains("account") || route.Contains("profile") || route.Contains("history"))
            return 3;
        if (route.Contains("order") || route.Contains("product") || route.Contains("cart") || route.Contains("track"))
            return 1;
        if (route.Contains("home"))
            return 0;

        return SelectedIndex;
    }

    private int ResolveRiderTabIndex(Page? page)
    {
        if (page != null)
        {
            var pageType = page.GetType().Name;
            if (pageType.Equals("RiderDashboardPage", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (pageType.Equals("RiderHistoryPage", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (pageType.Equals("LoginPage", StringComparison.OrdinalIgnoreCase))
                return 2;
        }

        var route = (Shell.Current?.CurrentState?.Location?.OriginalString ?? string.Empty).ToLowerInvariant();
        if (route.Contains("history"))
            return 1;
        if (route.Contains("account") || route.Contains("login"))
            return 2;
        if (route.Contains("dashboard") || route.Contains("home"))
            return 0;

        return SelectedIndex;
    }

    private int ResolveWaiterTabIndex(Page? page)
    {
        if (page != null)
        {
            var pageType = page.GetType().Name;
            if (pageType.Equals("WaiterTablesPage", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (pageType.Equals("WaiterOrderPage", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (pageType.Equals("WaiterMyOrdersPage", StringComparison.OrdinalIgnoreCase))
                return 2;
            if (pageType.Equals("LoginPage", StringComparison.OrdinalIgnoreCase))
                return 3;
        }

        var route = (Shell.Current?.CurrentState?.Location?.OriginalString ?? string.Empty).ToLowerInvariant();
        if (route.Contains("table"))
            return 0;
        if (route.Contains("myorder"))
            return 2;
        if (route.Contains("order"))
            return 1;
        if (route.Contains("account") || route.Contains("login"))
            return 3;

        return SelectedIndex;
    }

    private int ResolveAdminTabIndex(Page? page)
    {
        if (page != null)
        {
            var pageType = page.GetType().Name;
            if (pageType.Equals("AdminDashboardPage", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (pageType.Equals("LoginPage", StringComparison.OrdinalIgnoreCase))
                return 1;
        }

        var route = (Shell.Current?.CurrentState?.Location?.OriginalString ?? string.Empty).ToLowerInvariant();
        if (route.Contains("account") || route.Contains("login"))
            return 1;
        if (route.Contains("home") || route.Contains("dashboard"))
            return 0;

        return SelectedIndex;
    }

    private void ApplyMode()
    {
        switch (Mode)
        {
            case BubbleTabMode.Rider:
                _tabCount = 3;
                _labels = ["Home", "History", "Profile"];
                _routes = ["//rider/dashboard", "//rider/history", "//rider/account"];
                _activeIcons = ["tab_home_staff_active.png", "tab_history_staff_active.png", "tab_account_staff_active.png"];
                _inactiveIcons = ["tab_home_idle.png", "tab_history_idle.png", "tab_account_idle.png"];
                break;
            case BubbleTabMode.Waiter:
                _tabCount = 4;
                _labels = ["Tables", "Order", "Orders", "Profile"];
                _routes = ["//waiter/tables", "//waiter/order", "//waiter/myorders", "//waiter/account"];
                _activeIcons = ["tab_tables_active.png", "tab_order_active.png", "tab_myorders_active.png", "tab_account_staff_active.png"];
                _inactiveIcons = ["tab_tables_idle.png", "tab_order_idle.png", "tab_myorders_idle.png", "tab_account_idle.png"];
                break;
            case BubbleTabMode.Admin:
                _tabCount = 2;
                _labels = ["Dashboard", "Profile"];
                _routes = ["//admin/home", "//admin/account"];
                _activeIcons = ["tab_home_staff_active.png", "tab_account_staff_active.png"];
                _inactiveIcons = ["tab_home_idle.png", "tab_account_idle.png"];
                break;
            default: // Customer
                _tabCount = 4;
                _labels = ["Home", "Order", "Favorite", "Profile"];
                _routes = ["//customer/home", "//customer/order", "//customer/favorites", "//customer/account"];
                _activeIcons = ["tab_home_active.png", "tab_order_active.png", "tab_favorites_active.png", "tab_account_active.png"];
                _inactiveIcons = ["tab_home_idle.png", "tab_order_idle.png", "tab_favorites_idle.png", "tab_account_idle.png"];
                break;
        }

        if (Label0 != null) Label0.Text = _labels.ElementAtOrDefault(0) ?? string.Empty;
        if (Label1 != null) Label1.Text = _labels.ElementAtOrDefault(1) ?? string.Empty;
        if (Label2 != null) Label2.Text = _labels.ElementAtOrDefault(2) ?? string.Empty;
        if (Label3 != null) Label3.Text = _labels.ElementAtOrDefault(3) ?? string.Empty;

        var hosts = new[] { TabHost0, TabHost1, TabHost2, TabHost3 };
        var buttons = new[] { Tab0, Tab1, Tab2, Tab3 };
        var visuals = new[] { Tab0Visual, Tab1Visual, Tab2Visual, Tab3Visual };

        if (TabsGrid != null)
        {
            TabsGrid.ColumnDefinitions.Clear();
            for (var i = 0; i < _tabCount; i++)
            {
                TabsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            }
        }

        for (var i = 0; i < 4; i++)
        {
            var on = i < _tabCount;
            if (hosts[i] != null)
            {
                hosts[i].IsVisible = on;
                if (on)
                {
                    Grid.SetColumn(hosts[i], i);
                }
            }

            if (buttons[i] != null)
            {
                buttons[i].IsVisible = on;
                buttons[i].IsEnabled = on;
            }

            if (visuals[i] != null)
            {
                visuals[i].IsVisible = on;
            }
        }

        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        if (_activeIcons.Length < _tabCount || _inactiveIcons.Length < _tabCount)
        {
            ApplyMode();
            return;
        }

        var icons = new[] { Icon0, Icon1, Icon2, Icon3 };
        var labels = new[] { Label0, Label1, Label2, Label3 };
        var buttons = new[] { Tab0, Tab1, Tab2, Tab3 };

        for (var i = 0; i < 4; i++)
        {
            if (i >= _tabCount)
            {
                if (buttons[i] != null) buttons[i].IsVisible = false;
                continue;
            }

            if (buttons[i] != null)
            {
                buttons[i].IsVisible = true;
                buttons[i].IsEnabled = true;
            }

            var active = i == SelectedIndex;
            if (icons[i] != null)
            {
                icons[i].Source = active ? _activeIcons[i] : _inactiveIcons[i];
            }

            if (labels[i] != null)
            {
                labels[i].TextColor = active ? Accent : Inactive;
                labels[i].FontAttributes = active ? FontAttributes.Bold : FontAttributes.None;
            }
        }
    }

    private async void OnTab0Clicked(object? sender, EventArgs e) => await GoAsync(0);
    private async void OnTab1Clicked(object? sender, EventArgs e) => await GoAsync(1);
    private async void OnTab2Clicked(object? sender, EventArgs e) => await GoAsync(2);
    private async void OnTab3Clicked(object? sender, EventArgs e) => await GoAsync(3);

    /// <summary>Navigate only — never mutates cart, qty, or category selection.</summary>
    private async Task GoAsync(int index)
    {
        if (_navigating || index < 0 || index >= _tabCount)
        {
            return;
        }

        var route = _routes[index];
        if (string.IsNullOrWhiteSpace(route))
        {
            return;
        }

        // Same tab: still navigate to refresh shell state, but skip when already there.
        if (index == SelectedIndex)
        {
            return;
        }

        _navigating = true;
        SelectedIndex = index;
        LoadingService.Current.ShowTabLoader();
        try
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync(route);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BubbleTabBar] Navigate failed ({route}): {ex.Message}");
            LoadingService.Current.HideTabLoader();
        }
        finally
        {
            _navigating = false;
        }
    }

    public void UpdateCartBadge(int count)
    {
        try
        {
            if (CartBadge == null || CartBadgeLabel == null)
            {
                return;
            }

            if (count > 0)
            {
                CartBadgeLabel.Text = count > 99 ? "99+" : count.ToString();
                CartBadge.IsVisible = true;
            }
            else
            {
                CartBadge.IsVisible = false;
            }
        }
        catch
        {
        }
    }

    public async Task BounceCartTabAsync()
    {
        try
        {
            if (Tab1Visual != null)
            {
                await Tab1Visual.ScaleTo(1.15, 80, Easing.CubicOut);
                await Tab1Visual.ScaleTo(1.0, 80, Easing.SpringOut);
            }
        }
        catch
        {
        }
    }

    public async Task OnCartItemLandedAsync(int newBadgeCount)
    {
        try
        {
            UpdateCartBadge(newBadgeCount);

            if (Tab1Visual != null)
            {
                await Tab1Visual.ScaleTo(1.15, 80, Easing.CubicOut);
                await Tab1Visual.ScaleTo(1.0, 80, Easing.SpringOut);
            }
        }
        catch
        {
        }
    }
}
