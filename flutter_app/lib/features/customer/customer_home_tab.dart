import 'dart:async';

import 'package:flutter/material.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../../core/constants/app_colors.dart';
import '../../core/constants/branch_ui_copy.dart';
import '../../models/branch_models.dart';
import '../../models/product_models.dart';
import '../../models/deal_models.dart';
import '../../services/online_catalog_service.dart';
import '../../services/cart_service.dart';
import 'widgets/product_card.dart';
import 'widgets/product_detail_sheet.dart';
import 'widgets/branch_picker_dialog.dart';

class CustomerHomeTab extends StatefulWidget {
  final OnlineCatalogService catalogService;
  final CartService cartService;
  final OnlineBranchDto? currentBranch;
  final List<OnlineBranchDto> branches;
  final ValueChanged<OnlineBranchDto> onBranchChanged;

  const CustomerHomeTab({
    super.key,
    required this.catalogService,
    required this.cartService,
    required this.currentBranch,
    required this.branches,
    required this.onBranchChanged,
  });

  @override
  State<CustomerHomeTab> createState() => _CustomerHomeTabState();
}

class _CustomerHomeTabState extends State<CustomerHomeTab> {
  static const int _pageSize = 30;
  static const Color _catActive = Color(0xFFE23744);

  final ScrollController _scrollController = ScrollController();
  final PageController _heroPageController = PageController();
  Timer? _searchDebounce;
  Timer? _heroTimer;

  bool _isLoading = true;
  bool _isLoadingMore = false;
  int _totalCount = 0;
  int _heroIndex = 0;

  List<OnlineProductDto> _products = [];
  List<HeroSlideDto> _heroSlides = [];
  List<DealDto> _deals = [];
  Set<int> _favoriteIds = {};
  List<String> _categories = ['All'];
  List<String> _backendCategories = [];
  Map<String, String> _categoryImageByName = {};
  String _selectedCategory = 'All';
  String _searchQuery = '';

  bool get _showHeroBanners =>
      _heroSlides.isNotEmpty && _selectedCategory == 'All' && _searchQuery.trim().isEmpty;

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
    _reloadCatalog();
    widget.cartService.addListener(_onCartChanged);
  }

  @override
  void didUpdateWidget(covariant CustomerHomeTab oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.currentBranch?.id != widget.currentBranch?.id) {
      _reloadCatalog();
    }
  }

  @override
  void dispose() {
    _searchDebounce?.cancel();
    _stopHeroAutoPlay();
    _scrollController.removeListener(_onScroll);
    _scrollController.dispose();
    _heroPageController.dispose();
    widget.cartService.removeListener(_onCartChanged);
    super.dispose();
  }

  void _startHeroAutoPlay() {
    _stopHeroAutoPlay();
    if (_heroSlides.length <= 1) return;
    _heroTimer = Timer.periodic(const Duration(milliseconds: 5500), (_) {
      if (!mounted || !_heroPageController.hasClients || !_showHeroBanners) return;
      final next = (_heroIndex + 1) % _heroSlides.length;
      _heroPageController.animateToPage(
        next,
        duration: const Duration(milliseconds: 650),
        curve: Curves.easeInOutCubic,
      );
    });
  }

  void _stopHeroAutoPlay() {
    _heroTimer?.cancel();
    _heroTimer = null;
  }

  void _onCartChanged() {
    if (mounted) setState(() {});
  }

  void _onScroll() {
    if (!_scrollController.hasClients) return;
    final max = _scrollController.position.maxScrollExtent;
    if (_scrollController.position.pixels >= max - 240) {
      _loadNextPage();
    }
  }

  void _rebuildCategories() {
    final fromProducts = <String>{};
    for (final p in _products) {
      final name = p.categoryName?.trim() ?? '';
      if (name.isEmpty) continue;
      final lower = name.toLowerCase();
      if (lower == 'all' || lower.contains('deal')) continue;
      fromProducts.add(name);
    }

    // Prefer categories that have catalog items; fall back to backend list.
    final regular = fromProducts.isNotEmpty
        ? fromProducts.toList()
        : _backendCategories
            .where((c) {
              final lower = c.toLowerCase();
              return lower != 'all' && !lower.contains('deal');
            })
            .toList();

    regular.sort((a, b) => a.toLowerCase().compareTo(b.toLowerCase()));
    _categories = ['All', ...regular];
  }

  void _applyOnlineCategoryCatalog(List<OnlineCategoryDto> list) {
    final imageMap = Map<String, String>.from(_categoryImageByName);
    final names = <String>[];
    for (final cat in list) {
      final name = cat.name.trim();
      if (name.isEmpty) continue;
      names.add(name);
      final url = cat.imageUrl?.trim();
      if (url != null && url.isNotEmpty) {
        imageMap[name.toLowerCase()] = url;
      }
    }
    _categoryImageByName = imageMap;
    if (names.isNotEmpty) {
      _backendCategories = names;
    }
  }

  _CategoryVisual _categoryVisual(String cat) {
    final c = cat.toLowerCase().trim();
    final uploaded = _categoryImageByName[c];

    if (c == 'all') {
      return const _CategoryVisual(icon: Icons.explore_outlined, color: _catActive);
    }
    if (uploaded != null && uploaded.isNotEmpty) {
      return _CategoryVisual(icon: Icons.sell_outlined, color: const Color(0xFFBE185D), imageUrl: uploaded);
    }
    if (c.contains('deal') || c.contains('🔥')) {
      return const _CategoryVisual(icon: Icons.bolt, color: Color(0xFFEF4444));
    }
    if (c.contains('popular') || c.contains('✨') || c.contains('top')) {
      return const _CategoryVisual(icon: Icons.star, color: Color(0xFFF59E0B));
    }
    if (c.contains('burger') || c.contains('burgur') || c.contains('fast')) {
      return const _CategoryVisual(icon: Icons.lunch_dining, color: Color(0xFFF97316));
    }
    if (c.contains('shawarma') || c.contains('showarma') || c.contains('roll') || c.contains('wrap')) {
      return const _CategoryVisual(icon: Icons.kebab_dining, color: Color(0xFFD97706));
    }
    if (c.contains('drink') || c.contains('beverage') || c.contains('juice') || c.contains('soda')) {
      return const _CategoryVisual(icon: Icons.local_drink_outlined, color: Color(0xFF0284C7));
    }
    if (c.contains('pizza')) {
      return const _CategoryVisual(icon: Icons.local_pizza_outlined, color: Color(0xFFEA580C));
    }
    if (c.contains('desi') || c.contains('karahi') || c.contains('handi') || c.contains('biryani')) {
      return const _CategoryVisual(icon: Icons.restaurant, color: Color(0xFFB91C1C));
    }
    if (c.contains('chinese') || c.contains('soup') || c.contains('noodle')) {
      return const _CategoryVisual(icon: Icons.ramen_dining, color: Color(0xFFDC2626));
    }
    if (c.contains('coffee') || c.contains('tea') || c.contains('cafe')) {
      return const _CategoryVisual(icon: Icons.coffee, color: Color(0xFF78350F));
    }
    if (c.contains('sweet') || c.contains('mithai') || c.contains('cake') || c.contains('bakery')) {
      return const _CategoryVisual(icon: Icons.cake_outlined, color: Color(0xFFDB2777));
    }
    if (c.contains('fragrance') || c.contains('perfume') || c.contains('attar') || c.contains('oud')) {
      return const _CategoryVisual(icon: Icons.spa_outlined, color: Color(0xFF7C3AED));
    }
    return const _CategoryVisual(icon: Icons.category_outlined, color: Color(0xFF64748B));
  }

  String get _menuSectionIcon {
    final restaurant = widget.currentBranch?.isRestaurant ?? true;
    return restaurant ? '🍽️' : '🛍️';
  }

  String get _menuSectionTitle {
    final restaurant = widget.currentBranch?.isRestaurant ?? true;
    return restaurant ? 'Explore Food Menu' : 'Explore Products';
  }

  Future<void> _reloadCatalog() async {
    setState(() {
      _isLoading = true;
      _products = [];
      _totalCount = 0;
    });

    final branchId = widget.currentBranch?.id;

    try {
      final results = await Future.wait([
        widget.catalogService.getCatalogPage(
          branchId: branchId,
          skip: 0,
          take: _pageSize,
          categoryName: _selectedCategory == 'All' ? null : _selectedCategory,
          keyword: _searchQuery.isEmpty ? null : _searchQuery,
        ),
        widget.catalogService.getOnlineCategories(branchId: branchId),
        if (branchId != null)
          widget.catalogService.getHeroSlides(branchId)
        else
          Future.value(<HeroSlideDto>[]),
        if (branchId != null)
          widget.catalogService.getDeals(branchId)
        else
          Future.value(<DealDto>[]),
        widget.catalogService.getFavoriteProductIds(branchId: branchId),
      ]);

      final page = results[0] as ({List<OnlineProductDto> items, int totalCount});
      final onlineCats = results[1] as List<OnlineCategoryDto>;
      final slides = results[2] as List<HeroSlideDto>;
      final deals = results[3] as List<DealDto>;
      final favIds = results[4] as Set<int>;

      if (mounted) {
        setState(() {
          _products = page.items;
          _totalCount = page.totalCount;
          _heroSlides = slides;
          _heroIndex = 0;
          _deals = deals;
          _favoriteIds = favIds;
          _applyOnlineCategoryCatalog(onlineCats);
          _rebuildCategories();
          _isLoading = false;
        });
        _startHeroAutoPlay();
      }
    } catch (_) {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _loadNextPage() async {
    if (_isLoading || _isLoadingMore || _products.length >= _totalCount) return;

    setState(() => _isLoadingMore = true);
    final branchId = widget.currentBranch?.id;

    try {
      final page = await widget.catalogService.getCatalogPage(
        branchId: branchId,
        skip: _products.length,
        take: _pageSize,
        categoryName: _selectedCategory == 'All' ? null : _selectedCategory,
        keyword: _searchQuery.isEmpty ? null : _searchQuery,
      );

      if (mounted) {
        setState(() {
          _products = [..._products, ...page.items];
          _totalCount = page.totalCount;
          _rebuildCategories();
          _isLoadingMore = false;
        });
      }
    } catch (_) {
      if (mounted) setState(() => _isLoadingMore = false);
    }
  }

  void _onSearchChanged(String value) {
    _searchQuery = value;
    _searchDebounce?.cancel();
    _searchDebounce = Timer(const Duration(milliseconds: 300), () {
      if (mounted) _reloadCatalog();
    });
    setState(() {});
  }

  void _onCategorySelected(String cat) {
    if (_selectedCategory == cat) return;
    setState(() => _selectedCategory = cat);
    _reloadCatalog();
  }

  Future<void> _toggleFavorite(int productId) async {
    await widget.catalogService.toggleFavorite(productId, branchId: widget.currentBranch?.id);
    final ids = await widget.catalogService.getFavoriteProductIds(branchId: widget.currentBranch?.id);
    if (mounted) setState(() => _favoriteIds = ids);
  }

  @override
  Widget build(BuildContext context) {
    final branch = widget.currentBranch;

    return Scaffold(
      backgroundColor: AppColors.pageBackground,
      appBar: AppBar(
        backgroundColor: AppColors.pageBackground,
        elevation: 0,
        titleSpacing: 16,
        title: GestureDetector(
          onTap: () {
            showDialog(
              context: context,
              builder: (_) => BranchPickerDialog(
                branches: widget.branches,
                onSelect: widget.onBranchChanged,
              ),
            );
          },
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
            decoration: BoxDecoration(
              color: AppColors.cardBackground,
              borderRadius: BorderRadius.circular(20),
              border: Border.all(color: AppColors.borderSubtle),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.location_on, color: AppColors.primary, size: 18),
                const SizedBox(width: 6),
                Text(
                  branch?.name ?? 'Select Branch',
                  style: const TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                    color: AppColors.textPrimary,
                  ),
                ),
                const SizedBox(width: 4),
                const Icon(Icons.keyboard_arrow_down, color: AppColors.textSecondary, size: 18),
              ],
            ),
          ),
        ),
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
          : RefreshIndicator(
              color: AppColors.primary,
              onRefresh: _reloadCatalog,
              child: ListView(
                controller: _scrollController,
                padding: const EdgeInsets.only(bottom: 100),
                children: [
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                    child: TextField(
                      onChanged: _onSearchChanged,
                      decoration: InputDecoration(
                        hintText: BranchUiCopy.searchHint(
                          widget.currentBranch?.isRestaurant ?? true,
                        ),
                        prefixIcon: const Icon(Icons.search, color: AppColors.textMuted),
                        fillColor: AppColors.cardBackground,
                        filled: true,
                        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(16),
                          borderSide: const BorderSide(color: AppColors.borderSubtle),
                        ),
                      ),
                    ),
                  ),

                  if (_showHeroBanners) ...[
                    Padding(
                      padding: const EdgeInsets.fromLTRB(12, 4, 12, 12),
                      child: AspectRatio(
                        aspectRatio: 4 / 3,
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(16),
                          child: DecoratedBox(
                            decoration: BoxDecoration(
                              color: const Color(0xFF0B1220),
                              border: Border.all(color: Colors.black.withValues(alpha: 0.08)),
                              boxShadow: [
                                BoxShadow(
                                  color: Colors.black.withValues(alpha: 0.18),
                                  blurRadius: 24,
                                  offset: const Offset(0, 10),
                                ),
                              ],
                            ),
                            child: Stack(
                              fit: StackFit.expand,
                              children: [
                                PageView.builder(
                                  controller: _heroPageController,
                                  itemCount: _heroSlides.length,
                                  onPageChanged: (index) {
                                    setState(() => _heroIndex = index);
                                  },
                                  itemBuilder: (context, index) {
                                    final slide = _heroSlides[index];
                                    final imageUrl = slide.bannerImageUrl;
                                    final title = [
                                      slide.titleMain,
                                      slide.titleHighlight,
                                      slide.titleEnd,
                                    ].where((p) => p != null && p.trim().isNotEmpty).join(' ');

                                    return Stack(
                                      fit: StackFit.expand,
                                      children: [
                                        if (imageUrl.isNotEmpty)
                                          CachedNetworkImage(
                                            imageUrl: imageUrl,
                                            fit: BoxFit.cover,
                                            placeholder: (_, __) => Container(color: const Color(0xFF1E293B)),
                                            errorWidget: (_, __, ___) => Container(
                                              color: AppColors.primaryDark,
                                              alignment: Alignment.center,
                                              child: const Icon(Icons.image_outlined, color: Colors.white54, size: 42),
                                            ),
                                          )
                                        else
                                          Container(color: AppColors.primaryDark),
                                        DecoratedBox(
                                          decoration: BoxDecoration(
                                            gradient: LinearGradient(
                                              begin: Alignment.topCenter,
                                              end: Alignment.bottomCenter,
                                              colors: [
                                                Colors.black.withValues(alpha: 0.05),
                                                Colors.black.withValues(alpha: 0.55),
                                              ],
                                            ),
                                          ),
                                        ),
                                        if (title.isNotEmpty || (slide.badge?.trim().isNotEmpty ?? false))
                                          Positioned(
                                            left: 16,
                                            right: 16,
                                            bottom: 28,
                                            child: Column(
                                              crossAxisAlignment: CrossAxisAlignment.start,
                                              children: [
                                                if (slide.badge?.trim().isNotEmpty ?? false)
                                                  Container(
                                                    margin: const EdgeInsets.only(bottom: 8),
                                                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                                                    decoration: BoxDecoration(
                                                      color: Colors.white.withValues(alpha: 0.22),
                                                      borderRadius: BorderRadius.circular(999),
                                                    ),
                                                    child: Text(
                                                      slide.badge!,
                                                      style: const TextStyle(
                                                        color: Colors.white,
                                                        fontSize: 11,
                                                        fontWeight: FontWeight.w800,
                                                      ),
                                                    ),
                                                  ),
                                                if (title.isNotEmpty)
                                                  Text(
                                                    title,
                                                    maxLines: 2,
                                                    overflow: TextOverflow.ellipsis,
                                                    style: const TextStyle(
                                                      color: Colors.white,
                                                      fontSize: 20,
                                                      fontWeight: FontWeight.w900,
                                                      height: 1.15,
                                                    ),
                                                  ),
                                              ],
                                            ),
                                          ),
                                      ],
                                    );
                                  },
                                ),
                                if (_heroSlides.length > 1)
                                  Positioned(
                                    left: 0,
                                    right: 0,
                                    bottom: 10,
                                    child: Row(
                                      mainAxisAlignment: MainAxisAlignment.center,
                                      children: List.generate(_heroSlides.length, (i) {
                                        final active = i == _heroIndex;
                                        return AnimatedContainer(
                                          duration: const Duration(milliseconds: 220),
                                          margin: const EdgeInsets.symmetric(horizontal: 3),
                                          width: active ? 18 : 7,
                                          height: 7,
                                          decoration: BoxDecoration(
                                            color: active ? Colors.white : Colors.white.withValues(alpha: 0.45),
                                            borderRadius: BorderRadius.circular(999),
                                          ),
                                        );
                                      }),
                                    ),
                                  ),
                              ],
                            ),
                          ),
                        ),
                      ),
                    ),
                  ],

                  if (_categories.isNotEmpty) ...[
                    Padding(
                      padding: const EdgeInsets.fromLTRB(16, 4, 16, 8),
                      child: Row(
                        children: [
                          Text(_menuSectionIcon, style: const TextStyle(fontSize: 22)),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  _menuSectionTitle,
                                  style: const TextStyle(
                                    fontSize: 16,
                                    fontWeight: FontWeight.w800,
                                    color: AppColors.textPrimary,
                                  ),
                                ),
                                const Text(
                                  'Browse by category',
                                  style: TextStyle(fontSize: 12, color: AppColors.textMuted),
                                ),
                              ],
                            ),
                          ),
                          if (_selectedCategory != 'All')
                            TextButton(
                              onPressed: () => _onCategorySelected('All'),
                              style: TextButton.styleFrom(
                                foregroundColor: AppColors.textSecondary,
                                padding: const EdgeInsets.symmetric(horizontal: 8),
                                minimumSize: Size.zero,
                                tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                              ),
                              child: const Text(
                                'Clear',
                                style: TextStyle(fontWeight: FontWeight.w700, fontSize: 13),
                              ),
                            ),
                          if (_categories.length > 1)
                            Text(
                              '${_categories.length - 1} categories',
                              style: const TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.w700,
                                color: AppColors.textMuted,
                              ),
                            ),
                        ],
                      ),
                    ),
                    SizedBox(
                      height: 108,
                      child: ListView.separated(
                        padding: const EdgeInsets.fromLTRB(14, 4, 14, 8),
                        scrollDirection: Axis.horizontal,
                        itemCount: _categories.length,
                        separatorBuilder: (_, __) => const SizedBox(width: 14),
                        itemBuilder: (context, index) {
                          final cat = _categories[index];
                          final isSelected = cat == _selectedCategory;
                          final visual = _categoryVisual(cat);

                          return GestureDetector(
                            onTap: () => _onCategorySelected(cat),
                            child: SizedBox(
                              width: 76,
                              child: Column(
                                children: [
                                  AnimatedScale(
                                    scale: isSelected ? 1.05 : 1.0,
                                    duration: const Duration(milliseconds: 180),
                                    child: Container(
                                      width: 68,
                                      height: 68,
                                      decoration: BoxDecoration(
                                        shape: BoxShape.circle,
                                        color: const Color(0xFFF1F5F9),
                                        border: Border.all(
                                          color: isSelected ? _catActive : const Color(0xFFE2E8F0),
                                          width: 2.5,
                                        ),
                                        boxShadow: [
                                          BoxShadow(
                                            color: isSelected
                                                ? _catActive.withValues(alpha: 0.3)
                                                : Colors.black.withValues(alpha: 0.1),
                                            blurRadius: isSelected ? 16 : 12,
                                            offset: const Offset(0, 4),
                                          ),
                                        ],
                                      ),
                                      clipBehavior: Clip.antiAlias,
                                      child: Stack(
                                        fit: StackFit.expand,
                                        children: [
                                          if (visual.imageUrl != null && visual.imageUrl!.isNotEmpty)
                                            CachedNetworkImage(
                                              imageUrl: visual.imageUrl!,
                                              fit: BoxFit.cover,
                                              errorWidget: (_, __, ___) => _CategoryIconFallback(
                                                icon: visual.icon,
                                                color: visual.color,
                                              ),
                                              placeholder: (_, __) => _CategoryIconFallback(
                                                icon: visual.icon,
                                                color: visual.color,
                                              ),
                                            )
                                          else
                                            _CategoryIconFallback(
                                              icon: visual.icon,
                                              color: visual.color,
                                            ),
                                          if (isSelected)
                                            Positioned(
                                              right: 1,
                                              bottom: 1,
                                              child: Container(
                                                width: 18,
                                                height: 18,
                                                decoration: BoxDecoration(
                                                  color: _catActive,
                                                  shape: BoxShape.circle,
                                                  border: Border.all(color: Colors.white, width: 2),
                                                ),
                                                child: const Icon(Icons.check, size: 10, color: Colors.white),
                                              ),
                                            ),
                                        ],
                                      ),
                                    ),
                                  ),
                                  const SizedBox(height: 6),
                                  Text(
                                    cat,
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                    textAlign: TextAlign.center,
                                    style: TextStyle(
                                      fontSize: 11.5,
                                      fontWeight: isSelected ? FontWeight.w900 : FontWeight.w700,
                                      color: isSelected ? _catActive : const Color(0xFF475569),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          );
                        },
                      ),
                    ),
                  ],

                  const SizedBox(height: 8),

                  if (_deals.isNotEmpty) ...[
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Text(
                            '🔥 Special Deals',
                            style: TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.bold,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          Text(
                            '${_deals.length} deals',
                            style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 10),
                    SizedBox(
                      height: 130,
                      child: ListView.separated(
                        padding: const EdgeInsets.symmetric(horizontal: 16),
                        scrollDirection: Axis.horizontal,
                        itemCount: _deals.length,
                        separatorBuilder: (_, __) => const SizedBox(width: 12),
                        itemBuilder: (context, index) {
                          final deal = _deals[index];
                          return Container(
                            width: 260,
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              color: AppColors.cardBackground,
                              borderRadius: BorderRadius.circular(16),
                              border: Border.all(color: AppColors.borderSubtle),
                            ),
                            child: Row(
                              children: [
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    mainAxisAlignment: MainAxisAlignment.center,
                                    children: [
                                      Text(
                                        deal.name,
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                        style: const TextStyle(
                                          fontWeight: FontWeight.bold,
                                          fontSize: 14,
                                          color: AppColors.textPrimary,
                                        ),
                                      ),
                                      if (deal.description != null) ...[
                                        const SizedBox(height: 2),
                                        Text(
                                          deal.description!,
                                          maxLines: 2,
                                          overflow: TextOverflow.ellipsis,
                                          style: const TextStyle(fontSize: 11, color: AppColors.textSecondary),
                                        ),
                                      ],
                                      const SizedBox(height: 6),
                                      Text(
                                        deal.priceDisplay,
                                        style: const TextStyle(
                                          fontSize: 14,
                                          fontWeight: FontWeight.bold,
                                          color: AppColors.primary,
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ],
                            ),
                          );
                        },
                      ),
                    ),
                    const SizedBox(height: 16),
                  ],

                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          _selectedCategory == 'All' ? 'Popular Items' : _selectedCategory,
                          style: const TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        Text(
                          '${_products.length} of $_totalCount items',
                          style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: 12),

                  _products.isEmpty
                      ? const Padding(
                          padding: EdgeInsets.all(40),
                          child: Center(
                            child: Text(
                              'No products match your search.',
                              style: TextStyle(color: AppColors.textMuted),
                            ),
                          ),
                        )
                      : Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 16),
                          child: GridView.builder(
                            physics: const NeverScrollableScrollPhysics(),
                            shrinkWrap: true,
                            itemCount: _products.length,
                            gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                              crossAxisCount: 2,
                              childAspectRatio: 0.72,
                              crossAxisSpacing: 14,
                              mainAxisSpacing: 14,
                            ),
                            itemBuilder: (context, index) {
                              final product = _products[index];
                              final inCart = widget.cartService.getQuantityForProduct(product.id);

                              return ProductCard(
                                product: product,
                                inCartQty: inCart,
                                isFavorite: _favoriteIds.contains(product.id),
                                onToggleFavorite: () => _toggleFavorite(product.id),
                                onTap: () {
                                  ProductDetailSheet.show(
                                    context: context,
                                    product: product,
                                    catalogService: widget.catalogService,
                                    cartService: widget.cartService,
                                  );
                                },
                                onAdd: () {
                                  ProductDetailSheet.show(
                                    context: context,
                                    product: product,
                                    catalogService: widget.catalogService,
                                    cartService: widget.cartService,
                                  );
                                },
                              );
                            },
                          ),
                        ),

                  if (_isLoadingMore)
                    const Padding(
                      padding: EdgeInsets.symmetric(vertical: 20),
                      child: Center(child: CircularProgressIndicator(color: AppColors.primary)),
                    ),
                ],
              ),
            ),
    );
  }
}

class _CategoryVisual {
  final IconData icon;
  final Color color;
  final String? imageUrl;

  const _CategoryVisual({
    required this.icon,
    required this.color,
    this.imageUrl,
  });
}

class _CategoryIconFallback extends StatelessWidget {
  final IconData icon;
  final Color color;

  const _CategoryIconFallback({
    required this.icon,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return ColoredBox(
      color: color,
      child: Center(
        child: Icon(icon, color: Colors.white, size: 28),
      ),
    );
  }
}