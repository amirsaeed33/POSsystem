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

    [ObservableProperty]
    private RestaurantTableDto? selectedTable;

    [ObservableProperty]
    private bool hasSelectedTable;

    [ObservableProperty]
    private string selectedTableTitle = string.Empty;

    [ObservableProperty]
    private string startOrderText = "Start order";

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
        try
        {
            StartClock();
            await RefreshAsync();
        }
        finally
        {
            LoadingService.Current.HideTabLoader();
        }
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

            var previousId = SelectedTable?.Id;
            Tables.Clear();
            var active = filtered.Where(t => t.IsActive).ToList();
            foreach (var table in active)
            {
                Tables.Add(table);
            }

            var stillSelected = previousId is int id
                ? active.FirstOrDefault(t => t.Id == id)
                : null;
            ApplySelection(stillSelected);

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
    private void SelectTable(RestaurantTableDto? table) => ApplySelection(table);

    private void ApplySelection(RestaurantTableDto? table)
    {
        foreach (var item in Tables)
        {
            item.IsSelected = table != null && item.Id == table.Id;
        }

        SelectedTable = table;
        HasSelectedTable = table != null;
        SelectedTableTitle = table?.DisplayTitle ?? string.Empty;
        StartOrderText = table == null || table.IsFree ? "Start order" : "Continue order";
    }

    private async Task<RestaurantOrderDto?> ResolveLiveOrderAsync(RestaurantTableDto table)
    {
        if (table.IsFree)
        {
            return null;
        }

        if (table.CurrentOrderId is int orderId && orderId > 0)
        {
            var byId = await TryGetOrderAsync(orderId);
            if (IsLiveOrder(byId))
            {
                return byId;
            }
        }

        var byTable = await TryGetByTableAsync(table.Id);
        return IsLiveOrder(byTable) ? byTable : null;
    }

    private async Task<RestaurantOrderDto?> TryGetOrderAsync(int orderId)
    {
        try
        {
            return await _restaurant.GetOrderAsync(orderId);
        }
        catch (ApiException ex) when (IsMissingOrder(ex))
        {
            return null;
        }
    }

    private async Task<RestaurantOrderDto?> TryGetByTableAsync(int tableId)
    {
        try
        {
            return await _restaurant.GetByTableAsync(tableId);
        }
        catch (ApiException ex) when (IsMissingOrder(ex))
        {
            return null;
        }
    }

    private static bool IsLiveOrder(RestaurantOrderDto? order) =>
        order != null && order.Id > 0 && order.Status is 0 or 1;

    private static bool IsMissingOrder(Exception ex) =>
        ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase);

    [RelayCommand]
    private async Task OpenTableAsync()
    {
        var table = SelectedTable;
        if (table == null)
        {
            await Shell.Current.DisplayAlert("Select a table", "Choose a table first.", "OK");
            return;
        }

        if (_session.BranchId is not int branchId)
        {
            await Shell.Current.DisplayAlert("Order", "No branch assigned to this waiter.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            var order = await ResolveLiveOrderAsync(table);

            if (order == null)
            {
                if (!table.IsFree)
                {
                    await _restaurant.ChangeStatusAsync(new ChangeTableStatusDto
                    {
                        TableId = table.Id,
                        Status = 0,
                        CurrentOrderId = null
                    });
                    table.Status = 0;
                    table.CurrentOrderId = null;
                }

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

                try
                {
                    await _restaurant.AssignWaiterAsync(new AssignWaiterDto
                    {
                        TableId = table.Id,
                        WaiterName = _session.User?.DisplayName
                    });
                }
                catch
                {
                    // The table session is already open. Waiter name can be set later.
                }
            }

            _cart.LoadFromOrder(order);
            WaiterOrderSession.CurrentOrder = order;
            WaiterOrderSession.CurrentTable = table;
            await Shell.Current.GoToAsync("//waiter/order");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Order", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
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
