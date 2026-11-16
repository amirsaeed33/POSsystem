import 'package:flutter/material.dart';
import '../../core/constants/app_colors.dart';
import '../../core/widgets/app_button.dart';
import '../../models/cart_models.dart';
import '../../models/order_models.dart';
import '../../services/cart_service.dart';
import '../../services/online_catalog_service.dart';
import 'order_tracking_screen.dart';

class CartScreen extends StatefulWidget {
  final CartService cartService;
  final OnlineCatalogService catalogService;
  final int? branchId;

  const CartScreen({
    super.key,
    required this.cartService,
    required this.catalogService,
    this.branchId,
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

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController(text: widget.cartService.customerName);
    _phoneController = TextEditingController(text: widget.cartService.customerPhone);
    _addressController = TextEditingController(text: widget.cartService.deliveryAddress);
    _landmarkController = TextEditingController(text: widget.cartService.deliveryLandmark);
    _notesController = TextEditingController(text: widget.cartService.orderNotes);
    widget.cartService.addListener(_onCartChanged);
  }

  @override
  void dispose() {
    _nameController.dispose();
    _phoneController.dispose();
    _addressController.dispose();
    _landmarkController.dispose();
    _notesController.dispose();
    widget.cartService.removeListener(_onCartChanged);
    super.dispose();
  }

  void _onCartChanged() {
    if (mounted) setState(() {});
  }

  Future<void> _placeOrder() async {
    if (!_formKey.currentState!.validate()) return;
    if (widget.cartService.items.isEmpty) return;

    setState(() => _isSubmitting = true);

    widget.cartService.setCustomerDetails(
      name: _nameController.text.trim(),
      phone: _phoneController.text.trim(),
      address: _addressController.text.trim(),
      landmark: _landmarkController.text.trim(),
      notes: _notesController.text.trim(),
    );

    try {
      final lines = widget.cartService.items
          .map((item) => CreateCustomerOrderLineDto(
                productId: item.productId,
                quantity: item.quantity.toDouble(),
                unitPrice: item.unitPrice,
              ))
          .toList();

      final dto = CreateCustomerOrderDto(
        branchId: widget.branchId,
        customerName: _nameController.text.trim(),
        customerMobile: _phoneController.text.trim(),
        deliveryAddress: widget.cartService.orderType == OrderType.delivery
            ? _addressController.text.trim()
            : 'Takeaway / Pickup',
        deliveryLandmark: _landmarkController.text.trim().isNotEmpty
            ? _landmarkController.text.trim()
            : null,
        notes: _notesController.text.trim().isNotEmpty ? _notesController.text.trim() : null,
        subtotal: widget.cartService.subtotal,
        deliveryCharges: widget.cartService.deliveryFee,
        grandTotal: widget.cartService.grandTotal,
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

  @override
  Widget build(BuildContext context) {
    final cart = widget.cartService;

    if (cart.items.isEmpty) {
      return Scaffold(
        backgroundColor: AppColors.pageBackground,
        appBar: AppBar(title: const Text('My Cart')),
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
              const Text(
                'Browse delicious food and add items to your cart!',
                style: TextStyle(fontSize: 13, color: AppColors.textSecondary),
              ),
              const SizedBox(height: 24),
              AppButton(
                text: 'Explore Menu',
                width: 160,
                onPressed: () => Navigator.of(context).pop(),
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
            // Order Type Selector (Delivery vs Takeaway)
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

            // Cart Items List
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

                        // Stepper
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

            // Customer Details Form
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
                    decoration: const InputDecoration(
                      labelText: 'Mobile Phone *',
                      prefixIcon: Icon(Icons.phone_outlined, color: AppColors.textMuted),
                    ),
                    validator: (v) =>
                        v == null || v.trim().length < 8 ? 'Please enter a valid phone number' : null,
                  ),
                  if (cart.orderType == OrderType.delivery) ...[
                    const SizedBox(height: 10),
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

            // Bill Breakdown
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

            // Submit Button
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

