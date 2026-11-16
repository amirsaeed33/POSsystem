import 'package:flutter/material.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../../core/constants/app_colors.dart';
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
  final VoidCallback onOpenCart;

  const CustomerHomeTab({
    super.key,
    required this.catalogService,
    required this.cartService,
    required this.currentBranch,
    required this.branches,
    required this.onBranchChanged,
    required this.onOpenCart,
  });

  @override
  State<CustomerHomeTab> createState() => _CustomerHomeTabState();
}

class _CustomerHomeTabState extends State<CustomerHomeTab> {
  bool _isLoading = true;
  List<OnlineProductDto> _products = [];
  List<HeroSlideDto> _heroSlides = [];
  List<DealDto> _deals = [];
  List<String> _categories = ['All'];
  String _selectedCategory = 'All';
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    _loadData();
    widget.cartService.addListener(_onCartChanged);
  }

  @override
  void didUpdateWidget(covariant CustomerHomeTab oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.currentBranch?.id != widget.currentBranch?.id) {
      _loadData();
    }
  }

  @override
  void dispose() {
    widget.cartService.removeListener(_onCartChanged);
    super.dispose();
  }

  void _onCartChanged() {
    if (mounted) setState(() {});
  }

  Future<void> _loadData() async {
    setState(() => _isLoading = true);
    final branchId = widget.currentBranch?.id;

    try {
      final futures = await Future.wait([
        widget.catalogService.getCatalog(branchId),
        if (branchId != null) widget.catalogService.getHeroSlides(branchId) else Future.value(<HeroSlideDto>[]),
        if (branchId != null) widget.catalogService.getDeals(branchId) else Future.value(<DealDto>[]),
      ]);

      final products = futures[0] as List<OnlineProductDto>;
      final slides = futures[1] as List<HeroSlideDto>;
      final deals = futures[2] as List<DealDto>;

      // Extract unique categories
      final catSet = <String>{'All'};
      for (final p in products) {
        if (p.categoryName != null && p.categoryName!.trim().isNotEmpty) {
          catSet.add(p.categoryName!.trim());
        }
      }

      if (mounted) {
        setState(() {
          _products = products;
          _heroSlides = slides;
          _deals = deals;
          _categories = catSet.toList();
          _isLoading = false;
        });
      }
    } catch (_) {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  List<OnlineProductDto> get _filteredProducts {
    return _products.where((p) {
      final matchesCat = _selectedCategory == 'All' || p.categoryName == _selectedCategory;
      final matchesSearch = _searchQuery.isEmpty ||
          p.name.toLowerCase().contains(_searchQuery.toLowerCase()) ||
          (p.description?.toLowerCase().contains(_searchQuery.toLowerCase()) ?? false);
      return matchesCat && matchesSearch;
    }).toList();
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
        actions: [
          // Cart Button with badge
          GestureDetector(
            onTap: widget.onOpenCart,
            child: Container(
              margin: const EdgeInsets.only(right: 16),
              width: 44,
              height: 44,
              decoration: BoxDecoration(
                color: AppColors.cardBackground,
                shape: BoxShape.circle,
                border: Border.all(color: AppColors.borderSubtle),
              ),
              child: Stack(
                alignment: Alignment.center,
                children: [
                  const Icon(Icons.shopping_bag_outlined, color: AppColors.textPrimary, size: 22),
                  if (widget.cartService.itemCount > 0)
                    Positioned(
                      top: 6,
                      right: 6,
                      child: Container(
                        padding: const EdgeInsets.all(4),
                        decoration: const BoxDecoration(
                          color: AppColors.primary,
                          shape: BoxShape.circle,
                        ),
                        constraints: const BoxConstraints(minWidth: 16, minHeight: 16),
                        child: Text(
                          '${widget.cartService.itemCount}',
                          textAlign: TextAlign.center,
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 10,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                    ),
                ],
              ),
            ),
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
          : RefreshIndicator(
              color: AppColors.primary,
              onRefresh: _loadData,
              child: ListView(
                padding: const EdgeInsets.only(bottom: 100),
                children: [
                  // Search Bar
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                    child: TextField(
                      onChanged: (val) => setState(() => _searchQuery = val),
                      decoration: InputDecoration(
                        hintText: 'Search delicious meals, burgers, pizza...',
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

                  // Hero Banner Slides (if available)
                  if (_heroSlides.isNotEmpty) ...[
                    SizedBox(
                      height: 160,
                      child: PageView.builder(
                        itemCount: _heroSlides.length,
                        itemBuilder: (context, index) {
                          final slide = _heroSlides[index];
                          return Container(
                            margin: const EdgeInsets.symmetric(horizontal: 16),
                            padding: const EdgeInsets.all(16),
                            decoration: BoxDecoration(
                              gradient: const LinearGradient(
                                colors: [Color(0xFFC84B11), Color(0xFF8F3006)],
                                begin: Alignment.topLeft,
                                end: Alignment.bottomRight,
                              ),
                              borderRadius: BorderRadius.circular(20),
                            ),
                            child: Row(
                              children: [
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    mainAxisAlignment: MainAxisAlignment.center,
                                    children: [
                                      if (slide.badge != null)
                                        Container(
                                          padding: const EdgeInsets.symmetric(
                                              horizontal: 8, vertical: 4),
                                          decoration: BoxDecoration(
                                            color: Colors.white.withOpacity(0.25),
                                            borderRadius: BorderRadius.circular(8),
                                          ),
                                          child: Text(
                                            slide.badge!,
                                            style: const TextStyle(
                                              color: Colors.white,
                                              fontSize: 10,
                                              fontWeight: FontWeight.bold,
                                            ),
                                          ),
                                        ),
                                      const SizedBox(height: 6),
                                      Text(
                                        '${slide.titleMain ?? ''} ${slide.titleHighlight ?? ''}',
                                        maxLines: 2,
                                        style: const TextStyle(
                                          color: Colors.white,
                                          fontSize: 18,
                                          fontWeight: FontWeight.bold,
                                        ),
                                      ),
                                      if (slide.description != null) ...[
                                        const SizedBox(height: 4),
                                        Text(
                                          slide.description!,
                                          maxLines: 1,
                                          style: TextStyle(
                                            color: Colors.white.withOpacity(0.85),
                                            fontSize: 12,
                                          ),
                                        ),
                                      ],
                                    ],
                                  ),
                                ),
                                if (slide.imageUrl != null && slide.imageUrl!.isNotEmpty)
                                  ClipRRect(
                                    borderRadius: BorderRadius.circular(14),
                                    child: CachedNetworkImage(
                                      imageUrl: slide.imageUrl!,
                                      width: 90,
                                      height: 90,
                                      fit: BoxFit.cover,
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

                  // Category Chips Bar
                  SizedBox(
                    height: 42,
                    child: ListView.separated(
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      scrollDirection: Axis.horizontal,
                      itemCount: _categories.length,
                      separatorBuilder: (_, __) => const SizedBox(width: 8),
                      itemBuilder: (context, index) {
                        final cat = _categories[index];
                        final isSelected = cat == _selectedCategory;

                        return ChoiceChip(
                          label: Text(cat),
                          selected: isSelected,
                          onSelected: (_) => setState(() => _selectedCategory = cat),
                          selectedColor: AppColors.primary,
                          backgroundColor: AppColors.cardBackground,
                          labelStyle: TextStyle(
                            color: isSelected ? Colors.white : AppColors.textPrimary,
                            fontWeight: isSelected ? FontWeight.bold : FontWeight.w600,
                            fontSize: 13,
                          ),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(20),
                            side: BorderSide(
                              color: isSelected ? AppColors.primary : AppColors.borderSubtle,
                            ),
                          ),
                          showCheckmark: false,
                        );
                      },
                    ),
                  ),

                  const SizedBox(height: 16),

                  // Special Deals Carousel (if available)
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

                  // Section Title
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
                          '${_filteredProducts.length} items',
                          style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: 12),

                  // Products Grid
                  _filteredProducts.isEmpty
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
                            itemCount: _filteredProducts.length,
                            gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                              crossAxisCount: 2,
                              childAspectRatio: 0.72,
                              crossAxisSpacing: 14,
                              mainAxisSpacing: 14,
                            ),
                            itemBuilder: (context, index) {
                              final product = _filteredProducts[index];
                              final inCart = widget.cartService.getQuantityForProduct(product.id);

                              return ProductCard(
                                product: product,
                                inCartQty: inCart,
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
                ],
              ),
            ),
    );
  }
}

