using System.Collections.ObjectModel;
using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

public class CartService
{
    public ObservableCollection<CartLine> Lines { get; } = new();

    public event EventHandler? CartChanged;

    public decimal Subtotal => Lines.Sum(x => x.LineTotal);
    public int ItemCount => (int)Lines.Sum(x => x.Quantity);

    public int GetQty(int productId) =>
        (int)Lines.Where(x => x.ProductId == productId).Sum(x => x.Quantity);

    public void DecrementProduct(int productId)
    {
        var line = Lines.LastOrDefault(x => x.ProductId == productId);
        if (line == null)
        {
            return;
        }

        SetQuantity(line, line.Quantity - 1);
    }

    public void AddOrIncrement(OnlineProductDto product, decimal qty = 1, string? modifiers = null)
    {
        var existing = Lines.FirstOrDefault(x =>
            x.ProductId == product.Id &&
            string.Equals(x.SelectedModifiers ?? string.Empty, modifiers ?? string.Empty, StringComparison.Ordinal));
        if (existing != null)
        {
            existing.Quantity += qty;
            Refresh();
            return;
        }

        Lines.Add(new CartLine
        {
            ProductId = product.Id,
            Name = product.Name ?? "Item",
            UnitPrice = product.Price,
            Quantity = qty,
            ImagePath = product.ImagePath,
            SelectedModifiers = modifiers
        });
        CartChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddOrIncrement(ProductDto product, decimal qty = 1, string? modifiers = null, string? kitchenNotes = null)
    {
        var existing = Lines.FirstOrDefault(x =>
            x.ProductId == product.Id &&
            string.Equals(x.SelectedModifiers ?? string.Empty, modifiers ?? string.Empty, StringComparison.Ordinal) &&
            string.Equals(x.KitchenNotes ?? string.Empty, kitchenNotes ?? string.Empty, StringComparison.Ordinal));
        if (existing != null)
        {
            existing.Quantity += qty;
            Refresh();
            return;
        }

        Lines.Add(new CartLine
        {
            ProductId = product.Id,
            Name = product.Name ?? "Item",
            UnitPrice = product.Price,
            Quantity = qty,
            ImagePath = product.ImagePath,
            SelectedModifiers = modifiers,
            KitchenNotes = kitchenNotes
        });
        CartChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetQuantity(CartLine line, decimal qty)
    {
        if (qty <= 0)
        {
            Lines.Remove(line);
        }
        else
        {
            line.Quantity = qty;
        }

        Refresh();
    }

    public void Remove(CartLine line)
    {
        Lines.Remove(line);
        CartChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        Lines.Clear();
        CartChanged?.Invoke(this, EventArgs.Empty);
    }

    public void LoadFromOrder(RestaurantOrderDto order)
    {
        Lines.Clear();
        foreach (var line in order.Lines)
        {
            Lines.Add(new CartLine
            {
                LineId = line.Id,
                ProductId = line.ProductId,
                Name = line.ProductName ?? "Item",
                UnitPrice = line.UnitPrice,
                Quantity = line.Quantity,
                SelectedModifiers = line.SelectedModifiers,
                KitchenNotes = line.KitchenNotes,
                IsSentToKitchen = line.IsSentToKitchen,
                PrintedQuantity = line.PrintedQuantity
            });
        }

        CartChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 1-Click Re-Order: Loads line items from a past customer order into the active cart.
    /// </summary>
    /// <returns>Number of items added to cart.</returns>
    public int ReorderFromCustomerOrder(CustomerOrderDto order, bool clearCart = true)
    {
        if (order?.Lines == null || order.Lines.Count == 0)
        {
            return 0;
        }

        if (clearCart)
        {
            Lines.Clear();
        }

        var addedCount = 0;
        foreach (var line in order.Lines)
        {
            if (line.ProductId <= 0 || line.Quantity <= 0)
            {
                continue;
            }

            var existing = Lines.FirstOrDefault(x => x.ProductId == line.ProductId);
            if (existing != null)
            {
                existing.Quantity += line.Quantity;
            }
            else
            {
                Lines.Add(new CartLine
                {
                    ProductId = line.ProductId,
                    Name = line.ProductName ?? $"Item #{line.ProductId}",
                    UnitPrice = line.UnitPrice,
                    Quantity = line.Quantity
                });
            }

            addedCount++;
        }

        CartChanged?.Invoke(this, EventArgs.Empty);
        return addedCount;
    }

    private void Refresh()
    {
        // Force CollectionView refresh for mutable property changes
        var snapshot = Lines.ToList();
        Lines.Clear();
        foreach (var item in snapshot)
        {
            Lines.Add(item);
        }

        CartChanged?.Invoke(this, EventArgs.Empty);
    }
}
