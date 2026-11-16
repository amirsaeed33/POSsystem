using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.Controls;

public partial class TabLoadingOverlay : ContentView
{
    public TabLoadingOverlay()
    {
        InitializeComponent();
        BindingContext = LoadingService.Current;
    }
}

