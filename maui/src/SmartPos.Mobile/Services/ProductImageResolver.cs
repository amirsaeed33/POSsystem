using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Services;

/// <summary>
/// Resolves product images for every customer surface. Prefers API/DB ImagePath;
/// otherwise assigns a relevant Unsplash food photo by name/category (stable per product id).
/// </summary>
public static class ProductImageResolver
{
    private const string Q = "?auto=format&fit=crop&w=800&h=600&q=80";

    private static readonly string[] FallbackFoodPool =
    {
        "https://images.unsplash.com/photo-1555939594-58d7cb561ad1" + Q,
        "https://images.unsplash.com/photo-1504674900247-0877df9cc836" + Q,
        "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4" + Q,
        "https://images.unsplash.com/photo-1414235077428-338989a2e8c0" + Q,
        "https://images.unsplash.com/photo-1476224203421-9ac39bcb3327" + Q,
        "https://images.unsplash.com/photo-1540189549336-e6e99c3679fe" + Q,
        "https://images.unsplash.com/photo-1567620905732-2d1ec7ab7445" + Q,
        "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38" + Q,
    };

    private static readonly (string Keyword, string Url)[] KeywordImages =
    {
        ("burger", "https://images.unsplash.com/photo-1568901346375-23c9450c58cd" + Q),
        ("burgur", "https://images.unsplash.com/photo-1568901346375-23c9450c58cd" + Q),
        ("pizza", "https://images.unsplash.com/photo-1513104890138-7c749659a591" + Q),
        ("biryani", "https://images.unsplash.com/photo-1563379926898-05f4575a45d8" + Q),
        ("pulao", "https://images.unsplash.com/photo-1596797038530-2c107229654b" + Q),
        ("karahi", "https://images.unsplash.com/photo-1603894584373-5ac82b2ae398" + Q),
        ("korma", "https://images.unsplash.com/photo-1585937421612-70a008356fbe" + Q),
        ("nihari", "https://images.unsplash.com/photo-1585937421612-70a008356fbe" + Q),
        ("haleem", "https://images.unsplash.com/photo-1585937421612-70a008356fbe" + Q),
        ("handi", "https://images.unsplash.com/photo-1603894584373-5ac82b2ae398" + Q),
        ("bbq", "https://images.unsplash.com/photo-1529193591184-b1d58069ecdd" + Q),
        ("tikka", "https://images.unsplash.com/photo-1599487488170-d11ec9c172f2" + Q),
        ("boti", "https://images.unsplash.com/photo-1599487488170-d11ec9c172f2" + Q),
        ("kabab", "https://images.unsplash.com/photo-1599487488170-d11ec9c172f2" + Q),
        ("kebab", "https://images.unsplash.com/photo-1599487488170-d11ec9c172f2" + Q),
        ("seekh", "https://images.unsplash.com/photo-1544025162-d76694265947" + Q),
        ("grill", "https://images.unsplash.com/photo-1544025162-d76694265947" + Q),
        ("steak", "https://images.unsplash.com/photo-1544025162-d76694265947" + Q),
        ("chicken", "https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58" + Q),
        ("mutton", "https://images.unsplash.com/photo-1544025162-d76694265947" + Q),
        ("beef", "https://images.unsplash.com/photo-1544025162-d76694265947" + Q),
        ("fish", "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2" + Q),
        ("prawn", "https://images.unsplash.com/photo-1565680018434-b513d5ea2fc0" + Q),
        ("shrimp", "https://images.unsplash.com/photo-1565680018434-b513d5ea2fc0" + Q),
        ("fries", "https://images.unsplash.com/photo-1573080496219-bb080dd4f877" + Q),
        ("french fry", "https://images.unsplash.com/photo-1573080496219-bb080dd4f877" + Q),
        ("wing", "https://images.unsplash.com/photo-1527477396000-e2717f6c7e82" + Q),
        ("nugget", "https://images.unsplash.com/photo-1562967914-608f82629710" + Q),
        ("sandwich", "https://images.unsplash.com/photo-1528735602780-2552fd46c7af" + Q),
        ("shawarma", "https://images.unsplash.com/photo-1529006557810-274b9b2fc783" + Q),
        ("wrap", "https://images.unsplash.com/photo-1529006557810-274b9b2fc783" + Q),
        ("roll", "https://images.unsplash.com/photo-1601050690597-df0568f70950" + Q),
        ("naan", "https://images.unsplash.com/photo-1601050690597-df0568f70950" + Q),
        ("roti", "https://images.unsplash.com/photo-1601050690597-df0568f70950" + Q),
        ("paratha", "https://images.unsplash.com/photo-1601050690597-df0568f70950" + Q),
        ("bread", "https://images.unsplash.com/photo-1509440159596-0249088772ff" + Q),
        ("bun", "https://images.unsplash.com/photo-1509440159596-0249088772ff" + Q),
        ("cake", "https://images.unsplash.com/photo-1578985545062-69928b1d9587" + Q),
        ("pastry", "https://images.unsplash.com/photo-1517433670267-08bbd4be890f" + Q),
        ("cookie", "https://images.unsplash.com/photo-1499636136210-6f4ee915583e" + Q),
        ("biscuit", "https://images.unsplash.com/photo-1499636136210-6f4ee915583e" + Q),
        ("donut", "https://images.unsplash.com/photo-1551024601-bec78aea704b" + Q),
        ("doughnut", "https://images.unsplash.com/photo-1551024601-bec78aea704b" + Q),
        ("croissant", "https://images.unsplash.com/photo-1555507036-ab1f4038808a" + Q),
        ("muffin", "https://images.unsplash.com/photo-1607958996333-41aef7caefaa" + Q),
        ("dessert", "https://images.unsplash.com/photo-1488477181946-6428a0291777" + Q),
        ("ice cream", "https://images.unsplash.com/photo-1497034825429-c343d7c6a68f" + Q),
        ("kulfi", "https://images.unsplash.com/photo-1497034825429-c343d7c6a68f" + Q),
        ("sweet", "https://images.unsplash.com/photo-1488477181946-6428a0291777" + Q),
        ("halwa", "https://images.unsplash.com/photo-1488477181946-6428a0291777" + Q),
        ("salad", "https://images.unsplash.com/photo-1512621776951-a57141f2eefd" + Q),
        ("raita", "https://images.unsplash.com/photo-1512621776951-a57141f2eefd" + Q),
        ("soup", "https://images.unsplash.com/photo-1547592166-23ac45744acd" + Q),
        ("dal", "https://images.unsplash.com/photo-1546833999-b9f581a1996d" + Q),
        ("daal", "https://images.unsplash.com/photo-1546833999-b9f581a1996d" + Q),
        ("rice", "https://images.unsplash.com/photo-1516684669134-de6f7c473a2a" + Q),
        ("pasta", "https://images.unsplash.com/photo-1621996346565-e3dbc646d9a9" + Q),
        ("chowmein", "https://images.unsplash.com/photo-1612929633738-8fe44f7ec841" + Q),
        ("noodle", "https://images.unsplash.com/photo-1612929633738-8fe44f7ec841" + Q),
        ("chinese", "https://images.unsplash.com/photo-1585032226651-759b368d7246" + Q),
        ("manchurian", "https://images.unsplash.com/photo-1585032226651-759b368d7246" + Q),
        ("drink", "https://images.unsplash.com/photo-1544145945-f90425340c7e" + Q),
        ("juice", "https://images.unsplash.com/photo-1622597467836-f3285f2131b8" + Q),
        ("lassi", "https://images.unsplash.com/photo-1577805947697-89e18249d767" + Q),
        ("shake", "https://images.unsplash.com/photo-1572490122747-3968b75cc699" + Q),
        ("coffee", "https://images.unsplash.com/photo-1511920170033-f8396924c348" + Q),
        ("tea", "https://images.unsplash.com/photo-1571934811356-5cc061b6821f" + Q),
        ("chai", "https://images.unsplash.com/photo-1571934811356-5cc061b6821f" + Q),
        ("soda", "https://images.unsplash.com/photo-1622483767028-3f66f32aef97" + Q),
        ("water", "https://images.unsplash.com/photo-1548839140-29a749e1cf4d" + Q),
        ("milk", "https://images.unsplash.com/photo-1563636619-e9143da7973b" + Q),
        ("yogurt", "https://images.unsplash.com/photo-1488477181946-6428a0291777" + Q),
        ("egg", "https://images.unsplash.com/photo-1482049016688-2d3e1b311543" + Q),
        ("anda", "https://images.unsplash.com/photo-1482049016688-2d3e1b311543" + Q),
        ("breakfast", "https://images.unsplash.com/photo-1533089860892-a7c6f0a88666" + Q),
        ("platter", "https://images.unsplash.com/photo-1544025162-d76694265947" + Q),
        ("combo", "https://images.unsplash.com/photo-1555939594-58d7cb561ad1" + Q),
        ("deal", "https://images.unsplash.com/photo-1555939594-58d7cb561ad1" + Q),
        ("snack", "https://images.unsplash.com/photo-1621939514649-c9df7e3e6a5d" + Q),
        ("chips", "https://images.unsplash.com/photo-1566478989037-eec1753df085" + Q),
        ("chocolate", "https://images.unsplash.com/photo-1548907040-4baa42d10919" + Q),
        ("fruit", "https://images.unsplash.com/photo-1610832958506-aa56368176cf" + Q),
        ("vegetable", "https://images.unsplash.com/photo-1540420773420-3366772f4999" + Q),
        ("grocery", "https://images.unsplash.com/photo-1542838132-92c53300491e" + Q),
        ("flour", "https://images.unsplash.com/photo-1574323347407-f5e1ad6d020b" + Q),
        ("oil", "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5" + Q),
        ("spice", "https://images.unsplash.com/photo-1596040033229-a9821ebd058d" + Q),
        ("sauce", "https://images.unsplash.com/photo-1472476443507-c7a5948772fc" + Q),
        ("ketchup", "https://images.unsplash.com/photo-1472476443507-c7a5948772fc" + Q),
        ("mayo", "https://images.unsplash.com/photo-1472476443507-c7a5948772fc" + Q),
        ("cheese", "https://images.unsplash.com/photo-1486297678162-eb2a19b0a32d" + Q),
        ("bakery", "https://images.unsplash.com/photo-1509440159596-0249088772ff" + Q),
        ("tandoori", "https://images.unsplash.com/photo-1601050690597-df0568f70950" + Q),
        ("desi", "https://images.unsplash.com/photo-1585937421612-70a008356fbe" + Q),
        ("pakistani", "https://images.unsplash.com/photo-1585937421612-70a008356fbe" + Q),
        ("indian", "https://images.unsplash.com/photo-1585937421612-70a008356fbe" + Q),
        ("curry", "https://images.unsplash.com/photo-1585937421612-70a008356fbe" + Q),
        ("fry", "https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58" + Q),
        ("fried", "https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58" + Q),
    };

    public static string Resolve(
        Func<string?, string> resolveApiPath,
        string? imagePath,
        string? name,
        string? categoryName,
        int productId = 0)
    {
        if (TryResolveStoredPath(resolveApiPath, imagePath, out var storedUrl))
        {
            return storedUrl;
        }

        return PickUnsplash(name, categoryName, productId);
    }

    public static void Enrich(OnlineProductDto product, Func<string?, string> resolveApiPath)
    {
        if (product == null)
        {
            return;
        }

        var url = Resolve(resolveApiPath, product.ImagePath, product.Name, product.CategoryName, product.Id);
        product.ImageUrl = url;

        // Persist absolute URL when there was no real stored image (empty / local png placeholders).
        if (!TryResolveStoredPath(resolveApiPath, product.ImagePath, out _))
        {
            product.ImagePath = url;
        }
    }

    private static bool TryResolveStoredPath(
        Func<string?, string> resolveApiPath,
        string? imagePath,
        out string url)
    {
        url = string.Empty;
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return false;
        }

        var raw = imagePath.Trim();
        if (IsAbsoluteHttp(raw))
        {
            // Keep Unsplash / CDN / uploaded absolute URLs.
            url = raw;
            return true;
        }

        // Ignore local MAUI asset placeholders (burger_photo.png) — those are not absolute URIs.
        if (raw.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            raw.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            raw.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            raw.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
        {
            if (!raw.Contains('/') && !raw.Contains('\\'))
            {
                return false;
            }
        }

        // Server-relative uploads e.g. /ProductImages/{guid}.jpg
        if (raw.StartsWith('/') || raw.Contains("ProductImages", StringComparison.OrdinalIgnoreCase))
        {
            var resolved = resolveApiPath(raw);
            if (IsAbsoluteHttp(resolved))
            {
                url = resolved!;
                return true;
            }
        }

        return false;
    }

    public static void EnrichAll(IEnumerable<OnlineProductDto>? products, Func<string?, string> resolveApiPath)
    {
        if (products == null)
        {
            return;
        }

        foreach (var product in products)
        {
            Enrich(product, resolveApiPath);
        }
    }

    private static string PickUnsplash(string? name, string? categoryName, int productId)
    {
        var haystack = $"{name} {categoryName}".ToLowerInvariant();

        foreach (var (keyword, url) in KeywordImages)
        {
            if (haystack.Contains(keyword, StringComparison.Ordinal))
            {
                return url;
            }
        }

        var index = Math.Abs(productId == 0
            ? StringComparer.OrdinalIgnoreCase.GetHashCode(haystack)
            : productId) % FallbackFoodPool.Length;
        return FallbackFoodPool[index];
    }

    private static bool IsAbsoluteHttp(string? url)
    {
        return !string.IsNullOrWhiteSpace(url) &&
               Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
