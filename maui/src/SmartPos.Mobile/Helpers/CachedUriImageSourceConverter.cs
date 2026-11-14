using System.Globalization;

namespace SmartPos.Mobile.Helpers;

/// <summary>
/// Converts a remote image URL string into a disk-cached <see cref="UriImageSource"/>.
/// </summary>
public sealed class CachedUriImageSourceConverter : IValueConverter
{
    private static readonly TimeSpan DefaultCacheValidity = TimeSpan.FromDays(7);

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

        return new UriImageSource
        {
            Uri = uri,
            CachingEnabled = true,
            CacheValidity = DefaultCacheValidity
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
