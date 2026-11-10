using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;

namespace SmartPos.Mobile.ViewModels;

public partial class CustomerProfileViewModel : BaseViewModel
{
    private readonly OnlineOrderService _orders;
    private readonly SessionService _session;
    private readonly FavoritesService _favorites;
    private readonly CartService _cart;

    [ObservableProperty]
    private string fullName = string.Empty;

    [ObservableProperty]
    private string phoneNumber = string.Empty;

    [ObservableProperty]
    private string emailAddress = string.Empty;

    [ObservableProperty]
    private string profilePictureUrl = string.Empty;

    [ObservableProperty]
    private ImageSource? avatarImageSource;

    [ObservableProperty]
    private bool hasProfilePicture;

    [ObservableProperty]
    private string activeBranchName = "Main Branch";

    [ObservableProperty]
    private int orderCount;

    [ObservableProperty]
    private int favoriteCount;

    [ObservableProperty]
    private bool isSavingProfile;

    [ObservableProperty]
    private bool isLoadingAddresses;

    [ObservableProperty]
    private string appVersionText = "Version 1.0.0";

    public ObservableCollection<CustomerSavedAddressDto> SavedAddresses { get; } = new();

    public bool HasSavedAddresses => SavedAddresses.Count > 0;
    public bool HasPhoneNumber => !string.IsNullOrWhiteSpace(PhoneNumber);
    public string DisplayPhone => string.IsNullOrWhiteSpace(PhoneNumber) ? "No mobile number linked" : PhoneNumber;
    public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? "Valued Guest" : FullName;

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                return "👤";
            }

            var parts = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
            }

            return char.ToUpperInvariant(FullName.Trim()[0]).ToString();
        }
    }

    public CustomerProfileViewModel(
        OnlineOrderService orders,
        SessionService session,
        FavoritesService favorites,
        CartService cart)
    {
        _orders = orders;
        _session = session;
        _favorites = favorites;
        _cart = cart;
        Title = "My Profile";

        _favorites.FavoritesChanged += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                FavoriteCount = _favorites.Count;
            });
        };
    }

    partial void OnFullNameChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Initials));
    }

    partial void OnPhoneNumberChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayPhone));
        OnPropertyChanged(nameof(HasPhoneNumber));
    }

    [RelayCommand]
    public async Task AppearingAsync()
    {
        FullName = Preferences.Default.Get("sp.customerName", string.Empty);
        PhoneNumber = Preferences.Default.Get("sp.customerPhone", string.Empty);
        EmailAddress = Preferences.Default.Get("sp.customerEmail", string.Empty);
        ProfilePictureUrl = Preferences.Default.Get("sp.customerAvatar", string.Empty);

        ActiveBranchName = "Smart POS Storefront";
        if (_session.BranchId is int bid && bid > 0)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var branches = await _orders.GetBranchesAsync();
                    var branch = branches?.FirstOrDefault(b => b.Id == bid);
                    if (branch != null && !string.IsNullOrWhiteSpace(branch.Name))
                    {
                        MainThread.BeginInvokeOnMainThread(() => ActiveBranchName = branch.Name);
                    }
                }
                catch { }
            });
        }

        FavoriteCount = _favorites.Count;

        try
        {
            AppVersionText = $"Version {AppInfo.Current.VersionString} (Build {AppInfo.Current.BuildString})";
        }
        catch
        {
            AppVersionText = "Version 1.0.0";
        }

        LoadAvatarImage();

        if (!string.IsNullOrWhiteSpace(PhoneNumber) && NormalizePhoneDigits(PhoneNumber).Length >= 7)
        {
            await LoadSavedAddressesAsync();
            await LoadCustomerStatsAndProfileAsync();
        }
    }

    private void LoadAvatarImage()
    {
        try
        {
            var localAvatarFile = Path.Combine(FileSystem.AppDataDirectory, "customer_avatar.jpg");
            if (File.Exists(localAvatarFile))
            {
                AvatarImageSource = ImageSource.FromFile(localAvatarFile);
                HasProfilePicture = true;
                return;
            }

            if (!string.IsNullOrWhiteSpace(ProfilePictureUrl))
            {
                if (ProfilePictureUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    ProfilePictureUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    AvatarImageSource = ImageSource.FromUri(new Uri(ProfilePictureUrl));
                    HasProfilePicture = true;
                    return;
                }
            }

            AvatarImageSource = null;
            HasProfilePicture = false;
        }
        catch
        {
            AvatarImageSource = null;
            HasProfilePicture = false;
        }
    }

    private async Task LoadCustomerStatsAndProfileAsync()
    {
        var digits = NormalizePhoneDigits(PhoneNumber);
        if (digits.Length < 7) return;

        try
        {
            var branchId = _session.BranchId;
            var orders = await _orders.GetCustomerOrdersAsync(PhoneNumber.Trim(), branchId, maxResultCount: 50);
            OrderCount = orders?.Count ?? 0;
        }
        catch
        {
            // best-effort
        }

        try
        {
            if (_session.BranchId is int bid && bid > 0)
            {
                var profile = await _orders.GetOnlineCustomerProfileAsync(PhoneNumber.Trim(), bid);
                if (profile != null)
                {
                    if (string.IsNullOrWhiteSpace(FullName) && !string.IsNullOrWhiteSpace(profile.Name))
                    {
                        FullName = profile.Name;
                        Preferences.Default.Set("sp.customerName", profile.Name);
                    }
                    if (string.IsNullOrWhiteSpace(EmailAddress) && !string.IsNullOrWhiteSpace(profile.Email))
                    {
                        EmailAddress = profile.Email;
                        Preferences.Default.Set("sp.customerEmail", profile.Email);
                    }
                    if (!string.IsNullOrWhiteSpace(profile.ImagePath) && string.IsNullOrWhiteSpace(ProfilePictureUrl) && !HasProfilePicture)
                    {
                        ProfilePictureUrl = _orders.ResolveImageUrl(profile.ImagePath);
                        Preferences.Default.Set("sp.customerAvatar", ProfilePictureUrl);
                        LoadAvatarImage();
                    }
                }
            }
        }
        catch
        {
            // best-effort
        }
    }

    public async Task LoadSavedAddressesAsync()
    {
        IsLoadingAddresses = true;
        try
        {
            var list = await _orders.GetSavedAddressesAsync(PhoneNumber, _session.BranchId);
            SavedAddresses.Clear();
            foreach (var addr in list)
            {
                SavedAddresses.Add(addr);
            }
            OnPropertyChanged(nameof(HasSavedAddresses));
        }
        catch
        {
            // best-effort
        }
        finally
        {
            IsLoadingAddresses = false;
        }
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName))
        {
            await Shell.Current.DisplayAlert("Name Required", "Please enter your name.", "OK");
            return;
        }

        var digits = NormalizePhoneDigits(PhoneNumber);
        if (string.IsNullOrWhiteSpace(PhoneNumber) || digits.Length < 7)
        {
            await Shell.Current.DisplayAlert("Mobile Number Required", "Please enter a valid mobile number (at least 7 digits).", "OK");
            return;
        }

        await RunBusyAsync(async () =>
        {
            IsSavingProfile = true;
            try
            {
                var name = FullName.Trim();
                var phone = PhoneNumber.Trim();
                var email = EmailAddress?.Trim() ?? string.Empty;

                Preferences.Default.Set("sp.customerName", name);
                Preferences.Default.Set("sp.customerPhone", phone);
                Preferences.Default.Set("sp.customerEmail", email);

                _favorites.UpdateCustomerPhone(phone);

                var defaultAddr = SavedAddresses.FirstOrDefault(a => a.IsDefault)?.Address
                    ?? SavedAddresses.FirstOrDefault()?.Address;

                string? imageBase64 = null;
                var localAvatarFile = Path.Combine(FileSystem.AppDataDirectory, "customer_avatar.jpg");
                if (File.Exists(localAvatarFile))
                {
                    try
                    {
                        var bytes = await File.ReadAllBytesAsync(localAvatarFile);
                        imageBase64 = $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
                    }
                    catch { }
                }

                var input = new UpsertOnlineCustomerProfileInput
                {
                    BranchId = _session.BranchId > 0 ? _session.BranchId : null,
                    Name = name,
                    Phone = phone,
                    Email = string.IsNullOrWhiteSpace(email) ? null : email,
                    Address = defaultAddr,
                    ImageBase64 = imageBase64
                };

                var result = await _orders.UpsertOnlineCustomerProfileAsync(input);
                if (result != null && !string.IsNullOrWhiteSpace(result.ImagePath) && !HasProfilePicture)
                {
                    ProfilePictureUrl = _orders.ResolveImageUrl(result.ImagePath);
                    Preferences.Default.Set("sp.customerAvatar", ProfilePictureUrl);
                }

                await Shell.Current.DisplayAlert("Profile Saved", "Your profile details have been saved successfully.", "OK");
                await LoadCustomerStatsAndProfileAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Saved Locally", $"Saved on device. Cloud sync note: {ex.Message}", "OK");
            }
            finally
            {
                IsSavingProfile = false;
            }
        });
    }

    [RelayCommand]
    private async Task PickProfileImageAsync()
    {
        try
        {
            var action = await Shell.Current.DisplayActionSheet(
                "Profile Picture",
                "Cancel",
                HasProfilePicture ? "Remove Photo" : null,
                "📷 Take Photo",
                "🖼️ Choose from Gallery");

            if (string.IsNullOrWhiteSpace(action) || action == "Cancel")
            {
                return;
            }

            var localAvatarFile = Path.Combine(FileSystem.AppDataDirectory, "customer_avatar.jpg");

            if (action == "Remove Photo")
            {
                if (File.Exists(localAvatarFile))
                {
                    try { File.Delete(localAvatarFile); } catch { }
                }
                ProfilePictureUrl = string.Empty;
                Preferences.Default.Remove("sp.customerAvatar");
                AvatarImageSource = null;
                HasProfilePicture = false;

                if (!string.IsNullOrWhiteSpace(PhoneNumber))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _orders.UpsertOnlineCustomerProfileAsync(new UpsertOnlineCustomerProfileInput
                            {
                                BranchId = _session.BranchId > 0 ? _session.BranchId : null,
                                Name = string.IsNullOrWhiteSpace(FullName) ? "Customer" : FullName.Trim(),
                                Phone = PhoneNumber.Trim(),
                                ImageBase64 = string.Empty
                            });
                        }
                        catch { }
                    });
                }
                return;
            }

            FileResult? photo = null;
            if (action == "📷 Take Photo")
            {
                if (MediaPicker.Default.IsCaptureSupported)
                {
                    photo = await MediaPicker.Default.CapturePhotoAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Not Supported", "Camera capture is not supported on this device.", "OK");
                    return;
                }
            }
            else
            {
                photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Select Profile Photo"
                });
            }

            if (photo != null)
            {
                using (var stream = await photo.OpenReadAsync())
                using (var fileStream = File.Create(localAvatarFile))
                {
                    await stream.CopyToAsync(fileStream);
                }

                AvatarImageSource = ImageSource.FromFile(localAvatarFile);
                HasProfilePicture = true;

                var bytes = await File.ReadAllBytesAsync(localAvatarFile);
                var base64 = Convert.ToBase64String(bytes);
                var dataUri = $"data:image/jpeg;base64,{base64}";

                ProfilePictureUrl = dataUri;
                Preferences.Default.Set("sp.customerAvatar", dataUri);

                if (!string.IsNullOrWhiteSpace(PhoneNumber))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _orders.UpsertOnlineCustomerProfileAsync(new UpsertOnlineCustomerProfileInput
                            {
                                BranchId = _session.BranchId > 0 ? _session.BranchId : null,
                                Name = string.IsNullOrWhiteSpace(FullName) ? "Customer" : FullName.Trim(),
                                Phone = PhoneNumber.Trim(),
                                Email = EmailAddress,
                                ImageBase64 = dataUri
                            });
                        }
                        catch { }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Photo Error", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task AddNewAddressAsync()
    {
        var action = await Shell.Current.DisplayActionSheet("Select Address Type", "Cancel", null, "🏠 Home", "🏢 Office", "📍 Other");
        if (string.IsNullOrWhiteSpace(action) || action == "Cancel")
        {
            return;
        }

        var label = action.Replace("🏠", "").Replace("🏢", "").Replace("📍", "").Trim();

        var addressText = await Shell.Current.DisplayPromptAsync(
            "New Address",
            $"Enter your delivery address for {label}:",
            "Next",
            "Cancel",
            "e.g. House 14, Street 5, G-11/3",
            maxLength: 250);

        if (string.IsNullOrWhiteSpace(addressText))
        {
            return;
        }

        var landmarkText = await Shell.Current.DisplayPromptAsync(
            "Landmark (Optional)",
            "Nearby popular spot or delivery instruction:",
            "Save",
            "Skip",
            "e.g. Near Shell pump / Green gate",
            maxLength: 100);

        var isDefault = SavedAddresses.Count == 0 ||
            await Shell.Current.DisplayAlert("Default Address", $"Set this '{label}' address as your default delivery location?", "Yes", "No");

        try
        {
            var input = new SaveCustomerAddressInput
            {
                Phone = PhoneNumber,
                BranchId = _session.BranchId > 0 ? _session.BranchId : null,
                Label = label,
                Address = addressText.Trim(),
                Landmark = string.IsNullOrWhiteSpace(landmarkText) ? null : landmarkText.Trim(),
                IsDefault = isDefault
            };

            await _orders.SaveSavedAddressAsync(input);
            await LoadSavedAddressesAsync();
            await Shell.Current.DisplayAlert("Address Saved", $"'{label}' address saved successfully.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not save address: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteAddressAsync(CustomerSavedAddressDto? address)
    {
        if (address == null) return;

        var confirm = await Shell.Current.DisplayAlert(
            "Delete Address",
            $"Are you sure you want to delete your '{address.Label}' address?\n\n{address.Address}",
            "Delete",
            "Cancel");

        if (!confirm) return;

        try
        {
            await _orders.DeleteSavedAddressAsync(address.Id, PhoneNumber, _session.BranchId);
            SavedAddresses.Remove(address);
            OnPropertyChanged(nameof(HasSavedAddresses));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not delete address: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task SetDefaultAddressAsync(CustomerSavedAddressDto? address)
    {
        if (address == null || address.IsDefault) return;

        try
        {
            var input = new SaveCustomerAddressInput
            {
                Id = address.Id,
                Label = address.Label,
                Address = address.Address,
                Landmark = address.Landmark,
                DeliveryAreaId = address.DeliveryAreaId,
                DeliveryAreaName = address.DeliveryAreaName,
                Phone = PhoneNumber,
                BranchId = _session.BranchId,
                IsDefault = true
            };

            await _orders.SaveSavedAddressAsync(input);
            await LoadSavedAddressesAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not set default address: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task ViewOrderHistoryAsync()
    {
        await Shell.Current.GoToAsync("history");
    }

    [RelayCommand]
    private async Task ViewFavoritesAsync()
    {
        await Shell.Current.GoToAsync("//customer/favorites");
    }

    [RelayCommand]
    private async Task SwitchBranchAsync()
    {
        await Shell.Current.GoToAsync("//customer/home");
    }

    [RelayCommand]
    private async Task OpenStaffLoginAsync()
    {
        await Shell.Current.GoToAsync("//login");
    }

    [RelayCommand]
    private async Task LogoutOrResetAsync()
    {
        var confirm = await Shell.Current.DisplayAlert(
            "Reset Customer Details",
            "Are you sure you want to reset your saved customer details on this device? This will clear your name, mobile number, avatar, and active cart.",
            "Reset",
            "Cancel");

        if (!confirm) return;

        try
        {
            var localAvatarFile = Path.Combine(FileSystem.AppDataDirectory, "customer_avatar.jpg");
            if (File.Exists(localAvatarFile))
            {
                try { File.Delete(localAvatarFile); } catch { }
            }
        }
        catch { }

        Preferences.Default.Remove("sp.customerName");
        Preferences.Default.Remove("sp.customerPhone");
        Preferences.Default.Remove("sp.customerEmail");
        Preferences.Default.Remove("sp.customerAvatar");

        _cart.Clear();

        FullName = string.Empty;
        PhoneNumber = string.Empty;
        EmailAddress = string.Empty;
        ProfilePictureUrl = string.Empty;
        AvatarImageSource = null;
        HasProfilePicture = false;
        SavedAddresses.Clear();
        OrderCount = 0;
        OnPropertyChanged(nameof(HasSavedAddresses));

        await Shell.Current.DisplayAlert("Reset Complete", "Customer session details have been reset.", "OK");
    }

    private static string NormalizePhoneDigits(string? phone) =>
        new(phone?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());
}

