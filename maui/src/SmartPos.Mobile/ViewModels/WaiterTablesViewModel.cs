using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public partial class WaiterTablesViewModel : BaseViewModel
{
    private readonly RestaurantService _restaurant;
    private readonly SessionService _session;
    private readonly AuthService _auth;
    private readonly CartService _cart;

    [ObservableProperty]
    private bool myTablesOnly;

    [ObservableProperty]
    private string branchName = string.Empty;

    [ObservableProperty]
    private string availableCount = "0";

    [ObservableProperty]
    private string occupiedCount = "0";

    [ObservableProperty]
    private string billedCount = "0";

    public ObservableCollection<RestaurantTableDto> Tables { get; } = new();

    private IDispatcherTimer? _clock;

    public WaiterTablesViewModel(RestaurantService restaurant, SessionService session, AuthService auth, CartService cart)
    {
        _restaurant = restaurant;
        _session = session;
        _auth = auth;
        _cart = cart;
        Title = "Tables";
        BranchName = session.User?.BranchName ?? "Branch";
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        StartClock();
        await RefreshAsync();
    }

    [RelayCommand]
    private void Disappearing() => StopClock();

    private void StartClock()
    {
        if (Application.Current == null)
        {
            return;
        }

        _clock ??= Application.Current.Dispatcher.CreateTimer();
        _clock.Interval = TimeSpan.FromSeconds(1);
        _clock.Tick -= OnClockTick;
        _clock.Tick += OnClockTick;
        if (!_clock.IsRunning)
        {
            _clock.Start();
        }

        OnClockTick(this, EventArgs.Empty);
    }

    private void StopClock()
    {
        if (_clock != null)
        {
            _clock.Tick -= OnClockTick;
            _clock.Stop();
        }
    }

    private void OnClockTick(object? sender, EventArgs e)
    {
        foreach (var table in Tables)
        {
            table.RefreshElapsed();
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_session.BranchId is not int branchId)
        {
            await Shell.Current.DisplayAlert("Tables", "No branch assigned to this waiter.", "OK");
            return;
        }

        await RunBusyAsync(async () =>
        {
            var all = await _restaurant.GetTablesAsync(branchId);
            var waiterName = _session.User?.DisplayName ?? string.Empty;
            var filtered = MyTablesOnly
                ? all.Where(t =>
                    string.Equals(t.WaiterName, waiterName, StringComparison.OrdinalIgnoreCase) ||
                    t.IsFree ||
                    string.IsNullOrWhiteSpace(t.WaiterName)).ToList()
                : all;

            Tables.Clear();
            var active = filtered.Where(t => t.IsActive).ToList();
            foreach (var table in active)
            {
                Tables.Add(table);
            }

            AvailableCount = active.Count(t => t.IsFree).ToString();
            OccupiedCount = active.Count(t => t.IsOccupied).ToString();
            BilledCount = active.Count(t => t.IsBilled).ToString();

            await FillMissingStartTimesAsync(active);
            OnClockTick(this, EventArgs.Empty);
        });
    }

    private async Task FillMissingStartTimesAsync(List<RestaurantTableDto> tables)
    {
        var pending = tables
            .Where(t => t.CurrentOrderId is > 0 && t.OrderStartedAt is null)
            .ToList();

        if (pending.Count == 0)
        {
            return;
        }

        await Task.WhenAll(pending.Select(async table =>
        {
            try
            {
                var order = await _restaurant.GetOrderAsync(table.CurrentOrderId!.Value);
                if (order.CreationTime != default)
                {
                    table.OrderStartedAt = order.CreationTime;
                }
            }
            catch
            {
                // Timer stays hidden if this order cannot be loaded.
            }
        }));
    }

    partial void OnMyTablesOnlyChanged(bool value) => _ = RefreshAsync();

    [RelayCommand]
    private async Task OpenTableAsync(RestaurantTableDto? table)
    {
        if (table == null || _session.BranchId is not int branchId)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            RestaurantOrderDto order;
            if (table.CurrentOrderId is int orderId && orderId > 0)
            {
                order = await _restaurant.GetOrderAsync(orderId);
            }
            else if (table.IsFree)
            {
                string? carNumber = null;
                if (table.IsCarBay)
                {
                    carNumber = await Shell.Current.DisplayPromptAsync("Car bay", "Vehicle / plate number", "Open", "Cancel");
                    if (string.IsNullOrWhiteSpace(carNumber))
                    {
                        return;
                    }
                }

                order = await _restaurant.OpenTableSessionAsync(new OpenTableSessionDto
                {
                    BranchId = branchId,
                    TableId = table.Id,
                    WaiterName = _session.User?.DisplayName,
                    CarNumber = carNumber
                });

                await _restaurant.AssignWaiterAsync(new AssignWaiterDto
                {
                    TableId = table.Id,
                    WaiterName = _session.User?.DisplayName
                });
            }
            else
            {
                order = await _restaurant.GetByTableAsync(table.Id);
            }

            _cart.LoadFromOrder(order);
            WaiterOrderSession.CurrentOrder = order;
            WaiterOrderSession.CurrentTable = table;
            await Shell.Current.GoToAsync("//waiter/order");
        });
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _cart.Clear();
        WaiterOrderSession.Clear();
        await _auth.LogoutAsync();
        await Shell.Current.GoToAsync("//customer/home");
    }
}

public static class WaiterOrderSession
{
    public static RestaurantOrderDto? CurrentOrder { get; set; }
    public static RestaurantTableDto? CurrentTable { get; set; }

    public static void Clear()
    {
        CurrentOrder = null;
        CurrentTable = null;
    }
}
