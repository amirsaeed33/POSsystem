using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public partial class RiderDashboardViewModel : BaseViewModel
{
    private readonly RiderOrderService _rider;
    private readonly SessionService _session;
    private readonly AuthService _auth;
    private CancellationTokenSource? _pollCts;

    [ObservableProperty]
    private RiderDashboardStatsDto? stats;

    public ObservableCollection<RiderOrderDto> Orders { get; } = new();
    public ObservableCollection<RiderOrderDto> NewOrders { get; } = new();
    public ObservableCollection<RiderOrderDto> ActiveOrders { get; } = new();

    public bool HasNewOrders => NewOrders.Count > 0;
    public bool HasActiveOrders => ActiveOrders.Count > 0;
    public bool HasAnyOrders => Orders.Count > 0;
    public bool IsEmpty => Orders.Count == 0 && !IsBusy;
    public string NewOrdersTitle => $"New orders ({NewOrders.Count})";
    public string ActiveOrdersTitle => $"On the way ({ActiveOrders.Count})";
    public string ActiveCountText => (Stats?.ActiveAssignedCount ?? 0).ToString();
    public string DeliveredCountText => (Stats?.TodayDeliveredCount ?? 0).ToString();
    public string PendingAmountText => $"Rs {Stats?.ActivePendingAmount ?? 0:0.##}";
    public string TodayAmountText => $"Rs {Stats?.TodayDeliveredAmount ?? 0:0.##}";

    public RiderDashboardViewModel(RiderOrderService rider, SessionService session, AuthService auth)
    {
        _rider = rider;
        _session = session;
        _auth = auth;
        Title = "Rider Dashboard";
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        try
        {
            await RefreshAsync();
            _pollCts?.Cancel();
            _pollCts = new CancellationTokenSource();
            _ = PollAsync(_pollCts.Token);
        }
        finally
        {
            LoadingService.Current.HideTabLoader();
        }
    }

    [RelayCommand]
    private void Disappearing()
    {
        _pollCts?.Cancel();
    }

    private async Task PollAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(25), ct);
                await RefreshAsync();
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch
            {
                // ignore transient poll errors
            }
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await RunBusyAsync(async () =>
        {
            Stats = await _rider.GetDashboardStatsAsync();
            NotifyStatTexts();

            var list = await _rider.GetAssignedOrdersAsync() ?? new List<RiderOrderDto>();
            Orders.Clear();
            NewOrders.Clear();
            ActiveOrders.Clear();
            foreach (var order in list)
            {
                Orders.Add(order);
                if (order.IsNewOrder)
                {
                    NewOrders.Add(order);
                }
                else
                {
                    ActiveOrders.Add(order);
                }
            }

            NotifyOrderLists();
        });
    }

    private void NotifyStatTexts()
    {
        OnPropertyChanged(nameof(ActiveCountText));
        OnPropertyChanged(nameof(DeliveredCountText));
        OnPropertyChanged(nameof(PendingAmountText));
        OnPropertyChanged(nameof(TodayAmountText));
    }

    private void NotifyOrderLists()
    {
        OnPropertyChanged(nameof(HasNewOrders));
        OnPropertyChanged(nameof(HasActiveOrders));
        OnPropertyChanged(nameof(HasAnyOrders));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(NewOrdersTitle));
        OnPropertyChanged(nameof(ActiveOrdersTitle));
    }

    [RelayCommand]
    private async Task AcceptAsync(RiderOrderDto? order)
    {
        if (order == null) return;
        await RunBusyAsync(async () =>
        {
            await _rider.AcceptOrderAsync(order.Id);
            await RefreshAsync();
        });
    }

    [RelayCommand]
    private async Task RejectAsync(RiderOrderDto? order)
    {
        if (order == null) return;
        var reason = await Shell.Current.DisplayPromptAsync("Reject order", "Reason", "Reject", "Cancel", "Busy / unavailable");
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _rider.RejectOrderAsync(order.Id, reason);
            await RefreshAsync();
        });
    }

    [RelayCommand]
    private async Task CompleteAsync(RiderOrderDto? order)
    {
        if (order == null) return;
        var ok = await Shell.Current.DisplayAlert("Complete", $"Mark {order.OrderNo} as delivered?", "Yes", "No");
        if (!ok) return;
        await RunBusyAsync(async () =>
        {
            await _rider.CompleteDeliveryAsync(order.Id);
            await RefreshAsync();
        });
    }

    [RelayCommand]
    private async Task CallCustomerAsync(RiderOrderDto? order)
    {
        var phone = order?.CustomerPhone?.Trim();
        if (string.IsNullOrWhiteSpace(phone))
        {
            await Shell.Current.DisplayAlert("Call", "This order has no customer phone.", "OK");
            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(new Uri($"tel:{Uri.EscapeDataString(phone)}"));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Call", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task WhatsAppCustomerAsync(RiderOrderDto? order)
    {
        if (order == null)
        {
            return;
        }

        var number = order.WhatsAppNumber;
        if (string.IsNullOrWhiteSpace(number))
        {
            await Shell.Current.DisplayAlert("WhatsApp", "This order has no customer phone.", "OK");
            return;
        }

        var rider = string.IsNullOrWhiteSpace(Stats?.RiderName) ? "your rider" : Stats!.RiderName;
        var message = $"Hi {order.CustomerName}, this is {rider} from Aljannat. I'm your rider for order {order.OrderNo}.";
        var url = $"https://wa.me/{number}?text={Uri.EscapeDataString(message)}";

        try
        {
            await Launcher.Default.OpenAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("WhatsApp", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task OpenMapsAsync(RiderOrderDto? order)
    {
        if (order?.Latitude == null || order.Longitude == null)
        {
            await Shell.Current.DisplayAlert("Maps", "No location on this order.", "OK");
            return;
        }

        var url = $"https://www.google.com/maps/search/?api=1&query={order.Latitude},{order.Longitude}";
        await Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _auth.LogoutAsync();
        await Shell.Current.GoToAsync("//customer/home");
    }
}
