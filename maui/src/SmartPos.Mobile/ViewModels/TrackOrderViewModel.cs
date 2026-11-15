using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

[QueryProperty(nameof(OrderNo), "orderNo")]
public partial class TrackOrderViewModel : BaseViewModel, IDisposable
{
    private static readonly Color Saffron = Color.FromArgb("#D9822B");
    private static readonly Color BurntOrange = Color.FromArgb("#C84B11");
    private static readonly Color SoftRing = Color.FromArgb("#F0C4AE");
    private static readonly Color PendingFill = Color.FromArgb("#F4EFEA");
    private static readonly Color PendingBorder = Color.FromArgb("#EAE2DA");
    private static readonly Color MutedText = Color.FromArgb("#8C7E74");
    private static readonly Color TextPrimary = Color.FromArgb("#1E1916");
    private static readonly Color Gold = Color.FromArgb("#E89B3C");
    private static readonly Color StarIdle = Color.FromArgb("#EAE2DA");
    private static readonly Color ComplimentSelectedBg = Color.FromArgb("#FDF1EB");
    private static readonly Color ComplimentIdleBg = Color.FromArgb("#FAF7F4");

    private readonly OnlineOrderService _orders;
    private readonly CartService _cart;
    private CancellationTokenSource? _liveTrackingCts;
    private string? _lastTrackedNo;
    private bool _trackQueued;
    private bool _isSilentPolling;
    private int _countdownSeconds = 12;
    private readonly HashSet<string> _selectedCompliments = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    private string orderNo = string.Empty;

    [ObservableProperty]
    private CustomerOrderDto? order;

    [ObservableProperty]
    private string statusBanner = string.Empty;

    [ObservableProperty]
    private string elapsedTimeText = "Just now";

    [ObservableProperty]
    private string liveCountdownText = "Live • updates in 12s";

    [ObservableProperty]
    private string countdownText = "—";

    [ObservableProperty]
    private bool isLivePollingActive;

    [ObservableProperty]
    private bool isItemsExpanded = true;

    [ObservableProperty]
    private int customerRatingDraft = 5;

    [ObservableProperty]
    private string customerCommentDraft = string.Empty;

    [ObservableProperty]
    private bool isSubmittingRating;

    [ObservableProperty]
    private bool isReviewSubmitted;

    [ObservableProperty]
    private bool isCancelling;

    [ObservableProperty]
    private bool complimentPipingHot;

    [ObservableProperty]
    private bool complimentPoliteRider;

    [ObservableProperty]
    private bool complimentRichFlavor;

    [ObservableProperty]
    private bool complimentFastArrival;

    public ObservableCollection<CustomerOrderLineDto> Lines { get; } = new();

    public bool HasOrder => Order != null && !string.IsNullOrWhiteSpace(Order.OrderNo);
    public string OrderTitle => Order?.FormattedOrderNo ?? OrderNo;
    public string OrderNumber => string.IsNullOrWhiteSpace(OrderTitle) ? "ORDER #" : $"ORDER #{OrderTitle}";
    public string TotalText => Order == null ? string.Empty : $"Rs. {Order.TotalAmount:N0}";
    public string SubTotalText => Order == null ? string.Empty : $"Rs. {Order.SubTotal:N0}";
    public string DeliveryFeeText => Order == null
        ? string.Empty
        : (Order.DeliveryFee == 0 ? "FREE" : $"Rs. {Order.DeliveryFee:N0}");
    public string AreaText => Order?.DeliveryAreaName ?? string.Empty;
    public string CustomerAddressText => Order?.CustomerAddress ?? Order?.DeliveryAreaName ?? "Delivery Address";
    public string CustomerNotesText => string.IsNullOrWhiteSpace(Order?.Notes)
        ? "No special notes for this order."
        : Order!.Notes!;
    public bool HasCustomerNotes => !string.IsNullOrWhiteSpace(Order?.Notes);
    public string CustomerText => Order?.CustomerName ?? string.Empty;
    public string FormattedOrderDate => Order?.FormattedDate ?? string.Empty;

    public int StepIndex => CurrentStep;
    public bool IsSummaryExpanded => IsItemsExpanded;
    public int SelectedRating => CustomerRatingDraft;
    public string ReviewComment
    {
        get => CustomerCommentDraft;
        set => CustomerCommentDraft = value;
    }

    // --- Status Stage Logic ---
    // 0 = Pending/Received, 1 = Approved/In Kitchen, 2 = Rejected/Cancelled, 3 = InProgress/On the Way, 4/5 = Delivered
    public int CurrentStep => Order == null ? -1 : Order.Status switch
    {
        2 => -1,
        0 => 0,
        1 => 1,
        3 => 2,
        >= 4 => 3,
        _ => 0
    };

    public bool IsCancelled => Order?.Status == 2;
    public bool IsDelivered => Order?.Status >= 4;
    public bool CanCancelOrder => HasOrder && Order?.Status == 0 && !IsCancelling;
    public bool ShowReorderButton => HasOrder && IsCancelled;
    public bool ShowCancelAction => CanCancelOrder;
    public bool ShowActiveHeroActions => HasOrder && !IsCancelled;

    public bool IsStep1Completed => CurrentStep >= 0 && !IsCancelled;
    public bool IsStep1Active => CurrentStep == 0 && !IsCancelled;
    public bool IsStep2Completed => CurrentStep >= 1 && !IsCancelled;
    public bool IsStep2Active => CurrentStep == 1 && !IsCancelled;
    public bool IsStep3Completed => CurrentStep >= 2 && !IsCancelled;
    public bool IsStep3Active => CurrentStep == 2 && !IsCancelled;
    public bool IsStep4Completed => CurrentStep >= 3 && !IsCancelled;
    public bool IsStep4Active => CurrentStep == 3 && !IsCancelled;
    public bool IsLine1Active => CurrentStep >= 1 && !IsCancelled;
    public bool IsLine2Active => CurrentStep >= 2 && !IsCancelled;
    public bool IsLine3Active => CurrentStep >= 3 && !IsCancelled;
    public bool ShowNormalStepper => HasOrder && !IsCancelled;

    // Vertical timeline node styling (saffron / burnt orange — no green)
    public Color Step1StrokeColor => IsStep1Active ? SoftRing : (IsStep1Completed ? Saffron : PendingBorder);
    public Color Step1BgColor => IsStep1Active ? BurntOrange : (IsStep1Completed ? Saffron : PendingFill);
    public Color Step1TextColor => IsStep1Completed || IsStep1Active ? Colors.White : MutedText;
    public double Step1StrokeThickness => IsStep1Active ? 3 : 1.5;
    public Color Step1LabelColor => IsStep1Completed || IsStep1Active ? TextPrimary : MutedText;
    public Color Line1Color => IsLine1Active ? Saffron : PendingBorder;

    public Color Step2StrokeColor => IsStep2Active ? SoftRing : (IsStep2Completed ? Saffron : PendingBorder);
    public Color Step2BgColor => IsStep2Active ? BurntOrange : (IsStep2Completed ? Saffron : PendingFill);
    public Color Step2TextColor => IsStep2Completed || IsStep2Active ? Colors.White : MutedText;
    public double Step2StrokeThickness => IsStep2Active ? 3 : 1.5;
    public Color Step2LabelColor => IsStep2Completed || IsStep2Active ? TextPrimary : MutedText;
    public Color Line2Color => IsLine2Active ? Saffron : PendingBorder;

    public Color Step3StrokeColor => IsStep3Active ? SoftRing : (IsStep3Completed ? Saffron : PendingBorder);
    public Color Step3BgColor => IsStep3Active ? BurntOrange : (IsStep3Completed ? Saffron : PendingFill);
    public Color Step3TextColor => IsStep3Completed || IsStep3Active ? Colors.White : MutedText;
    public double Step3StrokeThickness => IsStep3Active ? 3 : 1.5;
    public Color Step3LabelColor => IsStep3Completed || IsStep3Active ? TextPrimary : MutedText;
    public Color Line3Color => IsLine3Active ? Saffron : PendingBorder;

    public Color Step4StrokeColor => IsStep4Active ? SoftRing : (IsStep4Completed ? Saffron : PendingBorder);
    public Color Step4BgColor => IsStep4Active ? BurntOrange : (IsStep4Completed ? Saffron : PendingFill);
    public Color Step4TextColor => IsStep4Completed || IsStep4Active ? Colors.White : MutedText;
    public double Step4StrokeThickness => IsStep4Active ? 3 : 1.5;
    public Color Step4LabelColor => IsStep4Completed || IsStep4Active ? TextPrimary : MutedText;

    public string Step1Glyph => IsStep1Completed && !IsStep1Active ? "✓" : "1";
    public string Step2Glyph => IsStep2Completed && !IsStep2Active ? "✓" : "2";
    public string Step3Glyph => IsStep3Completed && !IsStep3Active ? "✓" : "3";
    public string Step4Glyph => IsStep4Completed ? "✓" : "4";

    public string Step1Title => "Order Placed";
    public string Step2Title => "Preparing in Kitchen";
    public string Step3Title => "On the Way";
    public string Step4Title => "Delivered";

    public string Step1Detail
    {
        get
        {
            var kitchen = string.IsNullOrWhiteSpace(Order?.BranchName) ? "Aljannat Kitchen" : $"{Order!.BranchName} Kitchen";
            var time = FormatOrderClock(Order?.OrderDate);
            return $"Confirmed by {kitchen}{(string.IsNullOrEmpty(time) ? string.Empty : $" • {time}")}";
        }
    }

    public string Step2Detail
    {
        get
        {
            if (CurrentStep < 1)
            {
                return "Wok-seared fresh over charcoal flame";
            }

            var time = FormatOrderClock(Order?.OrderDate.AddMinutes(7));
            return $"Wok-seared fresh over charcoal flame{(string.IsNullOrEmpty(time) ? string.Empty : $" • {time}")}";
        }
    }

    public string Step3Detail
    {
        get
        {
            if (CurrentStep < 2)
            {
                return "Rider will pick up your insulated bag";
            }

            var rider = string.IsNullOrWhiteSpace(Order?.RiderName) ? "Your rider" : Order!.RiderName!;
            var time = FormatOrderClock(Order?.OrderDate.AddMinutes(19));
            return $"{rider} picked up your insulated bag{(string.IsNullOrEmpty(time) ? string.Empty : $" • {time}")}";
        }
    }

    public string Step4Detail => IsStep4Completed
        ? "Enjoy your royal dastarkhwan at doorstep"
        : "Enjoy your royal dastarkhwan at doorstep";

    public bool ShowStep1ProgressPill => IsStep1Active;
    public bool ShowStep2ProgressPill => IsStep2Active;
    public bool ShowStep3ProgressPill => IsStep3Active;
    public bool ShowStep4ProgressPill => IsStep4Active;

    public string LiveStatusLabel => IsCancelled
        ? "ORDER CANCELLED"
        : CurrentStep switch
        {
            3 => "DELIVERED",
            2 => "RIDER ON THE WAY",
            1 => "PREPARING IN KITCHEN",
            _ => "ORDER PLACED"
        };

    public string StatusHeadline => IsCancelled
        ? "This feast was cancelled"
        : CurrentStep switch
        {
            3 => "Your feast has arrived",
            2 => "Your feast is out for delivery",
            1 => "Your feast is being prepared",
            _ => "Your feast is confirmed"
        };

    public string StatusSubHeadline => IsCancelled
        ? "You can reorder this feast anytime from the button below."
        : CurrentStep switch
        {
            3 => "Tell us how the kitchen and rider did below.",
            2 => $"{RiderName} is heading to your doorstep with care.",
            1 => "Our chefs are wok-searing your order over charcoal.",
            _ => "The kitchen will confirm and start preparing shortly."
        };

    public Color StatusPillBackgroundColor => IsCancelled
        ? Color.FromArgb("#33B83227")
        : Color.FromArgb("#26E89B3C");

    public Color StatusPillTextColor => IsCancelled
        ? Color.FromArgb("#FDECEB")
        : Gold;

    public Color StatusPillStrokeColor => IsCancelled
        ? Color.FromArgb("#F5C6C2")
        : Color.FromArgb("#66E89B3C");

    public string KitchenRouteText
    {
        get
        {
            var kitchen = string.IsNullOrWhiteSpace(Order?.BranchName)
                ? "Aljannat Kitchen"
                : $"{Order!.BranchName} Kitchen";
            var address = CustomerAddressText;
            return $"{kitchen}  →  {address}";
        }
    }

    public string EstimatedDeliveryEta
    {
        get
        {
            if (Order == null || Order.OrderDate == default)
            {
                return "30 - 45 mins";
            }

            if (IsDelivered)
            {
                return "Delivered";
            }

            if (IsCancelled)
            {
                return "Cancelled";
            }

            var target = ToLocal(Order.OrderDate).AddMinutes(40);
            return $"{target:h:mm tt}";
        }
    }

    // --- Rider Info ---
    public string RiderName => string.IsNullOrWhiteSpace(Order?.RiderName) ? "Delivery Partner" : Order.RiderName!;
    public string RiderPhoneText => Order?.RiderPhone?.Trim() ?? string.Empty;
    public bool HasRiderPhone => !string.IsNullOrWhiteSpace(RiderPhoneText);
    public bool ShowRiderCard => !IsCancelled && CurrentStep >= 2 && (!string.IsNullOrWhiteSpace(Order?.RiderName) || HasRiderPhone);
    public string RiderBadgeText => "YOUR HOSPITALITY RIDER";
    public string RiderInitials
    {
        get
        {
            var name = RiderName;
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
            }

            return name.Length >= 2
                ? name[..2].ToUpperInvariant()
                : name.ToUpperInvariant();
        }
    }

    public string RiderVehicleText => "Hospitality fleet partner";
    public string RiderRatingText => "★ 4.9";

    // --- Accordion ---
    public string ItemsCountBadge => $"{TotalItemsCount} {(TotalItemsCount == 1 ? "Item" : "Items")}";
    public int TotalItemsCount => Lines.Sum(l => (int)l.Quantity);
    public string ItemsAccordionToggleGlyph => IsItemsExpanded ? "▴" : "▾";
    public string SummarySubtitle
    {
        get
        {
            var pay = FormatPaymentMethod(Order?.PaymentMethod);
            return $"{ItemsCountBadge}  •  {TotalText} ({pay})";
        }
    }

    public string PaymentFooterLabel
    {
        get
        {
            var pay = FormatPaymentMethod(Order?.PaymentMethod);
            return pay.Contains("COD", StringComparison.OrdinalIgnoreCase) ||
                   pay.Contains("Cash", StringComparison.OrdinalIgnoreCase)
                ? "Total Amount (Cash on Delivery)"
                : $"Total Amount ({pay})";
        }
    }

    // --- Feedback & Rating ---
    public bool ShowRatingSection => IsDelivered;
    public bool HasExistingRating => Order?.CustomerRating.HasValue == true && Order.CustomerRating.Value > 0;
    public string RatingSectionTitle => HasExistingRating ? "Your Hospitality Review" : "Rate Your Feast & Delivery";
    public string RatingBadgeText => HasExistingRating ? $"{Order?.CustomerRating}/5 ★" : string.Empty;
    public string RatingHintText => "Your feedback helps our kitchen & rider team";
    public string SubmitButtonLabel => IsSubmittingRating
        ? "Saving review…"
        : CanSubmitRating
            ? ((HasExistingRating || IsReviewSubmitted) ? "Update Hospitality Review" : "Submit Hospitality Review")
            : ((IsReviewSubmitted || HasExistingRating) ? "Review Submitted ✓" : "Submit Hospitality Review");

    public Color SubmitButtonBackground => CanSubmitRating
        ? BurntOrange
        : Saffron;

    public string Star1Glyph => CustomerRatingDraft >= 1 ? "★" : "☆";
    public string Star2Glyph => CustomerRatingDraft >= 2 ? "★" : "☆";
    public string Star3Glyph => CustomerRatingDraft >= 3 ? "★" : "☆";
    public string Star4Glyph => CustomerRatingDraft >= 4 ? "★" : "☆";
    public string Star5Glyph => CustomerRatingDraft >= 5 ? "★" : "☆";

    public Color Star1Color => CustomerRatingDraft >= 1 ? Saffron : StarIdle;
    public Color Star2Color => CustomerRatingDraft >= 2 ? Gold : StarIdle;
    public Color Star3Color => CustomerRatingDraft >= 3 ? Saffron : StarIdle;
    public Color Star4Color => CustomerRatingDraft >= 4 ? Gold : StarIdle;
    public Color Star5Color => CustomerRatingDraft >= 5 ? Saffron : StarIdle;

    public Color ComplimentPipingHotBg => ComplimentPipingHot ? ComplimentSelectedBg : ComplimentIdleBg;
    public Color ComplimentPoliteRiderBg => ComplimentPoliteRider ? ComplimentSelectedBg : ComplimentIdleBg;
    public Color ComplimentRichFlavorBg => ComplimentRichFlavor ? ComplimentSelectedBg : ComplimentIdleBg;
    public Color ComplimentFastArrivalBg => ComplimentFastArrival ? ComplimentSelectedBg : ComplimentIdleBg;

    public Color ComplimentPipingHotStroke => ComplimentPipingHot ? SoftRing : PendingBorder;
    public Color ComplimentPoliteRiderStroke => ComplimentPoliteRider ? SoftRing : PendingBorder;
    public Color ComplimentRichFlavorStroke => ComplimentRichFlavor ? SoftRing : PendingBorder;
    public Color ComplimentFastArrivalStroke => ComplimentFastArrival ? SoftRing : PendingBorder;

    public string RatingDescriptionText => CustomerRatingDraft switch
    {
        5 => "Excellent feast & delivery",
        4 => "Very good experience",
        3 => "Average experience",
        2 => "Below expectations",
        1 => "Needs improvement",
        _ => string.Empty
    };

    public bool CanSubmitRating
    {
        get
        {
            if (Order == null || string.IsNullOrWhiteSpace(Order.OrderNo) || IsSubmittingRating)
            {
                return false;
            }

            var draftComment = BuildCommentWithCompliments().Trim();
            var savedComment = (Order.CustomerFeedbackComment ?? string.Empty).Trim();
            var ratingChanged = CustomerRatingDraft != (Order.CustomerRating ?? 0);
            var commentChanged = draftComment != savedComment;
            return ratingChanged || commentChanged || _selectedCompliments.Count > 0;
        }
    }

    public TrackOrderViewModel(OnlineOrderService orders, CartService cart)
    {
        _orders = orders;
        _cart = cart;
        Title = "Track Order";
    }

    partial void OnOrderNoChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _ = TrackAsync();
    }

    partial void OnIsItemsExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsSummaryExpanded));
        OnPropertyChanged(nameof(ItemsAccordionToggleGlyph));
    }

    partial void OnCustomerRatingDraftChanged(int value)
    {
        OnPropertyChanged(nameof(SelectedRating));
        NotifyRatingVisuals();
    }

    partial void OnCustomerCommentDraftChanged(string value)
    {
        OnPropertyChanged(nameof(ReviewComment));
        OnPropertyChanged(nameof(CanSubmitRating));
    }

    partial void OnOrderChanged(CustomerOrderDto? value)
    {
        NotifyOrderPresentation();

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
                0 => "Waiting for the restaurant to confirm your order.",
                1 => "Your order is being prepared in the kitchen.",
                2 => "This order was cancelled.",
                3 => "Rider is delivering your food.",
                4 => "Delivered — enjoy your meal!",
                _ => StatusHeadline
            };

        if (value?.CustomerRating.HasValue == true && value.CustomerRating.Value > 0)
        {
            CustomerRatingDraft = value.CustomerRating.Value;
            CustomerCommentDraft = value.CustomerFeedbackComment ?? string.Empty;
            IsReviewSubmitted = true;
        }
        else
        {
            CustomerRatingDraft = 5;
            CustomerCommentDraft = string.Empty;
            IsReviewSubmitted = false;
        }

        ClearCompliments();
        NotifyRatingProperties();
        UpdateElapsedTime();
        UpdateArrivalCountdown();
    }

    public async Task AppearingAsync()
    {
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
        else if (HasOrder)
        {
            StartLiveTracking();
        }
    }

    public void Disappearing()
    {
        StopLiveTracking();
    }

    public void Dispose()
    {
        StopLiveTracking();
        GC.SuppressFinalize(this);
    }

    public void StartLiveTracking()
    {
        StopLiveTracking();
        _countdownSeconds = 12;
        IsLivePollingActive = true;
        _liveTrackingCts = new CancellationTokenSource();
        var token = _liveTrackingCts.Token;
        _ = RunLiveTrackingLoopAsync(token);
    }

    public void StopLiveTracking()
    {
        IsLivePollingActive = false;
        try
        {
            if (_liveTrackingCts != null)
            {
                _liveTrackingCts.Cancel();
                _liveTrackingCts.Dispose();
                _liveTrackingCts = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TrackOrderViewModel.StopLiveTracking] Cancel/dispose failed: {ex.Message}");
        }
    }

    private async Task RunLiveTrackingLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, token);
                if (token.IsCancellationRequested)
                {
                    break;
                }

                MainThread.BeginInvokeOnMainThread(TickSecond);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TrackOrder] Loop error: {ex.Message}");
            }
        }
    }

    private void TickSecond()
    {
        UpdateElapsedTime();
        UpdateArrivalCountdown();

        if (IsDelivered || IsCancelled)
        {
            LiveCountdownText = IsDelivered ? "Delivered" : "Cancelled";
            return;
        }

        _countdownSeconds--;
        if (_countdownSeconds <= 0)
        {
            _countdownSeconds = 12;
            _ = SilentPollOrderAsync();
        }

        LiveCountdownText = $"Live • updates in {_countdownSeconds}s";
    }

    private void UpdateArrivalCountdown()
    {
        if (Order == null || Order.OrderDate == default)
        {
            CountdownText = "40 mins";
            return;
        }

        if (IsDelivered)
        {
            CountdownText = "Arrived";
            return;
        }

        if (IsCancelled)
        {
            CountdownText = "Cancelled";
            return;
        }

        var target = ToLocal(Order.OrderDate).AddMinutes(40);
        var remaining = target - DateTime.Now;
        if (remaining.TotalSeconds <= 0)
        {
            CountdownText = "Any moment";
            return;
        }

        var mins = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
        CountdownText = mins == 1 ? "1 min" : $"{mins} mins";
    }

    private void UpdateElapsedTime()
    {
        if (Order == null || Order.OrderDate == default)
        {
            ElapsedTimeText = "Just now";
            return;
        }

        var orderDate = ToLocal(Order.OrderDate);
        var diff = DateTime.Now - orderDate;
        if (diff < TimeSpan.Zero)
        {
            diff = TimeSpan.Zero;
        }

        if (diff.TotalSeconds < 60)
        {
            ElapsedTimeText = $"{Math.Max(1, (int)diff.TotalSeconds)}s ago";
        }
        else if (diff.TotalHours < 1)
        {
            ElapsedTimeText = $"{(int)diff.TotalMinutes}m {diff.Seconds:D2}s ago";
        }
        else
        {
            ElapsedTimeText = $"{(int)diff.TotalHours}h {diff.Minutes:D2}m ago";
        }
    }

    private async Task SilentPollOrderAsync()
    {
        if (_isSilentPolling || string.IsNullOrWhiteSpace(OrderNo) || IsBusy)
        {
            return;
        }

        try
        {
            _isSilentPolling = true;
            var clean = OrderNo.Trim();
            var live = await _orders.TrackOrderAsync(clean);
            if (live != null && !string.IsNullOrWhiteSpace(live.OrderNo))
            {
                var previousStatus = Order?.Status;
                Order = live;
                if (previousStatus.HasValue && previousStatus.Value != live.Status && live.Status >= 4)
                {
                    _ = MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        _ = AljannatDialogService.Current.ShowAlertAsync("Delivered!", "Your order has been delivered! Please rate your experience below.", "OK");
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TrackOrder] Silent poll error: {ex.Message}");
        }
        finally
        {
            _isSilentPolling = false;
        }
    }

    [RelayCommand]
    private void ToggleItemsExpanded()
    {
        IsItemsExpanded = !IsItemsExpanded;
    }

    [RelayCommand]
    private void ToggleSummary() => ToggleItemsExpanded();

    [RelayCommand]
    private async Task CallRiderAsync()
    {
        if (!HasRiderPhone)
        {
            return;
        }

        try
        {
            var uri = new Uri($"tel:{NormalizeDialNumber(RiderPhoneText)}");
            await Launcher.Default.OpenAsync(uri);
        }
        catch (Exception ex)
        {
            await AljannatDialogService.Current.ShowAlertAsync("Call", $"Could not start the call. {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task WhatsAppRiderAsync()
    {
        if (!HasRiderPhone)
        {
            return;
        }

        try
        {
            var phone = ToWhatsAppNumber(RiderPhoneText);
            if (string.IsNullOrWhiteSpace(phone))
            {
                return;
            }

            var message = Uri.EscapeDataString($"Hi, regarding order {Order?.OrderNo}");
            var uri = new Uri($"https://wa.me/{phone}?text={message}");
            await Launcher.Default.OpenAsync(uri);
        }
        catch (Exception ex)
        {
            await AljannatDialogService.Current.ShowAlertAsync("WhatsApp", $"Could not open WhatsApp. {ex.Message}", "OK");
        }
    }

    private static string NormalizeDialNumber(string phone) =>
        new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());

    private static string ToWhatsAppNumber(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        if (digits.Length == 11 && digits.StartsWith('0'))
        {
            digits = "92" + digits[1..];
        }

        return digits;
    }

    [RelayCommand]
    private async Task TrackAsync()
    {
        if (string.IsNullOrWhiteSpace(OrderNo))
        {
            await AljannatDialogService.Current.ShowAlertAsync("Track", "Enter an order number (for example ORD-00012).", "OK");
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

        if (HasOrder)
        {
            StartLiveTracking();
        }

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

    [RelayCommand]
    private async Task CancelOrderAsync()
    {
        if (!CanCancelOrder || Order == null || string.IsNullOrWhiteSpace(Order.OrderNo))
        {
            return;
        }

        var confirm = await AljannatDialogService.Current.ShowConfirmAsync(
            "Cancel Order",
            $"Cancel order {Order.FormattedOrderNo}? This cannot be undone once the kitchen starts preparing.",
            "Yes, Cancel",
            "Keep Order",
            isDestructive: true);

        if (!confirm)
        {
            return;
        }

        IsCancelling = true;
        OnPropertyChanged(nameof(CanCancelOrder));
        OnPropertyChanged(nameof(ShowCancelAction));

        try
        {
            var updated = await _orders.CancelOrderAsync(Order.OrderNo);
            if (updated != null)
            {
                Order = updated;
                await AljannatDialogService.Current.ShowAlertAsync("Cancelled", "Your order has been cancelled.", "OK");
            }
            else
            {
                await AljannatDialogService.Current.ShowAlertAsync(
                    "Cancel", "Could not cancel this order. It may already be in the kitchen.",
                    "OK");
                await TrackAsync();
            }
        }
        catch (Exception ex)
        {
            await AljannatDialogService.Current.ShowAlertAsync("Cancel", $"Error: {ex.Message}", "OK");
        }
        finally
        {
            IsCancelling = false;
            OnPropertyChanged(nameof(CanCancelOrder));
            OnPropertyChanged(nameof(ShowCancelAction));
        }
    }

    [RelayCommand]
    private async Task ReorderAsync()
    {
        if (Order == null || !Order.CanReorder)
        {
            await AljannatDialogService.Current.ShowAlertAsync(
                "Re-order", "This order does not have any items available to re-order.",
                "OK");
            return;
        }

        var clearCart = true;
        if (_cart.Lines.Count > 0)
        {
            var action = await AljannatDialogService.Current.ShowActionSheetAsync(
                "You have items in your cart. How would you like to proceed?",
                "Cancel",
                null,
                "Replace Cart with this Order",
                "Add to Existing Cart Items");

            if (action == "Cancel" || string.IsNullOrWhiteSpace(action))
            {
                return;
            }

            clearCart = action.StartsWith("Replace", StringComparison.OrdinalIgnoreCase);
        }

        var count = _cart.ReorderFromCustomerOrder(Order, clearCart);
        if (count > 0)
        {
            await AljannatDialogService.Current.ShowAlertAsync("Added to Cart", $"{count} item(s) from order #{Order.FormattedOrderNo} added to your cart.", "Checkout Now");
            await Shell.Current.GoToAsync("//customer/cart");
        }
    }

    [RelayCommand]
    private void SelectStar(string starParam)
    {
        if (int.TryParse(starParam, out var star) && star >= 1 && star <= 5)
        {
            CustomerRatingDraft = star;
            IsReviewSubmitted = false;
            OnPropertyChanged(nameof(SubmitButtonLabel));
            OnPropertyChanged(nameof(SubmitButtonBackground));
        }
    }

    [RelayCommand]
    private void ToggleCompliment(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        var key = tag.Trim();
        if (!_selectedCompliments.Add(key))
        {
            _selectedCompliments.Remove(key);
        }

        ComplimentPipingHot = _selectedCompliments.Contains("Piping Hot");
        ComplimentPoliteRider = _selectedCompliments.Contains("Polite Rider");
        ComplimentRichFlavor = _selectedCompliments.Contains("Rich Flavor");
        ComplimentFastArrival = _selectedCompliments.Contains("Fast Arrival");
        NotifyComplimentVisuals();
        OnPropertyChanged(nameof(CanSubmitRating));
        IsReviewSubmitted = false;
        OnPropertyChanged(nameof(SubmitButtonLabel));
        OnPropertyChanged(nameof(SubmitButtonBackground));
    }

    private void ClearCompliments()
    {
        _selectedCompliments.Clear();
        ComplimentPipingHot = false;
        ComplimentPoliteRider = false;
        ComplimentRichFlavor = false;
        ComplimentFastArrival = false;
        NotifyComplimentVisuals();
    }

    private void NotifyComplimentVisuals()
    {
        OnPropertyChanged(nameof(ComplimentPipingHotBg));
        OnPropertyChanged(nameof(ComplimentPoliteRiderBg));
        OnPropertyChanged(nameof(ComplimentRichFlavorBg));
        OnPropertyChanged(nameof(ComplimentFastArrivalBg));
        OnPropertyChanged(nameof(ComplimentPipingHotStroke));
        OnPropertyChanged(nameof(ComplimentPoliteRiderStroke));
        OnPropertyChanged(nameof(ComplimentRichFlavorStroke));
        OnPropertyChanged(nameof(ComplimentFastArrivalStroke));
    }

    private string BuildCommentWithCompliments()
    {
        var baseComment = (CustomerCommentDraft ?? string.Empty).Trim();
        if (_selectedCompliments.Count == 0)
        {
            return baseComment;
        }

        if (string.IsNullOrWhiteSpace(baseComment))
        {
            return string.Join(" • ", _selectedCompliments.OrderBy(x => x));
        }

        foreach (var tag in _selectedCompliments.OrderBy(x => x))
        {
            if (baseComment.Contains(tag, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            baseComment = $"{baseComment} • {tag}";
        }

        return baseComment.Trim();
    }

    private void NotifyRatingVisuals()
    {
        OnPropertyChanged(nameof(Star1Glyph));
        OnPropertyChanged(nameof(Star2Glyph));
        OnPropertyChanged(nameof(Star3Glyph));
        OnPropertyChanged(nameof(Star4Glyph));
        OnPropertyChanged(nameof(Star5Glyph));
        OnPropertyChanged(nameof(Star1Color));
        OnPropertyChanged(nameof(Star2Color));
        OnPropertyChanged(nameof(Star3Color));
        OnPropertyChanged(nameof(Star4Color));
        OnPropertyChanged(nameof(Star5Color));
        OnPropertyChanged(nameof(RatingDescriptionText));
        OnPropertyChanged(nameof(CanSubmitRating));
        OnPropertyChanged(nameof(SelectedRating));
    }

    private void NotifyRatingProperties()
    {
        OnPropertyChanged(nameof(IsDelivered));
        OnPropertyChanged(nameof(ShowRatingSection));
        OnPropertyChanged(nameof(HasExistingRating));
        OnPropertyChanged(nameof(RatingSectionTitle));
        OnPropertyChanged(nameof(RatingBadgeText));
        OnPropertyChanged(nameof(RatingHintText));
        OnPropertyChanged(nameof(SubmitButtonLabel));
        OnPropertyChanged(nameof(SubmitButtonBackground));
        OnPropertyChanged(nameof(IsReviewSubmitted));
        NotifyRatingVisuals();
    }

    private void NotifyOrderPresentation()
    {
        OnPropertyChanged(nameof(HasOrder));
        OnPropertyChanged(nameof(OrderTitle));
        OnPropertyChanged(nameof(OrderNumber));
        OnPropertyChanged(nameof(TotalText));
        OnPropertyChanged(nameof(SubTotalText));
        OnPropertyChanged(nameof(DeliveryFeeText));
        OnPropertyChanged(nameof(AreaText));
        OnPropertyChanged(nameof(CustomerAddressText));
        OnPropertyChanged(nameof(CustomerNotesText));
        OnPropertyChanged(nameof(HasCustomerNotes));
        OnPropertyChanged(nameof(CustomerText));
        OnPropertyChanged(nameof(FormattedOrderDate));
        OnPropertyChanged(nameof(CurrentStep));
        OnPropertyChanged(nameof(StepIndex));
        OnPropertyChanged(nameof(IsCancelled));
        OnPropertyChanged(nameof(IsDelivered));
        OnPropertyChanged(nameof(CanCancelOrder));
        OnPropertyChanged(nameof(ShowReorderButton));
        OnPropertyChanged(nameof(ShowCancelAction));
        OnPropertyChanged(nameof(ShowActiveHeroActions));
        OnPropertyChanged(nameof(IsStep1Completed));
        OnPropertyChanged(nameof(IsStep1Active));
        OnPropertyChanged(nameof(IsStep2Completed));
        OnPropertyChanged(nameof(IsStep2Active));
        OnPropertyChanged(nameof(IsStep3Completed));
        OnPropertyChanged(nameof(IsStep3Active));
        OnPropertyChanged(nameof(IsStep4Completed));
        OnPropertyChanged(nameof(IsStep4Active));
        OnPropertyChanged(nameof(IsLine1Active));
        OnPropertyChanged(nameof(IsLine2Active));
        OnPropertyChanged(nameof(IsLine3Active));
        OnPropertyChanged(nameof(Step1Glyph));
        OnPropertyChanged(nameof(Step2Glyph));
        OnPropertyChanged(nameof(Step3Glyph));
        OnPropertyChanged(nameof(Step4Glyph));
        OnPropertyChanged(nameof(Step1Title));
        OnPropertyChanged(nameof(Step2Title));
        OnPropertyChanged(nameof(Step3Title));
        OnPropertyChanged(nameof(Step4Title));
        OnPropertyChanged(nameof(Step1Detail));
        OnPropertyChanged(nameof(Step2Detail));
        OnPropertyChanged(nameof(Step3Detail));
        OnPropertyChanged(nameof(Step4Detail));
        OnPropertyChanged(nameof(ShowStep1ProgressPill));
        OnPropertyChanged(nameof(ShowStep2ProgressPill));
        OnPropertyChanged(nameof(ShowStep3ProgressPill));
        OnPropertyChanged(nameof(ShowStep4ProgressPill));
        OnPropertyChanged(nameof(LiveStatusLabel));
        OnPropertyChanged(nameof(StatusHeadline));
        OnPropertyChanged(nameof(StatusSubHeadline));
        OnPropertyChanged(nameof(StatusPillBackgroundColor));
        OnPropertyChanged(nameof(StatusPillTextColor));
        OnPropertyChanged(nameof(StatusPillStrokeColor));
        OnPropertyChanged(nameof(KitchenRouteText));
        OnPropertyChanged(nameof(EstimatedDeliveryEta));
        OnPropertyChanged(nameof(CountdownText));
        OnPropertyChanged(nameof(RiderName));
        OnPropertyChanged(nameof(RiderPhoneText));
        OnPropertyChanged(nameof(HasRiderPhone));
        OnPropertyChanged(nameof(ShowRiderCard));
        OnPropertyChanged(nameof(RiderInitials));
        OnPropertyChanged(nameof(RiderVehicleText));
        OnPropertyChanged(nameof(RiderRatingText));
        OnPropertyChanged(nameof(RiderBadgeText));
        OnPropertyChanged(nameof(ShowRatingSection));
        OnPropertyChanged(nameof(ShowNormalStepper));
        OnPropertyChanged(nameof(Step1StrokeThickness));
        OnPropertyChanged(nameof(Step1StrokeColor));
        OnPropertyChanged(nameof(Step1BgColor));
        OnPropertyChanged(nameof(Step1TextColor));
        OnPropertyChanged(nameof(Step1LabelColor));
        OnPropertyChanged(nameof(Line1Color));
        OnPropertyChanged(nameof(Step2StrokeThickness));
        OnPropertyChanged(nameof(Step2StrokeColor));
        OnPropertyChanged(nameof(Step2BgColor));
        OnPropertyChanged(nameof(Step2TextColor));
        OnPropertyChanged(nameof(Step2LabelColor));
        OnPropertyChanged(nameof(Line2Color));
        OnPropertyChanged(nameof(Step3StrokeThickness));
        OnPropertyChanged(nameof(Step3StrokeColor));
        OnPropertyChanged(nameof(Step3BgColor));
        OnPropertyChanged(nameof(Step3TextColor));
        OnPropertyChanged(nameof(Step3LabelColor));
        OnPropertyChanged(nameof(Line3Color));
        OnPropertyChanged(nameof(Step4StrokeThickness));
        OnPropertyChanged(nameof(Step4StrokeColor));
        OnPropertyChanged(nameof(Step4BgColor));
        OnPropertyChanged(nameof(Step4TextColor));
        OnPropertyChanged(nameof(Step4LabelColor));
        OnPropertyChanged(nameof(ItemsCountBadge));
        OnPropertyChanged(nameof(TotalItemsCount));
        OnPropertyChanged(nameof(SummarySubtitle));
        OnPropertyChanged(nameof(PaymentFooterLabel));
        OnPropertyChanged(nameof(IsSummaryExpanded));
        OnPropertyChanged(nameof(ItemsAccordionToggleGlyph));
    }

    [RelayCommand]
    private async Task SubmitRatingAsync()
    {
        if (Order == null || string.IsNullOrWhiteSpace(Order.OrderNo) || IsSubmittingRating || !CanSubmitRating)
        {
            return;
        }

        IsSubmittingRating = true;
        OnPropertyChanged(nameof(SubmitButtonLabel));
        OnPropertyChanged(nameof(CanSubmitRating));

        try
        {
            var comment = BuildCommentWithCompliments();
            var updated = await _orders.RateOrderAsync(
                Order.OrderNo,
                CustomerRatingDraft,
                comment);

            if (updated != null)
            {
                Order = updated;
                CustomerRatingDraft = updated.CustomerRating ?? CustomerRatingDraft;
                CustomerCommentDraft = updated.CustomerFeedbackComment ?? string.Empty;
                ClearCompliments();
                IsReviewSubmitted = true;
                NotifyRatingProperties();

                await AljannatDialogService.Current.ShowAlertAsync("Thank you", "Your hospitality review was saved.", "OK");
            }
            else
            {
                await AljannatDialogService.Current.ShowAlertAsync(
                    "Rating", "Could not save your review. Please check your connection and try again.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await AljannatDialogService.Current.ShowAlertAsync("Rating", $"Error: {ex.Message}", "OK");
        }
        finally
        {
            IsSubmittingRating = false;
            OnPropertyChanged(nameof(SubmitButtonLabel));
            OnPropertyChanged(nameof(SubmitButtonBackground));
            OnPropertyChanged(nameof(CanSubmitRating));
        }
    }

    [RelayCommand]
    private Task SubmitReviewAsync() => SubmitRatingAsync();

    [RelayCommand]
    private Task SubmitFeedbackAsync() => SubmitRatingAsync();

    [RelayCommand]
    private async Task GoBackAsync()
    {
        StopLiveTracking();
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    [RelayCommand]
    private async Task ViewOrderHistoryAsync()
    {
        StopLiveTracking();
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//customer/history");
        }
    }

    [RelayCommand]
    private Task NavigateToOrderHistoryAsync() => ViewOrderHistoryAsync();

    private static DateTime ToLocal(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;

    private static string FormatOrderClock(DateTime? value)
    {
        if (value == null || value == default(DateTime))
        {
            return string.Empty;
        }

        return ToLocal(value.Value).ToString("h:mm tt");
    }

    private static string FormatPaymentMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            return "COD";
        }

        if (method.Contains("Cash", StringComparison.OrdinalIgnoreCase) ||
            method.Contains("COD", StringComparison.OrdinalIgnoreCase) ||
            method.Contains("CashOnDelivery", StringComparison.OrdinalIgnoreCase))
        {
            return "COD";
        }

        return method;
    }
}
