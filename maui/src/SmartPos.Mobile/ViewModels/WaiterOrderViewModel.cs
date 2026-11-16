using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public partial class WaiterCategoryChip : ObservableObject
{
    public string Name { get; set; } = string.Empty;

    [ObservableProperty]
    private bool isSelected;
}

public partial class WaiterOrderViewModel : BaseViewModel
{
    private readonly RestaurantService _restaurant;
    private readonly SessionService _session;
    private readonly CartService _cart;
    private readonly AbpApiClient _api;
    private List<ProductDto> _allProducts = new();

    [ObservableProperty]
    private string orderTitle = "No table selected";

    [ObservableProperty]
    private string selectedTableEyebrow = "SELECT TABLE • KOT POS";

    [ObservableProperty]
    private string customerLabel = "Walk-in Guest";

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private string selectedCategory = "All";

    [ObservableProperty]
    private string kotMessage = string.Empty;

    [ObservableProperty]
    private PosCustomerDto? selectedCustomer;

    [ObservableProperty]
    private bool isKotReviewSheetOpen;

    [ObservableProperty]
    private bool isCustomerPickerOpen;

    [ObservableProperty]
    private string customerSearchQuery = string.Empty;

    [ObservableProperty]
    private string kotChefNote = string.Empty;

    [ObservableProperty]
    private string kotReviewTableTitle = "Current Tray";

    [ObservableProperty]
    private string kotReviewGuestLabel = "Walk-in Guest";

    private int _customerId;
    private bool _lastSaveOk;

    public ObservableCollection<PosCustomerDto> Customers { get; } = new();
    public ObservableCollection<PosCustomerDto> FilteredCustomers { get; } = new();
    public ObservableCollection<ProductDto> Products { get; } = new();
    public ObservableCollection<ProductDto> FilteredMenuItems => Products;
    public ObservableCollection<WaiterCategoryChip> Categories { get; } = new();
    public ObservableCollection<CartLine> Lines => _cart.Lines;
    public ObservableCollection<KitchenSlipItemDto> PendingKotItems { get; } = new();

    /// <summary>Legacy alias used by older bindings / cart page refresh.</summary>
    public string SearchText
    {
        get => SearchQuery;
        set => SearchQuery = value;
    }

    public decimal Subtotal => _cart.Subtotal;
    public string SubtotalText => Subtotal.ToString("0.00");
    public int CartCount => _cart.ItemCount;
    public int KotCartCount => CartCount;
    public bool HasCartItems => CartCount > 0;
    public bool HasNoMenuResults => Products.Count == 0;
    public string KotCartTotalText => $"Rs. {Subtotal:N0}";
    public string KotTrayEyebrow =>
        CartCount <= 0
            ? "ACTIVE KOT TRAY"
            : $"READY FOR KITCHEN KOT  •  {CartCount} {(CartCount == 1 ? "ITEM" : "ITEMS")}";
    public string KotBarSummaryText =>
        CartCount <= 0
            ? "No items yet"
            : $"{CartCount} {(CartCount == 1 ? "Item" : "Items")} • {KotCartTotalText}";
    public string StickyDockEyebrow =>
        CartCount <= 0
            ? "ACTIVE KOT TRAY"
            : $"ACTIVE KOT TRAY  •  {CartCount} {(CartCount == 1 ? "ITEM" : "ITEMS")}";

    public WaiterOrderViewModel(RestaurantService restaurant, SessionService session, CartService cart, AbpApiClient api)
    {
        _restaurant = restaurant;
        _session = session;
        _cart = cart;
        _api = api;
        Title = "Order";
        _cart.CartChanged += (_, _) =>
        {
            NotifyCartChanged();
            SyncCartQtyBadges();
        };
    }

    public void RefreshCartDisplay()
    {
        RefreshTableContextLabels();
        NotifyCartChanged();
    }

    private void NotifyCartChanged()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(SubtotalText));
        OnPropertyChanged(nameof(CartCount));
        OnPropertyChanged(nameof(KotCartCount));
        OnPropertyChanged(nameof(HasCartItems));
        OnPropertyChanged(nameof(KotCartTotalText));
        OnPropertyChanged(nameof(KotTrayEyebrow));
        OnPropertyChanged(nameof(KotBarSummaryText));
        OnPropertyChanged(nameof(StickyDockEyebrow));
        OnPropertyChanged(nameof(Lines));
        RefreshKotReviewLabels();
    }

    private void RefreshTableContextLabels()
    {
        var order = WaiterOrderSession.CurrentOrder;
        var table = WaiterOrderSession.CurrentTable;
        if (order == null)
        {
            OrderTitle = "Select a table first";
            SelectedTableEyebrow = "SELECT TABLE • KOT POS";
            CustomerLabel = "Walk-in Guest";
            KotReviewTableTitle = "No table selected";
            return;
        }

        var tableTitle = table?.DisplayTitle ?? order.TableNo ?? "Table";
        var area = table?.AreaDisplay ?? "Dining";
        OrderTitle = $"{tableTitle} · {order.OrderNo}";
        SelectedTableEyebrow = $"{tableTitle.ToUpperInvariant()} • {area.ToUpperInvariant()} • KOT POS";
        RefreshCustomerLabel();
        RefreshKotReviewLabels();
    }

    private void RefreshCustomerLabel()
    {
        var table = WaiterOrderSession.CurrentTable;
        var tableTitle = table?.DisplayTitle ?? WaiterOrderSession.CurrentOrder?.TableNo ?? "Table";
        var guests = table?.Capacity > 0 ? $"{table.Capacity} Guests" : "Open seating";
        var name = !string.IsNullOrWhiteSpace(SelectedCustomer?.Name)
            ? SelectedCustomer!.Name!
            : (!string.IsNullOrWhiteSpace(WaiterOrderSession.CurrentOrder?.CustomerName)
                ? WaiterOrderSession.CurrentOrder!.CustomerName!
                : "Walk-in Guest");
        CustomerLabel = $"{name} • {tableTitle} ({guests})";
        KotReviewGuestLabel = CustomerLabel;
    }

    private void RefreshKotReviewLabels()
    {
        var table = WaiterOrderSession.CurrentTable;
        var order = WaiterOrderSession.CurrentOrder;
        var tableTitle = table?.DisplayTitle ?? order?.TableNo ?? "Table";
        var area = table?.AreaDisplay ?? "Dining";
        KotReviewTableTitle = $"{tableTitle} • {area}";
        RefreshCustomerLabel();
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        var order = WaiterOrderSession.CurrentOrder;
        if (order == null)
        {
            OrderTitle = "Select a table first";
            SelectedTableEyebrow = "SELECT TABLE • KOT POS";
            LoadingService.Current.HideTabLoader();
            await Shell.Current.GoToAsync("//waiter/tables");
            return;
        }

        RefreshTableContextLabels();

        try
        {
            if (!Customers.Any())
            {
                await LoadCustomersAsync();
            }
            else
            {
                ApplyCustomerSelection();
                RebuildFilteredCustomers();
            }

            if (!_allProducts.Any())
            {
                await LoadProductsAsync();
            }
            else
            {
                SyncCartQtyBadges();
            }

            NotifyCartChanged();
        }
        finally
        {
            LoadingService.Current.HideTabLoader();
        }
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            var list = await _restaurant.GetPosCustomersAsync();
            Customers.Clear();
            foreach (var customer in list.OrderBy(c => c.Name))
            {
                Customers.Add(customer);
            }
        }
        catch
        {
            // Order can still be taken if the customer list cannot be loaded.
        }

        ApplyCustomerSelection();
        RebuildFilteredCustomers();
    }

    private void ApplyCustomerSelection()
    {
        var currentId = WaiterOrderSession.CurrentOrder?.CustomerId;
        var match = currentId is int id && id > 0
            ? Customers.FirstOrDefault(c => c.Id == id)
            : null;
        match ??= Customers.FirstOrDefault(c =>
            (c.Name ?? string.Empty).Contains("walk", StringComparison.OrdinalIgnoreCase));
        match ??= Customers.FirstOrDefault();
        if (match != null)
        {
            ChooseCustomer(match);
        }
        else
        {
            RefreshCustomerLabel();
        }
    }

    public void ChooseCustomer(PosCustomerDto? customer)
    {
        if (customer == null || customer.Id <= 0)
        {
            return;
        }

        _customerId = customer.Id;
        if (!ReferenceEquals(SelectedCustomer, customer))
        {
            SelectedCustomer = customer;
        }

        var order = WaiterOrderSession.CurrentOrder;
        if (order != null)
        {
            order.CustomerId = customer.Id;
            order.CustomerName = customer.Name;
        }

        RefreshCustomerLabel();
        _ = PersistCustomerAsync(customer.Id);
    }

    private async Task PersistCustomerAsync(int customerId)
    {
        var order = WaiterOrderSession.CurrentOrder;
        if (order == null || order.Id <= 0 || customerId <= 0)
        {
            return;
        }

        try
        {
            var saved = await _restaurant.SetOrderCustomerAsync(order.Id, customerId);
            if (saved != null)
            {
                saved.CustomerId ??= customerId;
                WaiterOrderSession.CurrentOrder = saved;
                RefreshTableContextLabels();
            }
        }
        catch
        {
            // The current server stores CustomerId when the order is saved.
        }
    }

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        await RunBusyAsync(async () =>
        {
            _allProducts = await _restaurant.GetPosProductsAsync();
            foreach (var p in _allProducts)
            {
                var url = ProductImageResolver.Resolve(
                    _api.ResolveImageUrl, p.ImagePath, p.Name, p.CategoryName, p.Id);
                p.ImageUrl = url;
                if (string.IsNullOrWhiteSpace(p.ImagePath))
                {
                    p.ImagePath = url;
                }
            }

            Categories.Clear();
            Categories.Add(new WaiterCategoryChip { Name = "All", IsSelected = true });
            foreach (var cat in _allProducts.Select(p => p.CategoryName).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c))
            {
                Categories.Add(new WaiterCategoryChip { Name = cat! });
            }

            SelectedCategory = "All";
            ApplyFilters();
        });
    }

    [RelayCommand]
    private void SelectCategory(WaiterCategoryChip? category)
    {
        LoadingService.Current.ShowTabLoader();
        try
        {
            var name = string.IsNullOrWhiteSpace(category?.Name) ? "All" : category!.Name;
            SelectedCategory = name;
            foreach (var chip in Categories)
            {
                chip.IsSelected = string.Equals(chip.Name, name, StringComparison.OrdinalIgnoreCase);
            }

            ApplyFilters();
        }
        finally
        {
            LoadingService.Current.HideTabLoader();
        }
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilters();

    partial void OnCustomerSearchQueryChanged(string value) => RebuildFilteredCustomers();

    private static bool EnsureTableOpen() => WaiterOrderSession.CurrentOrder != null;

    private void ApplyFilters()
    {
        Products.Clear();
        IEnumerable<ProductDto> query = _allProducts;
        if (!string.Equals(SelectedCategory, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => string.Equals(p.CategoryName, SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            query = query.Where(p =>
                (p.Name?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Barcode?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.ItemCodeText?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Description?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        foreach (var p in query)
        {
            p.CartQty = _cart.GetQty(p.Id);
            Products.Add(p);
        }

        OnPropertyChanged(nameof(HasNoMenuResults));
        OnPropertyChanged(nameof(FilteredMenuItems));
    }

    private void SyncCartQtyBadges()
    {
        foreach (var p in _allProducts)
        {
            p.CartQty = _cart.GetQty(p.Id);
        }

        ApplyFilters();
    }

    private void RebuildFilteredCustomers()
    {
        FilteredCustomers.Clear();
        IEnumerable<PosCustomerDto> query = Customers;
        if (!string.IsNullOrWhiteSpace(CustomerSearchQuery))
        {
            var q = CustomerSearchQuery.Trim();
            query = query.Where(c =>
                (c.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.Phone?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.Display?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        foreach (var customer in query)
        {
            FilteredCustomers.Add(customer);
        }
    }

    [RelayCommand]
    private async Task OpenCartAsync()
    {
        await Shell.Current.GoToAsync("waiter/cart");
    }

    [RelayCommand]
    private void OpenKotReview()
    {
        RefreshKotReviewLabels();
        NotifyCartChanged();
        IsCustomerPickerOpen = false;
        IsKotReviewSheetOpen = true;
    }

    [RelayCommand]
    private void CloseKotReview() => IsKotReviewSheetOpen = false;

    [RelayCommand]
    private void OpenCustomerPicker()
    {
        CustomerSearchQuery = string.Empty;
        RebuildFilteredCustomers();
        IsKotReviewSheetOpen = false;
        IsCustomerPickerOpen = true;
    }

    [RelayCommand]
    private void CloseCustomerPicker() => IsCustomerPickerOpen = false;

    [RelayCommand]
    private void SelectCustomer(PosCustomerDto? customer)
    {
        if (customer == null)
        {
            return;
        }

        ChooseCustomer(customer);
        IsCustomerPickerOpen = false;
    }

    [RelayCommand]
    private void ClearKotTray()
    {
        _cart.Clear();
        KotChefNote = string.Empty;
        IsKotReviewSheetOpen = false;
        SyncCartQtyBadges();
        NotifyCartChanged();
    }

    [RelayCommand]
    private async Task FireKotAsync()
    {
        if (CartCount <= 0)
        {
            await AljannatDialogService.Current.ShowAlertAsync("KOT", "Add at least 1 dish first.", "OK");
            return;
        }

        var order = WaiterOrderSession.CurrentOrder;
        if (order != null && !string.IsNullOrWhiteSpace(KotChefNote))
        {
            var note = KotChefNote.Trim();
            order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                ? note
                : $"{order.Notes} | {note}";
        }

        await SendToKitchenAsync();
        if (_lastSaveOk)
        {
            KotChefNote = string.Empty;
            IsKotReviewSheetOpen = false;
            RefreshTableContextLabels();
            NotifyCartChanged();
        }
    }

    [RelayCommand]
    private void ResetMenuFilters()
    {
        SearchQuery = string.Empty;
        SelectCategory(Categories.FirstOrDefault(c =>
            string.Equals(c.Name, "All", StringComparison.OrdinalIgnoreCase)) ?? Categories.FirstOrDefault());
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//waiter/tables");
        }
        catch
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    [RelayCommand]
    private async Task NavigateTablesTabAsync() =>
        await Shell.Current.GoToAsync("//waiter/tables");

    [RelayCommand]
    private async Task NavigateOrdersTabAsync() =>
        await Shell.Current.GoToAsync("//waiter/myorders");

    [RelayCommand]
    private async Task NavigateProfileTabAsync() =>
        await Shell.Current.GoToAsync("//waiter/account");

    [RelayCommand]
    private async Task IncreaseCardAsync(ProductDto? product)
    {
        if (product == null || !EnsureTableOpen())
        {
            return;
        }

        if (product.CartQty > 0)
        {
            _cart.AddOrIncrement(product, 1);
            return;
        }

        await AddProductAsync(product);
    }

    [RelayCommand]
    private async Task IncrementItemAsync(ProductDto? product) => await IncreaseCardAsync(product);

    [RelayCommand]
    private void DecreaseCard(ProductDto? product)
    {
        if (product == null || product.CartQty <= 0)
        {
            return;
        }

        _cart.DecrementProduct(product.Id);
    }

    [RelayCommand]
    private void DecrementItem(ProductDto? product) => DecreaseCard(product);

    [RelayCommand]
    private async Task AddProductAsync(ProductDto? product)
    {
        if (product == null || !EnsureTableOpen())
        {
            return;
        }

        string? modifiers = null;
        var groups = await _restaurant.GetModifiersForProductAsync(product.Id);
        if (groups.Any())
        {
            var selected = new List<string>();
            decimal extra = 0;
            foreach (var group in groups)
            {
                var options = group.Options.Select(o => $"{o.Name} (+{o.Price:0.00})").ToArray();
                if (options.Length == 0) continue;
                var choice = await AljannatDialogService.Current.ShowActionSheetAsync(group.Name ?? "Options", "Skip", null, options);
                if (choice == null || choice == "Skip") continue;
                var opt = group.Options.FirstOrDefault(o => choice.StartsWith(o.Name ?? string.Empty, StringComparison.Ordinal));
                if (opt != null)
                {
                    selected.Add(opt.Name ?? choice);
                    extra += opt.Price;
                }
            }

            if (selected.Any())
            {
                modifiers = string.Join(", ", selected);
                product = new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price + extra,
                    ImagePath = product.ImagePath,
                    CategoryName = product.CategoryName,
                    CategoryId = product.CategoryId
                };
            }
        }

        var notes = await AljannatDialogService.Current.ShowPromptAsync("Kitchen notes", "Optional note for kitchen", "Add", "Skip", placeholder: "No onion, spicy...");
        _cart.AddOrIncrement(product, 1, modifiers, string.IsNullOrWhiteSpace(notes) ? null : notes);
    }

    [RelayCommand]
    private void Increase(CartLine? line)
    {
        if (line == null) return;
        _cart.SetQuantity(line, line.Quantity + 1);
    }

    [RelayCommand]
    private void Decrease(CartLine? line)
    {
        if (line == null) return;
        _cart.SetQuantity(line, line.Quantity - 1);
    }

    [RelayCommand]
    private async Task RemoveLineAsync(CartLine? line)
    {
        if (line == null)
        {
            return;
        }

        var ok = await AljannatDialogService.Current.ShowConfirmAsync("Remove item", $"Remove {line.Name} from this order?", "Remove", "Keep", isDestructive: true);
        if (!ok)
        {
            return;
        }

        _cart.Remove(line);
    }

    [RelayCommand]
    private async Task SaveOrderAsync()
    {
        var current = WaiterOrderSession.CurrentOrder;
        var table = WaiterOrderSession.CurrentTable;
        if (current == null || _session.BranchId is not int branchId)
        {
            _lastSaveOk = false;
            await AljannatDialogService.Current.ShowAlertAsync("Order", "Open a table first.", "OK");
            return;
        }

        if (_customerId <= 0)
        {
            await LoadCustomersAsync();
        }

        var customerId = _customerId > 0
            ? _customerId
            : (SelectedCustomer?.Id ?? current.CustomerId ?? 0);
        if (customerId <= 0)
        {
            _lastSaveOk = false;
            await AljannatDialogService.Current.ShowAlertAsync("Customer", "Select a customer before sending this order.", "OK");
            return;
        }

        _customerId = customerId;
        current.CustomerId = customerId;
        _lastSaveOk = false;

        await RunBusyAsync(async () =>
        {
            var dto = new CreateOrUpdateRestaurantOrderDto
            {
                Id = current.Id,
                BranchId = branchId,
                TableId = table?.Id ?? current.TableId,
                TableNo = table?.TableNo ?? current.TableNo,
                OrderType = current.OrderType,
                CarName = current.CarName,
                CarNumber = current.CarNumber,
                CarLocation = current.CarLocation,
                WaiterName = _session.User?.DisplayName,
                CustomerId = customerId,
                Notes = current.Notes,
                DiscountPercent = current.DiscountPercent,
                DiscountAmount = current.DiscountAmount,
                TaxPercent = current.TaxPercent,
                ServiceChargePercent = current.ServiceChargePercent,
                Lines = _cart.Lines.Select(l => new CreateOrUpdateRestaurantOrderLineDto
                {
                    Id = l.LineId,
                    ProductId = l.ProductId,
                    ProductName = l.Name,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    KitchenNotes = l.KitchenNotes,
                    SelectedModifiers = l.SelectedModifiers,
                    IsSentToKitchen = l.IsSentToKitchen,
                    PrintedQuantity = l.PrintedQuantity
                }).ToList()
            };

            var saved = await _restaurant.SaveOrderAsync(dto);
            saved.CustomerId ??= customerId;
            WaiterOrderSession.CurrentOrder = saved;
            _lastSaveOk = true;
            _cart.LoadFromOrder(saved);
            RefreshTableContextLabels();
        });
    }

    [RelayCommand]
    private async Task SendToKitchenAsync()
    {
        await SaveOrderAsync();
        var current = WaiterOrderSession.CurrentOrder;
        if (!_lastSaveOk || current == null) return;

        await RunBusyAsync(async () =>
        {
            var result = await _restaurant.SendToKitchenAsync(current.Id);
            PendingKotItems.Clear();
            foreach (var item in result.NewItems)
            {
                PendingKotItems.Add(item);
            }

            KotMessage = result.Message ?? (result.HasNewItems ? "KOT ready" : "No new items");
            if (result.HasNewItems && result.NewItems.Any())
            {
                var confirm = await AljannatDialogService.Current.ShowConfirmAsync("KOT", $"{result.NewItems.Count} item(s) for kitchen. Confirm printed?", "Confirm", "Later");
                if (confirm)
                {
                    await _restaurant.ConfirmKotPrintAsync(new ConfirmKotPrintDto
                    {
                        OrderId = current.Id,
                        LineIds = result.NewItems.Where(i => i.LineId.HasValue).Select(i => i.LineId!.Value).ToList()
                    });
                    var refreshed = await _restaurant.GetOrderAsync(current.Id);
                    WaiterOrderSession.CurrentOrder = refreshed;
                    _cart.LoadFromOrder(refreshed);
                }
            }
            else if (!string.IsNullOrWhiteSpace(KotMessage))
            {
                await AljannatDialogService.Current.ShowAlertAsync("KOT", KotMessage, "OK");
            }
        });
    }

    [RelayCommand]
    private async Task PrintBillAsync()
    {
        await SaveOrderAsync();
        var current = WaiterOrderSession.CurrentOrder;
        if (!_lastSaveOk || current == null) return;
        await RunBusyAsync(async () =>
        {
            var billed = await _restaurant.PrintBillAsync(current.Id);
            WaiterOrderSession.CurrentOrder = billed;
            await AljannatDialogService.Current.ShowAlertAsync("Bill", $"Bill printed. Total Rs. {billed.GrandTotal:N0}", "OK");
        });
    }

    [RelayCommand]
    private async Task SettleAsync()
    {
        var current = WaiterOrderSession.CurrentOrder;
        if (current == null) return;

        var cashText = await AljannatDialogService.Current.ShowPromptAsync(
            "Settle", $"Grand total Rs. {current.GrandTotal:N0}. Cash amount?",
            "Settle",
            "Cancel",
            placeholder: null,
            initialValue: current.GrandTotal.ToString("0.00"));
        if (string.IsNullOrWhiteSpace(cashText) || !decimal.TryParse(cashText, out var cash))
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _restaurant.SettleOrderAsync(new SettleRestaurantOrderDto
            {
                OrderId = current.Id,
                CustomerId = current.CustomerId ?? 0,
                PaymentType = 1,
                CashAmount = cash,
                DiscountAmount = current.DiscountAmount,
                DiscountPercent = current.DiscountPercent,
                TaxPercent = current.TaxPercent,
                ServiceChargePercent = current.ServiceChargePercent
            });
            _cart.Clear();
            WaiterOrderSession.Clear();
            await AljannatDialogService.Current.ShowAlertAsync("Settled", "Order settled.", "OK");
            await Shell.Current.GoToAsync("//waiter/tables");
        });
    }

    [RelayCommand]
    private async Task CancelOrderAsync()
    {
        var current = WaiterOrderSession.CurrentOrder;
        if (current == null) return;
        var ok = await AljannatDialogService.Current.ShowConfirmAsync(
            "Cancel Order", $"Cancel order {current.OrderNo}?",
            "Yes",
            "No",
            isDestructive: true);
        if (!ok) return;
        await RunBusyAsync(async () =>
        {
            await _restaurant.CancelOrderAsync(current.Id);
            _cart.Clear();
            WaiterOrderSession.Clear();
            await Shell.Current.GoToAsync("//waiter/tables");
        });
    }

    [RelayCommand]
    private async Task SplitBillAsync()
    {
        var current = WaiterOrderSession.CurrentOrder;
        if (current == null) return;
        var ok = await AljannatDialogService.Current.ShowConfirmAsync("Split bill", "Split this bill equally into 2?", "Split", "Cancel");
        if (!ok) return;
        await RunBusyAsync(async () =>
        {
            var split = await _restaurant.SplitBillAsync(new SplitBillDto
            {
                SourceOrderId = current.Id,
                NumberOfSplits = 2,
                SplitEqually = true
            });
            WaiterOrderSession.CurrentOrder = split;
            _cart.LoadFromOrder(split);
            await AljannatDialogService.Current.ShowAlertAsync("Split", $"New split order {split.OrderNo}", "OK");
        });
    }
}
