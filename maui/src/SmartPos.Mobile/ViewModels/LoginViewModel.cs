using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly AuthService _auth;
    private readonly SessionService _session;
    private readonly NavigationService _navigation;
    private readonly CartService _cart;

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string statusText = string.Empty;

    public LoginViewModel(AuthService auth, SessionService session, NavigationService navigation, AppSettings settings, CartService cart)
    {
        _auth = auth;
        _session = session;
        _navigation = navigation;
        _cart = cart;
        Title = "Staff Login";
        _ = settings;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
        {
            await Shell.Current.DisplayAlert("Login", "Enter username and password.", "OK");
            return;
        }

        await RunBusyAsync(async () =>
        {
            var (lat, lng) = await LocationHelper.TryGetAsync();
            // Tenancy omitted — API resolves it from username (same as web).
            var auth = await _auth.LoginAsync(null, UserName, Password, lat, lng);
            _cart.Clear();
            WaiterOrderSession.Clear();
            if (auth.AttendanceAutoCheckedIn)
            {
                StatusText = "Attendance auto check-in recorded.";
            }

            var mode = _session.ResolveRoleMode();
            if (mode == AppRoleMode.UnsupportedStaff)
            {
                await Shell.Current.GoToAsync("//unsupported");
                return;
            }

            await _navigation.GoToRoleHomeAsync();
        });
    }

    [RelayCommand]
    private async Task ContinueAsCustomerAsync()
    {
        await Shell.Current.GoToAsync("//customer/home");
    }
}
