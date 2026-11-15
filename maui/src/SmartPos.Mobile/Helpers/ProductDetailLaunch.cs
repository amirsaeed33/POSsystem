using SmartPos.Mobile.Models;

namespace SmartPos.Mobile.Helpers;

/// <summary>
/// Hands a fully-populated product/deal DTO to Product Detail when API lookup by id is unavailable
/// (e.g. local/fallback combo deals with negative ids).
/// </summary>
public static class ProductDetailLaunch
{
    private static OnlineProductDto? _prefill;

    public static void SetPrefill(OnlineProductDto product) => _prefill = product;

    public static OnlineProductDto? TakePrefill(int productId)
    {
        if (_prefill == null || _prefill.Id != productId)
        {
            return null;
        }

        var item = _prefill;
        _prefill = null;
        return item;
    }
}
