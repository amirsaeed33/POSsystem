import 'package:flutter/material.dart';
import '../../core/constants/app_colors.dart';
import '../../services/session_service.dart';
import '../../services/dashboard_service.dart';

class AdminDashboardScreen extends StatefulWidget {
  final SessionService sessionService;
  final DashboardService dashboardService;

  const AdminDashboardScreen({
    super.key,
    required this.sessionService,
    required this.dashboardService,
  });

  @override
  State<AdminDashboardScreen> createState() => _AdminDashboardScreenState();
}

class _AdminDashboardScreenState extends State<AdminDashboardScreen> {
  bool _loading = true;
  DashboardDto? _dash;
  OnlineOrderPipelineDto? _pipeline;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final dash = await widget.dashboardService.getDashboard();
    final pipe = await widget.dashboardService.getOnlineOrderPipeline(
      branchId: widget.sessionService.branchId,
    );
    if (!mounted) return;
    setState(() {
      _dash = dash;
      _pipeline = pipe;
      _loading = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.pageBackground,
      appBar: AppBar(
        title: const Text('Admin Overview'),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _load),
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () async {
              await widget.sessionService.logout();
              if (context.mounted) Navigator.of(context).pop();
            },
          ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                padding: const EdgeInsets.all(20),
                children: [
                  if (_dash?.userDisplayName != null)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: Text(
                        'Welcome, ${_dash!.userDisplayName}',
                        style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
                      ),
                    ),
                  Row(
                    children: [
                      Expanded(
                        child: _metric(
                          "Today's Sales",
                          'Rs. ${(_dash?.todaySales ?? 0).toStringAsFixed(0)}',
                          Icons.payments_outlined,
                          AppColors.primary,
                        ),
                      ),
                      const SizedBox(width: 14),
                      Expanded(
                        child: _metric(
                          'Orders Today',
                          '${_dash?.todaySalesCount ?? 0}',
                          Icons.receipt_long_outlined,
                          const Color(0xFF1976D2),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 14),
                  Row(
                    children: [
                      Expanded(
                        child: _metric(
                          'Today Profit',
                          'Rs. ${(_dash?.todayProfit ?? 0).toStringAsFixed(0)}',
                          Icons.trending_up,
                          const Color(0xFF2E7D32),
                        ),
                      ),
                      const SizedBox(width: 14),
                      Expanded(
                        child: _metric(
                          'Low Stock',
                          '${_dash?.lowStockCount ?? 0}',
                          Icons.warning_amber_outlined,
                          const Color(0xFFD9822B),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 24),
                  const Text('Online Order Pipeline', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                  const SizedBox(height: 12),
                  if (_pipeline != null)
                    Wrap(
                      spacing: 10,
                      runSpacing: 10,
                      children: [
                        _chip('Placed', _pipeline!.placed),
                        _chip('Preparing', _pipeline!.preparing),
                        _chip('On Route', _pipeline!.outForDelivery),
                        _chip('Delivered', _pipeline!.delivered),
                      ],
                    )
                  else
                    const Text('Pipeline unavailable', style: TextStyle(color: AppColors.textMuted)),
                ],
              ),
            ),
    );
  }

  Widget _chip(String label, int count) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.borderSubtle),
      ),
      child: Text('$label: $count', style: const TextStyle(fontWeight: FontWeight.w600)),
    );
  }

  Widget _metric(String title, String value, IconData icon, Color color) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.borderSubtle),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: color),
          const SizedBox(height: 10),
          Text(value, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
          const SizedBox(height: 4),
          Text(title, style: const TextStyle(fontSize: 12, color: AppColors.textMuted)),
        ],
      ),
    );
  }
}
