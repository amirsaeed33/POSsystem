using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public partial class UnsupportedStaffViewModel : BaseViewModel
{
    private readonly AuthService _auth;

    public UnsupportedStaffViewModel(AuthService auth)
    {
        _auth = auth;
        Title = "Not supported";
    }

    [RelayCommand]
    private async Task ContinueCustomerAsync()
    {
        await _auth.LogoutAsync();
        await Shell.Current.GoToAsync("//customer/home");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _auth.LogoutAsync();
        await Shell.Current.GoToAsync("//customer/account");
    }
}
