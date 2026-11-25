import 'package:flutter/material.dart';
import '../../core/constants/app_colors.dart';
import '../../models/order_models.dart';
import '../../services/online_catalog_service.dart';
import 'order_tracking_screen.dart';

class OrderHistoryTab extends StatefulWidget {
  final OnlineCatalogService catalogService;
  final int? branchId;
  final String? initialPhone;
  final bool isRestaurant;

  const OrderHistoryTab({
    super.key,
    required this.catalogService,
    this.branchId,
    this.initialPhone,
    this.isRestaurant = true,
  });

  @override
  State<OrderHistoryTab> createState() => _OrderHistoryTabState();
}

class _OrderHistoryTabState extends State<OrderHistoryTab> {
  bool _isLoading = false;
  List<CustomerOrderDto> _orders = [];
  final _phoneController = TextEditingController();

  @override
  void initState() {
    super.initState();
    if (widget.initialPhone != null && widget.initialPhone!.isNotEmpty) {
      _phoneController.text = widget.initialPhone!;
      _loadOrders();
    }
  }

  @override
  void dispose() {
    _phoneController.dispose();
    super.dispose();
  }

  Future<void> _loadOrders() async {
    final phone = _phoneController.text.trim();
    if (phone.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Enter the phone number used for orders')),
      );
      return;
    }
    setState(() => _isLoading = true);
    final orders = await widget.catalogService.getOrdersByPhone(
      phone,
      branchId: widget.branchId,
    );
    if (!mounted) return;
    setState(() {
      _orders = orders;
      _isLoading = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.pageBackground,
      appBar: AppBar(title: const Text('Order History')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _phoneController,
                    keyboardType: TextInputType.phone,
                    decoration: const InputDecoration(
                      labelText: 'Phone number',
                      hintText: '03XXXXXXXXX',
                    ),
                    onSubmitted: (_) => _loadOrders(),
                  ),
                ),
                const SizedBox(width: 8),
                ElevatedButton(onPressed: _isLoading ? null : _loadOrders, child: const Text('Find')),
              ],
            ),
          ),
          Expanded(
            child: _isLoading
                ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
                : _orders.isEmpty
                    ? const Center(child: Text('No orders found for this phone'))
                    : RefreshIndicator(
                        onRefresh: _loadOrders,
                        child: ListView.separated(
                          padding: const EdgeInsets.all(16),
                          itemCount: _orders.length,
                          separatorBuilder: (_, __) => const SizedBox(height: 12),
                          itemBuilder: (_, i) {
                            final o = _orders[i];
                            return ListTile(
                              tileColor: AppColors.cardBackground,
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(14),
                                side: const BorderSide(color: AppColors.borderSubtle),
                              ),
                              title: Text(o.displayOrderNo, style: const TextStyle(fontWeight: FontWeight.bold)),
                              subtitle: Text('${o.statusText} · ${o.orderDate.toLocal()}'),
                              trailing: Text(o.priceDisplay, style: const TextStyle(color: AppColors.primary)),
                              onTap: () {
                                Navigator.of(context).push(
                                  MaterialPageRoute(
                                    builder: (_) => OrderTrackingScreen(
                                      orderId: o.id,
                                      orderNo: o.displayOrderNo,
                                      catalogService: widget.catalogService,
                                      isRestaurant: widget.isRestaurant,
                                    ),
                                  ),
                                );
                              },
                            );
                          },
                        ),
                      ),
          ),
        ],
      ),
    );
  }
}
