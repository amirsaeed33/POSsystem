using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

[QueryProperty(nameof(OrderNo), "orderNo")]
public partial class TrackOrderViewModel : BaseViewModel
{
    private readonly OnlineOrderService _orders;

    [ObservableProperty]
    private string orderNo = string.Empty;

    [ObservableProperty]
    private CustomerOrderDto? order;

    public TrackOrderViewModel(OnlineOrderService orders)
    {
        _orders = orders;
        Title = "Track Order";
    }

    partial void OnOrderNoChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _ = TrackAsync();
        }
    }

    [RelayCommand]
    private async Task TrackAsync()
    {
        if (string.IsNullOrWhiteSpace(OrderNo))
        {
            await Shell.Current.DisplayAlert("Track", "Enter an order number.", "OK");
            return;
        }

        await RunBusyAsync(async () =>
        {
            Order = await _orders.TrackOrderAsync(OrderNo.Trim());
        });
    }
}
