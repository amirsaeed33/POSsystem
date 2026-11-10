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

    [ObservableProperty]
    private bool nameInvalid;

    [ObservableProperty]
    private bool mobileInvalid;

    [ObservableProperty]
    private bool addressInvalid;

    [ObservableProperty]
    private CustomerSavedAddressDto? selectedSavedAddress;

    [ObservableProperty]
    private bool hasSavedAddresses;

    [ObservableProperty]
    private bool isNewAddressMode = true;

    [ObservableProperty]
    private string addressLabel = "Home";

    [ObservableProperty]
    private bool isSaveAddressChecked = true;

    [ObservableProperty]
    private bool isLoadingAddresses;

    [ObservableProperty]
    private decimal freeDeliveryThreshold = 1500m;

    [ObservableProperty]
    private decimal standardDeliveryFee = 100m;

    [ObservableProperty]
    private decimal effectiveDeliveryFee;

    [ObservableProperty]
    private decimal minimumOrderAmount;

    [ObservableProperty]
    private double freeDeliveryProgress;

    [ObservableProperty]
    private decimal amountNeededForFreeDelivery;

    [ObservableProperty]
    private bool isFreeDeliveryUnlocked;

    [ObservableProperty]
    private bool isBelowMinimumOrder;

    [ObservableProperty]
    private decimal minimumOrderShortfall;

    [ObservableProperty]
    private string checkoutButtonText = "Place order";

    [ObservableProperty]
    private bool canPlaceOrder;

    [ObservableProperty]
    private string selectedPaymentMethod = "CashOnDelivery";

    public ObservableCollection<CartLine> Lines => _cart.Lines;
    public ObservableCollection<DeliveryAreaDto> Areas { get; } = new();
    public ObservableCollection<CustomerSavedAddressDto> SavedAddresses { get; } = new();

    public bool HasSelectedSavedAddress => SelectedSavedAddress != null;
    public bool IsHomeLabelSelected => string.Equals(AddressLabel, "Home", StringComparison.OrdinalIgnoreCase);
    public bool IsOfficeLabelSelected => string.Equals(AddressLabel, "Office", StringComparison.OrdinalIgnoreCase);
    public bool IsOtherLabelSelected => string.Equals(AddressLabel, "Other", StringComparison.OrdinalIgnoreCase);

    public bool IsCodSelected => string.Equals(SelectedPaymentMethod, "CashOnDelivery", StringComparison.OrdinalIgnoreCase);
    public bool IsOnlinePaymentSelected => string.Equals(SelectedPaymentMethod, "OnlinePayment", StringComparison.OrdinalIgnoreCase);
    public string PaymentMethodBadgeText => IsOnlinePaymentSelected ? "Online" : "COD";

    public bool HasFreeDeliveryThreshold => FreeDeliveryThreshold > 0;
    public bool IsFreeDeliveryLocked => !IsFreeDeliveryUnlocked;
    public string StandardDeliveryFeeText => StandardDeliveryFee.ToString("0.00");
    public string EffectiveDeliveryFeeText => EffectiveDeliveryFee == 0 ? "FREE" : EffectiveDeliveryFee.ToString("0.00");
    public string AmountNeededForFreeDeliveryText => AmountNeededForFreeDelivery.ToString("N0");
    public string MinimumOrderAmountText => MinimumOrderAmount.ToString("N0");
    public string MinimumOrderShortfallText => MinimumOrderShortfall.ToString("N0");
    public string FreeDeliveryMotivationText => IsFreeDeliveryUnlocked
        ? "Awesome! You unlocked FREE Delivery! 🎉"
        : $"Add PKR {AmountNeededForFreeDeliveryText} more to enjoy FREE delivery!";

    public decimal Subtotal => _cart.Subtotal;
    public decimal GrandTotal => Subtotal + EffectiveDeliveryFee;
    public string SubtotalText => Subtotal.ToString("0.00");
    public string DeliveryFeeText => EffectiveDeliveryFee == 0 ? "FREE" : EffectiveDeliveryFee.ToString("0.00");
    public string GrandTotalText => GrandTotal.ToString("0.00");
    public bool HasItems => Lines.Count > 0;
    public bool IsEmpty => !HasItems;
    public string ItemCountLabel => HasItems
        ? $"{Lines.Count} item{(Lines.Count == 1 ? string.Empty : "s")}"
        : "No items yet";

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
        RecalculateThresholds();
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(GrandTotal));
        OnPropertyChanged(nameof(SubtotalText));
        OnPropertyChanged(nameof(GrandTotalText));
        OnPropertyChanged(nameof(DeliveryFeeText));
        OnPropertyChanged(nameof(StandardDeliveryFeeText));
        OnPropertyChanged(nameof(EffectiveDeliveryFeeText));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(ItemCountLabel));
        OnPropertyChanged(nameof(HasFreeDeliveryThreshold));
        OnPropertyChanged(nameof(IsFreeDeliveryLocked));
        OnPropertyChanged(nameof(FreeDeliveryMotivationText));
        OnPropertyChanged(nameof(AmountNeededForFreeDeliveryText));
        OnPropertyChanged(nameof(MinimumOrderAmountText));
        OnPropertyChanged(nameof(MinimumOrderShortfallText));
        OnPropertyChanged(nameof(PaymentMethodBadgeText));
    }

    private void RecalculateThresholds()
    {
        StandardDeliveryFee = SelectedArea?.DeliveryFee ?? 0;
        MinimumOrderAmount = SelectedArea?.MinimumOrderAmount ?? 0;

        if (FreeDeliveryThreshold > 0 && Subtotal >= FreeDeliveryThreshold)
        {
            IsFreeDeliveryUnlocked = true;
            EffectiveDeliveryFee = 0;
            AmountNeededForFreeDelivery = 0;
            FreeDeliveryProgress = 1.0;
        }
        else if (FreeDeliveryThreshold > 0)
        {
            IsFreeDeliveryUnlocked = false;
            EffectiveDeliveryFee = StandardDeliveryFee;
            AmountNeededForFreeDelivery = Math.Max(0, FreeDeliveryThreshold - Subtotal);
            FreeDeliveryProgress = (double)Math.Clamp(Subtotal / FreeDeliveryThreshold, 0m, 1m);
        }
        else
        {
            IsFreeDeliveryUnlocked = false;
            EffectiveDeliveryFee = StandardDeliveryFee;
            AmountNeededForFreeDelivery = 0;
            FreeDeliveryProgress = 0;
        }

        DeliveryFee = EffectiveDeliveryFee;

        if (MinimumOrderAmount > 0 && Subtotal < MinimumOrderAmount)
        {
            IsBelowMinimumOrder = true;
            MinimumOrderShortfall = MinimumOrderAmount - Subtotal;
            CheckoutButtonText = $"Min Order PKR {MinimumOrderAmount:N0}";
            CanPlaceOrder = false;
        }
        else
        {
            IsBelowMinimumOrder = false;
            MinimumOrderShortfall = 0;
            CheckoutButtonText = "Place order";
            CanPlaceOrder = HasItems && !IsBusy;
        }
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        Shell.SetTabBarIsVisible(Shell.Current.CurrentPage, false);

        if (string.IsNullOrWhiteSpace(CustomerName))
        {
            CustomerName = Preferences.Default.Get("sp.customerName", string.Empty);
        }
        if (string.IsNullOrWhiteSpace(CustomerMobile))
        {
            CustomerMobile = Preferences.Default.Get("sp.customerPhone", string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(CustomerMobile))
        {
            var digits = new string(CustomerMobile.Where(char.IsDigit).ToArray());
            if (digits.Length >= 7)
            {
                _ = LoadSavedAddressesAsync(digits);
            }
        }

        if (_session.BranchId is not int branchId)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            try
            {
                var header = await _orders.GetStoreHeaderAsync(branchId);
                if (header != null && header.FreeDeliveryThreshold > 0)
                {
                    FreeDeliveryThreshold = header.FreeDeliveryThreshold;
                }
                else
                {
                    FreeDeliveryThreshold = 1500m;
                }
            }
            catch
            {
                FreeDeliveryThreshold = 1500m;
            }

            Areas.Clear();
            foreach (var area in await _orders.GetDeliveryAreasAsync(branchId))
            {
                Areas.Add(area);
            }

            SelectedArea = Areas.FirstOrDefault();
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
        NotifyTotals();
    }

    partial void OnCustomerNameChanged(string value)
    {
        if (NameInvalid && !string.IsNullOrWhiteSpace(value))
        {
            NameInvalid = false;
        }
    }

    partial void OnCustomerMobileChanged(string value)
    {
        if (MobileInvalid && !string.IsNullOrWhiteSpace(value))
        {
            MobileInvalid = false;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length >= 7)
        {
            _ = LoadSavedAddressesAsync(digits);
        }
        else if (digits.Length == 0)
        {
            SavedAddresses.Clear();
            HasSavedAddresses = false;
            SelectedSavedAddress = null;
        }
    }

    partial void OnAddressLabelChanged(string value)
    {
        OnPropertyChanged(nameof(IsHomeLabelSelected));
        OnPropertyChanged(nameof(IsOfficeLabelSelected));
        OnPropertyChanged(nameof(IsOtherLabelSelected));
    }

    partial void OnSelectedPaymentMethodChanged(string value)
    {
        OnPropertyChanged(nameof(IsCodSelected));
        OnPropertyChanged(nameof(IsOnlinePaymentSelected));
        OnPropertyChanged(nameof(PaymentMethodBadgeText));
    }

    partial void OnSelectedSavedAddressChanged(CustomerSavedAddressDto? value)
    {
        OnPropertyChanged(nameof(HasSelectedSavedAddress));
    }

    partial void OnDeliveryAddressChanged(string value)
    {
        if (AddressInvalid && !string.IsNullOrWhiteSpace(value))
        {
            AddressInvalid = false;
        }

        // As the customer types/selects an address, refresh GPS for the delivery pin.
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length >= 3)
        {
            _deliveryLocation.CaptureDebounced();
        }
    }

    private bool ValidateCheckoutFields()
    {
        NameInvalid = string.IsNullOrWhiteSpace(CustomerName);
        MobileInvalid = string.IsNullOrWhiteSpace(CustomerMobile);
        AddressInvalid = string.IsNullOrWhiteSpace(DeliveryAddress);
        return !NameInvalid && !MobileInvalid && !AddressInvalid;
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

        // Strict guard clause for Minimum Order Amount
        if (IsBelowMinimumOrder)
        {
            await Shell.Current.DisplayAlert(
                "Minimum Order Required",
                $"Minimum order amount for {SelectedArea?.Name ?? "delivery"} is PKR {MinimumOrderAmount:N0}.\n\nPlease add PKR {MinimumOrderShortfall:N0} more to place your order.",
                "OK");
            return;
        }

        // Field errors: red borders only — no alert popups.
        if (!ValidateCheckoutFields())
        {
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
                PaymentMethod = SelectedPaymentMethod,
                DeliveryAddress = DeliveryAddress.Trim(),
                DeliveryLandmark = DeliveryLandmark,
                DeliveryAreaId = SelectedArea?.Id,
                DeliveryArea = SelectedArea?.Name,
                Subtotal = Subtotal,
                DeliveryCharges = EffectiveDeliveryFee,
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

            var savedAddrPhone = CustomerMobile.Trim();
            var savedAddrLabel = string.IsNullOrWhiteSpace(AddressLabel) ? "Home" : AddressLabel;
            var savedAddrText = DeliveryAddress.Trim();
            var savedAddrLandmark = DeliveryLandmark?.Trim();
            var savedAreaId = SelectedArea?.Id;
            var savedAreaName = SelectedArea?.Name;
            var savedLat = _deliveryLocation.Latitude;
            var savedLng = _deliveryLocation.Longitude;
            var isSaveAddress = IsSaveAddressChecked;
            var branchId = _session.BranchId;

            var created = await _orders.CreateOnlineOrderAsync(input);
            Preferences.Default.Set("sp.customerPhone", CustomerMobile.Trim());
            Preferences.Default.Set("sp.customerName", CustomerName.Trim());

            if (isSaveAddress && !string.IsNullOrWhiteSpace(savedAddrText))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _orders.SaveSavedAddressAsync(new SaveCustomerAddressInput
                        {
                            BranchId = branchId,
                            Phone = savedAddrPhone,
                            Label = savedAddrLabel,
                            Address = savedAddrText,
                            Landmark = savedAddrLandmark,
                            DeliveryAreaId = savedAreaId,
                            DeliveryAreaName = savedAreaName,
                            Latitude = savedLat,
                            Longitude = savedLng,
                            IsDefault = SavedAddresses.Count == 0
                        });
                    }
                    catch
                    {
                        // best-effort address persistence
                    }
                });
            }

            _cart.Clear();
            var paymentNotice = IsOnlinePaymentSelected
                ? "\nPayment: Online / Digital Payment (Pending)"
                : "\nPayment: Cash on Delivery (COD)";
            await Shell.Current.DisplayAlert("Order placed", $"Order {created.OrderNo} submitted.{paymentNotice}", "OK");
            await Shell.Current.GoToAsync($"//customer/track?orderNo={Uri.EscapeDataString(created.OrderNo ?? string.Empty)}");
        });
    }

    [RelayCommand]
    public async Task LoadSavedAddressesAsync(string? phone = null)
    {
        var mobile = phone ?? CustomerMobile;
        if (string.IsNullOrWhiteSpace(mobile)) return;

        var digits = new string(mobile.Where(char.IsDigit).ToArray());
        if (digits.Length < 7)
        {
            SavedAddresses.Clear();
            HasSavedAddresses = false;
            return;
        }

        try
        {
            IsLoadingAddresses = true;
            var addresses = await _orders.GetSavedAddressesAsync(mobile, _session.BranchId);
            SavedAddresses.Clear();
            foreach (var addr in addresses)
            {
                SavedAddresses.Add(addr);
            }
            HasSavedAddresses = SavedAddresses.Count > 0;

            // If user hasn't typed an address yet, auto-select default address
            if (HasSavedAddresses && string.IsNullOrWhiteSpace(DeliveryAddress))
            {
                var defaultAddr = SavedAddresses.FirstOrDefault(a => a.IsDefault) ?? SavedAddresses.FirstOrDefault();
                if (defaultAddr != null)
                {
                    SelectSavedAddress(defaultAddr);
                }
            }
        }
        catch
        {
            // best-effort
        }
        finally
        {
            IsLoadingAddresses = false;
        }
    }

    [RelayCommand]
    public void SelectSavedAddress(CustomerSavedAddressDto? address)
    {
        if (address == null) return;

        SelectedSavedAddress = address;
        IsNewAddressMode = false;

        foreach (var item in SavedAddresses)
        {
            item.IsSelected = (item.Id > 0 && item.Id == address.Id) || ReferenceEquals(item, address);
        }

        DeliveryAddress = address.Address ?? string.Empty;
        DeliveryLandmark = address.Landmark ?? string.Empty;

        if (address.DeliveryAreaId.HasValue && address.DeliveryAreaId.Value > 0)
        {
            var match = Areas.FirstOrDefault(a => a.Id == address.DeliveryAreaId.Value);
            if (match != null)
            {
                SelectedArea = match;
            }
        }
        else if (!string.IsNullOrWhiteSpace(address.DeliveryAreaName))
        {
            var match = Areas.FirstOrDefault(a => string.Equals(a.Name, address.DeliveryAreaName, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                SelectedArea = match;
            }
        }

        if (address.Latitude.HasValue && address.Longitude.HasValue)
        {
            _deliveryLocation.SetCoordinates(address.Latitude.Value, address.Longitude.Value);
            RefreshLocationStatus();
        }
    }

    [RelayCommand]
    public void SwitchToNewAddress()
    {
        SelectedSavedAddress = null;
        IsNewAddressMode = true;
        foreach (var item in SavedAddresses)
        {
            item.IsSelected = false;
        }
        DeliveryAddress = string.Empty;
        DeliveryLandmark = string.Empty;
    }

    [RelayCommand]
    public void SetAddressLabel(string label)
    {
        AddressLabel = label;
    }

    [RelayCommand]
    public async Task DeleteSavedAddressAsync(CustomerSavedAddressDto? address)
    {
        if (address == null) return;

        var confirm = await Shell.Current.DisplayAlert(
            "Delete Address",
            $"Are you sure you want to delete saved address '{address.DisplayLabel}'?",
            "Delete",
            "Cancel");

        if (!confirm) return;

        await _orders.DeleteSavedAddressAsync(address.Id, CustomerMobile, _session.BranchId);
        SavedAddresses.Remove(address);
        HasSavedAddresses = SavedAddresses.Count > 0;

        if (SelectedSavedAddress == address)
        {
            SwitchToNewAddress();
        }
    }

    [RelayCommand]
    public void SelectPaymentMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method)) return;
        SelectedPaymentMethod = method;
    }
}
