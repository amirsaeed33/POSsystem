using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services.Home;

/// <summary>Pure catalog filter/sort helpers (safe to run off the UI thread).</summary>
public static class HomeCatalogFilter
{
    public static List<OnlineProductDto> Filter(
        IReadOnlyList<OnlineProductDto> source,
        bool favoritesOnly,
        string? selectedPill,
        Func<int, int> getCartQty)
    {
        IEnumerable<OnlineProductDto> query = source;

        if (favoritesOnly)
        {
            query = query.Where(p => p.IsFavorite);
        }

        if (!string.IsNullOrWhiteSpace(selectedPill) &&
            !selectedPill.Contains("Top Deals", StringComparison.OrdinalIgnoreCase))
        {
            if (selectedPill.Contains("Highly Rated", StringComparison.OrdinalIgnoreCase))
            {
                query = query
                    .Where(p => (4.7 + ((Math.Abs(p.Id) % 4) * 0.1)) >= 4.8)
                    .OrderByDescending(p => 4.7 + ((Math.Abs(p.Id) % 4) * 0.1));
            }
            else if (selectedPill.Contains("Budget Picks", StringComparison.OrdinalIgnoreCase))
            {
                query = query
                    .Where(p => p.Price > 0)
                    .OrderBy(p => p.Price);
            }
            else if (selectedPill.Contains("Fast Delivery", StringComparison.OrdinalIgnoreCase))
            {
                query = query
                    .Where(p => p.InStock)
                    .OrderByDescending(p => p.IsFreeDelivery);
            }
            else if (selectedPill.Contains("Near Me", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.InStock);
            }
        }

        var list = query.ToList();
        foreach (var p in list)
        {
            p.CartQty = getCartQty(p.Id);
        }

        return list;
    }

    public static void EnrichProducts(
        IEnumerable<OnlineProductDto> products,
        Func<string?, string> resolveImageUrl,
        Func<int, bool> isFavorite)
    {
        foreach (var p in products)
        {
            SmartPos.Mobile.Services.ProductImageResolver.Enrich(p, resolveImageUrl);
            p.IsFavorite = isFavorite(p.Id);
        }
    }
}
