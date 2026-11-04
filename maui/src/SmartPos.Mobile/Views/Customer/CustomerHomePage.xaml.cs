using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Customer;

public partial class CustomerHomePage : ContentPage
{
    private readonly CustomerHomeViewModel _vm;

    public CustomerHomePage() : this(ServiceHelper.Get<CustomerHomeViewModel>())
    {
    }

    public CustomerHomePage(CustomerHomeViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Shell.SetTabBarIsVisible(this, false);
        if (_vm.AppearingCommand.CanExecute(null))
        {
            _vm.AppearingCommand.Execute(null);
        }
    }

    protected override void OnDisappearing()
    {
        _vm.OnPageDisappearing();
        base.OnDisappearing();
    }

    private async void OnFavoriteHeartTapped(object sender, TappedEventArgs e)
    {
        if (sender is VisualElement view)
        {
            await view.ScaleTo(1.35, 120, Easing.CubicOut);
            await view.ScaleTo(0.9, 80, Easing.CubicIn);
            await view.ScaleTo(1.0, 100, Easing.SpringOut);
        }
    }

    private void OnFeedbackPointerPressed(object? sender, PointerEventArgs e)
    {
        _vm.PauseFeedbackAutoAdvanceCommand.Execute(null);
    }

    private void OnFeedbackPointerReleased(object? sender, PointerEventArgs e)
    {
        _vm.ResumeFeedbackAutoAdvanceCommand.Execute(null);
    }

    private void OnFeedbackPointerEntered(object? sender, PointerEventArgs e)
    {
        _vm.PauseFeedbackAutoAdvanceCommand.Execute(null);
    }

    private void OnFeedbackPointerExited(object? sender, PointerEventArgs e)
    {
        _vm.ResumeFeedbackAutoAdvanceCommand.Execute(null);
    }
}
