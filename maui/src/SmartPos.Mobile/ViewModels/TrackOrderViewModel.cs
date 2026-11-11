using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

[QueryProperty(nameof(OrderNo), "orderNo")]
public partial class TrackOrderViewModel : BaseViewModel, IDisposable
{
    private readonly OnlineOrderService _orders;
    private CancellationTokenSource? _liveTrackingCts;
    private string? _lastTrackedNo;
    private bool _trackQueued;
    private bool _isSilentPolling;
    private int _countdownSeconds = 12;

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
    private bool isLivePollingActive;

    [ObservableProperty]
    private bool isItemsExpanded = true;

    [ObservableProperty]
    private int customerRatingDraft = 5;

    [ObservableProperty]
    private string customerCommentDraft = string.Empty;

    [ObservableProperty]
    private bool isSubmittingRating;

    public ObservableCollection<CustomerOrderLineDto> Lines { get; } = new();

    public bool HasOrder => Order != null && !string.IsNullOrWhiteSpace(Order.OrderNo);
    public string OrderTitle => Order?.FormattedOrderNo ?? OrderNo;
    public string TotalText => Order == null ? string.Empty : $"Rs {Order.TotalAmount:0.##}";
    public string SubTotalText => Order == null ? string.Empty : $"Rs {Order.SubTotal:0.##}";
    public string DeliveryFeeText => Order == null
        ? string.Empty
        : (Order.DeliveryFee == 0 ? "FREE" : $"Rs {Order.DeliveryFee:0.##}");
    public string AreaText => Order?.DeliveryAreaName ?? string.Empty;
    public string CustomerAddressText => Order?.CustomerAddress ?? Order?.DeliveryAreaName ?? "Delivery Address";
    public string CustomerNotesText => Order?.Notes ?? string.Empty;
    public bool HasCustomerNotes => !string.IsNullOrWhiteSpace(CustomerNotesText);
    public string CustomerText => Order?.CustomerName ?? string.Empty;
    public string FormattedOrderDate => Order?.FormattedDate ?? string.Empty;

    // --- Status Stage Logic ---
    // 0 = Pending/Received, 1 = Approved/In Kitchen, 2 = Rejected/Cancelled, 3 = InProgress/On the Way, 4/5 = Delivered
    public int CurrentStep => Order == null ? -1 : Order.Status switch
    {
        2 => -1,   // Cancelled / Rejected
        0 => 0,    // Order Received
        1 => 1,    // In Kitchen
        3 => 2,    // Out for Delivery
        >= 4 => 3, // Delivered
        _ => 0
    };

    public bool IsCancelled => Order?.Status == 2;
    public bool IsDelivered => Order?.Status >= 4;

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

    // Stepper Styling Properties (Colors, Thickness, Text Colors)
    public double Step1StrokeThickness => IsStep1Active ? 2.5 : 1.5;
    public Color Step1StrokeColor => Color.FromArgb("#10B981");
    public Color Step1BgColor => IsStep1Completed && !IsStep1Active ? Color.FromArgb("#10B981") : (IsStep1Active ? Color.FromArgb("#ECFDF5") : Color.FromArgb("#FFFFFF"));
    public Color Step1TextColor => IsStep1Completed && !IsStep1Active ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#10B981");
    public Color Step1LabelColor => IsStep1Completed ? Color.FromArgb("#10B981") : Color.FromArgb("#9CA3AF");

    public Color Line1Color => IsLine1Active ? Color.FromArgb("#10B981") : Color.FromArgb("#E5E7EB");

    public double Step2StrokeThickness => IsStep2Active ? 2.5 : 1.5;
    public Color Step2StrokeColor => IsStep2Completed || IsStep2Active ? Color.FromArgb("#10B981") : Color.FromArgb("#E5E7EB");
    public Color Step2BgColor => IsStep2Completed && !IsStep2Active ? Color.FromArgb("#10B981") : (IsStep2Active ? Color.FromArgb("#ECFDF5") : Color.FromArgb("#F9FAFB"));
    public Color Step2TextColor => IsStep2Completed && !IsStep2Active ? Color.FromArgb("#FFFFFF") : (IsStep2Active ? Color.FromArgb("#10B981") : Color.FromArgb("#9CA3AF"));
    public Color Step2LabelColor => IsStep2Completed || IsStep2Active ? Color.FromArgb("#10B981") : Color.FromArgb("#9CA3AF");

    public Color Line2Color => IsLine2Active ? Color.FromArgb("#10B981") : Color.FromArgb("#E5E7EB");

    public double Step3StrokeThickness => IsStep3Active ? 2.5 : 1.5;
    public Color Step3StrokeColor => IsStep3Completed || IsStep3Active ? Color.FromArgb("#10B981") : Color.FromArgb("#E5E7EB");
    public Color Step3BgColor => IsStep3Completed && !IsStep3Active ? Color.FromArgb("#10B981") : (IsStep3Active ? Color.FromArgb("#ECFDF5") : Color.FromArgb("#F9FAFB"));
    public Color Step3TextColor => IsStep3Completed && !IsStep3Active ? Color.FromArgb("#FFFFFF") : (IsStep3Active ? Color.FromArgb("#10B981") : Color.FromArgb("#9CA3AF"));
    public Color Step3LabelColor => IsStep3Completed || IsStep3Active ? Color.FromArgb("#10B981") : Color.FromArgb("#9CA3AF");

    public Color Line3Color => IsLine3Active ? Color.FromArgb("#10B981") : Color.FromArgb("#E5E7EB");

    public double Step4StrokeThickness => IsStep4Active ? 2.5 : 1.5;
    public Color Step4StrokeColor => IsStep4Completed ? Color.FromArgb("#10B981") : Color.FromArgb("#E5E7EB");
    public Color Step4BgColor => IsStep4Completed ? Color.FromArgb("#10B981") : Color.FromArgb("#F9FAFB");
    public Color Step4TextColor => IsStep4Completed ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#9CA3AF");
    public Color Step4LabelColor => IsStep4Completed ? Color.FromArgb("#10B981") : Color.FromArgb("#9CA3AF");

    // Stepper Glyphs
    public string Step1Glyph => IsStep1Completed && !IsStep1Active ? "✓" : "📋";
    public string Step2Glyph => IsStep2Completed && !IsStep2Active ? "✓" : "🍳";
    public string Step3Glyph => IsStep3Completed && !IsStep3Active ? "✓" : "🛵";
    public string Step4Glyph => IsStep4Completed ? "✓" : "📍";

    // Status titles & badges
    public string StatusHeadline => IsCancelled
        ? "Order Cancelled"
        : (CurrentStep switch
        {
            3 => "Delivered! Enjoy your meal 🎉",
            2 => "Out for Delivery 🛵",
            1 => "In Kitchen • Cooking your meal 🍳",
            _ => "Order Received 📋"
        });

    public string StatusSubHeadline => IsCancelled
        ? "The restaurant was unable to process this order."
        : (CurrentStep switch
        {
            3 => "Your order has been delivered. Tell us how it was below!",
            2 => $"Rider {(string.IsNullOrWhiteSpace(Order?.RiderName) ? "partner" : Order.RiderName)} is heading to your address.",
            1 => "The chef is preparing your fresh meal with care.",
            _ => "We have received your order and the store will confirm shortly."
        });

    public Color StatusPillBackgroundColor => IsCancelled
        ? Color.FromArgb("#FEE2E2")
        : (CurrentStep switch
        {
            3 => Color.FromArgb("#D1FAE5"),
            2 => Color.FromArgb("#FEF3C7"),
            1 => Color.FromArgb("#DBEAFE"),
            _ => Color.FromArgb("#FEF3C7")
        });

    public Color StatusPillTextColor => IsCancelled
        ? Color.FromArgb("#DC2626")
        : (CurrentStep switch
        {
            3 => Color.FromArgb("#059669"),
            2 => Color.FromArgb("#D97706"),
            1 => Color.FromArgb("#2563EB"),
            _ => Color.FromArgb("#D97706")
        });

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
            var target = Order.OrderDate.ToLocalTime().AddMinutes(40);
            return $"Expected by {target:hh:mm tt}";
        }
    }

    // --- Rider Info ---
    public string RiderName => string.IsNullOrWhiteSpace(Order?.RiderName) ? "Delivery Partner" : Order.RiderName;
    public string RiderPhoneText => Order?.RiderPhone?.Trim() ?? string.Empty;
    public bool HasRiderPhone => !string.IsNullOrWhiteSpace(RiderPhoneText);
    public bool ShowRiderCard => !IsCancelled && !string.IsNullOrWhiteSpace(Order?.RiderName) && CurrentStep >= 2;
    public string RiderBadgeText => "Verified Restaurant Delivery Partner 🛵";

    // --- Accordion ---
    public string ItemsCountBadge => $"{Lines.Count} {(Lines.Count == 1 ? "item" : "items")}";
    public string ItemsAccordionToggleGlyph => IsItemsExpanded ? "▲" : "▼";

    // --- Feedback & Rating ---
    public bool ShowRatingSection => IsDelivered;
    public bool HasExistingRating => Order?.CustomerRating.HasValue == true && Order.CustomerRating.Value > 0;
    public string RatingSectionTitle => HasExistingRating ? "Your Verified Feedback" : "Rate Your Experience";
    public string RatingBadgeText => HasExistingRating ? $"{Order?.CustomerRating}/5 ★" : string.Empty;
    public string RatingHintText => HasExistingRating
        ? "You can update your rating or review anytime."
        : "How was the food and delivery quality?";
    public string SubmitButtonLabel => IsSubmittingRating
        ? "Saving review…"
        : (HasExistingRating ? "Update Review" : "Submit Review");

    public string Star1Glyph => CustomerRatingDraft >= 1 ? "★" : "☆";
    public string Star2Glyph => CustomerRatingDraft >= 2 ? "★" : "☆";
    public string Star3Glyph => CustomerRatingDraft >= 3 ? "★" : "☆";
    public string Star4Glyph => CustomerRatingDraft >= 4 ? "★" : "☆";
    public string Star5Glyph => CustomerRatingDraft >= 5 ? "★" : "☆";

    public Color Star1Color => CustomerRatingDraft >= 1 ? Color.FromArgb("#F59E0B") : Color.FromArgb("#CBD5E1");
    public Color Star2Color => CustomerRatingDraft >= 2 ? Color.FromArgb("#F59E0B") : Color.FromArgb("#CBD5E1");
    public Color Star3Color => CustomerRatingDraft >= 3 ? Color.FromArgb("#F59E0B") : Color.FromArgb("#CBD5E1");
    public Color Star4Color => CustomerRatingDraft >= 4 ? Color.FromArgb("#F59E0B") : Color.FromArgb("#CBD5E1");
    public Color Star5Color => CustomerRatingDraft >= 5 ? Color.FromArgb("#F59E0B") : Color.FromArgb("#CBD5E1");

    public string RatingDescriptionText => CustomerRatingDraft switch
    {
        5 => "⭐⭐⭐⭐⭐ Excellent! Loved the food & delivery",
        4 => "⭐⭐⭐⭐ Very Good, enjoyed it",
        3 => "⭐⭐⭐ Average experience",
        2 => "⭐⭐ Below expectations",
        1 => "⭐ Poor experience",
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
            var draftComment = (CustomerCommentDraft ?? string.Empty).Trim();
            var savedComment = (Order.CustomerFeedbackComment ?? string.Empty).Trim();
            var ratingChanged = CustomerRatingDraft != (Order.CustomerRating ?? 0);
            var commentChanged = draftComment != savedComment;
            return ratingChanged || commentChanged;
        }
    }

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
        OnPropertyChanged(nameof(IsCancelled));
        OnPropertyChanged(nameof(IsDelivered));
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
        OnPropertyChanged(nameof(StatusHeadline));
        OnPropertyChanged(nameof(StatusSubHeadline));
        OnPropertyChanged(nameof(StatusPillBackgroundColor));
        OnPropertyChanged(nameof(StatusPillTextColor));
        OnPropertyChanged(nameof(EstimatedDeliveryEta));
        OnPropertyChanged(nameof(RiderName));
        OnPropertyChanged(nameof(RiderPhoneText));
        OnPropertyChanged(nameof(HasRiderPhone));
        OnPropertyChanged(nameof(ShowRiderCard));
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

        Lines.Clear();
        if (value?.Lines != null)
        {
            foreach (var line in value.Lines)
            {
                Lines.Add(line);
            }
        }
        OnPropertyChanged(nameof(ItemsCountBadge));

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
        }
        else
        {
            CustomerRatingDraft = 5;
            CustomerCommentDraft = string.Empty;
        }
        NotifyRatingProperties();
        UpdateElapsedTime();
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
        catch { }
    }

    private async Task RunLiveTrackingLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, token);
                if (token.IsCancellationRequested) break;

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

    private void UpdateElapsedTime()
    {
        if (Order == null || Order.OrderDate == default)
        {
            ElapsedTimeText = "Just now";
            return;
        }

        var orderDate = Order.OrderDate.Kind == DateTimeKind.Utc
            ? Order.OrderDate.ToLocalTime()
            : Order.OrderDate;
        var diff = DateTime.Now - orderDate;
        if (diff < TimeSpan.Zero) diff = TimeSpan.Zero;

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
                        Shell.Current?.DisplayAlert("Delivered! 🎉", "Your order has been delivered! Please rate your experience below.", "OK");
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
        OnPropertyChanged(nameof(ItemsAccordionToggleGlyph));
    }

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
            await Shell.Current.DisplayAlert("Call", $"Could not start the call. {ex.Message}", "OK");
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
            await Shell.Current.DisplayAlert("WhatsApp", $"Could not open WhatsApp. {ex.Message}", "OK");
        }
    }

    private static string NormalizeDialNumber(string phone) =>
        new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());

    /// <summary>WhatsApp expects digits with country code (no + / spaces).</summary>
    private static string ToWhatsAppNumber(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        // Local PK mobiles: 03xxxxxxxxx → 923xxxxxxxxx
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

    partial void OnCustomerCommentDraftChanged(string value)
    {
        OnPropertyChanged(nameof(CanSubmitRating));
    }

    [RelayCommand]
    private void SelectStar(string starParam)
    {
        if (int.TryParse(starParam, out var star) && star >= 1 && star <= 5)
        {
            CustomerRatingDraft = star;
            NotifyRatingVisuals();
        }
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
        NotifyRatingVisuals();
    }

    [RelayCommand]
    private async Task SubmitRatingAsync()
    {
        if (Order == null || string.IsNullOrWhiteSpace(Order.OrderNo) || IsSubmittingRating || !CanSubmitRating)
        {
            return;
        }

        var isUpdate = HasExistingRating;
        IsSubmittingRating = true;
        OnPropertyChanged(nameof(SubmitButtonLabel));
        OnPropertyChanged(nameof(CanSubmitRating));

        try
        {
            var updated = await _orders.RateOrderAsync(
                Order.OrderNo,
                CustomerRatingDraft,
                CustomerCommentDraft?.Trim());

            if (updated != null)
            {
                Order = updated;
                CustomerRatingDraft = updated.CustomerRating ?? CustomerRatingDraft;
                CustomerCommentDraft = updated.CustomerFeedbackComment ?? string.Empty;
                NotifyRatingProperties();

                await Shell.Current.DisplayAlert(
                    "Thanks!",
                    isUpdate ? "Your feedback was updated." : "Your review was saved. Thank you!",
                    "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert(
                    "Rating",
                    "Could not save your review. Please check your connection and try again.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Rating", $"Error: {ex.Message}", "OK");
        }
        finally
        {
            IsSubmittingRating = false;
            OnPropertyChanged(nameof(SubmitButtonLabel));
            OnPropertyChanged(nameof(CanSubmitRating));
        }
    }

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
}
