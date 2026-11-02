using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Rider;

public partial class RiderDashboardPage : ContentPage
{
    private readonly RiderDashboardViewModel _vm;

    public RiderDashboardPage() : this(ServiceHelper.Get<RiderDashboardViewModel>())
    {
    }

    public RiderDashboardPage(RiderDashboardViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_vm.AppearingCommand.CanExecute(null))
        {
            _vm.AppearingCommand.Execute(null);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_vm.DisappearingCommand.CanExecute(null))
        {
            _vm.DisappearingCommand.Execute(null);
        }
    }
}
