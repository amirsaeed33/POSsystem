using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

[QueryProperty(nameof(OrderNo), "orderNo")]
public partial class TrackOrderViewModel : BaseViewModel
{
    private readonly OnlineOrderService _orders;
    private string? _lastTrackedNo;
    private bool _trackQueued;

    [ObservableProperty]
    private string orderNo = string.Empty;

    [ObservableProperty]
    private CustomerOrderDto? order;

    [ObservableProperty]
    private string statusBanner = string.Empty;

    [ObservableProperty]
    private int customerRatingDraft = 5;

    [ObservableProperty]
    private string customerCommentDraft = string.Empty;

    [ObservableProperty]
    private bool isSubmittingRating;

    public ObservableCollection<CustomerOrderLineDto> Lines { get; } = new();

    public bool HasOrder => Order != null && !string.IsNullOrWhiteSpace(Order.OrderNo);
    public string OrderTitle => Order?.OrderNo ?? string.Empty;
    public string StatusText => string.IsNullOrWhiteSpace(Order?.StatusName)
        ? (Order == null ? string.Empty : $"Status {Order.Status}")
        : Order!.StatusName!;
    public string TotalText => Order == null ? string.Empty : $"Rs {Order.TotalAmount:0.##}";
    public string AreaText => Order?.DeliveryAreaName ?? string.Empty;
    public string RiderText => string.IsNullOrWhiteSpace(Order?.RiderName)
        ? "Rider not assigned yet"
        : $"Rider: {Order!.RiderName}";
    public string RiderPhoneText => Order?.RiderPhone?.Trim() ?? string.Empty;
    public bool HasRiderPhone => !string.IsNullOrWhiteSpace(RiderPhoneText);
    public string CustomerText => Order?.CustomerName ?? string.Empty;

    public bool IsDelivered => Order?.Status == 4;
    public bool HasExistingRating => Order?.CustomerRating.HasValue == true && Order.CustomerRating.Value > 0;
    public string RatingSectionTitle => HasExistingRating ? "Your feedback" : "Rate your delivery";
    public string RatingBadgeText => HasExistingRating ? $"{Order?.CustomerRating}/5" : string.Empty;
    public string RatingHintText => HasExistingRating
        ? "You can update your stars or comment anytime."
        : "How was your experience with this delivery?";
    public string SubmitButtonLabel => IsSubmittingRating
        ? "Saving…"
        : (HasExistingRating ? "Update feedback" : "Submit feedback");

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
        5 => "⭐⭐⭐⭐⭐ Excellent! Loved it",
        4 => "⭐⭐⭐⭐ Very Good",
        3 => "⭐⭐⭐ Average",
        2 => "⭐⭐ Below Expectations",
        1 => "⭐ Poor Experience",
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
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(TotalText));
        OnPropertyChanged(nameof(AreaText));
        OnPropertyChanged(nameof(RiderText));
        OnPropertyChanged(nameof(RiderPhoneText));
        OnPropertyChanged(nameof(HasRiderPhone));
        OnPropertyChanged(nameof(CustomerText));

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
                0 => "Waiting for the restaurant to accept your order.",
                1 or 3 => "Your order is being prepared.",
                2 => "This order was rejected.",
                4 => "Delivered — enjoy your meal!",
                _ => StatusText
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
                    isUpdate ? "Your feedback was updated." : "Your feedback was saved and verified.",
                    "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert(
                    "Rating",
                    "Could not save your rating. Please check your connection and try again.",
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
}
