using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public partial class FavoritesViewModel : BaseViewModel
{
    private readonly FavoritesService _favorites;

    [ObservableProperty]
    private int favoriteCount;

    public FavoritesViewModel(FavoritesService favorites)
    {
        _favorites = favorites;
        Title = "Favorites";
        FavoriteCount = _favorites.Count;
        _favorites.FavoritesChanged += (_, _) =>
        {
            FavoriteCount = _favorites.Count;
        };
    }

    [RelayCommand]
    private async Task BrowseMenuAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//customer/home");
        }
    }
}

