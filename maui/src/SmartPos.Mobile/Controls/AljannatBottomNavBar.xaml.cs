using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.Controls;

public partial class AljannatBottomNavBar : ContentView
{
    public const string TabHome = "Home";
    public const string TabOrder = "Order";
    public const string TabFavorites = "Favorites";
    public const string TabProfile = "Profile";

    private static readonly Color ActiveColor = Color.FromArgb("#C84B11");
    private static readonly Color InactiveColor = Color.FromArgb("#8C7E74");
    private static readonly Color TransparentColor = Colors.Transparent;

    public static readonly BindableProperty ActiveTabProperty =
        BindableProperty.Create(
            nameof(ActiveTab),
            typeof(string),
            typeof(AljannatBottomNavBar),
            TabHome,
            propertyChanged: (bindable, _, _) =>
            {
                if (bindable is AljannatBottomNavBar bar)
                {
                    bar.RefreshVisuals();
                }
            });

    public static readonly BindableProperty CartCountProperty =
        BindableProperty.Create(
            nameof(CartCount),
            typeof(int),
            typeof(AljannatBottomNavBar),
            0,
            propertyChanged: (bindable, _, newValue) =>
            {
                if (bindable is AljannatBottomNavBar bar && newValue is int count)
                {
                    bar.UpdateCartBadge(count);
                }
            });

    private CartService? _cart;
    private bool _navigating;
    private bool _cartSubscribed;
    private bool _pageAppearingHooked;
    private readonly CurvedBottomBarDrawable _barDrawable = new();

    public string ActiveTab
    {
        get => (string)GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }

    public int CartCount
    {
        get => (int)GetValue(CartCountProperty);
        set => SetValue(CartCountProperty, value);
    }

    public AljannatBottomNavBar()
    {
        InitializeComponent();
        if (BarGraphics != null)
        {
            BarGraphics.Drawable = _barDrawable;
        }

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
        RefreshVisuals();
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        BarGraphics?.Invalidate();
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        EnsureCartSubscription();
        SyncCartCountFromService();
        HookPageAppearing();
        SyncActiveTabFromShell();
        RefreshVisuals();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        UnhookPageAppearing();
        UnsubscribeCart();
    }

    private void HookPageAppearing()
    {
        if (_pageAppearingHooked)
        {
            return;
        }

        if (FindParentPage() is not ContentPage page)
        {
            return;
        }

        page.Appearing += OnParentPageAppearing;
        _pageAppearingHooked = true;
    }

    private void UnhookPageAppearing()
    {
        if (!_pageAppearingHooked)
        {
            return;
        }

        if (FindParentPage() is ContentPage page)
        {
            page.Appearing -= OnParentPageAppearing;
        }

        _pageAppearingHooked = false;
    }

    private void OnParentPageAppearing(object? sender, EventArgs e)
    {
        SyncActiveTabFromShell();
        SyncCartCountFromService();
    }

    private ContentPage? FindParentPage()
    {
        Element? current = this;
        while (current != null)
        {
            if (current is ContentPage page)
            {
                return page;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Re-highlight the tab that matches the current Shell route.
    /// Needed because Shell caches pages and NavigateAsync must not leave a stale ActiveTab behind.
    /// </summary>
    public void SyncActiveTabFromShell()
    {
        var loc = Shell.Current?.CurrentState?.Location?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(loc))
        {
            RefreshVisuals();
            return;
        }

        // Cart is not a side tab — keep whatever was declared for this page.
        if (IsCustomerTab(loc, "cart"))
        {
            RefreshVisuals();
            return;
        }

        if (IsCustomerTab(loc, "favorites"))
        {
            ActiveTab = TabFavorites;
        }
        else if (IsCustomerTab(loc, "account"))
        {
            ActiveTab = TabProfile;
        }
        else if (IsCustomerTab(loc, "order"))
        {
            ActiveTab = TabOrder;
        }
        else if (IsCustomerTab(loc, "home"))
        {
            ActiveTab = TabHome;
        }

        RefreshVisuals();
    }

    /// <summary>
    /// True when location is exactly //customer/{segment} (optional query), not a longer route like orderhistory.
    /// </summary>
    private static bool IsCustomerTab(string location, string segment)
    {
        // Examples: //customer/order  //customer/order?category=X  customer/favorites
        var marker = $"customer/{segment}";
        var idx = location.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return false;
        }

        var after = idx + marker.Length;
        if (after >= location.Length)
        {
            return true;
        }

        var next = location[after];
        return next is '/' or '?' or '#' or ';';
    }

    private void EnsureCartSubscription()
    {
        if (_cartSubscribed)
        {
            return;
        }

        try
        {
            _cart = ServiceHelper.Get<CartService>();
            _cart.CartChanged += OnCartChanged;
            _cartSubscribed = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AljannatBottomNavBar] Cart subscribe failed: {ex.Message}");
        }
    }

    private void UnsubscribeCart()
    {
        if (_cart != null && _cartSubscribed)
        {
            _cart.CartChanged -= OnCartChanged;
        }

        _cartSubscribed = false;
    }

    private void OnCartChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(SyncCartCountFromService);
    }

    private void SyncCartCountFromService()
    {
        if (_cart == null)
        {
            try
            {
                _cart = ServiceHelper.Get<CartService>();
            }
            catch
            {
                return;
            }
        }

        CartCount = _cart.ItemCount;
    }

    public void UpdateCartBadge(int count)
    {
        if (CartBadge == null || CartBadgeLabel == null)
        {
            return;
        }

        CartBadge.IsVisible = count > 0;
        CartBadgeLabel.Text = count > 99 ? "99+" : count.ToString();
    }

    public async Task OnCartItemLandedAsync(int newBadgeCount)
    {
        try
        {
            CartCount = newBadgeCount;
            UpdateCartBadge(newBadgeCount);

            if (CartMedallion != null)
            {
                await CartMedallion.ScaleTo(1.12, 80, Easing.CubicOut);
                await CartMedallion.ScaleTo(1.0, 100, Easing.SpringOut);
            }
        }
        catch
        {
            // Feedback is optional.
        }
    }

    private void RefreshVisuals()
    {
        if (HomeIcon == null)
        {
            return;
        }

        var tab = NormalizeTab(ActiveTab);
        ApplyTabVisual(HomeIcon, HomeLabel, HomePill, tab == TabHome);
        ApplyTabVisual(OrderIcon, OrderLabel, OrderPill, tab == TabOrder);
        ApplyTabVisual(FavoritesIcon, FavoritesLabel, FavoritesPill, tab == TabFavorites);
        ApplyTabVisual(ProfileIcon, ProfileLabel, ProfilePill, tab == TabProfile);
        UpdateCartBadge(CartCount);
    }

    private static void ApplyTabVisual(Microsoft.Maui.Controls.Shapes.Path icon, Label label, BoxView pill, bool active)
    {
        icon.Fill = active ? ActiveColor : InactiveColor;
        label.TextColor = active ? ActiveColor : InactiveColor;
        label.FontAttributes = FontAttributes.Bold;
        pill.Color = active ? ActiveColor : TransparentColor;
    }

    private static string NormalizeTab(string? tab)
    {
        if (string.Equals(tab, TabOrder, StringComparison.OrdinalIgnoreCase))
        {
            return TabOrder;
        }

        if (string.Equals(tab, TabFavorites, StringComparison.OrdinalIgnoreCase))
        {
            return TabFavorites;
        }

        if (string.Equals(tab, TabProfile, StringComparison.OrdinalIgnoreCase))
        {
            return TabProfile;
        }

        return TabHome;
    }

    private async void OnHomeTapped(object? sender, TappedEventArgs e) =>
        await NavigateAsync(TabHome, "//customer/home");

    private async void OnOrderTapped(object? sender, TappedEventArgs e) =>
        await NavigateAsync(TabOrder, "//customer/order");

    private async void OnFavoritesTapped(object? sender, TappedEventArgs e) =>
        await NavigateAsync(TabFavorites, "//customer/favorites");

    private async void OnProfileTapped(object? sender, TappedEventArgs e) =>
        await NavigateAsync(TabProfile, "//customer/account");

    private async void OnCartTapped(object? sender, TappedEventArgs e)
    {
        if (CartMedallion != null)
        {
            try
            {
                await CartMedallion.ScaleTo(0.92, 80, Easing.CubicOut);
                await CartMedallion.ScaleTo(1.0, 120, Easing.SpringOut);
            }
            catch
            {
                // Animation is optional feedback.
            }
        }

        await NavigateAsync(null, "//customer/cart");
    }

    private async Task NavigateAsync(string? tabToActivate, string route)
    {
        if (_navigating || Shell.Current == null)
        {
            return;
        }

        try
        {
            var current = Shell.Current.CurrentState?.Location?.ToString() ?? string.Empty;
            var routeKey = route.TrimStart('/');
            if (current.Contains(routeKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(current, route, StringComparison.OrdinalIgnoreCase))
            {
                // Already on this tab — just refresh highlight on this bar.
                if (!string.IsNullOrWhiteSpace(tabToActivate))
                {
                    ActiveTab = tabToActivate;
                }

                return;
            }

            _navigating = true;
            // Do NOT mutate ActiveTab before leaving — Shell caches pages and a stale
            // ActiveTab would show the wrong highlight when the user returns.
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AljannatBottomNavBar] Navigate {route} failed: {ex.Message}");
        }
        finally
        {
            _navigating = false;
        }
    }
}
