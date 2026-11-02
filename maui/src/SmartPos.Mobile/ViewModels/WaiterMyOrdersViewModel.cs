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

    public ObservableCollection<RestaurantOrderDto> Orders { get; } = new();

    public WaiterMyOrdersViewModel(RestaurantService restaurant, SessionService session, CartService cart)
    {
        _restaurant = restaurant;
        _session = session;
        _cart = cart;
        Title = "My Orders";
    }

    [RelayCommand]
    private async Task AppearingAsync() => await RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_session.BranchId is not int branchId)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            var all = await _restaurant.GetActiveOrdersAsync(branchId);
            var waiter = _session.User?.DisplayName ?? string.Empty;
            Orders.Clear();
            foreach (var order in all.Where(o =>
                         string.IsNullOrWhiteSpace(waiter) ||
                         string.Equals(o.WaiterName, waiter, StringComparison.OrdinalIgnoreCase)))
            {
                Orders.Add(order);
            }
        });
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
