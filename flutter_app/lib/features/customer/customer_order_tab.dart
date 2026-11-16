import 'dart:async';

import 'package:flutter/material.dart';
import '../../core/constants/app_colors.dart';
import '../../models/branch_models.dart';
import '../../models/product_models.dart';
import '../../services/online_catalog_service.dart';
import '../../services/cart_service.dart';
import 'widgets/product_card.dart';
import 'widgets/product_detail_sheet.dart';

class CustomerOrderTab extends StatefulWidget {
  final OnlineCatalogService catalogService;
  final CartService cartService;
  final OnlineBranchDto? currentBranch;

  const CustomerOrderTab({
    super.key,
    required this.catalogService,
    required this.cartService,
    required this.currentBranch,
  });

  @override
  State<CustomerOrderTab> createState() => _CustomerOrderTabState();
}

class _CustomerOrderTabState extends State<CustomerOrderTab> {
  static const int _pageSize = 30;

  final ScrollController _scrollController = ScrollController();
  Timer? _searchDebounce;

  bool _isLoading = true;
  bool _isLoadingMore = false;
  int _totalCount = 0;

  List<OnlineProductDto> _products = [];
  Set<int> _favoriteIds = {};
  List<String> _categories = ['All'];
  String _selectedCategory = 'All';
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
    _reloadCatalog();
  }

  @override
  void didUpdateWidget(covariant CustomerOrderTab oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.currentBranch?.id != widget.currentBranch?.id) {
      _reloadCatalog();
    }
  }

  @override
  void dispose() {
    _searchDebounce?.cancel();
    _scrollController.removeListener(_onScroll);
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (!_scrollController.hasClients) return;
    if (_scrollController.position.pixels >= _scrollController.position.maxScrollExtent - 200) {
      _loadNextPage();
    }
  }

  void _rebuildCategories() {
    final catSet = <String>{'All'};
    for (final p in _products) {
      if (p.categoryName != null && p.categoryName!.trim().isNotEmpty) {
        catSet.add(p.categoryName!.trim());
      }
    }
    _categories = catSet.toList();
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
        widget.catalogService.getFavoriteProductIds(branchId: branchId),
      ]);

      final page = results[0] as ({List<OnlineProductDto> items, int totalCount});
      final favIds = results[1] as Set<int>;

      if (mounted) {
        setState(() {
          _products = page.items;
          _totalCount = page.totalCount;
          _favoriteIds = favIds;
          _rebuildCategories();
          _isLoading = false;
        });
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
    return Scaffold(
      backgroundColor: AppColors.pageBackground,
      appBar: AppBar(
        title: const Text('Full Menu'),
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
          : Column(
              children: [
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                  child: TextField(
                    onChanged: _onSearchChanged,
                    decoration: InputDecoration(
                      hintText: 'Search menu items...',
                      prefixIcon: const Icon(Icons.search, color: AppColors.textMuted),
                      fillColor: AppColors.cardBackground,
                      filled: true,
                    ),
                  ),
                ),

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
                        onSelected: (_) => _onCategorySelected(cat),
                        selectedColor: AppColors.primary,
                        backgroundColor: AppColors.cardBackground,
                        labelStyle: TextStyle(
                          color: isSelected ? Colors.white : AppColors.textPrimary,
                          fontWeight: isSelected ? FontWeight.bold : FontWeight.w600,
                        ),
                        showCheckmark: false,
                      );
                    },
                  ),
                ),
                const SizedBox(height: 12),

                Expanded(
                  child: _products.isEmpty
                      ? const Center(
                          child: Text('No products match your search.', style: TextStyle(color: AppColors.textMuted)),
                        )
                      : GridView.builder(
                          controller: _scrollController,
                          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                          itemCount: _products.length + (_isLoadingMore ? 1 : 0),
                          gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                            crossAxisCount: 2,
                            childAspectRatio: 0.72,
                            crossAxisSpacing: 14,
                            mainAxisSpacing: 14,
                          ),
                          itemBuilder: (context, index) {
                            if (index >= _products.length) {
                              return const Center(
                                child: CircularProgressIndicator(color: AppColors.primary),
                              );
                            }

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
              ],
            ),
    );
  }
}