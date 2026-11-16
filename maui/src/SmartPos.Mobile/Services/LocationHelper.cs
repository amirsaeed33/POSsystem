namespace SmartPos.Mobile.Services;

public static class LocationHelper
{
    public static async Task<(double? Lat, double? Lng)> TryGetAsync(CancellationToken ct = default)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                return (null, null);
            }

            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(12)),
                ct);
            return location == null ? (null, null) : (location.Latitude, location.Longitude);
        }
        catch
        {
            return (null, null);
        }
    }
}
