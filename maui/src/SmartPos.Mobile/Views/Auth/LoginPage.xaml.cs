using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Auth;

public partial class LoginPage : ContentPage
{
    public LoginPage() : this(ServiceHelper.Get<LoginViewModel>())
    {
    }

    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
