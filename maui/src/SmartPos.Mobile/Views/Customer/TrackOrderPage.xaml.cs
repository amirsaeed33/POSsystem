using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Customer;

public partial class TrackOrderPage : ContentPage
{
    public TrackOrderPage() : this(ServiceHelper.Get<TrackOrderViewModel>())
    {
    }

    public TrackOrderPage(TrackOrderViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
