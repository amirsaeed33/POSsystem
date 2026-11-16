using System.Collections.Concurrent;
using System.Globalization;

namespace SmartPos.Mobile.Helpers;

/// <summary>
/// Converts a remote image URL string into a disk-cached <see cref="UriImageSource"/>.
/// Instances are reused per URL so CollectionView rebinds do not restart image downloads.
/// </summary>
public sealed class CachedUriImageSourceConverter : IValueConverter
{
    private static readonly TimeSpan DefaultCacheValidity = TimeSpan.FromDays(7);
    private static readonly ConcurrentDictionary<string, UriImageSource> SourceCache =
        new(StringComparer.OrdinalIgnoreCase);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var key = uri.AbsoluteUri;
        return SourceCache.GetOrAdd(key, static (_, state) => new UriImageSource
        {
            Uri = state,
            CachingEnabled = true,
            CacheValidity = DefaultCacheValidity
        }, uri);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
