import 'dart:async';

import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import '../../core/constants/app_colors.dart';
import '../../core/constants/branch_ui_copy.dart';
import '../../core/widgets/app_button.dart';
import '../../models/cart_models.dart';
import '../../models/checkout_models.dart';
import '../../models/order_models.dart';
import '../../services/cart_service.dart';
import '../../services/online_catalog_service.dart';
import 'order_tracking_screen.dart';

class CartScreen extends StatefulWidget {
  final CartService cartService;
  final OnlineCatalogService catalogService;
  final int? branchId;
  final bool isRestaurant;
  /// When true, hides the back button (used as bottom-nav tab).
  final bool asTab;
  final VoidCallback? onContinueShopping;

  const CartScreen({
    super.key,
    required this.cartService,
    required this.catalogService,
    this.branchId,
    this.isRestaurant = true,
    this.asTab = false,
    this.onContinueShopping,
  });

  @override
  State<CartScreen> createState() => _CartScreenState();
}

class _CartScreenState extends State<CartScreen> {
  final _formKey = GlobalKey<FormState>();
  late TextEditingController _nameController;
  late TextEditingController _phoneController;
  late TextEditingController _addressController;
  late TextEditingController _landmarkController;
  late TextEditingController _notesController;
  bool _isSubmitting = false;
  bool _isCapturingGps = false;
  bool _loadingDeliveryMeta = false;

  List<DeliveryAreaDto> _deliveryAreas = [];
  List<CustomerSavedAddressDto> _savedAddresses = [];
  Timer? _phoneDebounce;
  OrderType? _trackedOrderType;

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController(text: widget.cartService.customerName);
    _phoneController = TextEditingController(text: widget.cartService.customerPhone);
    _addressController = TextEditingController(text: widget.cartService.deliveryAddress);
    _landmarkController = TextEditingController(text: widget.cartService.deliveryLandmark);
    _notesController = TextEditingController(text: widget.cartService.orderNotes);
    _phoneController.addListener(_onPhoneChanged);
    widget.cartService.addListener(_onCartChanged);
    _trackedOrderType = widget.cartService.orderType;
    if (widget.cartService.orderType == OrderType.delivery) {
      _loadDeliveryMeta();
    }
    final phone = _phoneController.text.trim();
    if (phone.length >= 8) {
      _loadSavedAddresses(phone);
    }
  }

  @override
  void dispose() {
    _phoneDebounce?.cancel();
    _phoneController.removeListener(_onPhoneChanged);
    _nameController.dispose();
    _phoneController.dispose();
    _addressController.dispose();
    _landmarkController.dispose();
    _notesController.dispose();
    widget.cartService.removeListener(_onCartChanged);
    super.dispose();
  }

  void _onCartChanged() {
    final type = widget.cartService.orderType;
    if (type == OrderType.delivery && _trackedOrderType != OrderType.delivery) {
      _loadDeliveryMeta();
    }
    _trackedOrderType = type;
    if (mounted) setState(() {});
  }

  void _onPhoneChanged() {
    _phoneDebounce?.cancel();
    _phoneDebounce = Timer(const Duration(milliseconds: 450), () {
      final phone = _phoneController.text.trim();
      if (phone.length >= 8) {
        _loadSavedAddresses(phone);
      } else if (phone.isEmpty) {
        if (mounted) {
          setState(() => _savedAddresses = []);
        }
      }
    });
  }

  Future<void> _loadDeliveryMeta() async {
    final branchId = widget.branchId;
    if (branchId == null || branchId <= 0) return;

    setState(() => _loadingDeliveryMeta = true);
    try {
      final header = await widget.catalogService.getStoreHeader(branchId);
      if (header != null) {
        widget.cartService.setDeliveryConfig(
          baseFee: header.baseDeliveryFee > 0 ? header.baseDeliveryFee : 150,
          freeThreshold: header.freeDeliveryThreshold,
        );
      }

      final areas = await widget.catalogService.getDeliveryAreas(branchId);
      if (!mounted) return;
      setState(() => _deliveryAreas = areas);

      if (areas.isNotEmpty) {
        final selectedId = widget.cartService.deliveryAreaId;
        DeliveryAreaDto? match;
        if (selectedId != null) {
          for (final a in areas) {
            if (a.id == selectedId) {
              match = a;
              break;
            }
          }
        }
        final area = match ?? areas.first;
        widget.cartService.setDeliveryArea(
          id: area.id,
          name: area.name,
          fee: area.deliveryFee,
          minOrder: area.minimumOrderAmount,
        );
      }
    } catch (_) {
      // Keep checkout usable with defaults.
    } finally {
      if (mounted) setState(() => _loadingDeliveryMeta = false);
    }
  }

  Future<void> _loadSavedAddresses(String phone) async {
    try {
      final list = await widget.catalogService.getSavedAddresses(
        phone,
        branchId: widget.branchId,
      );
      if (!mounted) return;
      setState(() => _savedAddresses = list);
    } catch (_) {
      if (mounted) setState(() => _savedAddresses = []);
    }
  }

  void _selectSavedAddress(CustomerSavedAddressDto addr) {
    _addressController.text = addr.address;
    _landmarkController.text = addr.landmark ?? '';
    widget.cartService.setCustomerDetails(
      address: addr.address,
      landmark: addr.landmark ?? '',
    );

    DeliveryAreaDto? areaMatch;
    if (addr.deliveryAreaId != null) {
      for (final a in _deliveryAreas) {
        if (a.id == addr.deliveryAreaId) {
          areaMatch = a;
          break;
        }
      }
    }
    if (areaMatch == null && addr.deliveryAreaName != null) {
      final name = addr.deliveryAreaName!.toLowerCase();
      for (final a in _deliveryAreas) {
        if (a.name.toLowerCase() == name) {
          areaMatch = a;
          break;
        }
      }
    }

    if (areaMatch != null) {
      widget.cartService.setDeliveryArea(
        id: areaMatch.id,
        name: areaMatch.name,
        fee: areaMatch.deliveryFee,
        minOrder: areaMatch.minimumOrderAmount,
      );
    }

    if (addr.latitude != null && addr.longitude != null) {
      widget.cartService.setGps(lat: addr.latitude, lng: addr.longitude);
    }
    setState(() {});
  }

  Future<void> _captureGps() async {
    setState(() => _isCapturingGps = true);
    try {
      var permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }
      if (permission == LocationPermission.denied || permission == LocationPermission.deniedForever) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Location permission is required for delivery GPS.'),
              backgroundColor: AppColors.badgeRed,
            ),
          );
        }
        return;
      }

      final enabled = await Geolocator.isLocationServiceEnabled();
      if (!enabled && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Turn on GPS to share your delivery location.'),
            backgroundColor: AppColors.badgeRed,
          ),
        );
        return;
      }

      final position = await Geolocator.getCurrentPosition(
        desiredAccuracy: LocationAccuracy.high,
      );
      widget.cartService.setGps(lat: position.latitude, lng: position.longitude);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Could not get location: $e'), backgroundColor: AppColors.badgeRed),
        );
      }
    } finally {
      if (mounted) setState(() => _isCapturingGps = false);
    }
  }

  Future<void> _placeOrder() async {
    if (!_formKey.currentState!.validate()) return;
    if (widget.cartService.items.isEmpty) return;

    if (!widget.cartService.meetsMinimumOrder) {
      final min = widget.cartService.minimumOrderAmount;
      final shortfall = (min - widget.cartService.subtotal).clamp(0.0, double.infinity);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Minimum order for ${widget.cartService.deliveryAreaName ?? "delivery"} is Rs. ${min.toStringAsFixed(0)}. '
            'Add Rs. ${shortfall.toStringAsFixed(0)} more.',
          ),
          backgroundColor: AppColors.badgeRed,
        ),
      );
      return;
    }

    setState(() => _isSubmitting = true);

    widget.cartService.setCustomerDetails(
      name: _nameController.text.trim(),
      phone: _phoneController.text.trim(),
      address: _addressController.text.trim(),
      landmark: _landmarkController.text.trim(),
      notes: _notesController.text.trim(),
    );

    try {
      final cart = widget.cartService;
      final lines = cart.items
          .map((item) => CreateCustomerOrderLineDto(
                productId: item.productId,
                quantity: item.quantity.toDouble(),
                unitPrice: item.unitPrice,
              ))
          .toList();

      final isDelivery = cart.orderType == OrderType.delivery;
      final dto = CreateCustomerOrderDto(
        branchId: widget.branchId,
        customerName: _nameController.text.trim(),
        customerMobile: _phoneController.text.trim(),
        deliveryAddress: isDelivery ? _addressController.text.trim() : 'Takeaway / Pickup',
        deliveryLandmark: _landmarkController.text.trim().isNotEmpty
            ? _landmarkController.text.trim()
            : null,
        notes: _notesController.text.trim().isNotEmpty ? _notesController.text.trim() : null,
        latitude: isDelivery ? cart.latitude : null,
        longitude: isDelivery ? cart.longitude : null,
        deliveryAreaId: isDelivery ? cart.deliveryAreaId : null,
        deliveryArea: isDelivery ? cart.deliveryAreaName : null,
        subtotal: cart.subtotal,
        deliveryCharges: cart.deliveryFee,
        grandTotal: cart.grandTotal,
        lines: lines,
      );

      final created = await widget.catalogService.createCustomerOrder(dto);
      widget.cartService.clearCart();

      if (mounted) {
        setState(() => _isSubmitting = false);
        Navigator.of(context).pushReplacement(
          MaterialPageRoute(
            builder: (_) => OrderTrackingScreen(
              orderId: created.id,
              orderNo: created.displayOrderNo,
              catalogService: widget.catalogService,
              isRestaurant: widget.isRestaurant,
            ),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isSubmitting = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Order submission failed: $e'),
            backgroundColor: AppColors.badgeRed,
          ),
        );
      }
    }
  }

  Widget _buildFreeDeliveryBar(CartService cart) {
    if (cart.orderType != OrderType.delivery || cart.freeDeliveryThreshold <= 0) {
      return const SizedBox.shrink();
    }

    final progress = cart.freeDeliveryProgress;
    final unlocked = cart.deliveryFee == 0 && cart.subtotal > 0;
    final remaining = (cart.freeDeliveryThreshold - cart.subtotal).clamp(0.0, double.infinity);

    return Container(
      margin: const EdgeInsets.only(bottom: 16),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.borderSubtle),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(
                unlocked ? Icons.local_shipping : Icons.delivery_dining,
                color: unlocked ? Colors.green : AppColors.primary,
                size: 20,
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  unlocked
                      ? 'You unlocked free delivery!'
                      : 'Add Rs. ${remaining.toStringAsFixed(0)} more for free delivery',
                  style: const TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                    color: AppColors.textPrimary,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          ClipRRect(
            borderRadius: BorderRadius.circular(8),
            child: LinearProgressIndicator(
              value: progress,
              minHeight: 8,
              backgroundColor: AppColors.primaryLight,
              color: unlocked ? Colors.green : AppColors.primary,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDeliveryAreas(CartService cart) {
    if (_loadingDeliveryMeta && _deliveryAreas.isEmpty) {
      return const Padding(
        padding: EdgeInsets.only(bottom: 12),
        child: LinearProgressIndicator(color: AppColors.primary, minHeight: 3),
      );
    }
    if (_deliveryAreas.isEmpty) return const SizedBox.shrink();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Delivery Area',
          style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: AppColors.textSecondary),
        ),
        const SizedBox(height: 8),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: _deliveryAreas.map((area) {
            final selected = cart.deliveryAreaId == area.id;
            return ChoiceChip(
              label: Text('${area.name} • ${area.feeLabel}'),
              selected: selected,
              onSelected: (_) {
                cart.setDeliveryArea(
                  id: area.id,
                  name: area.name,
                  fee: area.deliveryFee,
                  minOrder: area.minimumOrderAmount,
                );
              },
              selectedColor: AppColors.primary,
              backgroundColor: AppColors.pageBackground,
              labelStyle: TextStyle(
                color: selected ? Colors.white : AppColors.textPrimary,
                fontWeight: FontWeight.w600,
                fontSize: 12,
              ),
              showCheckmark: false,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(20),
                side: BorderSide(color: selected ? AppColors.primary : AppColors.borderSubtle),
              ),
            );
          }).toList(),
        ),
        const SizedBox(height: 12),
      ],
    );
  }

  Widget _buildSavedAddressChips() {
    if (_savedAddresses.isEmpty) return const SizedBox.shrink();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Saved addresses',
          style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: AppColors.textSecondary),
        ),
        const SizedBox(height: 8),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: _savedAddresses.map((addr) {
            return ActionChip(
              avatar: Icon(
                addr.isDefault ? Icons.star : Icons.home_outlined,
                size: 16,
                color: AppColors.primary,
              ),
              label: Text(addr.displayLabel, style: const TextStyle(fontSize: 12)),
              onPressed: () => _selectSavedAddress(addr),
              backgroundColor: AppColors.primaryLight,
              side: const BorderSide(color: AppColors.borderSubtle),
            );
          }).toList(),
        ),
        const SizedBox(height: 12),
      ],
    );
  }

  Widget _buildGpsRow(CartService cart) {
    final lat = cart.latitude;
    final lng = cart.longitude;
    return Row(
      children: [
        Expanded(
          child: OutlinedButton.icon(
            onPressed: _isCapturingGps ? null : _captureGps,
            icon: _isCapturingGps
                ? const SizedBox(
                    width: 16,
                    height: 16,
                    child: CircularProgressIndicator(strokeWidth: 2, color: AppColors.primary),
                  )
                : const Icon(Icons.my_location, size: 18),
            label: Text(_isCapturingGps ? 'Getting GPS…' : 'Use my location'),
            style: OutlinedButton.styleFrom(
              foregroundColor: AppColors.primary,
              side: const BorderSide(color: AppColors.primary),
            ),
          ),
        ),
        if (lat != null && lng != null) ...[
          const SizedBox(width: 8),
          Chip(
            label: Text(
              '${lat.toStringAsFixed(5)}, ${lng.toStringAsFixed(5)}',
              style: const TextStyle(fontSize: 11),
            ),
            backgroundColor: AppColors.primaryLight,
            deleteIcon: const Icon(Icons.close, size: 16),
            onDeleted: () => cart.setGps(lat: null, lng: null),
          ),
        ],
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    final cart = widget.cartService;

    if (cart.items.isEmpty) {
      return Scaffold(
        backgroundColor: AppColors.pageBackground,
        appBar: AppBar(
          title: const Text('My Cart'),
          automaticallyImplyLeading: !widget.asTab,
        ),
        body: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                width: 80,
                height: 80,
                decoration: BoxDecoration(
                  color: AppColors.primaryLight,
                  borderRadius: BorderRadius.circular(24),
                ),
                child: const Icon(Icons.shopping_bag_outlined, color: AppColors.primary, size: 40),
              ),
              const SizedBox(height: 16),
              const Text(
                'Your cart is empty',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
              ),
              const SizedBox(height: 8),
              Text(
                BranchUiCopy.emptyCartHint(widget.isRestaurant),
                style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
              ),
              const SizedBox(height: 24),
              AppButton(
                text: BranchUiCopy.exploreCatalogCta(widget.isRestaurant),
                width: 160,
                onPressed: () {
                  if (widget.onContinueShopping != null) {
                    widget.onContinueShopping!();
                  } else {
                    Navigator.of(context).maybePop();
                  }
                },
              ),
            ],
          ),
        ),
      );
    }

    return Scaffold(
      backgroundColor: AppColors.pageBackground,
      appBar: AppBar(
        title: const Text('My Cart'),
        automaticallyImplyLeading: !widget.asTab,
        actions: [
          IconButton(
            icon: const Icon(Icons.delete_outline, color: AppColors.badgeRed),
            onPressed: () => cart.clearCart(),
          ),
        ],
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Container(
              padding: const EdgeInsets.all(4),
              decoration: BoxDecoration(
                color: AppColors.cardBackground,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: AppColors.borderSubtle),
              ),
              child: Row(
                children: [
                  Expanded(
                    child: GestureDetector(
                      onTap: () => cart.setOrderType(OrderType.delivery),
                      child: Container(
                        padding: const EdgeInsets.symmetric(vertical: 10),
                        decoration: BoxDecoration(
                          color: cart.orderType == OrderType.delivery
                              ? AppColors.primary
                              : Colors.transparent,
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Center(
                          child: Text(
                            '🛵 Delivery',
                            style: TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.bold,
                              color: cart.orderType == OrderType.delivery
                                  ? Colors.white
                                  : AppColors.textSecondary,
                            ),
                          ),
                        ),
                      ),
                    ),
                  ),
                  Expanded(
                    child: GestureDetector(
                      onTap: () => cart.setOrderType(OrderType.takeaway),
                      child: Container(
                        padding: const EdgeInsets.symmetric(vertical: 10),
                        decoration: BoxDecoration(
                          color: cart.orderType == OrderType.takeaway
                              ? AppColors.primary
                              : Colors.transparent,
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Center(
                          child: Text(
                            '🛍️ Takeaway',
                            style: TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.bold,
                              color: cart.orderType == OrderType.takeaway
                                  ? Colors.white
                                  : AppColors.textSecondary,
                            ),
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 16),

            Container(
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: AppColors.cardBackground,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: AppColors.borderSubtle),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Order Items',
                    style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
                  ),
                  const SizedBox(height: 10),
                  for (final item in cart.items) ...[
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                item.productName,
                                style: const TextStyle(
                                  fontSize: 14,
                                  fontWeight: FontWeight.bold,
                                  color: AppColors.textPrimary,
                                ),
                              ),
                              if (item.modifiers.isNotEmpty) ...[
                                const SizedBox(height: 2),
                                Text(
                                  item.modifierSummary,
                                  style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
                                ),
                              ],
                              const SizedBox(height: 4),
                              Text(
                                'Rs. ${item.totalPrice.toStringAsFixed(0)}',
                                style: const TextStyle(
                                  fontSize: 13,
                                  fontWeight: FontWeight.bold,
                                  color: AppColors.primary,
                                ),
                              ),
                            ],
                          ),
                        ),
                        Container(
                          decoration: BoxDecoration(
                            color: AppColors.pageBackground,
                            borderRadius: BorderRadius.circular(8),
                            border: Border.all(color: AppColors.borderSubtle),
                          ),
                          child: Row(
                            children: [
                              GestureDetector(
                                onTap: () => cart.updateQuantity(item.id, -1),
                                child: const Padding(
                                  padding: EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                                  child: Icon(Icons.remove, size: 16, color: AppColors.primary),
                                ),
                              ),
                              Text(
                                '${item.quantity}',
                                style: const TextStyle(fontSize: 13, fontWeight: FontWeight.bold),
                              ),
                              GestureDetector(
                                onTap: () => cart.updateQuantity(item.id, 1),
                                child: const Padding(
                                  padding: EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                                  child: Icon(Icons.add, size: 16, color: AppColors.primary),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                    const Divider(color: AppColors.borderSubtle, height: 20),
                  ],
                ],
              ),
            ),

            const SizedBox(height: 16),

            _buildFreeDeliveryBar(cart),

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
                    'Delivery Details',
                    style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: _nameController,
                    decoration: const InputDecoration(
                      labelText: 'Full Name *',
                      prefixIcon: Icon(Icons.person_outline, color: AppColors.textMuted),
                    ),
                    validator: (v) => v == null || v.trim().isEmpty ? 'Please enter your name' : null,
                  ),
                  const SizedBox(height: 10),
                  TextFormField(
                    controller: _phoneController,
                    keyboardType: TextInputType.phone,
                    onEditingComplete: () => _loadSavedAddresses(_phoneController.text.trim()),
                    decoration: const InputDecoration(
                      labelText: 'Mobile Phone *',
                      prefixIcon: Icon(Icons.phone_outlined, color: AppColors.textMuted),
                    ),
                    validator: (v) =>
                        v == null || v.trim().length < 8 ? 'Please enter a valid phone number' : null,
                  ),
                  if (cart.orderType == OrderType.delivery) ...[
                    const SizedBox(height: 12),
                    _buildDeliveryAreas(cart),
                    _buildSavedAddressChips(),
                    const SizedBox(height: 4),
                    _buildGpsRow(cart),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: _addressController,
                      maxLines: 2,
                      decoration: const InputDecoration(
                        labelText: 'Delivery Address *',
                        prefixIcon: Icon(Icons.home_outlined, color: AppColors.textMuted),
                      ),
                      validator: (v) =>
                          v == null || v.trim().isEmpty ? 'Please enter delivery address' : null,
                    ),
                    const SizedBox(height: 10),
                    TextFormField(
                      controller: _landmarkController,
                      decoration: const InputDecoration(
                        labelText: 'Nearby Landmark (Optional)',
                        prefixIcon: Icon(Icons.pin_drop_outlined, color: AppColors.textMuted),
                      ),
                    ),
                  ],
                  const SizedBox(height: 10),
                  TextFormField(
                    controller: _notesController,
                    decoration: const InputDecoration(
                      labelText: 'Order Notes (Optional)',
                      prefixIcon: Icon(Icons.note_alt_outlined, color: AppColors.textMuted),
                    ),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 16),

            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: AppColors.cardBackground,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: AppColors.borderSubtle),
              ),
              child: Column(
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text('Subtotal', style: TextStyle(color: AppColors.textSecondary)),
                      Text('Rs. ${cart.subtotal.toStringAsFixed(0)}',
                          style: const TextStyle(fontWeight: FontWeight.w600)),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text('Delivery Fee', style: TextStyle(color: AppColors.textSecondary)),
                      Text(
                        cart.deliveryFee == 0 ? 'FREE' : 'Rs. ${cart.deliveryFee.toStringAsFixed(0)}',
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          color: cart.deliveryFee == 0 ? Colors.green : AppColors.textPrimary,
                        ),
                      ),
                    ],
                  ),
                  if (cart.orderType == OrderType.delivery && cart.minimumOrderAmount > 0) ...[
                    const SizedBox(height: 8),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text('Minimum order', style: TextStyle(color: AppColors.textSecondary)),
                        Text(
                          'Rs. ${cart.minimumOrderAmount.toStringAsFixed(0)}',
                          style: TextStyle(
                            fontWeight: FontWeight.w600,
                            color: cart.meetsMinimumOrder ? Colors.green : AppColors.badgeRed,
                          ),
                        ),
                      ],
                    ),
                  ],
                  const Divider(color: AppColors.borderSubtle, height: 20),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text(
                        'Total',
                        style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
                      ),
                      Text(
                        'Rs. ${cart.grandTotal.toStringAsFixed(0)}',
                        style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: AppColors.primary),
                      ),
                    ],
                  ),
                ],
              ),
            ),

            const SizedBox(height: 24),

            AppButton(
              text: 'Place Order • Rs. ${cart.grandTotal.toStringAsFixed(0)}',
              isLoading: _isSubmitting,
              onPressed: _placeOrder,
            ),
            const SizedBox(height: 20),
          ],
        ),
      ),
    );
  }
}