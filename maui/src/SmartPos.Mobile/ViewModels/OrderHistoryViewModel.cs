using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public partial class OrderHistoryFilterChip : ObservableObject
{
    public string Label { get; init; } = string.Empty;

    [ObservableProperty]
    private bool isSelected;

    public Color ChipBackground => IsSelected
        ? Color.FromArgb("#C84B11")
        : Color.FromArgb("#FFFFFF");

    public Color ChipStroke => IsSelected
        ? Color.FromArgb("#C84B11")
        : Color.FromArgb("#EAE2DA");

    public Color ChipTextColor => IsSelected
        ? Colors.White
        : Color.FromArgb("#1E1916");

    public FontAttributes ChipFontAttributes => FontAttributes.Bold;

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(ChipBackground));
        OnPropertyChanged(nameof(ChipStroke));
        OnPropertyChanged(nameof(ChipTextColor));
    }
}

public partial class OrderHistoryViewModel : BaseViewModel
{
    private readonly OnlineOrderService _orders;
    private readonly SessionService _session;
    private readonly CartService _cart;

    [ObservableProperty]
    private string phoneNumber = string.Empty;

    [ObservableProperty]
    private string savedPhoneNumber = string.Empty;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private bool isSearching;

    [ObservableProperty]
    private bool hasSearched;

    [ObservableProperty]
    private CustomerOrderDto? selectedOrder;

    [ObservableProperty]
    private CustomerOrderDto? selectedReceiptOrder;

    [ObservableProperty]
    private string searchErrorMessage = string.Empty;

    [ObservableProperty]
    private string selectedFilter = "All";

    [ObservableProperty]
    private bool isReceiptSheetVisible;

    [ObservableProperty]
    private bool isReorderSheetVisible;

    [ObservableProperty]
    private string reorderTotalText = "Rs. 0";

    public ObservableCollection<CustomerOrderDto> Orders { get; } = new();

    public ObservableCollection<CustomerOrderDto> FilteredOrders { get; } = new();

    public ObservableCollection<OrderHistoryFilterChip> FilterOptions { get; } = new();

    public ObservableCollection<CustomerOrderLineDto> ReorderPreviewLines { get; } = new();

    public int OrdersCount => FilteredOrders.Count;

    public string OrdersCountText =>
        OrdersCount == 1 ? "1 Order" : $"{OrdersCount} Orders";

    public bool HasOrders => Orders.Count > 0;

    public bool HasFilteredOrders => FilteredOrders.Count > 0;

    public bool IsEmptyState => !IsBusy && !IsSearching && HasSearched && !HasOrders;

    public bool IsFilteredEmpty =>
        !IsBusy && !IsSearching && HasSearched && FilteredOrders.Count == 0;

    public bool HasSavedPhone => !string.IsNullOrWhiteSpace(SavedPhoneNumber) &&
                                 !string.Equals(PhoneNumber.Trim(), SavedPhoneNumber.Trim(), StringComparison.OrdinalIgnoreCase);

    public bool HasSearchError => !string.IsNullOrWhiteSpace(SearchErrorMessage);

    public bool ShowOrderList => HasFilteredOrders && !IsSearching;

    public bool HasSelectedReceiptOrder => SelectedReceiptOrder != null;

    public OrderHistoryViewModel(OnlineOrderService orders, SessionService session, CartService cart)
    {
        _orders = orders;
        _session = session;
        _cart = cart;
        Title = "Order History";
        InitializeFilters();
    }

    private void InitializeFilters()
    {
        FilterOptions.Clear();
        foreach (var label in new[] { "All", "Active", "Delivered", "Cancelled" })
        {
            FilterOptions.Add(new OrderHistoryFilterChip
            {
                Label = label,
                IsSelected = label == SelectedFilter
            });
        }
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        SavedPhoneNumber = Preferences.Default.Get("sp.customerPhone", string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(PhoneNumber) && !string.IsNullOrWhiteSpace(SavedPhoneNumber))
        {
            PhoneNumber = SavedPhoneNumber;
        }

        if (!HasSearched && NormalizePhoneDigits(PhoneNumber).Length >= 7)
        {
            await SearchOrdersAsync();
        }
    }

    [RelayCommand]
    private void UseSavedPhone()
    {
        if (!string.IsNullOrWhiteSpace(SavedPhoneNumber))
        {
            PhoneNumber = SavedPhoneNumber;
        }
    }

    [RelayCommand]
    private Task FindOrdersAsync() => SearchOrdersAsync();

    [RelayCommand]
    private async Task SearchOrdersAsync()
    {
        var digits = NormalizePhoneDigits(PhoneNumber);
        if (digits.Length < 7)
        {
            SearchErrorMessage = "Please enter a valid phone number (at least 7 digits).";
            Orders.Clear();
            HasSearched = true;
            RebuildFilteredOrders();
            NotifyOrderState();
            return;
        }

        SearchErrorMessage = string.Empty;

        Preferences.Default.Set("sp.customerPhone", PhoneNumber.Trim());
        SavedPhoneNumber = PhoneNumber.Trim();

        try
        {
            IsSearching = true;
            IsBusy = true;
            ErrorMessage = null;

            var branchId = _session.BranchId;
            var list = await _orders.GetCustomerOrdersAsync(PhoneNumber.Trim(), branchId, maxResultCount: 40);

            Orders.Clear();
            if (list != null)
            {
                foreach (var order in list)
                {
                    Orders.Add(order);
                }
            }

            HasSearched = true;
            RebuildFilteredOrders();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OrderHistoryViewModel] Search failed: {ex}");
            SearchErrorMessage = ex.Message ?? "Failed to fetch orders. Please check your connection.";
            Orders.Clear();
            HasSearched = true;
            RebuildFilteredOrders();
        }
        finally
        {
            IsSearching = false;
            IsBusy = false;
            NotifyOrderState();
        }
    }

    [RelayCommand]
    private async Task RefreshOrdersAsync()
    {
        IsRefreshing = true;
        try
        {
            if (NormalizePhoneDigits(PhoneNumber).Length >= 7)
            {
                await SearchOrdersAsync();
            }
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private void SelectFilter(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return;
        }

        SelectedFilter = filter.Trim();
        foreach (var chip in FilterOptions)
        {
            chip.IsSelected = string.Equals(chip.Label, SelectedFilter, StringComparison.OrdinalIgnoreCase);
        }

        RebuildFilteredOrders();
        NotifyOrderState();
    }

    [RelayCommand]
    private async Task TrackOrderAsync(CustomerOrderDto? order)
    {
        if (order == null || string.IsNullOrWhiteSpace(order.OrderNo))
        {
            return;
        }

        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync($"//customer/track?orderNo={Uri.EscapeDataString(order.OrderNo.Trim())}");
        }
    }

    [RelayCommand]
    private async Task TrackByOrderNoAsync(string? orderNo)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
        {
            return;
        }

        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync($"//customer/track?orderNo={Uri.EscapeDataString(orderNo.Trim())}");
        }
    }

    [RelayCommand]
    private async Task PrimaryOrderActionAsync(CustomerOrderDto? order)
    {
        if (order == null)
        {
            return;
        }

        if (order.IsActiveOrder)
        {
            await TrackOrderAsync(order);
            return;
        }

        OpenReceipt(order);
    }

    [RelayCommand]
    private void OpenReceipt(CustomerOrderDto? order)
    {
        if (order == null)
        {
            return;
        }

        SelectedOrder = order;
        SelectedReceiptOrder = order;
        IsReorderSheetVisible = false;
        IsReceiptSheetVisible = true;
    }

    [RelayCommand]
    private void CloseReceipt()
    {
        IsReceiptSheetVisible = false;
    }

    [RelayCommand]
    private async Task OpenReorderSheetAsync(CustomerOrderDto? order)
    {
        if (order == null)
        {
            return;
        }

        if (!order.CanReorder)
        {
            if (Shell.Current != null)
            {
                await Shell.Current.DisplayAlert(
                    "Re-order",
                    "This order does not have any items available to re-order.",
                    "OK");
            }

            return;
        }

        // 1-click: replace tray contents with this past order.
        var count = _cart.ReorderFromCustomerOrder(order, clearCart: true);
        if (count <= 0)
        {
            if (Shell.Current != null)
            {
                await Shell.Current.DisplayAlert(
                    "Re-order",
                    "No items could be added to your cart from this order.",
                    "OK");
            }

            return;
        }

        SelectedOrder = order;
        SelectedReceiptOrder = order;
        IsReceiptSheetVisible = false;

        ReorderPreviewLines.Clear();
        foreach (var line in order.Lines)
        {
            ReorderPreviewLines.Add(line);
        }

        ReorderTotalText = order.GrandTotalDisplay;
        IsReorderSheetVisible = true;
    }

    /// <summary>Kept for any remaining bindings; opens the 1-click reorder sheet.</summary>
    [RelayCommand]
    private Task ReorderAsync(CustomerOrderDto? order) => OpenReorderSheetAsync(order);

    [RelayCommand]
    private void CloseReorderSheet()
    {
        IsReorderSheetVisible = false;
    }

    [RelayCommand]
    private void KeepBrowsing()
    {
        IsReorderSheetVisible = false;
    }

    [RelayCommand]
    private async Task ProceedToCheckoutAsync()
    {
        IsReorderSheetVisible = false;
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//customer/cart");
        }
    }

    [RelayCommand]
    private async Task CopyOrderNoAsync(string? orderNo)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
        {
            return;
        }

        try
        {
            await Clipboard.Default.SetTextAsync(orderNo.Trim());
            if (Shell.Current != null)
            {
                await Shell.Current.DisplayAlert("Copied", $"Order #{orderNo} copied to clipboard.", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OrderHistoryViewModel] Clipboard error: {ex}");
        }
    }

    [RelayCommand]
    private async Task BrowseMenuAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//customer/home");
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    partial void OnPhoneNumberChanged(string value)
    {
        OnPropertyChanged(nameof(HasSavedPhone));
        if (!string.IsNullOrWhiteSpace(SearchErrorMessage))
        {
            SearchErrorMessage = string.Empty;
        }
    }

    partial void OnSearchErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasSearchError));
    }

    partial void OnIsSearchingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsFilteredEmpty));
        OnPropertyChanged(nameof(ShowOrderList));
        OnPropertyChanged(nameof(IsEmptyState));
    }

    partial void OnSelectedReceiptOrderChanged(CustomerOrderDto? value)
    {
        OnPropertyChanged(nameof(HasSelectedReceiptOrder));
    }

    private void RebuildFilteredOrders()
    {
        FilteredOrders.Clear();

        IEnumerable<CustomerOrderDto> query = Orders;
        switch (SelectedFilter)
        {
            case "Active":
                query = Orders.Where(o => o.Status is 0 or 1 or 3);
                break;
            case "Delivered":
                query = Orders.Where(o => o.Status is 4 or 5);
                break;
            case "Cancelled":
                query = Orders.Where(o => o.Status == 2);
                break;
        }

        foreach (var order in query)
        {
            FilteredOrders.Add(order);
        }
    }

    private void NotifyOrderState()
    {
        OnPropertyChanged(nameof(HasOrders));
        OnPropertyChanged(nameof(HasFilteredOrders));
        OnPropertyChanged(nameof(IsEmptyState));
        OnPropertyChanged(nameof(IsFilteredEmpty));
        OnPropertyChanged(nameof(ShowOrderList));
        OnPropertyChanged(nameof(HasSavedPhone));
        OnPropertyChanged(nameof(OrdersCount));
        OnPropertyChanged(nameof(OrdersCountText));
        OnPropertyChanged(nameof(HasSearchError));
    }

    private static string NormalizePhoneDigits(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        return new string(phone.Where(char.IsDigit).ToArray());
    }
}
