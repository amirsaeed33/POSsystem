using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;
using SmartPos.Mobile.Services.Home;

namespace SmartPos.Mobile.ViewModels;

public partial class CustomerHomeViewModel
{
    private async Task LoadHeroSlidesAsync(int branchId, bool forceRefresh = false)
    {
        ClearHeroSlides();

        var slides = await _orders.GetHeroSlidesAsync(branchId, forceRefresh);
        if (slides.Count == 0)
        {
            slides = HomeCatalogDefaults.CreateDefaultHeroSlides();
        }

        foreach (var slide in slides.OrderBy(s => s.SortOrder).ThenBy(s => s.Id))
        {
            if (string.IsNullOrWhiteSpace(slide.ImageUrl))
            {
                slide.ImageUrl = _api.ResolveImageUrl(slide.ImagePath);
            }

            if (string.IsNullOrWhiteSpace(slide.BgImageUrl))
            {
                var bgPath = !string.IsNullOrWhiteSpace(slide.BgImagePath)
                    ? slide.BgImagePath
                    : slide.ImagePath;
                slide.BgImageUrl = _api.ResolveImageUrl(bgPath);
            }

            if (string.IsNullOrWhiteSpace(slide.BgImageUrl) && !string.IsNullOrWhiteSpace(slide.ImageUrl))
            {
                slide.BgImageUrl = slide.ImageUrl;
            }

            HeroSlides.Add(slide);
        }

        HeroPosition = 0;
        OnPropertyChanged(nameof(HasHeroSlides));
        StartHeroAutoAdvance();
    }

    private void ClearHeroSlides()
    {
        StopHeroAutoAdvance();
        HeroSlides.Clear();
        HeroPosition = 0;
        OnPropertyChanged(nameof(HasHeroSlides));
    }

    private void StartHeroAutoAdvance()
    {
        StopHeroAutoAdvance();
        if (HeroSlides.Count <= 1 || Application.Current?.Dispatcher is null)
        {
            return;
        }

        _heroTimer = Application.Current.Dispatcher.CreateTimer();
        _heroTimer.Interval = TimeSpan.FromMilliseconds(4500);
        _heroTimer.Tick += OnHeroTimerTick;
        _heroTimer.Start();
    }

    private void OnHeroTimerTick(object? sender, EventArgs e)
    {
        if (HeroSlides.Count == 0)
        {
            return;
        }

        HeroPosition = (HeroPosition + 1) % HeroSlides.Count;
    }

    private void StopHeroAutoAdvance()
    {
        if (_heroTimer is null)
        {
            return;
        }

        _heroTimer.Tick -= OnHeroTimerTick;
        _heroTimer.Stop();
        _heroTimer = null;
    }

    private IDispatcherTimer? _feedbackTimer;
    private bool _isFeedbackPaused;

    private void StartFeedbackAutoAdvance()
    {
        StopFeedbackAutoAdvance();
        if (Feedbacks.Count <= 1 || Application.Current?.Dispatcher is null)
        {
            return;
        }

        _feedbackTimer = Application.Current.Dispatcher.CreateTimer();
        _feedbackTimer.Interval = TimeSpan.FromMilliseconds(3800);
        _feedbackTimer.Tick += OnFeedbackTimerTick;
        _feedbackTimer.Start();
    }

    private void OnFeedbackTimerTick(object? sender, EventArgs e)
    {
        if (_isFeedbackPaused || Feedbacks.Count == 0)
        {
            return;
        }

        FeedbackPosition = (FeedbackPosition + 1) % Feedbacks.Count;
    }

    private void StopFeedbackAutoAdvance()
    {
        if (_feedbackTimer is null)
        {
            return;
        }

        _feedbackTimer.Tick -= OnFeedbackTimerTick;
        _feedbackTimer.Stop();
        _feedbackTimer = null;
    }

    [RelayCommand]
    public void PauseFeedbackAutoAdvance() => _isFeedbackPaused = true;

    [RelayCommand]
    public void ResumeFeedbackAutoAdvance() => _isFeedbackPaused = false;

    public void OnPageAppearing()
    {
        if (HeroSlides.Count > 1)
        {
            StartHeroAutoAdvance();
        }

        if (Feedbacks.Count > 1)
        {
            StartFeedbackAutoAdvance();
        }
    }

    public void OnPageDisappearing()
    {
        StopHeroAutoAdvance();
        StopFeedbackAutoAdvance();
    }

    private async Task LoadFeedbacksAsync(int branchId)
    {
        try
        {
            IsLoadingFeedbacks = true;
            Feedbacks.Clear();
            var list = await _orders.GetBranchFeedbacksAsync(branchId);
            if (list == null || list.Count == 0)
            {
                list = HomeCatalogDefaults.CreateDefaultFeedbacks(SelectedBranch?.Name ?? "Our Kitchen");
            }

            var localAvatarFile = Path.Combine(FileSystem.AppDataDirectory, "customer_avatar.jpg");
            var localAvatarExists = File.Exists(localAvatarFile);
            var currentCustomerName = Preferences.Default.Get("sp.customerName", string.Empty).Trim();

            foreach (var item in list)
            {
                if (localAvatarExists && !string.IsNullOrWhiteSpace(currentCustomerName)
                    && (string.Equals(item.CustomerName?.Trim(), currentCustomerName, StringComparison.OrdinalIgnoreCase)
                        || (currentCustomerName.Length >= 4 && item.CustomerName != null && item.CustomerName.Contains(currentCustomerName, StringComparison.OrdinalIgnoreCase))))
                {
                    item.CustomerPhotoUrl = localAvatarFile;
                }

                Feedbacks.Add(item);
            }

            if (Feedbacks.Count > 0)
            {
                var avg = Feedbacks.Average(f => f.Rating);
                AverageRating = Math.Round(avg, 1);
                RatingScoreDisplay = AverageRating.ToString("0.0");
            }

            FeedbackPosition = 0;
            OnPropertyChanged(nameof(BranchFeedbacks));
            OnPropertyChanged(nameof(HasFeedbacks));
            OnPropertyChanged(nameof(TotalReviewsLabel));
            OnPropertyChanged(nameof(TotalReviewsCount));
            OnPropertyChanged(nameof(TotalReviewsSubtitle));
            OnPropertyChanged(nameof(AverageRatingScore));
            StartFeedbackAutoAdvance();
        }
        catch
        {
            var defaults = HomeCatalogDefaults.CreateDefaultFeedbacks(SelectedBranch?.Name ?? "Our Kitchen");
            foreach (var item in defaults)
            {
                Feedbacks.Add(item);
            }
            FeedbackPosition = 0;
            OnPropertyChanged(nameof(BranchFeedbacks));
            OnPropertyChanged(nameof(HasFeedbacks));
            OnPropertyChanged(nameof(TotalReviewsLabel));
            OnPropertyChanged(nameof(TotalReviewsCount));
            OnPropertyChanged(nameof(TotalReviewsSubtitle));
            OnPropertyChanged(nameof(AverageRatingScore));
            StartFeedbackAutoAdvance();
        }
        finally
        {
            IsLoadingFeedbacks = false;
        }
    }
}
