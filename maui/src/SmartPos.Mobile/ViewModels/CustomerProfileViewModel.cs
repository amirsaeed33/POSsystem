using System.Collections.ObjectModel;
using System.Diagnostics;
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
    private DateTimeOffset _lastProfileNetworkLoad = DateTimeOffset.MinValue;
    private static readonly TimeSpan ProfileNetworkTtl = TimeSpan.FromSeconds(60);

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
    private string appVersionText = "Aljannat Hospitality App  •  v1.0.0";

    [ObservableProperty]
    private bool isEditMode;

    [ObservableProperty]
    private string memberTier = "Dastarkhwan Gold";

    [ObservableProperty]
    private string loyaltyPointsDisplay = "1,450 Pts";

    [ObservableProperty]
    private bool isLogoutSheetVisible;

    [ObservableProperty]
    private bool isBranchSheetVisible;

    [ObservableProperty]
    private bool isLoadingBranches;

    public ObservableCollection<CustomerSavedAddressDto> SavedAddresses { get; } = new();
    public ObservableCollection<BranchSheetItem> BranchSheetItems { get; } = new();

    public bool HasSavedAddresses => SavedAddresses.Count > 0;
    public bool HasPhoneNumber => !string.IsNullOrWhiteSpace(PhoneNumber);
    /// <summary>True when local customer identity (phone) is present — drives Log Out visibility.</summary>
    public bool IsCustomerSignedIn => NormalizePhoneDigits(PhoneNumber).Length >= 7;
    public bool IsViewMode => !IsEditMode;
    public string EditModeButtonText => IsEditMode ? "Save" : "Edit Details";
    public string PersonalEditPillText => IsEditMode ? "Save" : "Edit";
    public string DisplayPhone => string.IsNullOrWhiteSpace(PhoneNumber) ? "No mobile number linked" : PhoneNumber;
    public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? "Valued Guest" : FullName;
    public string DisplayEmail => string.IsNullOrWhiteSpace(EmailAddress) ? "Not provided" : EmailAddress;
    public string VerifiedPhoneBadgeText => $"Verified  •  {DisplayPhone}";
    public string MemberStatusSubtitle => $"{MemberTier}  •  {LoyaltyPointsDisplay}";
    public string OrderCountDisplay => OrderCount.ToString("N0");
    public string FavoriteCountDisplay => FavoriteCount.ToString("N0");
    public string FavoriteCountSubtitle => FavoriteCount == 1 ? "1 saved dish" : $"{FavoriteCount} saved dishes";
    public string OrderCountSubtitle => OrderCount == 1 ? "1 past order" : $"{OrderCount} past orders";

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                return "VG";
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
        OnPropertyChanged(nameof(VerifiedPhoneBadgeText));
        OnPropertyChanged(nameof(IsCustomerSignedIn));
    }

    partial void OnEmailAddressChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayEmail));
    }

    partial void OnIsEditModeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsViewMode));
        OnPropertyChanged(nameof(EditModeButtonText));
        OnPropertyChanged(nameof(PersonalEditPillText));
    }

    partial void OnOrderCountChanged(int value)
    {
        OnPropertyChanged(nameof(OrderCountDisplay));
        OnPropertyChanged(nameof(OrderCountSubtitle));
    }

    partial void OnFavoriteCountChanged(int value)
    {
        OnPropertyChanged(nameof(FavoriteCountDisplay));
        OnPropertyChanged(nameof(FavoriteCountSubtitle));
    }

    partial void OnMemberTierChanged(string value)
    {
        OnPropertyChanged(nameof(MemberStatusSubtitle));
    }

    partial void OnLoyaltyPointsDisplayChanged(string value)
    {
        OnPropertyChanged(nameof(MemberStatusSubtitle));
    }

    [RelayCommand]
    public async Task AppearingAsync()
    {
        try
        {
            FullName = Preferences.Default.Get("sp.customerName", string.Empty);
            PhoneNumber = Preferences.Default.Get("sp.customerPhone", string.Empty);
            EmailAddress = Preferences.Default.Get("sp.customerEmail", string.Empty);
            ProfilePictureUrl = Preferences.Default.Get("sp.customerAvatar", string.Empty);
            MemberTier = Preferences.Default.Get("sp.memberTier", "Dastarkhwan Gold");
            LoyaltyPointsDisplay = Preferences.Default.Get("sp.loyaltyPoints", "1,450 Pts");
            IsEditMode = false;
            IsLogoutSheetVisible = false;
            IsBranchSheetVisible = false;

            ActiveBranchName = "Gulberg III Flagship, Lahore";
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
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[CustomerProfileViewModel] Load branch name failed: {ex.Message}");
                    }
                });
            }

            FavoriteCount = _favorites.Count;

            try
            {
                AppVersionText = $"Aljannat Hospitality App  •  v{AppInfo.Current.VersionString} (Build {AppInfo.Current.BuildString})";
            }
            catch
            {
                AppVersionText = "Aljannat Hospitality App  •  v2.4.0 (Build 108)";
            }

            LoadAvatarImage();

            if (!string.IsNullOrWhiteSpace(PhoneNumber) && NormalizePhoneDigits(PhoneNumber).Length >= 7)
            {
                // Skip network reload on rapid tab switches — was a major Profile hang source.
                if (DateTimeOffset.UtcNow - _lastProfileNetworkLoad >= ProfileNetworkTtl)
                {
                    await LoadSavedAddressesAsync();
                    await LoadCustomerStatsAndProfileAsync();
                    _lastProfileNetworkLoad = DateTimeOffset.UtcNow;
                }
            }
        }
        finally
        {
            LoadingService.Current.HideTabLoader();
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
    private async Task ToggleEditModeAsync()
    {
        if (!IsEditMode)
        {
            IsEditMode = true;
            return;
        }

        var saved = await TrySaveProfileAsync();
        if (saved)
        {
            IsEditMode = false;
        }
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        var saved = await TrySaveProfileAsync();
        if (saved)
        {
            IsEditMode = false;
        }
    }

    private async Task<bool> TrySaveProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName))
        {
            await AljannatDialogService.Current.ShowAlertAsync("Name Required", "Please enter your name.", "OK");
            return false;
        }

        var digits = NormalizePhoneDigits(PhoneNumber);
        if (string.IsNullOrWhiteSpace(PhoneNumber) || digits.Length < 7)
        {
            await AljannatDialogService.Current.ShowAlertAsync("Mobile Number Required", "Please enter a valid mobile number (at least 7 digits).", "OK");
            return false;
        }

        var success = false;
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
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[CustomerProfileViewModel] Read avatar for upsert failed: {ex.Message}");
                    }
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

                await AljannatDialogService.Current.ShowAlertAsync("Profile Saved", "Your profile details have been saved successfully.", "OK");
                await LoadCustomerStatsAndProfileAsync();
                success = true;
            }
            catch (Exception ex)
            {
                await AljannatDialogService.Current.ShowAlertAsync("Saved Locally", $"Saved on device. Cloud sync note: {ex.Message}", "OK");
                success = true;
            }
            finally
            {
                IsSavingProfile = false;
            }
        });

        return success;
    }

    [RelayCommand]
    private async Task PickProfileImageAsync()
    {
        try
        {
            var action = await AljannatDialogService.Current.ShowActionSheetAsync(
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
                    try
                    {
                        File.Delete(localAvatarFile);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[CustomerProfileViewModel] Delete avatar file failed: {ex.Message}");
                    }
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
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[CustomerProfileViewModel] Sync remove-photo profile failed: {ex.Message}");
                        }
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
                    await AljannatDialogService.Current.ShowAlertAsync("Not Supported", "Camera capture is not supported on this device.", "OK");
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
                var dataUri = $"data:image/jpeg;base64, {base64}";

                ProfilePictureUrl = dataUri;
                Preferences.Default.Set("sp.customerAvatar", dataUri);

                var phoneToSync = !string.IsNullOrWhiteSpace(PhoneNumber)
                    ? PhoneNumber.Trim()
                    : Preferences.Default.Get("sp.customerPhone", string.Empty).Trim();
                var nameToSync = !string.IsNullOrWhiteSpace(FullName)
                    ? FullName.Trim()
                    : Preferences.Default.Get("sp.customerName", "Customer").Trim();

                if (!string.IsNullOrWhiteSpace(phoneToSync))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _orders.UpsertOnlineCustomerProfileAsync(new UpsertOnlineCustomerProfileInput
                            {
                                BranchId = _session.BranchId > 0 ? _session.BranchId : null,
                                Name = nameToSync,
                                Phone = phoneToSync,
                                Email = EmailAddress,
                                ImageBase64 = dataUri
                            });
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[CustomerProfileViewModel] Sync avatar profile failed: {ex.Message}");
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await AljannatDialogService.Current.ShowAlertAsync("Photo Error", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task AddNewAddressAsync()
    {
        const string label = "Address";

        var addressText = await AljannatDialogService.Current.ShowPromptAsync(
            "New Address", "Enter your delivery address:",
            "Next",
            "Cancel",
            "e.g. House 14, Street 5, G-11/3", initialValue: "");

        if (string.IsNullOrWhiteSpace(addressText))
        {
            return;
        }

        var landmarkText = await AljannatDialogService.Current.ShowPromptAsync(
            "Landmark (Optional)",
            "Nearby popular spot or delivery instruction:",
            "Save",
            "Skip",
            "e.g. Near Shell pump / Green gate");

        var isDefault = SavedAddresses.Count == 0 ||
            await AljannatDialogService.Current.ShowConfirmAsync("Default Address", "Set this address as your default delivery location?", "Yes", "No");

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
            await AljannatDialogService.Current.ShowAlertAsync("Address Saved", "Address saved successfully.", "OK");
        }
        catch (Exception ex)
        {
            await AljannatDialogService.Current.ShowAlertAsync("Error", $"Could not save address: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteAddressAsync(CustomerSavedAddressDto? address)
    {
        if (address == null) return;

        var confirm = await AljannatDialogService.Current.ShowConfirmAsync("Delete Address", $"Are you sure you want to delete your '{address.Label}' address?\n\n{address.Address}", "Delete", "Cancel", isDestructive: true);

        if (!confirm) return;

        try
        {
            await _orders.DeleteSavedAddressAsync(address.Id, PhoneNumber, _session.BranchId);
            SavedAddresses.Remove(address);
            OnPropertyChanged(nameof(HasSavedAddresses));
        }
        catch (Exception ex)
        {
            await AljannatDialogService.Current.ShowAlertAsync("Error", $"Could not delete address: {ex.Message}", "OK");
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
                Id = address.Id, Label = address.Label,
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
            await AljannatDialogService.Current.ShowAlertAsync("Error", $"Could not set default address: {ex.Message}", "OK");
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
        IsLogoutSheetVisible = false;
        IsBranchSheetVisible = true;
        await LoadBranchSheetAsync();
    }

    [RelayCommand]
    private void CloseBranchSheet()
    {
        IsBranchSheetVisible = false;
    }

    [RelayCommand]
    private async Task SelectBranchFromSheetAsync(BranchSheetItem? item)
    {
        if (item?.Branch == null)
        {
            return;
        }

        var branch = item.Branch;
        var currentId = _session.BranchId ?? 0;
        if (branch.Id == currentId)
        {
            IsBranchSheetVisible = false;
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _session.SetBranchAsync(branch.Id);
            _cart.Clear();
            _favorites.Initialize(branch.Id);

            ActiveBranchName = string.IsNullOrWhiteSpace(branch.Name) ? "Selected Branch" : branch.Name!;
            FavoriteCount = _favorites.Count;

            foreach (var row in BranchSheetItems)
            {
                row.IsActive = row.Branch.Id == branch.Id;
            }

            IsBranchSheetVisible = false;

            if (!string.IsNullOrWhiteSpace(PhoneNumber) && NormalizePhoneDigits(PhoneNumber).Length >= 7)
            {
                await LoadSavedAddressesAsync();
                await LoadCustomerStatsAndProfileAsync();
            }
        });
    }

    private async Task LoadBranchSheetAsync()
    {
        IsLoadingBranches = true;
        try
        {
            var branches = await _orders.GetBranchesAsync();
            var activeId = _session.BranchId ?? 0;

            BranchSheetItems.Clear();
            foreach (var branch in (branches ?? []).OrderBy(b => b.Name))
            {
                BranchSheetItems.Add(new BranchSheetItem(branch, branch.Id == activeId));
            }

            if (BranchSheetItems.Count == 0)
            {
                // Soft fallback so the sheet never looks empty while offline/API fails.
                BranchSheetItems.Add(new BranchSheetItem(
                    new OnlineBranchDto { Id = activeId > 0 ? activeId : 1, Name = ActiveBranchName, IsRestaurant = true },
                    isActive: true));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CustomerProfileViewModel] LoadBranchSheetAsync failed: {ex.Message}");
            if (BranchSheetItems.Count == 0)
            {
                BranchSheetItems.Add(new BranchSheetItem(
                    new OnlineBranchDto { Id = _session.BranchId ?? 1, Name = ActiveBranchName, IsRestaurant = true },
                    isActive: true));
            }
        }
        finally
        {
            IsLoadingBranches = false;
        }
    }

    [RelayCommand]
    private async Task OpenStaffLoginAsync()
    {
        await Shell.Current.GoToAsync("//login");
    }

    [RelayCommand]
    private void ShowLogoutSheet()
    {
        if (!IsCustomerSignedIn)
        {
            return;
        }

        IsLogoutSheetVisible = true;
    }

    [RelayCommand]
    private void CancelLogout()
    {
        IsLogoutSheetVisible = false;
    }

    [RelayCommand]
    private async Task ConfirmLogoutAsync()
    {
        IsLogoutSheetVisible = false;
        await PerformLogoutResetAsync();
    }

    [RelayCommand]
    private async Task LogoutOrResetAsync()
    {
        ShowLogoutSheet();
        await Task.CompletedTask;
    }

    private async Task PerformLogoutResetAsync()
    {
        try
        {
            var localAvatarFile = Path.Combine(FileSystem.AppDataDirectory, "customer_avatar.jpg");
            if (File.Exists(localAvatarFile))
            {
                try
                {
                    File.Delete(localAvatarFile);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CustomerProfileViewModel.PerformLogoutResetAsync] Delete avatar failed: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CustomerProfileViewModel.PerformLogoutResetAsync] Reset cleanup failed: {ex.Message}");
        }

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
        IsEditMode = false;
        SavedAddresses.Clear();
        OrderCount = 0;
        FavoriteCount = 0;
        OnPropertyChanged(nameof(HasSavedAddresses));
        OnPropertyChanged(nameof(IsCustomerSignedIn));

        await AljannatDialogService.Current.ShowAlertAsync("Logged Out", "You have been signed out of this device session.", "OK");
        await Shell.Current.GoToAsync("//customer/home");
    }

    private static string NormalizePhoneDigits(string? phone) =>
        new(phone?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());
}

public partial class BranchSheetItem : ObservableObject
{
    public BranchSheetItem(OnlineBranchDto branch, bool isActive)
    {
        Branch = branch;
        IsActive = isActive;
    }

    public OnlineBranchDto Branch { get; }

    public string Name => string.IsNullOrWhiteSpace(Branch.Name) ? "Branch" : Branch.Name!;

    public string StatusSubtitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Branch.Address))
            {
                var addr = Branch.Address.Trim();
                return addr.Length > 42 ? addr[..39] + "…" : addr;
            }

            return Branch.IsRestaurant
                ? "Open till 2:00 AM  •  25–35 min"
                : "Store open  •  Pickup available";
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotActive))]
    [NotifyPropertyChangedFor(nameof(ActionLabel))]
    private bool isActive;

    public bool IsNotActive => !IsActive;

    public string ActionLabel => IsActive ? "✓ Active" : "Select";
}

