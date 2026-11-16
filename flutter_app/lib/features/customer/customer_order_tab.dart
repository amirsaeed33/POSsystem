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
  bool _isLoading = true;
  List<OnlineProductDto> _products = [];
  List<String> _categories = ['All'];
  String _selectedCategory = 'All';
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    _loadMenu();
  }

  Future<void> _loadMenu() async {
    setState(() => _isLoading = true);
    final products = await widget.catalogService.getCatalog(widget.currentBranch?.id);
    final catSet = <String>{'All'};
    for (final p in products) {
      if (p.categoryName != null && p.categoryName!.trim().isNotEmpty) {
        catSet.add(p.categoryName!.trim());
      }
    }

    if (mounted) {
      setState(() {
        _products = products;
        _categories = catSet.toList();
        _isLoading = false;
      });
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
    return Scaffold(
      backgroundColor: AppColors.pageBackground,
      appBar: AppBar(
        title: const Text('Full Menu'),
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
          : Column(
              children: [
                // Search bar
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                  child: TextField(
                    onChanged: (val) => setState(() => _searchQuery = val),
                    decoration: InputDecoration(
                      hintText: 'Search menu items...',
                      prefixIcon: const Icon(Icons.search, color: AppColors.textMuted),
                      fillColor: AppColors.cardBackground,
                      filled: true,
                    ),
                  ),
                ),

                // Category chips
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
                        ),
                        showCheckmark: false,
                      );
                    },
                  ),
                ),
                const SizedBox(height: 12),

                // Grid
                Expanded(
                  child: GridView.builder(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
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
    );
  }
}

