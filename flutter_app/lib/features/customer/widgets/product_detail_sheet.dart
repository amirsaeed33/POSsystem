import 'package:flutter/material.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../../../core/constants/app_colors.dart';
import '../../../core/widgets/app_button.dart';
import '../../../models/product_models.dart';
import '../../../models/cart_models.dart';
import '../../../services/online_catalog_service.dart';
import '../../../services/cart_service.dart';

class ProductDetailSheet extends StatefulWidget {
  final OnlineProductDto product;
  final OnlineCatalogService catalogService;
  final CartService cartService;

  const ProductDetailSheet({
    super.key,
    required this.product,
    required this.catalogService,
    required this.cartService,
  });

  static Future<void> show({
    required BuildContext context,
    required OnlineProductDto product,
    required OnlineCatalogService catalogService,
    required CartService cartService,
  }) {
    return showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => ProductDetailSheet(
        product: product,
        catalogService: catalogService,
        cartService: cartService,
      ),
    );
  }

  @override
  State<ProductDetailSheet> createState() => _ProductDetailSheetState();
}

class _ProductDetailSheetState extends State<ProductDetailSheet> {
  bool _isLoadingModifiers = true;
  List<ProductModifierGroupDto> _modifierGroups = [];
  final Map<int, Set<int>> _selectedOptionsByGroup = {}; // groupId -> Set<optionId>
  final TextEditingController _notesController = TextEditingController();
  int _quantity = 1;

  @override
  void initState() {
    super.initState();
    _loadModifiers();
  }

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  Future<void> _loadModifiers() async {
    try {
      final groups = await widget.catalogService.getProductModifiers(widget.product.id);
      if (mounted) {
        setState(() {
          _modifierGroups = groups;
          _isLoadingModifiers = false;
          // Auto-select first option for required single-select groups
          for (final group in groups) {
            if (group.isRequired && !group.allowMultiple && group.options.isNotEmpty) {
              _selectedOptionsByGroup[group.id] = {group.options.first.id};
            } else {
              _selectedOptionsByGroup[group.id] = {};
            }
          }
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _isLoadingModifiers = false;
        });
      }
    }
  }

  double get _totalPrice {
    double modsPrice = 0.0;
    for (final group in _modifierGroups) {
      final selectedIds = _selectedOptionsByGroup[group.id] ?? {};
      for (final opt in group.options) {
        if (selectedIds.contains(opt.id)) {
          modsPrice += opt.price;
        }
      }
    }
    return (widget.product.price + modsPrice) * _quantity;
  }

  bool get _isSatisfied {
    for (final group in _modifierGroups) {
      if (group.isRequired) {
        final selectedIds = _selectedOptionsByGroup[group.id] ?? {};
        if (selectedIds.isEmpty) return false;
      }
    }
    return true;
  }

  void _toggleOption(ProductModifierGroupDto group, ModifierOptionDto option) {
    setState(() {
      final current = _selectedOptionsByGroup[group.id] ?? {};
      if (group.allowMultiple) {
        if (current.contains(option.id)) {
          current.remove(option.id);
        } else {
          if (group.maxSelection <= 1 || current.length < group.maxSelection) {
            current.add(option.id);
          }
        }
        _selectedOptionsByGroup[group.id] = current;
      } else {
        // Single select (radio)
        _selectedOptionsByGroup[group.id] = {option.id};
      }
    });
  }

  void _addToCart() {
    if (!_isSatisfied) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Please select required options before adding.'),
          backgroundColor: AppColors.badgeRed,
        ),
      );
      return;
    }

    final selectedModifiers = <CartSelectedModifier>[];
    for (final group in _modifierGroups) {
      final selectedIds = _selectedOptionsByGroup[group.id] ?? {};
      for (final opt in group.options) {
        if (selectedIds.contains(opt.id)) {
          selectedModifiers.add(
            CartSelectedModifier(
              optionId: opt.id,
              groupId: group.id,
              groupName: group.name,
              optionName: opt.name,
              price: opt.price,
            ),
          );
        }
      }
    }

    widget.cartService.addItem(
      productId: widget.product.id,
      productName: widget.product.name,
      basePrice: widget.product.price,
      imageUrl: widget.product.imageUrl,
      modifiers: selectedModifiers,
      notes: _notesController.text.trim().isNotEmpty ? _notesController.text.trim() : null,
      quantity: _quantity,
    );

    Navigator.of(context).pop();
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text('Added ${widget.product.name} to cart!'),
        backgroundColor: AppColors.primary,
        duration: const Duration(seconds: 2),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      constraints: BoxConstraints(
        maxHeight: MediaQuery.of(context).size.height * 0.88,
      ),
      decoration: const BoxDecoration(
        color: AppColors.pageBackground,
        borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
      ),
      child: Column(
        children: [
          // Drag handle
          Container(
            margin: const EdgeInsets.only(top: 10, bottom: 6),
            width: 44,
            height: 5,
            decoration: BoxDecoration(
              color: AppColors.gray300,
              borderRadius: BorderRadius.circular(10),
            ),
          ),

          Expanded(
            child: ListView(
              padding: const EdgeInsets.all(20),
              children: [
                // Product Header Card
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: AppColors.cardBackground,
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(color: AppColors.borderSubtle),
                  ),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      ClipRRect(
                        borderRadius: BorderRadius.circular(14),
                        child: Container(
                          width: 80,
                          height: 80,
                          color: AppColors.primaryLight,
                          child: widget.product.imageUrl != null &&
                                  widget.product.imageUrl!.isNotEmpty
                              ? CachedNetworkImage(
                                  imageUrl: widget.product.imageUrl!,
                                  fit: BoxFit.cover,
                                  errorWidget: (_, __, ___) => Center(
                                    child: Text(
                                      widget.product.initials,
                                      style: const TextStyle(
                                        color: AppColors.primary,
                                        fontWeight: FontWeight.bold,
                                        fontSize: 24,
                                      ),
                                    ),
                                  ),
                                )
                              : Center(
                                  child: Text(
                                    widget.product.initials,
                                    style: const TextStyle(
                                      color: AppColors.primary,
                                      fontWeight: FontWeight.bold,
                                      fontSize: 24,
                                    ),
                                  ),
                                ),
                        ),
                      ),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              widget.product.name,
                              style: const TextStyle(
                                fontSize: 18,
                                fontWeight: FontWeight.bold,
                                color: AppColors.textPrimary,
                              ),
                            ),
                            if (widget.product.description != null &&
                                widget.product.description!.isNotEmpty) ...[
                              const SizedBox(height: 4),
                              Text(
                                widget.product.description!,
                                style: const TextStyle(
                                  fontSize: 12,
                                  color: AppColors.textSecondary,
                                ),
                              ),
                            ],
                            const SizedBox(height: 8),
                            Text(
                              widget.product.priceDisplay,
                              style: const TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.bold,
                                color: AppColors.primary,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),

                const SizedBox(height: 20),

                // Modifiers Section
                if (_isLoadingModifiers)
                  const Padding(
                    padding: EdgeInsets.all(24),
                    child: Center(
                      child: CircularProgressIndicator(color: AppColors.primary),
                    ),
                  )
                else ...[
                  for (final group in _modifierGroups) ...[
                    _buildModifierGroupSection(group),
                    const SizedBox(height: 16),
                  ],
                ],

                // Special Instructions Input
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: AppColors.cardBackground,
                    borderRadius: BorderRadius.circular(16),
                    border: Border.all(color: AppColors.borderSubtle),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Special Instructions',
                        style: TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.bold,
                          color: AppColors.textPrimary,
                        ),
                      ),
                      const SizedBox(height: 8),
                      TextField(
                        controller: _notesController,
                        maxLines: 2,
                        decoration: InputDecoration(
                          hintText: 'e.g. No onion, extra spicy, sauce on the side...',
                          filled: true,
                          fillColor: AppColors.pageBackground,
                          border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(10),
                            borderSide: const BorderSide(color: AppColors.borderSubtle),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 20),
              ],
            ),
          ),

          // Bottom Bar with Quantity & Add Button
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
            decoration: BoxDecoration(
              color: AppColors.cardBackground,
              border: const Border(top: BorderSide(color: AppColors.borderSubtle)),
              boxShadow: const [
                BoxShadow(
                  color: Color(0x0C000000),
                  offset: Offset(0, -4),
                  blurRadius: 10,
                ),
              ],
            ),
            child: SafeArea(
              child: Row(
                children: [
                  // Quantity Selector
                  Container(
                    decoration: BoxDecoration(
                      color: AppColors.pageBackground,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: AppColors.borderSubtle),
                    ),
                    child: Row(
                      children: [
                        IconButton(
                          icon: const Icon(Icons.remove, size: 18),
                          color: AppColors.primary,
                          onPressed: () {
                            if (_quantity > 1) {
                              setState(() => _quantity--);
                            }
                          },
                        ),
                        Text(
                          '$_quantity',
                          style: const TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.bold,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        IconButton(
                          icon: const Icon(Icons.add, size: 18),
                          color: AppColors.primary,
                          onPressed: () => setState(() => _quantity++),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 14),

                  // Add to Cart Button
                  Expanded(
                    child: AppButton(
                      text: 'Add • Rs. ${_totalPrice.toStringAsFixed(0)}',
                      onPressed: _addToCart,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildModifierGroupSection(ProductModifierGroupDto group) {
    final selectedIds = _selectedOptionsByGroup[group.id] ?? {};

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.borderSubtle),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                group.name,
                style: const TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: group.isRequired ? AppColors.primaryLight : AppColors.gray100,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  group.requirementLabel,
                  style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.bold,
                    color: group.isRequired ? AppColors.primary : AppColors.textSecondary,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 4),
          Text(
            group.selectionHint,
            style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
          ),
          const SizedBox(height: 12),

          // Options list
          Column(
            children: group.options.map((opt) {
              final isSelected = selectedIds.contains(opt.id);

              return InkWell(
                onTap: () => _toggleOption(group, opt),
                borderRadius: BorderRadius.circular(10),
                child: Container(
                  margin: const EdgeInsets.only(bottom: 8),
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                  decoration: BoxDecoration(
                    color: isSelected ? AppColors.primaryLight : AppColors.pageBackground,
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(
                      color: isSelected ? AppColors.primary : AppColors.borderSubtle,
                      width: isSelected ? 1.5 : 1.0,
                    ),
                  ),
                  child: Row(
                    children: [
                      Container(
                        width: 20,
                        height: 20,
                        decoration: BoxDecoration(
                          shape: group.allowMultiple ? BoxShape.rectangle : BoxShape.circle,
                          borderRadius: group.allowMultiple ? BorderRadius.circular(4) : null,
                          border: Border.all(
                            color: isSelected ? AppColors.primary : AppColors.borderSubtle,
                            width: 2,
                          ),
                          color: isSelected ? AppColors.primary : Colors.transparent,
                        ),
                        child: isSelected
                            ? const Icon(Icons.check, size: 14, color: Colors.white)
                            : null,
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Text(
                          opt.name,
                          style: TextStyle(
                            fontSize: 14,
                            fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
                            color: isSelected ? AppColors.primary : AppColors.textPrimary,
                          ),
                        ),
                      ),
                      Text(
                        opt.priceLabel,
                        style: TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.bold,
                          color: isSelected ? AppColors.primary : AppColors.textSecondary,
                        ),
                      ),
                    ],
                  ),
                ),
              );
            }).toList(),
          ),
        ],
      ),
    );
  }
}

