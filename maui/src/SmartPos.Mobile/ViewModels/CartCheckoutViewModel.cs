using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public partial class CartCheckoutViewModel : BaseViewModel
{
    private const decimal DefaultFreeDeliveryThreshold = 2500m;
    private const decimal DefaultStandardDeliveryFee = 180m;
    private const decimal DefaultMinimumOrderAmount = 800m;
    private const decimal QuickNaanPrice = 180m;

    private readonly AppState _appState;
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
    private bool isEditingAddress;

    [ObservableProperty]
    private decimal freeDeliveryThreshold = DefaultFreeDeliveryThreshold;

    [ObservableProperty]
    private decimal standardDeliveryFee = DefaultStandardDeliveryFee;

    [ObservableProperty]
    private decimal effectiveDeliveryFee;

    [ObservableProperty]
    private decimal minimumOrderAmount = DefaultMinimumOrderAmount;

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
    private string checkoutButtonText = "Place Order";

    [ObservableProperty]
    private bool canPlaceOrder;

    [ObservableProperty]
    private string selectedPaymentMethod = "CashOnDelivery";

    public ObservableCollection<CartLine> Lines => _appState.CartLines;
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
    public string StandardDeliveryFeeText => StandardDeliveryFee.ToString("0");
    public string EffectiveDeliveryFeeText => EffectiveDeliveryFee == 0 ? "FREE" : $"Rs. {EffectiveDeliveryFee:0}";
    public string DeliveryFeeSavedText => $"SAVED Rs. {StandardDeliveryFee:0}";
    public string AmountNeededForFreeDeliveryText => AmountNeededForFreeDelivery.ToString("N0");
    public string MinimumOrderAmountText => MinimumOrderAmount.ToString("N0");
    public string MinimumOrderShortfallText => MinimumOrderShortfall.ToString("N0");
    public string FreeDeliveryMotivationText => IsFreeDeliveryUnlocked
        ? "You unlocked FREE Delivery!"
        : $"Add Rs. {AmountNeededForFreeDeliveryText} more for FREE Delivery";
    public string PackagingFeeText => "Rs. 0";
    public string MinimumOrderWarningText => $"Minimum order for delivery is Rs. {MinimumOrderAmount:0}";

    public decimal Subtotal => _appState.CartSubtotal;
    public decimal GrandTotal => Subtotal + EffectiveDeliveryFee;
    public string SubtotalText => $"Rs. {Subtotal:0}";
    public string DeliveryFeeText => EffectiveDeliveryFee == 0 ? "FREE" : $"Rs. {EffectiveDeliveryFee:0}";
    public string GrandTotalText => $"Rs. {GrandTotal:0}";
    public string PlaceOrderButtonText => IsBelowMinimumOrder
        ? $"Min Order  •  Rs. {MinimumOrderAmount:0}"
        : $"Place Order  •  Rs. {GrandTotal:0}";
    public bool HasItems => Lines.Count > 0;
    public bool IsEmpty => !HasItems;
    public string ItemCountLabel => HasItems
        ? $"{Lines.Count} item{(Lines.Count == 1 ? string.Empty : "s")}"
        : "No items yet";
    public int CartCount => _cart.ItemCount;
    public string AddressDisplayText => string.IsNullOrWhiteSpace(DeliveryAddress)
        ? "Add your street, block, or landmark for delivery"
        : DeliveryAddress.Trim();
    public bool HasAddressText => !string.IsNullOrWhiteSpace(DeliveryAddress);
    public bool HasLocationStatus => !string.IsNullOrWhiteSpace(LocationStatusText);

    public CartCheckoutViewModel(
        AppState appState,
        OnlineOrderService orders,
        DeliveryLocationService deliveryLocation)
    {
        _appState = appState;
        _cart = appState.Cart;
        _session = appState.Session;
        _orders = orders;
        _deliveryLocation = deliveryLocation;
        Title = "Cart";
        _appState.CartChanged += (_, _) => NotifyTotals();
        _deliveryLocation.Changed += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(RefreshLocationStatus);
        };
        RefreshLocationStatus();
    }

    private void RefreshLocationStatus()
    {
        LocationStatusText = _deliveryLocation.StatusText;
        OnPropertyChanged(nameof(HasLocationStatus));
    }

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
        OnPropertyChanged(nameof(DeliveryFeeSavedText));
        OnPropertyChanged(nameof(PackagingFeeText));
        OnPropertyChanged(nameof(PlaceOrderButtonText));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(ItemCountLabel));
        OnPropertyChanged(nameof(CartCount));
        OnPropertyChanged(nameof(HasFreeDeliveryThreshold));
        OnPropertyChanged(nameof(IsFreeDeliveryLocked));
        OnPropertyChanged(nameof(FreeDeliveryMotivationText));
        OnPropertyChanged(nameof(AmountNeededForFreeDeliveryText));
        OnPropertyChanged(nameof(MinimumOrderAmountText));
        OnPropertyChanged(nameof(MinimumOrderShortfallText));
        OnPropertyChanged(nameof(MinimumOrderWarningText));
        OnPropertyChanged(nameof(PaymentMethodBadgeText));
        OnPropertyChanged(nameof(AddressDisplayText));
        OnPropertyChanged(nameof(HasAddressText));
    }

    private void RecalculateThresholds()
    {
        var areaFee = SelectedArea?.DeliveryFee ?? 0;
        StandardDeliveryFee = areaFee > 0 ? areaFee : DefaultStandardDeliveryFee;

        var areaMin = SelectedArea?.MinimumOrderAmount ?? 0;
        MinimumOrderAmount = areaMin > 0 ? areaMin : DefaultMinimumOrderAmount;

        if (FreeDeliveryThreshold <= 0)
        {
            FreeDeliveryThreshold = DefaultFreeDeliveryThreshold;
        }

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
            CheckoutButtonText = PlaceOrderButtonText;
            CanPlaceOrder = false;
        }
        else
        {
            IsBelowMinimumOrder = false;
            MinimumOrderShortfall = 0;
            CheckoutButtonText = PlaceOrderButtonText;
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

        if (string.IsNullOrWhiteSpace(DeliveryAddress))
        {
            IsEditingAddress = true;
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
            FreeDeliveryThreshold = DefaultFreeDeliveryThreshold;
            NotifyTotals();
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
                else if (header != null && header.BaseDeliveryFee > 0)
                {
                    FreeDeliveryThreshold = DefaultFreeDeliveryThreshold;
                    if (SelectedArea == null)
                    {
                        StandardDeliveryFee = header.BaseDeliveryFee;
                    }
                }
                else
                {
                    FreeDeliveryThreshold = DefaultFreeDeliveryThreshold;
                }
            }
            catch
            {
                FreeDeliveryThreshold = DefaultFreeDeliveryThreshold;
            }

            Areas.Clear();
            foreach (var area in await _orders.GetDeliveryAreasAsync(branchId))
            {
                Areas.Add(area);
            }

            SelectedArea = Areas.FirstOrDefault();
            NotifyTotals();

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

        var match = SavedAddresses.FirstOrDefault(a =>
            string.Equals(a.Label, value, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            SelectSavedAddress(match);
        }
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

        OnPropertyChanged(nameof(AddressDisplayText));
        OnPropertyChanged(nameof(HasAddressText));

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
    private async Task GoBackAsync()
    {
        if (Shell.Current == null)
        {
            return;
        }

        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch
        {
            await Shell.Current.GoToAsync("//customer/home");
        }
    }

    [RelayCommand]
    private async Task ClearCartAsync()
    {
        if (!HasItems)
        {
            return;
        }

        var confirm = await Shell.Current.DisplayAlert(
            "Clear Tray",
            "Remove all items from your tray?",
            "Clear",
            "Cancel");
        if (!confirm)
        {
            return;
        }

        _cart.Clear();
        NotifyTotals();
    }

    [RelayCommand]
    private async Task ExploreMenuAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//customer/order");
        }
    }

    [RelayCommand]
    private void EditAddress()
    {
        IsEditingAddress = !IsEditingAddress;
    }

    [RelayCommand]
    private void AppendNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return;
        }

        var clean = note.Trim();
        if (string.IsNullOrWhiteSpace(Notes))
        {
            Notes = clean;
            return;
        }

        if (Notes.Contains(clean, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Notes = $"{Notes.Trim().TrimEnd(',')}, {clean}";
    }

    [RelayCommand]
    private async Task AddQuickNaanAsync()
    {
        OnlineProductDto? naan = null;
        try
        {
            if (_session.BranchId is int branchId)
            {
                var catalog = await _orders.GetCatalogAsync(branchId);
                naan = catalog.FirstOrDefault(p =>
                    !string.IsNullOrWhiteSpace(p.Name) &&
                    p.Name.Contains("Naan", StringComparison.OrdinalIgnoreCase));
            }
        }
        catch
        {
            // fall through to synthetic line
        }

        if (naan != null)
        {
            _cart.AddOrIncrement(naan);
        }
        else
        {
            _cart.AddOrIncrement(new OnlineProductDto
            {
                Id = -180,
                Name = "Naan",
                Price = QuickNaanPrice
            });
        }

        NotifyTotals();
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
            LocationStatusText = "Getting your location…";
            var ok = await _deliveryLocation.CaptureAsync();
            if (!ok)
            {
                LocationStatusText = "Couldn't get location — check permission and GPS, then try again.";
            }
            else
            {
                RefreshLocationStatus();
                if (string.IsNullOrWhiteSpace(DeliveryAddress) && !string.IsNullOrWhiteSpace(LocationStatusText))
                {
                    // Keep typed address primary; status still shown under location pill.
                }
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

        if (IsBelowMinimumOrder)
        {
            await Shell.Current.DisplayAlert(
                "Minimum Order Required",
                $"Minimum order amount for {SelectedArea?.Name ?? "delivery"} is Rs. {MinimumOrderAmount:N0}.\n\nPlease add Rs. {MinimumOrderShortfall:N0} more to place your order.",
                "OK");
            return;
        }

        if (!ValidateCheckoutFields())
        {
            IsEditingAddress = true;
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

            if (HasSavedAddresses && string.IsNullOrWhiteSpace(DeliveryAddress))
            {
                var labeled = SavedAddresses.FirstOrDefault(a =>
                    string.Equals(a.Label, AddressLabel, StringComparison.OrdinalIgnoreCase));
                var defaultAddr = labeled
                    ?? SavedAddresses.FirstOrDefault(a => a.IsDefault)
                    ?? SavedAddresses.FirstOrDefault();
                if (defaultAddr != null)
                {
                    if (!string.IsNullOrWhiteSpace(defaultAddr.Label))
                    {
                        AddressLabel = defaultAddr.Label!;
                    }
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
        IsEditingAddress = false;

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
        IsEditingAddress = true;
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
