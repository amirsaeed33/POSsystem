using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public partial class RiderHistoryViewModel : BaseViewModel
{
    private readonly RiderOrderService _rider;
    private readonly StaffDataCache _staffCache;

    [ObservableProperty]
    private string keyword = string.Empty;

    public ObservableCollection<RiderOrderDto> Orders { get; } = new();

    public RiderHistoryViewModel(RiderOrderService rider, StaffDataCache staffCache)
    {
        _rider = rider;
        _staffCache = staffCache;
        Title = "Order History";
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        try
        {
            await LoadHistoryAsync(forceRefresh: false);
        }
        finally
        {
            LoadingService.Current.HideTabLoader();
        }
    }

    partial void OnKeywordChanged(string value) => _ = LoadHistoryAsync(forceRefresh: true);

    [RelayCommand]
    private Task LoadAsync() => LoadHistoryAsync(forceRefresh: true);

    private async Task LoadHistoryAsync(bool forceRefresh)
    {
        if (!forceRefresh &&
            _staffCache.TryGetRiderHistory(Keyword, out var cached) &&
            cached != null)
        {
            ReplaceOrders(cached);
            return;
        }

        await RunBusyAsync(async () =>
        {
            var page = await _rider.GetOrderHistoryAsync(0, 50, Keyword);
            var items = page.Items ?? new List<RiderOrderDto>();
            _staffCache.SetRiderHistory(Keyword, items);
            ReplaceOrders(items);
        });
    }

    private void ReplaceOrders(IEnumerable<RiderOrderDto> items)
    {
        Orders.Clear();
        foreach (var order in items)
        {
            Orders.Add(order);
        }
    }
}
