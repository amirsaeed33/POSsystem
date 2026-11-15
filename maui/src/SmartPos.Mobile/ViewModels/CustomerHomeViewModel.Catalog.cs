using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPos.Mobile.Helpers;
using SmartPos.Mobile.Models;
using SmartPos.Mobile.Services;
using SmartPos.Mobile.Services.Home;

namespace SmartPos.Mobile.ViewModels;

public partial class CustomerHomeViewModel
{
    private async Task LoadCatalogFirstPageAsync(
        int branchId,
        bool forceRefresh,
        string? keyword = null,
        bool dealsOnly = false,
        IReadOnlyCollection<string>? categoryNames = null)
    {
        var request = new PagedOnlineCatalogRequest
        {
            BranchId = branchId,
            SkipCount = 0,
            MaxResultCount = CatalogPageSize,
            Keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim(),
            DealsOnly = dealsOnly,
            CategoryNames = categoryNames is { Count: > 0 } ? categoryNames.ToList() : null,
            CategoryName = categoryNames is { Count: 1 } ? categoryNames.First() : null
        };

        var page = await _orders.GetCatalogPageAsync(request, forceRefresh);
        _catalogTotalCount = page.TotalCount;
        var items = (page.Items ?? new List<OnlineProductDto>()).ToList();
        var resolveUrl = _api.ResolveImageUrl;
        var fav = _favorites;
        await Task.Run(() =>
            HomeCatalogFilter.EnrichProducts(items, path => resolveUrl(path), id => fav.IsFavorite(id)));
        _allProducts = items;
    }

    private void CancelSearchDebounce()
    {
        try
        {
            _searchDebounceCts?.Cancel();
        }
        catch
        {
            // ignored
        }

        _searchDebounceCts?.Dispose();
        _searchDebounceCts = null;
    }

    private void ScheduleSearchFilterDebounce()
    {
        CancelSearchDebounce();
        var cts = new CancellationTokenSource();
        _searchDebounceCts = cts;
        var token = cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(SearchDebounceMs, token);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        await ReloadCatalogForCurrentFiltersAsync();
                    }
                });
            }
            catch (TaskCanceledException)
            {
                // ignored
            }
        }, token);
    }

    private async Task ReloadCatalogForCurrentFiltersAsync()
    {
        if (SelectedBranch == null)
        {
            return;
        }

        var dealsOnly = !string.IsNullOrWhiteSpace(SelectedPill) &&
                        SelectedPill.Contains("Top Deals", StringComparison.OrdinalIgnoreCase);
        var categoryNames = !IsFavoritesCategorySelected && !IsAllCategoriesSelected && SelectedCategories.Count > 0
            ? SelectedCategories.ToList()
            : null;

        try
        {
            await LoadCatalogFirstPageAsync(
                SelectedBranch.Id,
                forceRefresh: false,
                keyword: SearchText,
                dealsOnly: dealsOnly,
                categoryNames: categoryNames);
            await ApplyFiltersAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CustomerHomeViewModel] Catalog reload failed: {ex.Message}");
            ErrorMessage = ex.Message;
        }
    }

    private void RebuildCategories()
    {
        var names = new List<string>();
        foreach (var c in StoreHeader?.Categories ?? new List<string>())
        {
            if (!string.IsNullOrWhiteSpace(c) &&
                !names.Contains(c, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(c!);
            }
        }

        foreach (var c in _allProducts
                     .Select(p => p.CategoryName)
                     .Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (!names.Contains(c!, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(c!);
            }
        }

        _allCategoryChips.Clear();
        ExploreCategories.Clear();

        _allCategoryChips.Add(new CategoryChipItem
        {
            Name = "All",
            Count = _allProducts.Count,
            IsSelected = string.Equals(SelectedCategory, "All", StringComparison.OrdinalIgnoreCase)
        });

        _allCategoryChips.Add(new CategoryChipItem
        {
            Name = "❤️ Favorites",
            Count = _allProducts.Count(p => p.IsFavorite),
            IsSelected = IsFavoritesCategorySelected
        });

        foreach (var name in names)
        {
            var categoryProducts = _allProducts
                .Where(p => string.Equals(p.CategoryName, name, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var cover = categoryProducts.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.ImageUrl))
                        ?? categoryProducts.FirstOrDefault();

            var chip = new CategoryChipItem
            {
                Name = name,
                Count = categoryProducts.Count,
                ImageUrl = cover?.ImageUrl,
                IsSelected = SelectedCategories.Contains(name)
            };
            _allCategoryChips.Add(chip);
            ExploreCategories.Add(chip);
        }

        BuildDisplayCategories(names);
        ApplyCategorySearchFilter();
        OnPropertyChanged(nameof(DeliverToLabel));
    }

    private void ApplyCategorySearchFilter()
    {
        Categories.Clear();
        IEnumerable<CategoryChipItem> query = _allCategoryChips;
        if (!string.IsNullOrWhiteSpace(CategorySearchText))
        {
            var q = CategorySearchText.Trim();
            query = _allCategoryChips.Where(c =>
                c.IsAll ||
                c.IsFavorites ||
                (c.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        foreach (var chip in query)
        {
            chip.IsSelected = chip.IsAll
                ? IsAllCategoriesSelected
                : (chip.IsFavorites ? IsFavoritesCategorySelected : SelectedCategories.Contains(chip.Name));
            Categories.Add(chip);
        }
    }

    [RelayCommand]
    private async Task ChangeBranchAsync()
    {
        _cart.Clear();
        SelectedBranch = null;
        NeedsBranchSelection = true;
        StoreHeader = null;
        Products.Clear();
        Categories.Clear();
        DealsList.Clear();
        OnPropertyChanged(nameof(HasDeals));
        ClearHeroSlides();
        _allProducts.Clear();
        await _session.ClearBranchAsync();
        _session.ClearTenantHeader();
        await RunBusyAsync(LoadBranchesAsync);
    }

    [RelayCommand]
    public void ToggleCategory(CategoryChipItem? chip)
    {
        if (chip == null) return;

        if (chip.IsAll)
        {
            SelectedCategories.Clear();
            IsAllCategoriesSelected = true;
            foreach (var c in _allCategoryChips)
            {
                c.IsSelected = c.IsAll;
            }
        }
        else
        {
            var targetName = chip.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(targetName)) return;

            if (SelectedCategories.Contains(targetName))
            {
                SelectedCategories.Remove(targetName);
                chip.IsSelected = false;
            }
            else
            {
                SelectedCategories.Add(targetName);
                chip.IsSelected = true;
            }

            var allChip = _allCategoryChips.FirstOrDefault(c => c.IsAll);
            if (SelectedCategories.Count > 0)
            {
                if (allChip != null) allChip.IsSelected = false;
                IsAllCategoriesSelected = false;
            }
            else
            {
                if (allChip != null) allChip.IsSelected = true;
                IsAllCategoriesSelected = true;
            }
        }

        UpdateCategorySelectionTitles();
        ApplyCategorySearchFilter();
        ShowProductGrid = true;
        _ = ReloadCatalogForCurrentFiltersAsync();
    }

    [RelayCommand]
    private void SelectCategory(CategoryChipItem? category)
    {
        ToggleCategory(category);
    }

    [RelayCommand]
    private void SelectCategoryByName(string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return;
        }

        if (string.Equals(categoryName, "More", StringComparison.OrdinalIgnoreCase))
        {
            _ = ViewAllMenuAsync();
            return;
        }

        if (string.Equals(categoryName, "Promo", StringComparison.OrdinalIgnoreCase))
        {
            SelectedPill = "Top Deals";
            _ = ReloadCatalogForCurrentFiltersAsync();
            return;
        }

        var chip = _allCategoryChips.FirstOrDefault(c => string.Equals(c.Name?.Trim(), categoryName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (chip != null)
        {
            ToggleCategory(chip);
        }
        else
        {
            var targetName = categoryName.Trim();
            if (SelectedCategories.Contains(targetName))
            {
                SelectedCategories.Remove(targetName);
            }
            else
            {
                SelectedCategories.Add(targetName);
            }

            IsAllCategoriesSelected = SelectedCategories.Count == 0;
            var allChip = _allCategoryChips.FirstOrDefault(c => c.IsAll);
            if (allChip != null) allChip.IsSelected = IsAllCategoriesSelected;

            foreach (var c in _allCategoryChips)
            {
                c.IsSelected = c.IsAll ? IsAllCategoriesSelected : SelectedCategories.Contains(c.Name?.Trim() ?? "");
            }

            UpdateCategorySelectionTitles();
            ApplyCategorySearchFilter();
            ShowProductGrid = true;
            _ = ReloadCatalogForCurrentFiltersAsync();
        }
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchText = string.Empty;
        _ = ReloadCatalogForCurrentFiltersAsync();
    }

    [RelayCommand]
    public void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedPill = string.Empty;
        foreach (var p in FilterPills) p.IsSelected = false;
        SelectedCategories.Clear();
        foreach (var c in _allCategoryChips)
        {
            c.IsSelected = c.IsAll;
        }
        IsAllCategoriesSelected = true;
        UpdateCategorySelectionTitles();
        _ = ReloadCatalogForCurrentFiltersAsync();
    }

    private void UpdateCategorySelectionTitles()
    {
        if (IsAllCategoriesSelected || SelectedCategories.Count == 0)
        {
            SelectedCategory = "All";
        }
        else if (SelectedCategories.Count == 1)
        {
            SelectedCategory = SelectedCategories.First();
        }
        else
        {
            var firstTwo = string.Join(", ", SelectedCategories.Take(2));
            SelectedCategory = SelectedCategories.Count > 2
                ? $"{firstTwo} +{SelectedCategories.Count - 2}"
                : firstTwo;
        }

        OnPropertyChanged(nameof(SelectedCategoryTitle));
        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(IsFavoritesCategorySelected));
        UpdateDisplayCategorySelection();
    }

    [RelayCommand]
    private async Task SelectHomeCategory(HomeCategoryDisplayDto? category)
    {
        if (category == null) return;

        if (category.IsMoreButton)
        {
            await ViewAllMenuAsync();
            return;
        }

        SelectCategoryByName(category.Name);
        UpdateDisplayCategorySelection();

        if (Shell.Current != null)
        {
            try
            {
                var route = $"//customer/order?category={Uri.EscapeDataString(category.Name)}";
                await Shell.Current.GoToAsync(route);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SelectHomeCategory] Navigation error: {ex}");
                await Shell.Current.GoToAsync("//customer/order");
            }
        }
    }

    private void UpdateDisplayCategorySelection()
    {
        foreach (var cat in DisplayCategories)
        {
            if (cat.IsMoreButton) continue;
            cat.IsSelected = SelectedCategories.Contains(cat.Name);
        }
    }

    private void BuildDisplayCategories(List<string> categoryNames)
    {
        DisplayCategories.Clear();

        var categoriesToTake = categoryNames
            .Where(n => !string.IsNullOrWhiteSpace(n) &&
                        !string.Equals(n, "All", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(n, "❤️ Favorites", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(n, "Favorites", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var items = new List<HomeCategoryDisplayDto>();

        if (categoriesToTake.Count > 7)
        {
            // Up to first 7 active categories, and 8th is "More"
            for (var i = 0; i < 7; i++)
            {
                var name = categoriesToTake[i];
                var catProducts = _allProducts
                    .Where(p => string.Equals(p.CategoryName, name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var cover = catProducts.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.ImageUrl))
                            ?? catProducts.FirstOrDefault();
                var coverImg = !string.IsNullOrWhiteSpace(cover?.ImageUrl)
                    ? _orders.ResolveImageUrl(cover!.ImageUrl)
                    : null;
                if (coverImg != null && !coverImg.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !coverImg.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    coverImg = null;
                }

                items.Add(new HomeCategoryDisplayDto
                {
                    Id = catProducts.FirstOrDefault()?.CategoryId ?? (i + 1),
                    Name = name,
                    ImageUrl = coverImg,
                    IconOrEmoji = ResolveCategoryEmoji(name),
                    IsSelected = SelectedCategories.Contains(name),
                    IsMoreButton = false,
                    GridRow = i / 4,
                    GridColumn = i % 4
                });
            }

            // 8th card: "More"
            items.Add(new HomeCategoryDisplayDto
            {
                Id = -1,
                Name = "More",
                IconOrEmoji = "⠿",
                IsMoreButton = true,
                IsSelected = false,
                GridRow = 1,
                GridColumn = 3
            });
        }
        else if (categoriesToTake.Count > 0)
        {
            // 7 or fewer: show directly
            for (var i = 0; i < categoriesToTake.Count; i++)
            {
                var name = categoriesToTake[i];
                var catProducts = _allProducts
                    .Where(p => string.Equals(p.CategoryName, name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var cover = catProducts.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.ImageUrl))
                            ?? catProducts.FirstOrDefault();
                var coverImg = !string.IsNullOrWhiteSpace(cover?.ImageUrl)
                    ? _orders.ResolveImageUrl(cover!.ImageUrl)
                    : null;
                if (coverImg != null && !coverImg.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !coverImg.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    coverImg = null;
                }

                items.Add(new HomeCategoryDisplayDto
                {
                    Id = catProducts.FirstOrDefault()?.CategoryId ?? (i + 1),
                    Name = name,
                    ImageUrl = coverImg,
                    IconOrEmoji = ResolveCategoryEmoji(name),
                    IsSelected = SelectedCategories.Contains(name),
                    IsMoreButton = false,
                    GridRow = i / 4,
                    GridColumn = i % 4
                });
            }

            // Append "More" shortcut if space allows and at least 4 categories exist
            if (items.Count is >= 4 and < 8)
            {
                var moreIdx = items.Count;
                items.Add(new HomeCategoryDisplayDto
                {
                    Id = -1,
                    Name = "More",
                    IconOrEmoji = "⠿",
                    IsMoreButton = true,
                    IsSelected = false,
                    GridRow = moreIdx / 4,
                    GridColumn = moreIdx % 4
                });
            }
        }
        else
        {
            items = CreateDefaultDisplayCategories();
        }

        foreach (var item in items)
        {
            DisplayCategories.Add(item);
        }

        OnPropertyChanged(nameof(DisplayCategories));
    }

    private static List<HomeCategoryDisplayDto> CreateDefaultDisplayCategories() =>
        HomeCatalogDefaults.CreateDefaultDisplayCategories();

    public static string ResolveCategoryEmoji(string? name) =>
        HomeCatalogDefaults.ResolveCategoryEmoji(name);

    [RelayCommand]
    private async Task NavigateToProductDetailAsync(object? param)
    {
        int productId = 0;
        if (param is int id)
        {
            productId = id;
        }
        else if (param is string s && int.TryParse(s, out var parsed))
        {
            productId = parsed;
        }
        else if (param is OnlineProductDto p)
        {
            productId = p.Id;
        }
        else
        {
            productId = Products.FirstOrDefault()?.Id ?? 1;
        }

        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync($"productdetail?productId={productId}");
        }
    }

    [RelayCommand]
    private void ToggleCategorySearch()
    {
        IsCategorySearchVisible = !IsCategorySearchVisible;
        if (!IsCategorySearchVisible)
        {
            CategorySearchText = string.Empty;
            ApplyCategorySearchFilter();
        }
    }

    [RelayCommand]
    private void ClearCategorySearch()
    {
        CategorySearchText = string.Empty;
        ApplyCategorySearchFilter();
    }

    [RelayCommand]
    private async Task ViewAllMenuAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//customer/order");
        }
    }

    [RelayCommand]
    private async Task OrderNowAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//customer/order");
        }
    }

    [RelayCommand]
    private void SetDeliveryMode() => IsDeliveryMode = true;

    [RelayCommand]
    private void SetPickupMode() => IsDeliveryMode = false;

    partial void OnSearchTextChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            ShowProductGrid = true;
        }

        ScheduleSearchFilterDebounce();
    }

    partial void OnCategorySearchTextChanged(string value) => ApplyCategorySearchFilter();

    partial void OnSelectedCategoryChanged(string value)
    {
        OnPropertyChanged(nameof(SelectedCategoryTitle));
    }

    private void ApplyFilters() => _ = ApplyFiltersAsync();

    private async Task ApplyFiltersAsync()
    {
        // Server already applied Keyword / CategoryNames / DealsOnly into _allProducts.
        // Favorites + non-deal quick pills remain local over the loaded server page.
        var generation = Interlocked.Increment(ref _filterGeneration);
        var snapshot = _allProducts.ToList();
        var favoritesOnly = IsFavoritesCategorySelected;
        var pill = SelectedPill;
        var cart = _cart;

        List<OnlineProductDto> filtered;
        try
        {
            filtered = await Task.Run(() =>
                HomeCatalogFilter.Filter(snapshot, favoritesOnly, pill, id => cart.GetQty(id)));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApplyFiltersAsync] {ex.Message}");
            return;
        }

        if (generation != _filterGeneration)
        {
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (generation != _filterGeneration)
            {
                return;
            }

            Products.Clear();
            foreach (var p in filtered)
            {
                Products.Add(p);
            }

            OnPropertyChanged(nameof(FilteredItemsLabel));
            OnPropertyChanged(nameof(SelectedCategoryTitle));
            OnPropertyChanged(nameof(IsFavoritesCategorySelected));
            OnPropertyChanged(nameof(EmptyStateIcon));
            OnPropertyChanged(nameof(EmptyStateTitle));
            OnPropertyChanged(nameof(EmptyStateSubtitle));
            OnPropertyChanged(nameof(HasProducts));
            OnPropertyChanged(nameof(HasActiveFilters));
            OnPropertyChanged(nameof(HasSearchText));
        });
    }

    [RelayCommand]
    private void SelectPill(QuickFilterPillItem? pill)
    {
        if (pill == null)
        {
            return;
        }

        if (string.Equals(SelectedPill, pill.Title, StringComparison.OrdinalIgnoreCase))
        {
            SelectedPill = string.Empty;
            pill.IsSelected = false;
        }
        else
        {
            SelectedPill = pill.Title;
            foreach (var p in FilterPills)
            {
                p.IsSelected = string.Equals(p.Title, pill.Title, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (string.IsNullOrWhiteSpace(SelectedPill) ||
            SelectedPill.Contains("Top Deals", StringComparison.OrdinalIgnoreCase))
        {
            _ = ReloadCatalogForCurrentFiltersAsync();
        }
        else
        {
            ApplyFilters();
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(OnlineProductDto? product)
    {
        if (product == null)
        {
            return;
        }

        var isFav = await _favorites.ToggleFavoriteAsync(product.Id);
        product.IsFavorite = isFav;

        var favChip = _allCategoryChips.FirstOrDefault(c => c.IsFavorites);
        if (favChip != null)
        {
            favChip.Count = _allProducts.Count(p => p.IsFavorite);
        }

        if (IsFavoritesCategorySelected)
        {
            ApplyFilters();
        }
    }

    private void SyncFavorites()
    {
        foreach (var p in _allProducts)
        {
            p.IsFavorite = _favorites.IsFavorite(p.Id);
        }

        var favChip = _allCategoryChips.FirstOrDefault(c => c.IsFavorites);
        if (favChip != null)
        {
            favChip.Count = _allProducts.Count(p => p.IsFavorite);
        }

        if (IsFavoritesCategorySelected)
        {
            ApplyFilters();
        }
    }

    private void SyncCartQtyBadges()
    {
        foreach (var p in _allProducts)
        {
            p.CartQty = _cart.GetQty(p.Id);
        }

        ApplyFilters();
    }

    [RelayCommand]
    private async Task IncreaseCardAsync(OnlineProductDto? product)
    {
        await OpenProductAsync(product);
    }

    [RelayCommand]
    private void DecreaseCard(OnlineProductDto? product)
    {
        if (product == null || product.CartQty <= 0)
        {
            return;
        }

        _cart.DecrementProduct(product.Id);
    }

    [RelayCommand]
    private async Task OpenProductAsync(OnlineProductDto? product)
    {
        if (product == null || Shell.Current == null)
        {
            return;
        }

        // Deals + regular products share the same Aljannat Product Detail screen.
        ProductDetailLaunch.SetPrefill(product);
        await Shell.Current.GoToAsync($"productdetail?productId={product.Id}");
    }
}

