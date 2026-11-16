import 'dart:async';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../core/constants/app_colors.dart';
import '../../models/rider_models.dart';
import '../../services/session_service.dart';
import '../../services/rider_service.dart';
import 'rider_history_screen.dart';

class RiderDashboardScreen extends StatefulWidget {
  final SessionService sessionService;
  final RiderService riderService;

  const RiderDashboardScreen({
    super.key,
    required this.sessionService,
    required this.riderService,
  });

  @override
  State<RiderDashboardScreen> createState() => _RiderDashboardScreenState();
}

class _RiderDashboardScreenState extends State<RiderDashboardScreen> {
  bool _isLoading = true;
  List<RiderOrderDto> _orders = [];
  RiderDashboardStatsDto? _stats;
  Timer? _poll;

  @override
  void initState() {
    super.initState();
    _load();
    _poll = Timer.periodic(const Duration(seconds: 25), (_) => _load(silent: true));
  }

  @override
  void dispose() {
    _poll?.cancel();
    super.dispose();
  }

  Future<void> _load({bool silent = false}) async {
    if (!silent) setState(() => _isLoading = true);
    final stats = await widget.riderService.getDashboardStats();
    final orders = await widget.riderService.getAssignedOrders();
    if (!mounted) return;
    setState(() {
      _stats = stats;
      _orders = orders;
      _isLoading = false;
    });
  }

  Future<void> _call(String phone) async {
    if (phone.trim().isEmpty) return;
    final uri = Uri.parse('tel:$phone');
    if (await canLaunchUrl(uri)) await launchUrl(uri);
  }

  Future<void> _accept(RiderOrderDto order) async {
    final updated = await widget.riderService.acceptOrder(order.id);
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(updated != null ? 'Order accepted' : 'Accept failed')),
    );
    _load();
  }

  Future<void> _complete(RiderOrderDto order) async {
    final updated = await widget.riderService.completeDelivery(order.id);
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(updated != null ? 'Delivery completed' : 'Complete failed')),
    );
    _load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.pageBackground,
      appBar: AppBar(
        title: const Text('Rider Deliveries'),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _load),
          IconButton(
            icon: const Icon(Icons.history),
            onPressed: () {
              Navigator.of(context).push(
                MaterialPageRoute(
                  builder: (_) => RiderHistoryScreen(
                    sessionService: widget.sessionService,
                    riderService: widget.riderService,
                  ),
                ),
              );
            },
          ),
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () async {
              await widget.sessionService.logout();
              if (context.mounted) Navigator.of(context).pop();
            },
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  if (_stats != null) ...[
                    Row(
                      children: [
                        Expanded(
                          child: _statCard('Active', '${_stats!.activeAssignedCount}', Icons.delivery_dining),
                        ),
                        const SizedBox(width: 10),
                        Expanded(
                          child: _statCard('Delivered', '${_stats!.todayDeliveredCount}', Icons.check_circle_outline),
                        ),
                      ],
                    ),
                    const SizedBox(height: 16),
                  ],
                  if (_orders.isEmpty)
                    const Padding(
                      padding: EdgeInsets.only(top: 48),
                      child: Center(child: Text('No active deliveries assigned')),
                    )
                  else
                    ..._orders.map(_orderCard),
                ],
              ),
            ),
    );
  }

  Widget _statCard(String title, String value, IconData icon) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.borderSubtle),
      ),
      child: Row(
        children: [
          Icon(icon, color: AppColors.primary),
          const SizedBox(width: 10),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(value, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
              Text(title, style: const TextStyle(fontSize: 12, color: AppColors.textMuted)),
            ],
          ),
        ],
      ),
    );
  }

  Widget _orderCard(RiderOrderDto order) {
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.borderSubtle),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Text(order.orderNo, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
              const Spacer(),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: order.isNewOrder ? AppColors.primaryLight : const Color(0xFFE8F5E9),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Text(order.badgeText, style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w600)),
              ),
            ],
          ),
          const SizedBox(height: 6),
          Text(order.customerName, style: const TextStyle(fontWeight: FontWeight.w600)),
          Text(order.deliveryAddress, style: const TextStyle(color: AppColors.textSecondary, fontSize: 13)),
          const SizedBox(height: 4),
          Text(order.priceDisplay, style: const TextStyle(color: AppColors.primary, fontWeight: FontWeight.bold)),
          const SizedBox(height: 10),
          Row(
            children: [
              IconButton(
                onPressed: () => _call(order.customerPhone),
                icon: const Icon(Icons.phone, color: AppColors.primary),
              ),
              const Spacer(),
              if (order.isNewOrder)
                ElevatedButton(onPressed: () => _accept(order), child: const Text('Accept'))
              else
                ElevatedButton(onPressed: () => _complete(order), child: const Text('Delivered')),
            ],
          ),
        ],
      ),
    );
  }
}
