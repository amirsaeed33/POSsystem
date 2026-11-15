using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly AuthService _auth;
    private readonly AppState _appState;
    private readonly NavigationService _navigation;

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private bool isPasswordHidden = true;

    [ObservableProperty]
    private bool isLoggedIn;

    [ObservableProperty]
    private string welcomeName = string.Empty;

    public string PasswordEyeIcon => IsPasswordHidden ? "icon_eye_off.png" : "icon_eye.png";
    public bool ShowLoginForm => !IsLoggedIn;

    public LoginViewModel(AuthService auth, AppState appState, NavigationService navigation, AppSettings settings)
    {
        _auth = auth;
        _appState = appState;
        _navigation = navigation;
        Title = "Staff Login";
        _ = settings;
        _appState.SessionChanged += (_, _) => MainThread.BeginInvokeOnMainThread(RefreshSessionUi);
        RefreshSessionUi();
    }

    partial void OnIsPasswordHiddenChanged(bool value) => OnPropertyChanged(nameof(PasswordEyeIcon));
    partial void OnIsLoggedInChanged(bool value) => OnPropertyChanged(nameof(ShowLoginForm));

    public void RefreshSessionUi()
    {
        IsLoggedIn = _appState.IsAuthenticated;
        WelcomeName = _appState.User?.Name
            ?? _appState.User?.UserName
            ?? UserName
            ?? "Staff";
        StatusText = IsLoggedIn ? $"Signed in as {WelcomeName}" : string.Empty;
    }

    [RelayCommand]
    private void TogglePasswordVisibility() => IsPasswordHidden = !IsPasswordHidden;

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
            var auth = await _auth.LoginAsync(null, UserName, Password, lat, lng);
            _appState.ClearStaffWorkingState();
            if (auth.AttendanceAutoCheckedIn)
            {
                StatusText = "Attendance auto check-in recorded.";
            }

            RefreshSessionUi();

            var mode = _appState.RoleMode;
            if (mode == AppRoleMode.UnsupportedStaff)
            {
                await Shell.Current.GoToAsync("//unsupported");
                return;
            }

            await _navigation.GoToRoleHomeAsync();
        });
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await RunBusyAsync(async () =>
        {
            await _auth.LogoutAsync();
            _appState.ClearStaffWorkingState();
            Password = string.Empty;
            RefreshSessionUi();
            StatusText = "Signed out.";
            await Shell.Current.GoToAsync("//customer/account");
        });
    }

    [RelayCommand]
    private async Task ContinueAsCustomerAsync()
    {
        await Shell.Current.GoToAsync("//customer/home");
    }

    [RelayCommand]
    private async Task ContinueToRoleHomeAsync()
    {
        await _navigation.GoToRoleHomeAsync();
    }
}
