import 'package:flutter/material.dart';
import '../../core/constants/app_colors.dart';
import '../../models/waiter_models.dart';
import '../../services/session_service.dart';
import '../../services/waiter_service.dart';

class WaiterMyOrdersScreen extends StatefulWidget {
  final SessionService sessionService;
  final WaiterService waiterService;

  const WaiterMyOrdersScreen({
    super.key,
    required this.sessionService,
    required this.waiterService,
  });

  @override
  State<WaiterMyOrdersScreen> createState() => _WaiterMyOrdersScreenState();
}

class _WaiterMyOrdersScreenState extends State<WaiterMyOrdersScreen> {
  bool _loading = true;
  List<RestaurantOrderDto> _orders = [];

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final branchId = widget.sessionService.branchId;
    if (branchId == null) {
      setState(() {
        _loading = false;
        _orders = [];
      });
      return;
    }
    final orders = await widget.waiterService.getActiveOrders(branchId);
    if (!mounted) return;
    setState(() {
      _orders = orders;
      _loading = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.pageBackground,
      appBar: AppBar(
        title: const Text('Active Orders'),
        actions: [IconButton(icon: const Icon(Icons.refresh), onPressed: _load)],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
          : _orders.isEmpty
              ? const Center(child: Text('No active restaurant orders'))
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView.separated(
                    padding: const EdgeInsets.all(16),
                    itemCount: _orders.length,
                    separatorBuilder: (_, __) => const SizedBox(height: 10),
                    itemBuilder: (_, i) {
                      final o = _orders[i];
                      return Container(
                        padding: const EdgeInsets.all(14),
                        decoration: BoxDecoration(
                          color: AppColors.cardBackground,
                          borderRadius: BorderRadius.circular(14),
                          border: Border.all(color: AppColors.borderSubtle),
                        ),
                        child: Row(
                          children: [
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(o.orderNo, style: const TextStyle(fontWeight: FontWeight.bold)),
                                  Text(o.tableName ?? 'Table ${o.tableId ?? '-'}',
                                      style: const TextStyle(color: AppColors.textMuted, fontSize: 13)),
                                  Text('${o.items.length} items · ${o.covers} covers',
                                      style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
                                ],
                              ),
                            ),
                            Text(
                              'Rs. ${o.grandTotal.toStringAsFixed(0)}',
                              style: const TextStyle(fontWeight: FontWeight.bold, color: AppColors.primary),
                            ),
                          ],
                        ),
                      );
                    },
                  ),
                ),
    );
  }
}
