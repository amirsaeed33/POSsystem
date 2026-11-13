using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public partial class RiderHistoryViewModel : BaseViewModel
{
    private readonly RiderOrderService _rider;

    [ObservableProperty]
    private string keyword = string.Empty;

    public ObservableCollection<RiderOrderDto> Orders { get; } = new();

    public RiderHistoryViewModel(RiderOrderService rider)
    {
        _rider = rider;
        Title = "Order History";
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        try
        {
            await LoadAsync();
        }
        finally
        {
            LoadingService.Current.HideTabLoader();
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var page = await _rider.GetOrderHistoryAsync(0, 50, Keyword);
            Orders.Clear();
            foreach (var order in page.Items)
            {
                Orders.Add(order);
            }
        });
    }
}
