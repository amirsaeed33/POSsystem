using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Rider;

public partial class RiderHistoryPage : ContentPage
{
    private readonly RiderHistoryViewModel _vm;

    public RiderHistoryPage() : this(ServiceHelper.Get<RiderHistoryViewModel>())
    {
    }

    public RiderHistoryPage(RiderHistoryViewModel vm)
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
}
