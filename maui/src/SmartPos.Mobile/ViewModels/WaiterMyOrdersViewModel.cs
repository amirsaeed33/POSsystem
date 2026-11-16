using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public partial class WaiterMyOrdersViewModel : BaseViewModel
{
    private readonly RestaurantService _restaurant;
    private readonly SessionService _session;
    private readonly CartService _cart;
    private readonly StaffDataCache _staffCache;

    public ObservableCollection<RestaurantOrderDto> Orders { get; } = new();

    public WaiterMyOrdersViewModel(
        RestaurantService restaurant,
        SessionService session,
        CartService cart,
        StaffDataCache staffCache)
    {
        _restaurant = restaurant;
        _session = session;
        _cart = cart;
        _staffCache = staffCache;
        Title = "My Orders";
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        try
        {
            await LoadOrdersAsync(forceRefresh: false);
        }
        finally
        {
            LoadingService.Current.HideTabLoader();
        }
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadOrdersAsync(forceRefresh: true);

    private async Task LoadOrdersAsync(bool forceRefresh)
    {
        if (_session.BranchId is not int branchId)
        {
            return;
        }

        if (!forceRefresh &&
            _staffCache.TryGetActiveOrders(branchId, out var cached) &&
            cached != null)
        {
            ApplyOrders(cached);
            _ = SoftRefreshOrdersInBackgroundAsync(branchId);
            return;
        }

        await RunBusyAsync(async () =>
        {
            var all = await _restaurant.GetActiveOrdersAsync(branchId);
            _staffCache.SetActiveOrders(branchId, all);
            ApplyOrders(all);
        });
    }

    private async Task SoftRefreshOrdersInBackgroundAsync(int branchId)
    {
        try
        {
            var all = await _restaurant.GetActiveOrdersAsync(branchId);
            _staffCache.SetActiveOrders(branchId, all);
            MainThread.BeginInvokeOnMainThread(() => ApplyOrders(all));
        }
        catch
        {
            // Keep cached list.
        }
    }

    private void ApplyOrders(IEnumerable<RestaurantOrderDto> all)
    {
        var waiter = _session.User?.DisplayName ?? string.Empty;
        Orders.Clear();
        foreach (var order in all.Where(o =>
                     string.IsNullOrWhiteSpace(waiter) ||
                     string.Equals(o.WaiterName, waiter, StringComparison.OrdinalIgnoreCase)))
        {
            Orders.Add(order);
        }
    }

    [RelayCommand]
    private async Task OpenOrderAsync(RestaurantOrderDto? order)
    {
        if (order == null) return;
        await RunBusyAsync(async () =>
        {
            var full = await _restaurant.GetOrderAsync(order.Id);
            WaiterOrderSession.CurrentOrder = full;
            WaiterOrderSession.CurrentTable = full.TableId is int tid
                ? new RestaurantTableDto { Id = tid, TableNo = full.TableNo }
                : null;
            _cart.LoadFromOrder(full);
            await Shell.Current.GoToAsync("//waiter/order");
        });
    }
}
