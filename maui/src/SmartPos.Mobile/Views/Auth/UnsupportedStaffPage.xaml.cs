using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Auth;

public partial class UnsupportedStaffPage : ContentPage
{
    public UnsupportedStaffPage() : this(ServiceHelper.Get<UnsupportedStaffViewModel>())
    {
    }

    public UnsupportedStaffPage(UnsupportedStaffViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
