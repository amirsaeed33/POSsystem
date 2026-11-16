using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;
using SmartPos.Mobile.Services.Home;

namespace SmartPos.Mobile.ViewModels;

public partial class CustomerHomeViewModel
{
    // =========================================================================
    // 🔥 DEALS FETCHING & DATA ENGINE
    // =========================================================================
    public async Task LoadDealsAsync(int branchId, bool forceRefresh = false, List<DealDto>? prefetchedRemoteDeals = null)
    {
        try
        {
            IsLoadingDeals = true;
            DealsList.Clear();

            // 1. Check for explicit deals already present in catalog
            var catalogDeals = _allProducts
                .Where(p => p.IsDeal || (p.CategoryName != null && p.CategoryName.Contains("deal", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var d in catalogDeals)
            {
                d.IsDeal = true;
                if (string.IsNullOrWhiteSpace(d.DealItemsSummary))
                {
                    d.DealItemsSummary = d.Description ?? "Combo Deal Bundle";
                }
                DealsList.Add(d);
            }

            // 2. Fetch active deals from the dedicated DealAppService backend endpoint
            //    (or reuse a list already fetched in parallel with catalog)
            var remoteDeals = prefetchedRemoteDeals
                ?? await _orders.GetActiveDealsAsync(branchId, forceRefresh);
            foreach (var rd in remoteDeals.Where(x => x.IsActive && !x.IsExpired))
            {
                if (DealsList.Any(x => x.Id == rd.ProductId || (rd.ProductId.HasValue && x.Id == rd.ProductId.Value) || x.DealId == rd.Id))
                {
                    continue;
                }

                var summary = rd.Items != null && rd.Items.Count > 0
                    ? string.Join(" • ", rd.Items.Select(i => $"{i.Quantity}x {i.ProductName}"))
                    : (rd.Description ?? "Special Combo Deal");

                DealsList.Add(new OnlineProductDto
                {
                    Id = rd.ProductId ?? -rd.Id,
                    DealId = rd.Id,
                    Name = rd.Name,
                    Description = rd.Description,
                    CategoryName = "Deals",
                    Price = rd.DealPrice,
                    OriginalPrice = rd.TotalOriginalPrice > 0 ? rd.TotalOriginalPrice : null,
                    ImagePath = rd.ImagePath,
                    ImageUrl = rd.ImageUrl,
                    InStock = true,
                    IsDeal = true,
                    DealItemsSummary = summary
                });
            }

            // 3. Fallback: If no explicit deals in DB yet, create mouth-watering combo deals linked to catalog items
            if (DealsList.Count == 0)
            {
                var fallbackDeals = CreateDefaultComboDeals();
                foreach (var fd in fallbackDeals)
                {
                    DealsList.Add(fd);
                }
            }

            OnPropertyChanged(nameof(HasDeals));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadDealsAsync] Error: {ex.Message}");
        }
        finally
        {
            IsLoadingDeals = false;
        }
    }

    private static List<OnlineProductDto> CreateDefaultComboDeals() =>
        HomeCatalogDefaults.CreateDefaultComboDeals();


    // 🍔 COMBO CUSTOMIZATION MODAL (BOTTOM SHEET)
    // =========================================================================
    [RelayCommand]
    public async Task OpenDealModalAsync(OnlineProductDto? dealProduct)
    {
        if (dealProduct == null)
        {
            return;
        }

        ComboProduct = dealProduct;
        ComboQuantity = 1;
        DealCustomiseItems.Clear();
        IsLoadingComboDeal = true;
        IsComboModalVisible = true;
        RefreshComboTotal();

        try
        {
            DealDto? deal = null;
            if (dealProduct.DealId.HasValue && dealProduct.DealId.Value > 0)
            {
                deal = await _orders.GetDealByIdAsync(dealProduct.DealId.Value);
            }
            if (deal == null && dealProduct.Id > 0)
            {
                deal = await _orders.GetDealByProductIdAsync(dealProduct.Id);
            }

            var itemsToCustomise = new List<DealCustomiseItem>();

            if (deal != null && deal.Items != null && deal.Items.Count > 0)
            {
                foreach (var item in deal.Items)
                {
                    var customItem = new DealCustomiseItem
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName ?? $"Item #{item.ProductId}",
                        Quantity = item.Quantity > 0 ? item.Quantity : 1,
                        UnitPrice = item.UnitPrice
                    };

                    var groups = await _orders.GetModifiersForProductAsync(item.ProductId);
                    if (groups.Count == 0)
                    {
                        groups = CreateFallbackModifiersForName(customItem.ProductName);
                    }

                    PopulateModifierGroups(customItem, groups);
                    itemsToCustomise.Add(customItem);
                }
            }
            else
            {
                // Fallback / simulated combo items for rich immediate testing
                itemsToCustomise = CreateFallbackComboItems(dealProduct);
            }

            DealCustomiseItems.Clear();
            foreach (var itm in itemsToCustomise)
            {
                DealCustomiseItems.Add(itm);
            }

            OnPropertyChanged(nameof(HasComboItems));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpenDealModalAsync] Error: {ex.Message}");
            // Still populate fallback combo items so user is never stuck
            var fallback = CreateFallbackComboItems(dealProduct);
            DealCustomiseItems.Clear();
            foreach (var itm in fallback)
            {
                DealCustomiseItems.Add(itm);
            }
            OnPropertyChanged(nameof(HasComboItems));
        }
        finally
        {
            for (var i = 0; i < DealCustomiseItems.Count; i++)
            {
                DealCustomiseItems[i].ItemIndex = i + 1;
                DealCustomiseItems[i].TotalItemCount = DealCustomiseItems.Count;
                DealCustomiseItems[i].NotifyStateChanged();
            }

            IsLoadingComboDeal = false;
            ValidateComboSelections();
            RefreshComboTotal();
        }
    }

    private List<DealCustomiseItem> CreateFallbackComboItems(OnlineProductDto dealProduct)
    {
        var list = new List<DealCustomiseItem>();
        var name = (dealProduct.Name ?? string.Empty).ToLowerInvariant();

        if (name.Contains("pizza"))
        {
            var pizzaItem = new DealCustomiseItem { ProductId = 1160, ProductName = "Chicken Tikka Pizza (Medium)", Quantity = 1 };
            PopulateModifierGroups(pizzaItem,
            [
                new ProductModifierGroupDto
                {
                    Id = 201,
                    Name = "Crust & Cheese",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 2001, Name = "Pan Crust (Crispy)", IsDefault = true },
                        new ProductModifierOptionDto { Id = 2002, Name = "Thin Crust (Traditional)" },
                        new ProductModifierOptionDto { Id = 2003, Name = "Cheese Stuffed Crust (+PKR 180)", Price = 180 }
                    ]
                },
                new ProductModifierGroupDto
                {
                    Id = 202,
                    Name = "Special Dip Sauce",
                    IsRequired = false,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 2004, Name = "Garlic Mayo Dip (+PKR 50)", Price = 50 },
                        new ProductModifierOptionDto { Id = 2005, Name = "Spicy Ranch (+PKR 60)", Price = 60 }
                    ]
                }
            ]);
            list.Add(pizzaItem);

            var wingsItem = new DealCustomiseItem { ProductId = 1155, ProductName = "Hot Wings (10 Pcs)", Quantity = 1 };
            PopulateModifierGroups(wingsItem,
            [
                new ProductModifierGroupDto
                {
                    Id = 203,
                    Name = "Wings Flavor",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 2006, Name = "Spicy Buffalo Glaze", IsDefault = true },
                        new ProductModifierOptionDto { Id = 2007, Name = "Smoky BBQ Sauce" },
                        new ProductModifierOptionDto { Id = 2008, Name = "Crispy Fried (No Sauce)" }
                    ]
                }
            ]);
            list.Add(wingsItem);

            var drinkItem = new DealCustomiseItem { ProductId = 1206, ProductName = "Chilled Beverage (1.5L)", Quantity = 1 };
            PopulateModifierGroups(drinkItem,
            [
                new ProductModifierGroupDto
                {
                    Id = 204,
                    Name = "Drink Choice",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 2009, Name = "Pepsi 1.5L", IsDefault = true },
                        new ProductModifierOptionDto { Id = 2010, Name = "7Up 1.5L" },
                        new ProductModifierOptionDto { Id = 2011, Name = "Mirinda 1.5L" }
                    ]
                }
            ]);
            list.Add(drinkItem);
        }
        else if (name.Contains("bbq") || name.Contains("platter"))
        {
            var botiItem = new DealCustomiseItem { ProductId = 1171, ProductName = "Chicken Malai Boti", Quantity = 1 };
            PopulateModifierGroups(botiItem,
            [
                new ProductModifierGroupDto
                {
                    Id = 301,
                    Name = "Spice & Herb Level",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 3001, Name = "Creamy Mild Malai", IsDefault = true },
                        new ProductModifierOptionDto { Id = 3002, Name = "Medium Masala Boti" }
                    ]
                }
            ]);
            list.Add(botiItem);

            var kababItem = new DealCustomiseItem { ProductId = 1172, ProductName = "Chicken Seekh Kabab (4 Pcs)", Quantity = 1 };
            PopulateModifierGroups(kababItem,
            [
                new ProductModifierGroupDto
                {
                    Id = 302,
                    Name = "Chutney & Sauce",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 3003, Name = "Fresh Mint Raita", IsDefault = true },
                        new ProductModifierOptionDto { Id = 3004, Name = "Spicy Imli Chutney" }
                    ]
                }
            ]);
            list.Add(kababItem);

            var naanItem = new DealCustomiseItem { ProductId = 1202, ProductName = "Roghni Naan", Quantity = 2 };
            list.Add(naanItem);
        }
        else
        {
            // Standard Burger / Zinger / Fast Food combo
            var burgerItem = new DealCustomiseItem { ProductId = 1149, ProductName = "Zinger Burger", Quantity = name.Contains("duo") ? 2 : 1 };
            PopulateModifierGroups(burgerItem,
            [
                new ProductModifierGroupDto
                {
                    Id = 101,
                    Name = "Spice Level",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 1001, Name = "Spicy Crispy Zinger", IsDefault = true },
                        new ProductModifierOptionDto { Id = 1002, Name = "Mild / Normal Crunch" }
                    ]
                },
                new ProductModifierGroupDto
                {
                    Id = 102,
                    Name = "Extra Toppings",
                    IsRequired = false,
                    AllowMultiple = true,
                    MaxSelection = 2,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 1003, Name = "Extra Golden Cheese Slice (+PKR 60)", Price = 60 },
                        new ProductModifierOptionDto { Id = 1004, Name = "Spicy Jalapenos (+PKR 40)", Price = 40 }
                    ]
                }
            ]);
            list.Add(burgerItem);

            var friesItem = new DealCustomiseItem { ProductId = 1157, ProductName = "French Fries", Quantity = name.Contains("duo") ? 2 : 1 };
            PopulateModifierGroups(friesItem,
            [
                new ProductModifierGroupDto
                {
                    Id = 103,
                    Name = "Fries Seasoning",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 1005, Name = "Classic Salted", IsDefault = true },
                        new ProductModifierOptionDto { Id = 1006, Name = "Spicy Chatpata Masala" },
                        new ProductModifierOptionDto { Id = 1007, Name = "Melted Cheese Sauce (+PKR 70)", Price = 70 }
                    ]
                }
            ]);
            list.Add(friesItem);

            var drinkItem = new DealCustomiseItem { ProductId = 1206, ProductName = "Soft Drink (Can 250ml)", Quantity = name.Contains("duo") ? 2 : 1 };
            PopulateModifierGroups(drinkItem,
            [
                new ProductModifierGroupDto
                {
                    Id = 104,
                    Name = "Drink Choice",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = 1008, Name = "Pepsi Cold Can", IsDefault = true },
                        new ProductModifierOptionDto { Id = 1009, Name = "7Up / Sprite Can" },
                        new ProductModifierOptionDto { Id = 1010, Name = "Mirinda / Fanta Can" },
                        new ProductModifierOptionDto { Id = 1011, Name = "Mountain Dew Can" }
                    ]
                }
            ]);
            list.Add(drinkItem);
        }

        return list;
    }

    private static List<ProductModifierGroupDto> CreateFallbackModifiersForName(string productName) =>
        HomeCatalogDefaults.CreateFallbackModifiersForName(productName);


    private static void PopulateModifierGroups(DealCustomiseItem item, List<ProductModifierGroupDto> groups)
    {
        item.ModifierGroups.Clear();
        foreach (var g in groups.Where(x => x.Options is { Count: > 0 }))
        {
            var groupItem = new ModifierGroupItem
            {
                Id = g.Id,
                Name = string.IsNullOrWhiteSpace(g.Name) ? "Options" : g.Name!,
                IsRequired = g.IsRequired || g.MinSelection > 0,
                AllowMultiple = g.AllowMultiple || g.MaxSelection > 1,
                MaxSelection = g.MaxSelection <= 0 ? 1 : g.MaxSelection
            };

            var defaultApplied = false;
            foreach (var opt in g.Options)
            {
                var optionItem = new ModifierOptionItem
                {
                    Id = opt.Id,
                    GroupId = g.Id,
                    Name = opt.Name ?? "Option",
                    Price = opt.EffectivePrice,
                    AllowMultiple = groupItem.AllowMultiple,
                    IsRequired = groupItem.IsRequired,
                    MaxSelection = groupItem.MaxSelection,
                    IsSelected = opt.IsDefault && !defaultApplied
                };

                if (optionItem.IsSelected)
                {
                    defaultApplied = true;
                }

                groupItem.Options.Add(optionItem);
            }

            if (groupItem.IsRequired && !groupItem.Options.Any(o => o.IsSelected) && groupItem.Options.Count > 0)
            {
                groupItem.Options[0].IsSelected = true;
            }

            item.ModifierGroups.Add(groupItem);
        }
    }

    [RelayCommand]
    private void SelectComboOption(ModifierOptionItem? option)
    {
        if (option == null)
        {
            return;
        }

        foreach (var item in DealCustomiseItems)
        {
            var group = item.ModifierGroups.FirstOrDefault(g => g.Id == option.GroupId);
            if (group != null)
            {
                if (group.AllowMultiple || group.MaxSelection > 1)
                {
                    if (option.IsSelected)
                    {
                        if (group.IsRequired && group.Options.Count(o => o.IsSelected) <= 1)
                        {
                            return;
                        }
                        option.IsSelected = false;
                    }
                    else
                    {
                        var selectedCount = group.Options.Count(o => o.IsSelected);
                        if (group.MaxSelection > 0 && selectedCount >= group.MaxSelection)
                        {
                            var first = group.Options.FirstOrDefault(o => o.IsSelected);
                            if (first != null) first.IsSelected = false;
                        }
                        option.IsSelected = true;
                    }
                }
                else
                {
                    foreach (var o in group.Options)
                    {
                        o.IsSelected = ReferenceEquals(o, option);
                    }
                }
                item.NotifyStateChanged();
                break;
            }
        }

        ValidateComboSelections();
        RefreshComboTotal();
    }

    private bool ValidateComboSelections()
    {
        if (DealCustomiseItems == null || DealCustomiseItems.Count == 0)
        {
            IsComboValidationPassed = true;
            ComboValidationMessage = string.Empty;
            OnPropertyChanged(nameof(CanAddComboToCart));
            OnPropertyChanged(nameof(HasComboValidationMessage));
            return true;
        }

        foreach (var item in DealCustomiseItems)
        {
            item.NotifyStateChanged();
            foreach (var group in item.ModifierGroups.Where(g => g.IsRequired))
            {
                if (!group.Options.Any(o => o.IsSelected))
                {
                    IsComboValidationPassed = false;
                    ComboValidationMessage = $"Please select {group.Name} for {item.ProductName}.";
                    OnPropertyChanged(nameof(CanAddComboToCart));
                    OnPropertyChanged(nameof(HasComboValidationMessage));
                    return false;
                }
            }
        }

        IsComboValidationPassed = true;
        ComboValidationMessage = string.Empty;
        OnPropertyChanged(nameof(CanAddComboToCart));
        OnPropertyChanged(nameof(HasComboValidationMessage));
        return true;
    }

    [RelayCommand]
    private void IncreaseComboQty()
    {
        ComboQuantity++;
        RefreshComboTotal();
    }

    [RelayCommand]
    private void DecreaseComboQty()
    {
        if (ComboQuantity <= 1)
        {
            return;
        }

        ComboQuantity--;
        RefreshComboTotal();
    }

    private void RefreshComboTotal()
    {
        if (ComboProduct == null)
        {
            ComboTotalLabel = "Rs 0";
            return;
        }

        var extra = DealCustomiseItems
            .SelectMany(i => i.ModifierGroups)
            .SelectMany(g => g.Options.Where(o => o.IsSelected))
            .Sum(o => o.Price);

        var total = (ComboProduct.Price + extra) * ComboQuantity;
        ComboTotalLabel = $"Rs {total:0}";
        OnPropertyChanged(nameof(ComboTotalLabel));
    }

    [RelayCommand]
    private void AddComboToCart()
    {
        if (ComboProduct == null)
        {
            return;
        }

        // Validate mandatory selections for every item in the combo
        if (!ValidateComboSelections())
        {
            if (Shell.Current != null && !string.IsNullOrWhiteSpace(ComboValidationMessage))
            {
                _ = AljannatDialogService.Current.ShowAlertAsync("Choice Required", ComboValidationMessage, "OK");
            }
            return;
        }

        var itemSummaries = new List<string>();
        foreach (var item in DealCustomiseItems)
        {
            var selectedOpts = item.ModifierGroups
                .SelectMany(g => g.Options.Where(o => o.IsSelected))
                .Select(o => o.Price > 0 ? $"{o.Name} (+Rs {o.Price:0})" : o.Name)
                .ToList();

            if (selectedOpts.Count > 0)
            {
                itemSummaries.Add($"{item.HeaderTitle} ({string.Join(", ", selectedOpts)})");
            }
            else
            {
                itemSummaries.Add(item.HeaderTitle);
            }
        }

        var extra = DealCustomiseItems
            .SelectMany(i => i.ModifierGroups)
            .SelectMany(g => g.Options.Where(o => o.IsSelected))
            .Sum(o => o.Price);

        var modifiersSummary = string.Join(" • ", itemSummaries);

        var lineProduct = new OnlineProductDto
        {
            Id = ComboProduct.Id,
            Name = ComboProduct.Name,
            Price = ComboProduct.Price + extra,
            ImagePath = ComboProduct.ImagePath,
            CategoryName = ComboProduct.CategoryName ?? "Deals",
            InStock = true,
            IsDeal = true
        };

        var dealName = lineProduct.Name;
        _cart.AddOrIncrement(lineProduct, ComboQuantity, modifiersSummary);
        CloseComboModal();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (Shell.Current != null)
            {
                await AljannatDialogService.Current.ShowAlertAsync("Deal Added", $"{dealName} added to your cart!", "OK");
            }
        });
    }

    [RelayCommand]
    private void CloseComboModal()
    {
        IsComboModalVisible = false;
        ComboProduct = null;
        ComboQuantity = 1;
        DealCustomiseItems.Clear();
        IsLoadingComboDeal = false;
    }
}

