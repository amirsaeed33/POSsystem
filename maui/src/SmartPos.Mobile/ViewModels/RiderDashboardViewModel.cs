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
        await RefreshAsync();
        _pollCts?.Cancel();
        _pollCts = new CancellationTokenSource();
        _ = PollAsync(_pollCts.Token);
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
            Orders.Clear();
            foreach (var order in await _rider.GetAssignedOrdersAsync())
            {
                Orders.Add(order);
            }
        });
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
