using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public partial class CartCheckoutViewModel : BaseViewModel
{
    private readonly CartService _cart;
    private readonly OnlineOrderService _orders;
    private readonly SessionService _session;
    private readonly DeliveryLocationService _deliveryLocation;

    [ObservableProperty]
    private string customerName = string.Empty;

    [ObservableProperty]
    private string customerMobile = string.Empty;

    [ObservableProperty]
    private string deliveryAddress = string.Empty;

    [ObservableProperty]
    private string deliveryLandmark = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private DeliveryAreaDto? selectedArea;

    [ObservableProperty]
    private decimal deliveryFee;

    [ObservableProperty]
    private string locationStatusText = string.Empty;

    [ObservableProperty]
    private bool isCapturingLocation;

    public ObservableCollection<CartLine> Lines => _cart.Lines;
    public ObservableCollection<DeliveryAreaDto> Areas { get; } = new();

    public decimal Subtotal => _cart.Subtotal;
    public decimal GrandTotal => Subtotal + DeliveryFee;
    public string SubtotalText => Subtotal.ToString("0.00");
    public string DeliveryFeeText => DeliveryFee.ToString("0.00");
    public string GrandTotalText => GrandTotal.ToString("0.00");

    public CartCheckoutViewModel(
        CartService cart,
        OnlineOrderService orders,
        SessionService session,
        DeliveryLocationService deliveryLocation)
    {
        _cart = cart;
        _orders = orders;
        _session = session;
        _deliveryLocation = deliveryLocation;
        Title = "Cart";
        _cart.CartChanged += (_, _) => NotifyTotals();
        _deliveryLocation.Changed += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(RefreshLocationStatus);
        };
        RefreshLocationStatus();
    }

    private void RefreshLocationStatus() => LocationStatusText = _deliveryLocation.StatusText;

    private void NotifyTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(GrandTotal));
        OnPropertyChanged(nameof(SubtotalText));
        OnPropertyChanged(nameof(GrandTotalText));
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        Shell.SetTabBarIsVisible(Shell.Current.CurrentPage, false);

        if (_session.BranchId is not int branchId)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            Areas.Clear();
            foreach (var area in await _orders.GetDeliveryAreasAsync(branchId))
            {
                Areas.Add(area);
            }

            SelectedArea = Areas.FirstOrDefault();
            DeliveryFee = SelectedArea?.DeliveryFee ?? 0;
            NotifyTotals();

            // Pin live location as soon as checkout opens.
            if (!_deliveryLocation.HasFix)
            {
                await CaptureLocationAsync();
            }
            else
            {
                RefreshLocationStatus();
            }
        });
    }

    partial void OnSelectedAreaChanged(DeliveryAreaDto? value)
    {
        DeliveryFee = value?.DeliveryFee ?? 0;
        OnPropertyChanged(nameof(DeliveryFeeText));
        NotifyTotals();
    }

    partial void OnDeliveryAddressChanged(string value)
    {
        // As the customer types/selects an address, refresh GPS for the delivery pin.
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length >= 3)
        {
            _deliveryLocation.CaptureDebounced();
        }
    }

    [RelayCommand]
    private async Task CaptureLocationAsync()
    {
        if (IsCapturingLocation)
        {
            return;
        }

        try
        {
            IsCapturingLocation = true;
            LocationStatusText = "📍 Getting your location…";
            var ok = await _deliveryLocation.CaptureAsync();
            if (!ok)
            {
                LocationStatusText = "📍 Couldn’t get location — check permission and GPS, then try again.";
            }
            else
            {
                RefreshLocationStatus();
            }
        }
        finally
        {
            IsCapturingLocation = false;
        }
    }

    [RelayCommand]
    private void Increase(CartLine? line)
    {
        if (line == null) return;
        _cart.SetQuantity(line, line.Quantity + 1);
        NotifyTotals();
    }

    [RelayCommand]
    private void Decrease(CartLine? line)
    {
        if (line == null) return;
        _cart.SetQuantity(line, line.Quantity - 1);
        NotifyTotals();
    }

    [RelayCommand]
    private void Remove(CartLine? line)
    {
        if (line == null) return;
        _cart.Remove(line);
        NotifyTotals();
    }

    [RelayCommand]
    private async Task PlaceOrderAsync()
    {
        if (!_cart.Lines.Any())
        {
            await Shell.Current.DisplayAlert("Cart", "Cart is empty.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(CustomerName) || string.IsNullOrWhiteSpace(CustomerMobile))
        {
            await Shell.Current.DisplayAlert("Checkout", "Name and mobile are required.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(DeliveryAddress))
        {
            await Shell.Current.DisplayAlert("Checkout", "Delivery address is required.", "OK");
            return;
        }

        await RunBusyAsync(async () =>
        {
            if (!_deliveryLocation.HasFix)
            {
                await _deliveryLocation.CaptureAsync();
            }

            var input = new CreateCustomerOrderDto
            {
                CustomerName = CustomerName.Trim(),
                CustomerMobile = CustomerMobile.Trim(),
                BranchId = _session.BranchId,
                OrderDate = DateTime.Now,
                Notes = Notes,
                DeliveryAddress = DeliveryAddress.Trim(),
                DeliveryLandmark = DeliveryLandmark,
                DeliveryAreaId = SelectedArea?.Id,
                DeliveryArea = SelectedArea?.Name,
                Subtotal = Subtotal,
                DeliveryCharges = DeliveryFee,
                GrandTotal = GrandTotal,
                Latitude = _deliveryLocation.Latitude,
                Longitude = _deliveryLocation.Longitude,
                Lines = _cart.Lines.Select(l => new CreateCustomerOrderLineDto
                {
                    ProductId = l.ProductId,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice
                }).ToList()
            };

            var created = await _orders.CreateOnlineOrderAsync(input);
            _cart.Clear();
            await Shell.Current.DisplayAlert("Order placed", $"Order {created.OrderNo} submitted.", "OK");
            await Shell.Current.GoToAsync($"//customer/track?orderNo={Uri.EscapeDataString(created.OrderNo ?? string.Empty)}");
        });
    }
}
