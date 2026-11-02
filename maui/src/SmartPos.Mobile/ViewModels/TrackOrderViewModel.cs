using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

[QueryProperty(nameof(OrderNo), "orderNo")]
public partial class TrackOrderViewModel : BaseViewModel
{
    private readonly OnlineOrderService _orders;
    private string? _lastTrackedNo;
    private bool _trackQueued;

    [ObservableProperty]
    private string orderNo = string.Empty;

    [ObservableProperty]
    private CustomerOrderDto? order;

    [ObservableProperty]
    private string statusBanner = string.Empty;

    public ObservableCollection<CustomerOrderLineDto> Lines { get; } = new();

    public bool HasOrder => Order != null && !string.IsNullOrWhiteSpace(Order.OrderNo);
    public string OrderTitle => Order?.OrderNo ?? string.Empty;
    public string StatusText => string.IsNullOrWhiteSpace(Order?.StatusName)
        ? (Order == null ? string.Empty : $"Status {Order.Status}")
        : Order!.StatusName!;
    public string TotalText => Order == null ? string.Empty : $"Rs {Order.TotalAmount:0.##}";
    public string AreaText => Order?.DeliveryAreaName ?? string.Empty;
    public string RiderText => string.IsNullOrWhiteSpace(Order?.RiderName)
        ? "Rider not assigned yet"
        : $"Rider: {Order!.RiderName}";
    public string CustomerText => Order?.CustomerName ?? string.Empty;

    public TrackOrderViewModel(OnlineOrderService orders)
    {
        _orders = orders;
        Title = "Track Order";
    }

    partial void OnOrderNoChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        // Shell query may arrive before the page is ready; queue one track.
        _ = TrackAsync();
    }

    partial void OnOrderChanged(CustomerOrderDto? value)
    {
        OnPropertyChanged(nameof(HasOrder));
        OnPropertyChanged(nameof(OrderTitle));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(TotalText));
        OnPropertyChanged(nameof(AreaText));
        OnPropertyChanged(nameof(RiderText));
        OnPropertyChanged(nameof(CustomerText));

        Lines.Clear();
        if (value?.Lines != null)
        {
            foreach (var line in value.Lines)
            {
                Lines.Add(line);
            }
        }

        StatusBanner = value == null
            ? string.Empty
            : value.Status switch
            {
                0 => "Waiting for the restaurant to accept your order.",
                1 or 3 => "Your order is being prepared.",
                2 => "This order was rejected.",
                4 => "Delivered — enjoy your meal!",
                _ => StatusText
            };
    }

    public async Task AppearingAsync()
    {
        // Re-apply query from Shell location when QueryProperty is missed on // routes.
        try
        {
            var loc = Shell.Current?.CurrentState?.Location?.OriginalString ?? string.Empty;
            var qIndex = loc.IndexOf("orderNo=", StringComparison.OrdinalIgnoreCase);
            if (qIndex >= 0)
            {
                var raw = loc[(qIndex + "orderNo=".Length)..];
                var amp = raw.IndexOf('&');
                if (amp >= 0)
                {
                    raw = raw[..amp];
                }

                var fromQuery = Uri.UnescapeDataString(raw).Trim();
                if (!string.IsNullOrWhiteSpace(fromQuery) &&
                    !string.Equals(OrderNo, fromQuery, StringComparison.OrdinalIgnoreCase))
                {
                    OrderNo = fromQuery;
                    return;
                }
            }
        }
        catch
        {
            // ignore parse issues
        }

        if (!string.IsNullOrWhiteSpace(OrderNo) &&
            !string.Equals(_lastTrackedNo, OrderNo.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            await TrackAsync();
        }
    }

    [RelayCommand]
    private async Task TrackAsync()
    {
        if (string.IsNullOrWhiteSpace(OrderNo))
        {
            await Shell.Current.DisplayAlert("Track", "Enter an order number (for example ORD-00012).", "OK");
            return;
        }

        if (IsBusy)
        {
            _trackQueued = true;
            return;
        }

        var clean = OrderNo.Trim();
        await RunBusyAsync(async () =>
        {
            Order = await _orders.TrackOrderAsync(clean);
            _lastTrackedNo = clean;
            if (Order != null && !string.IsNullOrWhiteSpace(Order.OrderNo))
            {
                OrderNo = Order.OrderNo;
            }
        });

        if (_trackQueued)
        {
            _trackQueued = false;
            if (!string.IsNullOrWhiteSpace(OrderNo) &&
                !string.Equals(_lastTrackedNo, OrderNo.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                await TrackAsync();
            }
        }
    }
}
