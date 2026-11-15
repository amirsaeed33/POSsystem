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
    // 🍔 SINGLE PRODUCT CUSTOMIZATION MODAL
    // =========================================================================
    [RelayCommand]
    private void CloseProductModal()
    {
        IsProductModalVisible = false;
        ModalProduct = null;
        ModalModifierGroups.Clear();
        IsLoadingModifiers = false;
        DetailQuantity = 1;
    }

    [RelayCommand]
    private void SelectModalOption(ModifierOptionItem? option)
    {
        if (option == null)
        {
            return;
        }

        var group = ModalModifierGroups.FirstOrDefault(g => g.Id == option.GroupId);
        if (group == null)
        {
            return;
        }

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
                    if (first != null)
                    {
                        first.IsSelected = false;
                    }
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

        RefreshModalTotal();
    }

    [RelayCommand]
    private void IncreaseDetailQty()
    {
        DetailQuantity++;
        RefreshModalTotal();
    }

    [RelayCommand]
    private void DecreaseDetailQty()
    {
        if (DetailQuantity <= 1)
        {
            return;
        }

        DetailQuantity--;
        RefreshModalTotal();
    }

    [RelayCommand]
    private void AddModalToCart()
    {
        if (ModalProduct == null)
        {
            return;
        }

        foreach (var group in ModalModifierGroups.Where(g => g.IsRequired))
        {
            if (!group.Options.Any(o => o.IsSelected))
            {
                _ = AljannatDialogService.Current.ShowAlertAsync("Required", $"Please select {group.Name}.", "OK");
                return;
            }
        }

        var selected = ModalModifierGroups
            .SelectMany(g => g.Options.Where(o => o.IsSelected))
            .ToList();
        var extra = selected.Sum(o => o.Price);
        var modifiers = selected.Count == 0
            ? null
            : string.Join(", ", selected.Select(o => o.Name));

        var lineProduct = new OnlineProductDto
        {
            Id = ModalProduct.Id,
            Name = ModalProduct.Name,
            Price = ModalProduct.Price + extra,
            ImagePath = ModalProduct.ImagePath,
            CategoryName = ModalProduct.CategoryName,
            InStock = ModalProduct.InStock
        };

        _cart.AddOrIncrement(lineProduct, DetailQuantity, modifiers);
        CloseProductModal();
    }

    private async Task OpenProductModalAsync(OnlineProductDto product)
    {
        var groups = await _orders.GetModifiersForProductAsync(product.Id);
        groups = groups.Where(g => g.Options is { Count: > 0 }).ToList();

        if (groups.Count == 0 && HomeCatalogDefaults.LooksLikePizza(product))
        {
            groups =
            [
                new ProductModifierGroupDto
                {
                    Id = -1,
                    Name = "Size",
                    IsRequired = true,
                    MaxSelection = 1,
                    Options =
                    [
                        new ProductModifierOptionDto { Id = -11, Name = "Small", IsDefault = true },
                        new ProductModifierOptionDto { Id = -12, Name = "Medium" },
                        new ProductModifierOptionDto { Id = -13, Name = "Large" },
                        new ProductModifierOptionDto { Id = -14, Name = "XL" }
                    ]
                }
            ];
        }

        // No options to choose — add one unit immediately (no popup).
        if (groups.Count == 0)
        {
            _cart.AddOrIncrement(product, 1);
            return;
        }

        ModalProduct = product;
        DetailQuantity = 1;
        ModalModifierGroups.Clear();
        IsLoadingModifiers = false;
        IsProductModalVisible = true;
        RefreshModalTotal();

        foreach (var g in groups)
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
                var item = new ModifierOptionItem
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
                if (item.IsSelected)
                {
                    defaultApplied = true;
                }

                groupItem.Options.Add(item);
            }

            if (groupItem.IsRequired && !groupItem.Options.Any(o => o.IsSelected) && groupItem.Options.Count > 0)
            {
                groupItem.Options[0].IsSelected = true;
            }

            ModalModifierGroups.Add(groupItem);
        }

        RefreshModalTotal();
    }



    private void RefreshModalTotal()
    {
        if (ModalProduct == null)
        {
            ModalTotalLabel = "PKR 0";
            return;
        }

        var extra = ModalModifierGroups
            .SelectMany(g => g.Options.Where(o => o.IsSelected))
            .Sum(o => o.Price);
        var total = (ModalProduct.Price + extra) * DetailQuantity;
        ModalTotalLabel = $"PKR {total:0}";
    }
}


