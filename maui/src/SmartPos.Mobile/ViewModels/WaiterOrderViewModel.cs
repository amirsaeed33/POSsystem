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
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedCategory = "All";

    [ObservableProperty]
    private string kotMessage = string.Empty;

    public ObservableCollection<ProductDto> Products { get; } = new();
    public ObservableCollection<WaiterCategoryChip> Categories { get; } = new();
    public ObservableCollection<CartLine> Lines => _cart.Lines;
    public ObservableCollection<KitchenSlipItemDto> PendingKotItems { get; } = new();

    public decimal Subtotal => _cart.Subtotal;
    public string SubtotalText => Subtotal.ToString("0.00");

    public WaiterOrderViewModel(RestaurantService restaurant, SessionService session, CartService cart, AbpApiClient api)
    {
        _restaurant = restaurant;
        _session = session;
        _cart = cart;
        _api = api;
        Title = "Order";
        _cart.CartChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(SubtotalText));
            SyncCartQtyBadges();
        };
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        var order = WaiterOrderSession.CurrentOrder;
        var table = WaiterOrderSession.CurrentTable;
        OrderTitle = order == null
            ? "Select a table first"
            : $"{table?.DisplayTitle ?? order.TableNo} · {order.OrderNo}";

        if (!_allProducts.Any())
        {
            await LoadProductsAsync();
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
                p.ImageUrl = _api.ResolveImageUrl(p.ImagePath);
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
        var name = string.IsNullOrWhiteSpace(category?.Name) ? "All" : category!.Name;
        SelectedCategory = name;
        foreach (var chip in Categories)
        {
            chip.IsSelected = string.Equals(chip.Name, name, StringComparison.OrdinalIgnoreCase);
        }

        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        Products.Clear();
        IEnumerable<ProductDto> query = _allProducts;
        if (!string.Equals(SelectedCategory, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => string.Equals(p.CategoryName, SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(p =>
                (p.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Barcode?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        foreach (var p in query)
        {
            p.CartQty = _cart.GetQty(p.Id);
            Products.Add(p);
        }
    }

    private void SyncCartQtyBadges()
    {
        foreach (var p in _allProducts)
        {
            p.CartQty = _cart.GetQty(p.Id);
        }

        ApplyFilters();
    }

    [RelayCommand]
    private async Task IncreaseCardAsync(ProductDto? product)
    {
        if (product == null)
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
    private void DecreaseCard(ProductDto? product)
    {
        if (product == null || product.CartQty <= 0)
        {
            return;
        }

        _cart.DecrementProduct(product.Id);
    }

    [RelayCommand]
    private async Task AddProductAsync(ProductDto? product)
    {
        if (product == null)
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
                var choice = await Shell.Current.DisplayActionSheet(group.Name ?? "Options", "Skip", null, options);
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

        var notes = await Shell.Current.DisplayPromptAsync("Kitchen notes", "Optional note for kitchen", "Add", "Skip", placeholder: "No onion, spicy...");
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
    private async Task SaveOrderAsync()
    {
        var current = WaiterOrderSession.CurrentOrder;
        var table = WaiterOrderSession.CurrentTable;
        if (current == null || _session.BranchId is not int branchId)
        {
            await Shell.Current.DisplayAlert("Order", "Open a table first.", "OK");
            return;
        }

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
            WaiterOrderSession.CurrentOrder = saved;
            _cart.LoadFromOrder(saved);
            OrderTitle = $"{table?.DisplayTitle ?? saved.TableNo} · {saved.OrderNo}";
            await Shell.Current.DisplayAlert("Saved", "Order saved.", "OK");
        });
    }

    [RelayCommand]
    private async Task SendToKitchenAsync()
    {
        await SaveOrderAsync();
        var current = WaiterOrderSession.CurrentOrder;
        if (current == null) return;

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
                var confirm = await Shell.Current.DisplayAlert(
                    "KOT",
                    $"{result.NewItems.Count} item(s) for kitchen. Confirm printed?",
                    "Confirm",
                    "Later");
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
        });
    }

    [RelayCommand]
    private async Task PrintBillAsync()
    {
        await SaveOrderAsync();
        var current = WaiterOrderSession.CurrentOrder;
        if (current == null) return;
        await RunBusyAsync(async () =>
        {
            var billed = await _restaurant.PrintBillAsync(current.Id);
            WaiterOrderSession.CurrentOrder = billed;
            await Shell.Current.DisplayAlert("Bill", $"Bill printed. Total {billed.GrandTotal:0.00}", "OK");
        });
    }

    [RelayCommand]
    private async Task SettleAsync()
    {
        var current = WaiterOrderSession.CurrentOrder;
        if (current == null) return;

        var cashText = await Shell.Current.DisplayPromptAsync(
            "Settle",
            $"Grand total {current.GrandTotal:0.00}. Cash amount?",
            "Settle",
            "Cancel",
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
            await Shell.Current.DisplayAlert("Settled", "Order settled.", "OK");
            await Shell.Current.GoToAsync("//waiter/tables");
        });
    }

    [RelayCommand]
    private async Task CancelOrderAsync()
    {
        var current = WaiterOrderSession.CurrentOrder;
        if (current == null) return;
        var ok = await Shell.Current.DisplayAlert("Cancel", $"Cancel order {current.OrderNo}?", "Yes", "No");
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
        var ok = await Shell.Current.DisplayAlert("Split bill", "Split this bill equally into 2?", "Split", "Cancel");
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
            await Shell.Current.DisplayAlert("Split", $"New split order {split.OrderNo}", "OK");
        });
    }
}
