using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

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
    private string searchErrorMessage = string.Empty;

    public ObservableCollection<CustomerOrderDto> Orders { get; } = new();

    public bool HasOrders => Orders.Count > 0;
    public bool IsEmptyState => !IsBusy && !IsSearching && HasSearched && !HasOrders;
    public bool HasSavedPhone => !string.IsNullOrWhiteSpace(SavedPhoneNumber) &&
                                 !string.Equals(PhoneNumber.Trim(), SavedPhoneNumber.Trim(), StringComparison.OrdinalIgnoreCase);

    public OrderHistoryViewModel(OnlineOrderService orders, SessionService session, CartService cart)
    {
        _orders = orders;
        _session = session;
        _cart = cart;
        Title = "Order History";
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        SavedPhoneNumber = Preferences.Default.Get("sp.customerPhone", string.Empty).Trim();

        // If phone is empty, auto-populate from saved
        if (string.IsNullOrWhiteSpace(PhoneNumber) && !string.IsNullOrWhiteSpace(SavedPhoneNumber))
        {
            PhoneNumber = SavedPhoneNumber;
        }

        // If we have a valid phone and haven't loaded orders yet, trigger auto-search
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
    private async Task SearchOrdersAsync()
    {
        var digits = NormalizePhoneDigits(PhoneNumber);
        if (digits.Length < 7)
        {
            SearchErrorMessage = "Please enter a valid phone number (at least 7 digits).";
            Orders.Clear();
            HasSearched = true;
            NotifyOrderState();
            return;
        }

        SearchErrorMessage = string.Empty;

        // Remember phone number for future sessions
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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OrderHistoryViewModel] Search failed: {ex}");
            SearchErrorMessage = ex.Message ?? "Failed to fetch orders. Please check your connection.";
            Orders.Clear();
            HasSearched = true;
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
    private async Task ReorderAsync(CustomerOrderDto? order)
    {
        if (order == null || !order.CanReorder)
        {
            if (Shell.Current != null)
            {
                await Shell.Current.DisplayAlert("Re-order", "This order does not have any items available to re-order.", "OK");
            }
            return;
        }

        var clearCart = true;
        if (_cart.Lines.Count > 0)
        {
            if (Shell.Current != null)
            {
                var action = await Shell.Current.DisplayActionSheet(
                    "You have items in your cart. How would you like to proceed?",
                    "Cancel",
                    null,
                    "Replace Cart with this Order",
                    "Add to Existing Cart Items");

                if (action == "Cancel" || string.IsNullOrWhiteSpace(action))
                {
                    return;
                }

                clearCart = action.StartsWith("Replace", StringComparison.OrdinalIgnoreCase);
            }
        }

        var count = _cart.ReorderFromCustomerOrder(order, clearCart);
        if (count > 0)
        {
            if (Shell.Current != null)
            {
                await Shell.Current.DisplayAlert(
                    "Added to Cart",
                    $"{count} item(s) from order #{order.FormattedOrderNo} added to your cart.",
                    "Checkout Now");

                await Shell.Current.GoToAsync("//customer/cart");
            }
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

    private void NotifyOrderState()
    {
        OnPropertyChanged(nameof(HasOrders));
        OnPropertyChanged(nameof(IsEmptyState));
        OnPropertyChanged(nameof(HasSavedPhone));
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

