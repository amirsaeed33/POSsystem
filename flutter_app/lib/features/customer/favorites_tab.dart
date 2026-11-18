import 'package:flutter/material.dart';
import '../../core/constants/app_colors.dart';
import '../../core/constants/branch_ui_copy.dart';
import '../../models/product_models.dart';
import '../../services/online_catalog_service.dart';
import '../../services/cart_service.dart';
import 'widgets/product_card.dart';
import 'widgets/product_detail_sheet.dart';

class FavoritesTab extends StatefulWidget {
  final OnlineCatalogService catalogService;
  final CartService cartService;
  final int? branchId;
  final bool isRestaurant;

  const FavoritesTab({
    super.key,
    required this.catalogService,
    required this.cartService,
    this.branchId,
    this.isRestaurant = true,
  });

  @override
  State<FavoritesTab> createState() => _FavoritesTabState();
}

class _FavoritesTabState extends State<FavoritesTab> {
  bool _loading = true;
  List<OnlineProductDto> _favorites = [];

  @override
  void initState() {
    super.initState();
    _load();
    widget.cartService.addListener(_onCart);
  }

  @override
  void didUpdateWidget(covariant FavoritesTab oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.branchId != widget.branchId) _load();
  }

  @override
  void dispose() {
    widget.cartService.removeListener(_onCart);
    super.dispose();
  }

  void _onCart() {
    if (mounted) setState(() {});
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final ids = await widget.catalogService.getFavoriteProductIds(branchId: widget.branchId);
      final catalog = await widget.catalogService.getCatalog(widget.branchId);
      final favs = catalog.where((p) => ids.contains(p.id)).toList();
      if (!mounted) return;
      setState(() {
        _favorites = favs;
        _loading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _favorites = [];
        _loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.pageBackground,
      appBar: AppBar(
        title: const Text('My Favorites'),
        actions: [IconButton(icon: const Icon(Icons.refresh), onPressed: _load)],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
          : _favorites.isEmpty
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.favorite_border, color: AppColors.primary, size: 40),
                      const SizedBox(height: 12),
                      const Text('No favorites yet', style: TextStyle(fontWeight: FontWeight.bold)),
                      const SizedBox(height: 6),
                      Text(
                        BranchUiCopy.emptyFavoritesHint(widget.isRestaurant),
                        style: const TextStyle(color: AppColors.textSecondary),
                      ),
                    ],
                  ),
                )
              : GridView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: _favorites.length,
                  gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: 2,
                    childAspectRatio: 0.72,
                    crossAxisSpacing: 14,
                    mainAxisSpacing: 14,
                  ),
                  itemBuilder: (context, index) {
                    final p = _favorites[index];
                    return ProductCard(
                      product: p,
                      isFavorite: true,
                      inCartQty: widget.cartService.getQuantityForProduct(p.id),
                      onTap: () {
                        ProductDetailSheet.show(
                          context: context,
                          product: p,
                          catalogService: widget.catalogService,
                          cartService: widget.cartService,
                        );
                      },
                      onAdd: () {
                        ProductDetailSheet.show(
                          context: context,
                          product: p,
                          catalogService: widget.catalogService,
                          cartService: widget.cartService,
                        );
                      },
                    );
                  },
                ),
    );
  }
}
