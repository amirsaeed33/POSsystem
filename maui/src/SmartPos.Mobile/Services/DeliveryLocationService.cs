namespace SmartPos.Mobile.Services;

/// <summary>
/// Holds the customer's live GPS position for delivery orders.
/// Refreshed when the address is entered or "Use my location" is tapped.
/// </summary>
public sealed class DeliveryLocationService
{
    private CancellationTokenSource? _debounceCts;

    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public DateTimeOffset? CapturedAtUtc { get; private set; }
    public bool HasFix => Latitude.HasValue && Longitude.HasValue;
    public string StatusText =>
        HasFix
            ? $"📍 Location pinned ({Latitude:0.#####}, {Longitude:0.#####})"
            : "📍 Location not pinned yet — allow location or tap Use my location";

    public event EventHandler? Changed;

    public async Task<bool> CaptureAsync(CancellationToken ct = default)
    {
        var (lat, lng) = await LocationHelper.TryGetAsync(ct);
        if (lat == null || lng == null)
        {
            Changed?.Invoke(this, EventArgs.Empty);
            return false;
        }

        Latitude = lat;
        Longitude = lng;
        CapturedAtUtc = DateTimeOffset.UtcNow;
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Debounced capture while the user types an address.</summary>
    public void CaptureDebounced(int delayMs = 700)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMs, token);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                await CaptureAsync(token);
            }
            catch (TaskCanceledException)
            {
                // ignored
            }
        }, token);
    }

    public void Clear()
    {
        Latitude = null;
        Longitude = null;
        CapturedAtUtc = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
