using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.Services;
using SmartPos.Mobile.ViewModels;

namespace SmartPos.Mobile.Views.Admin;

public partial class AdminDashboardPage : ContentPage
{
    private readonly AdminDashboardViewModel _vm;

    public AdminDashboardPage() : this(ServiceHelper.Get<AdminDashboardViewModel>())
    {
    }

    public AdminDashboardPage(AdminDashboardViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Shell.SetTabBarIsVisible(this, false);
        try
        {
            await _vm.LoadAsync();
        }
        finally
        {
            LoadingService.Current.HideTabLoader();
        }
    }
}
