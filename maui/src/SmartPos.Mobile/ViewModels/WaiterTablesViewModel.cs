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
    private readonly StaffDataCache _staffCache;

    private List<RestaurantTableDto> _allTables = new();
    private IDispatcherTimer? _clock;

    [ObservableProperty]
    private bool isMyTablesOnly;

    [ObservableProperty]
    private string branchName = string.Empty;

    [ObservableProperty]
    private string branchEyebrow = "GULBERG III FLAGSHIP  •  FLOOR OPS";

    [ObservableProperty]
    private string waiterBadgeText = "Waiter";

    [ObservableProperty]
    private string freeCount = "0";

    [ObservableProperty]
    private string busyCount = "0";

    [ObservableProperty]
    private string billedCount = "0";

    [ObservableProperty]
    private string gridCountLabel = "Showing 0 Floor Tables";

    [ObservableProperty]
    private string? activeStatusFilter;

    [ObservableProperty]
    private string activeZoneFilter = "All";

    [ObservableProperty]
    private bool isZoneAll = true;

    [ObservableProperty]
    private bool isZoneLounge;

    [ObservableProperty]
    private bool isZoneFamily;

    [ObservableProperty]
    private bool isZoneMajlis;

    [ObservableProperty]
    private bool isZoneCourtyard;

    [ObservableProperty]
    private bool isStatusFilterFree;

    [ObservableProperty]
    private bool isStatusFilterBusy;

    [ObservableProperty]
    private bool isStatusFilterBilled;

    [ObservableProperty]
    private RestaurantTableDto? selectedTable;

    [ObservableProperty]
    private bool hasSelectedTable;

    [ObservableProperty]
    private string selectedTableTitle = string.Empty;

    [ObservableProperty]
    private string selectedDockEyebrow = "SELECTED TABLE";

    [ObservableProperty]
    private string selectedDockTitle = string.Empty;

    [ObservableProperty]
    private string selectedDockSubtitle = string.Empty;

    [ObservableProperty]
    private string primaryActionButtonText = "Start Order →";

    [ObservableProperty]
    private string startOrderText = "Start order";

    [ObservableProperty]
    private bool isKotDrawerOpen;

    [ObservableProperty]
    private int drawerGuestCount = 2;

    [ObservableProperty]
    private string drawerTitle = string.Empty;

    [ObservableProperty]
    private string drawerSubtitle = string.Empty;

    [ObservableProperty]
    private string drawerStatusLabel = "FREE";

    [ObservableProperty]
    private string drawerRunningTotalText = "Rs. 0";

    [ObservableProperty]
    private string drawerGuestCountText = "Guests: 2";

    [ObservableProperty]
    private bool drawerHasItems;

    [ObservableProperty]
    private string drawerEmptyItemsText = "No active items yet — open the menu to start the KOT.";

    public ObservableCollection<RestaurantTableDto> Tables { get; } = new();

    public ObservableCollection<RestaurantTableDto> FilteredTables { get; } = new();

    public ObservableCollection<WaiterKotLineItem> DrawerKotLines { get; } = new();

    // Back-compat aliases used by older bindings / plan naming.
    public string AvailableCount
    {
        get => FreeCount;
        set => FreeCount = value;
    }

    public string OccupiedCount
    {
        get => BusyCount;
        set => BusyCount = value;
    }

    public bool MyTablesOnly
    {
        get => IsMyTablesOnly;
        set => IsMyTablesOnly = value;
    }

    public WaiterTablesViewModel(
        RestaurantService restaurant,
        SessionService session,
        AuthService auth,
        CartService cart,
        StaffDataCache staffCache)
    {
        _staffCache = staffCache;
        _restaurant = restaurant;
        _session = session;
        _auth = auth;
        _cart = cart;
        Title = "Tables";
        BranchName = session.User?.BranchName ?? "Branch";
        BranchEyebrow = string.IsNullOrWhiteSpace(BranchName)
            ? "GULBERG III FLAGSHIP  •  FLOOR OPS"
            : $"{BranchName.ToUpperInvariant()}  •  FLOOR OPS";
        WaiterBadgeText = BuildWaiterBadge();
    }

    private string BuildWaiterBadge()
    {
        var name = _session.User?.DisplayName ?? "Waiter";
        var id = _session.User?.Id ?? 0;
        var code = id > 0 ? $"W-{id % 100:00}" : "W-00";
        return $"{name} • {code}";
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        try
        {
            WaiterBadgeText = BuildWaiterBadge();
            BranchName = _session.User?.BranchName ?? BranchName;
            BranchEyebrow = string.IsNullOrWhiteSpace(BranchName)
                ? "GULBERG III FLAGSHIP  •  FLOOR OPS"
                : $"{BranchName.ToUpperInvariant()}  •  FLOOR OPS";
            StartClock();
            await LoadTablesAsync(forceRefresh: false);
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

        if (SelectedTable != null)
        {
            UpdateDockStrings(SelectedTable);
            if (IsKotDrawerOpen)
            {
                UpdateDrawerHeader(SelectedTable);
            }
        }
    }

    [RelayCommand]
    private Task RefreshTablesAsync() => LoadTablesAsync(forceRefresh: true);

    private async Task LoadTablesAsync(bool forceRefresh)
    {
        if (_session.BranchId is not int branchId)
        {
            await AljannatDialogService.Current.ShowAlertAsync("Tables", "No branch assigned to this waiter.", "OK");
            return;
        }

        if (!forceRefresh &&
            _staffCache.TryGetTables(branchId, out var cached) &&
            cached is { Count: > 0 })
        {
            ApplyTablesPayload(cached, skipHeavyPrefetch: true);
            OnClockTick(this, EventArgs.Empty);
            _ = SoftRefreshTablesInBackgroundAsync(branchId);
            return;
        }

        await RunBusyAsync(async () =>
        {
            var all = await _restaurant.GetTablesAsync(branchId);
            _staffCache.SetTables(branchId, all);
            ApplyTablesPayload(all, skipHeavyPrefetch: false);
            OnClockTick(this, EventArgs.Empty);
        });
    }

    private async Task SoftRefreshTablesInBackgroundAsync(int branchId)
    {
        try
        {
            var all = await _restaurant.GetTablesAsync(branchId);
            _staffCache.SetTables(branchId, all);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ApplyTablesPayload(all, skipHeavyPrefetch: true);
                OnClockTick(this, EventArgs.Empty);
            });
            await FillMissingStartTimesAsync(_allTables);
            await PrefetchRunningBillsAsync(_allTables.Where(t => !t.IsFree).ToList());
        }
        catch
        {
            // Keep showing the cached floor map.
        }
    }

    private void ApplyTablesPayload(IReadOnlyList<RestaurantTableDto> all, bool skipHeavyPrefetch)
    {
        _allTables = all.Where(t => t.IsActive).ToList();
        var waiterName = _session.User?.DisplayName ?? string.Empty;

        foreach (var table in _allTables)
        {
            if (string.IsNullOrWhiteSpace(table.WaiterName))
            {
                table.AssignedWaiterLabel = "Unassigned";
            }
            else if (string.Equals(table.WaiterName, waiterName, StringComparison.OrdinalIgnoreCase))
            {
                table.AssignedWaiterLabel = $"{table.WaiterName} (You)";
            }
            else
            {
                table.AssignedWaiterLabel = table.WaiterName!;
            }
        }

        FreeCount = _allTables.Count(t => t.IsFree).ToString();
        BusyCount = _allTables.Count(t => t.IsOccupied).ToString();
        BilledCount = _allTables.Count(t => t.IsBilled).ToString();
        OnPropertyChanged(nameof(AvailableCount));
        OnPropertyChanged(nameof(OccupiedCount));

        var previousId = SelectedTable?.Id;
        ReplaceCollection(Tables, _allTables);
        ApplyFilters();

        var stillSelected = previousId is int id
            ? _allTables.FirstOrDefault(t => t.Id == id)
            : SelectedTable;
        ApplySelection(stillSelected ?? FilteredTables.FirstOrDefault());

        if (!skipHeavyPrefetch)
        {
            _ = FillMissingStartTimesAsync(_allTables);
            _ = PrefetchRunningBillsAsync(_allTables.Where(t => !t.IsFree).ToList());
        }
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    // Alias for older RefreshCommand name used elsewhere in the page lifecycle docs.
    [RelayCommand]
    private Task RefreshAsync() => LoadTablesAsync(forceRefresh: true);

    private void ApplyFilters()
    {
        var waiterName = _session.User?.DisplayName ?? string.Empty;
        IEnumerable<RestaurantTableDto> query = _allTables;

        if (IsMyTablesOnly)
        {
            query = query.Where(t =>
                string.Equals(t.WaiterName, waiterName, StringComparison.OrdinalIgnoreCase) ||
                t.IsFree ||
                string.IsNullOrWhiteSpace(t.WaiterName));
        }

        if (!string.IsNullOrWhiteSpace(ActiveStatusFilter))
        {
            query = ActiveStatusFilter switch
            {
                "FREE" => query.Where(t => t.IsFree),
                "BUSY" => query.Where(t => t.IsOccupied),
                "BILLED" => query.Where(t => t.IsBilled),
                _ => query
            };
        }

        if (!string.Equals(ActiveZoneFilter, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t => ZoneMatches(t, ActiveZoneFilter));
        }

        var list = query.ToList();
        FilteredTables.Clear();
        foreach (var table in list)
        {
            FilteredTables.Add(table);
        }

        GridCountLabel = IsMyTablesOnly
            ? $"My Assigned Tables ({list.Count})"
            : $"Showing {list.Count} Floor {(list.Count == 1 ? "Table" : "Tables")}";
    }

    private static bool ZoneMatches(RestaurantTableDto table, string zone)
    {
        var area = table.AreaDisplay ?? string.Empty;
        return area.Contains(zone, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnIsMyTablesOnlyChanged(bool value)
    {
        OnPropertyChanged(nameof(MyTablesOnly));
        ApplyFilters();
        if (SelectedTable != null && FilteredTables.All(t => t.Id != SelectedTable.Id))
        {
            ApplySelection(FilteredTables.FirstOrDefault());
        }
    }

    [RelayCommand]
    private void ToggleMyTables() => IsMyTablesOnly = !IsMyTablesOnly;

    [RelayCommand]
    private void ToggleStatusFilter(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return;
        }

        ActiveStatusFilter = string.Equals(ActiveStatusFilter, status, StringComparison.OrdinalIgnoreCase)
            ? null
            : status.ToUpperInvariant();

        IsStatusFilterFree = ActiveStatusFilter == "FREE";
        IsStatusFilterBusy = ActiveStatusFilter == "BUSY";
        IsStatusFilterBilled = ActiveStatusFilter == "BILLED";

        ApplyFilters();
        if (SelectedTable != null && FilteredTables.All(t => t.Id != SelectedTable.Id))
        {
            ApplySelection(FilteredTables.FirstOrDefault());
        }
    }

    [RelayCommand]
    private void SelectZone(string? zone)
    {
        ActiveZoneFilter = string.IsNullOrWhiteSpace(zone) ? "All" : zone;
        IsZoneAll = ActiveZoneFilter == "All";
        IsZoneLounge = ActiveZoneFilter == "Lounge";
        IsZoneFamily = ActiveZoneFilter == "Family";
        IsZoneMajlis = ActiveZoneFilter == "Majlis";
        IsZoneCourtyard = ActiveZoneFilter == "Courtyard";
        ApplyFilters();
        if (SelectedTable != null && FilteredTables.All(t => t.Id != SelectedTable.Id))
        {
            ApplySelection(FilteredTables.FirstOrDefault());
        }
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

                table.RunningBillAmount = order.GrandTotal;
            }
            catch
            {
                // Timer stays hidden if this order cannot be loaded.
            }
        }));
    }

    private async Task PrefetchRunningBillsAsync(List<RestaurantTableDto> tables)
    {
        var pending = tables
            .Where(t => t.CurrentOrderId is > 0 && t.RunningBillAmount <= 0)
            .Take(8)
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
                table.RunningBillAmount = order.GrandTotal;
            }
            catch
            {
                // Keep card without bill amount.
            }
        }));
    }

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
        UpdateDockStrings(table);

        if (table != null)
        {
            DrawerGuestCount = table.Capacity > 0 ? table.Capacity : 2;
            UpdateDrawerGuestText();
        }
    }

    private void UpdateDockStrings(RestaurantTableDto? table)
    {
        if (table == null)
        {
            SelectedDockEyebrow = "SELECTED TABLE";
            SelectedDockTitle = string.Empty;
            SelectedDockSubtitle = string.Empty;
            PrimaryActionButtonText = "Start Order →";
            return;
        }

        SelectedDockEyebrow = $"SELECTED TABLE  •  {table.FloorStatusLabel}";
        SelectedDockTitle = $"{table.DisplayTitle}  •  {table.AreaDisplay}";
        var waiter = string.IsNullOrWhiteSpace(table.WaiterName) ? "Unassigned" : table.WaiterName!;
        var bill = table.HasRunningBill ? table.RunningBillText : "Rs. 0";
        SelectedDockSubtitle = table.IsFree
            ? $"Waiter: {waiter}  •  Ready for guests"
            : $"Waiter: {waiter}  •  Running: {bill}";

        PrimaryActionButtonText = table.IsFree
            ? "Start Order →"
            : table.IsBilled
                ? "Settle Bill →"
                : "Add Items / KOT →";
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
    private async Task OpenKotDrawerAsync()
    {
        var table = SelectedTable;
        if (table == null)
        {
            await AljannatDialogService.Current.ShowAlertAsync("Select a table", "Choose a table first.", "OK");
            return;
        }

        DrawerGuestCount = table.Capacity > 0 ? Math.Max(1, table.Capacity) : Math.Max(1, DrawerGuestCount);
        UpdateDrawerGuestText();
        UpdateDrawerHeader(table);
        DrawerKotLines.Clear();
        DrawerHasItems = false;
        DrawerRunningTotalText = "Rs. 0";
        DrawerEmptyItemsText = table.IsFree
            ? "Table is free — seat guests and open the menu to start the KOT."
            : "Loading active table items…";

        IsKotDrawerOpen = true;

        if (!table.IsFree)
        {
            try
            {
                var order = await ResolveLiveOrderAsync(table);
                PopulateDrawerFromOrder(order);
                if (order != null)
                {
                    table.RunningBillAmount = order.GrandTotal;
                    UpdateDockStrings(table);
                }
            }
            catch
            {
                DrawerEmptyItemsText = "Could not load active items for this table.";
            }
        }
    }

    private void UpdateDrawerHeader(RestaurantTableDto table)
    {
        DrawerTitle = $"{table.DisplayTitle} • {table.AreaDisplay}";
        var waiter = string.IsNullOrWhiteSpace(table.WaiterName)
            ? (_session.User?.DisplayName ?? "Waiter")
            : table.WaiterName!;
        var session = table.IsFree ? "Ready to seat" : $"Active Session {table.ElapsedFloorText}";
        DrawerSubtitle = $"Waiter: {waiter} • {session}";
        DrawerStatusLabel = table.FloorStatusLabel;
    }

    private void PopulateDrawerFromOrder(RestaurantOrderDto? order)
    {
        DrawerKotLines.Clear();
        if (order == null || order.Lines == null || order.Lines.Count == 0)
        {
            DrawerHasItems = false;
            DrawerRunningTotalText = "Rs. 0";
            DrawerEmptyItemsText = "No active items yet — open the menu to add to the KOT.";
            return;
        }

        foreach (var line in order.Lines)
        {
            var qty = line.Quantity % 1 == 0
                ? ((int)line.Quantity).ToString()
                : line.Quantity.ToString("0.##");
            DrawerKotLines.Add(new WaiterKotLineItem
            {
                QuantityLabel = $"{qty}×",
                ProductName = line.ProductName ?? "Item",
                LineTotalText = $"Rs. {line.LineTotal:N0}"
            });
        }

        DrawerHasItems = DrawerKotLines.Count > 0;
        DrawerRunningTotalText = $"Rs. {order.GrandTotal:N0}";
    }

    [RelayCommand]
    private void CloseKotDrawer() => IsKotDrawerOpen = false;

    [RelayCommand]
    private void IncreaseDrawerGuests()
    {
        if (DrawerGuestCount >= 20)
        {
            return;
        }

        DrawerGuestCount++;
        UpdateDrawerGuestText();
    }

    [RelayCommand]
    private void DecreaseDrawerGuests()
    {
        if (DrawerGuestCount <= 1)
        {
            return;
        }

        DrawerGuestCount--;
        UpdateDrawerGuestText();
    }

    private void UpdateDrawerGuestText() => DrawerGuestCountText = $"Guests: {DrawerGuestCount}";

    [RelayCommand]
    private async Task PrintOrViewBillAsync()
    {
        var table = SelectedTable;
        if (table == null)
        {
            await AljannatDialogService.Current.ShowAlertAsync("Bill", "Choose a table first.", "OK");
            return;
        }

        if (table.IsFree)
        {
            await AljannatDialogService.Current.ShowAlertAsync("Bill", "This table has no running order yet.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            var order = await ResolveLiveOrderAsync(table);
            if (order == null)
            {
                await AljannatDialogService.Current.ShowAlertAsync("Bill", "No live order found for this table.", "OK");
                return;
            }

            _cart.LoadFromOrder(order);
            WaiterOrderSession.CurrentOrder = order;
            WaiterOrderSession.CurrentTable = table;
            IsKotDrawerOpen = false;
            await Shell.Current.GoToAsync("waiter/cart");
        }
        catch (Exception ex)
        {
            await AljannatDialogService.Current.ShowAlertAsync("Bill", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenMenuFromDrawerAsync()
    {
        IsKotDrawerOpen = false;
        await OpenTableAsync();
    }

    [RelayCommand]
    private async Task OpenTableAsync()
    {
        var table = SelectedTable;
        if (table == null)
        {
            await AljannatDialogService.Current.ShowAlertAsync("Select a table", "Choose a table first.", "OK");
            return;
        }

        if (_session.BranchId is not int branchId)
        {
            await AljannatDialogService.Current.ShowAlertAsync("Order", "No branch assigned to this waiter.", "OK");
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
                    carNumber = await AljannatDialogService.Current.ShowPromptAsync("Car bay", "Vehicle / plate number", "Open", "Cancel");
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
            _staffCache.InvalidateTables(branchId);
            _staffCache.InvalidateActiveOrders(branchId);
            IsKotDrawerOpen = false;
            await Shell.Current.GoToAsync("//waiter/order");
        }
        catch (Exception ex)
        {
            await AljannatDialogService.Current.ShowAlertAsync("Order", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateOrderTabAsync()
    {
        if (WaiterOrderSession.CurrentOrder == null)
        {
            await AljannatDialogService.Current.ShowAlertAsync(
                "Select a table",
                "Choose a table first, then start the order.",
                "OK");
            return;
        }

        await Shell.Current.GoToAsync("//waiter/order");
    }

    [RelayCommand]
    private async Task NavigateOrdersTabAsync() =>
        await Shell.Current.GoToAsync("//waiter/myorders");

    [RelayCommand]
    private async Task NavigateProfileTabAsync() =>
        await Shell.Current.GoToAsync("//waiter/account");

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _cart.Clear();
        WaiterOrderSession.Clear();
        await _auth.LogoutAsync();
        await Shell.Current.GoToAsync("//customer/home");
    }
}
