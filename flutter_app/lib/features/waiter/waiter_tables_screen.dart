import 'package:flutter/material.dart';
import '../../core/constants/app_colors.dart';
import '../../models/waiter_models.dart';
import '../../services/session_service.dart';
import '../../services/waiter_service.dart';
import 'waiter_my_orders_screen.dart';
import 'waiter_order_screen.dart';

class WaiterTablesScreen extends StatefulWidget {
  final SessionService sessionService;
  final WaiterService waiterService;

  const WaiterTablesScreen({
    super.key,
    required this.sessionService,
    required this.waiterService,
  });

  @override
  State<WaiterTablesScreen> createState() => _WaiterTablesScreenState();
}

class _WaiterTablesScreenState extends State<WaiterTablesScreen> {
  bool _isLoading = true;
  String? _error;
  List<RestaurantTableDto> _tables = [];

  int? get _branchId => widget.sessionService.branchId;

  @override
  void initState() {
    super.initState();
    _loadTables();
  }

  Future<void> _loadTables() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final branchId = _branchId;
      if (branchId == null || branchId <= 0) {
        setState(() {
          _isLoading = false;
          _error = 'No branch assigned to this waiter. Please re-login.';
          _tables = [];
        });
        return;
      }
      final tables = await widget.waiterService.getTables(branchId);
      if (!mounted) return;
      setState(() {
        _tables = tables;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _isLoading = false;
        _error = e.toString();
      });
    }
  }

  Color _statusColor(int status) {
    switch (status) {
      case 0:
        return const Color(0xFF2E7D32);
      case 1:
        return AppColors.primary;
      case 2:
        return const Color(0xFF1976D2);
      case 3:
        return const Color(0xFF7B1FA2);
      default:
        return AppColors.textMuted;
    }
  }

  Future<void> _onTableTap(RestaurantTableDto table) async {
    if (table.isAvailable) {
      await _openSession(table);
    } else {
      await _openOrder(table);
    }
  }

  Future<void> _openSession(RestaurantTableDto table) async {
    int covers = 2;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: Text('Open ${table.tableNumber}'),
          content: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              IconButton(
                onPressed: () => setDialogState(() {
                  if (covers > 1) covers--;
                }),
                icon: const Icon(Icons.remove_circle_outline),
              ),
              Text('$covers guests', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
              IconButton(
                onPressed: () => setDialogState(() => covers++),
                icon: const Icon(Icons.add_circle_outline),
              ),
            ],
          ),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
            ElevatedButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Open')),
          ],
        ),
      ),
    );
    if (confirmed != true) return;

    try {
      final order = await widget.waiterService.openTableSession(tableId: table.id, covers: covers);
      if (!mounted) return;
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => WaiterOrderScreen(
            sessionService: widget.sessionService,
            waiterService: widget.waiterService,
            table: table,
            order: order,
          ),
        ),
      );
      _loadTables();
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Failed to open table: $e'), backgroundColor: AppColors.badgeRed),
      );
    }
  }

  Future<void> _openOrder(RestaurantTableDto table) async {
    try {
      RestaurantOrderDto? order;
      if (table.activeOrderId != null) {
        order = await widget.waiterService.getOrder(table.activeOrderId!);
      }
      order ??= await widget.waiterService.getOrderByTable(table.id);
      if (!mounted) return;
      if (order == null) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('No active order found for this table')),
        );
        return;
      }
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => WaiterOrderScreen(
            sessionService: widget.sessionService,
            waiterService: widget.waiterService,
            table: table,
            order: order!,
          ),
        ),
      );
      _loadTables();
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('$e'), backgroundColor: AppColors.badgeRed),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.pageBackground,
      appBar: AppBar(
        title: const Text('Waiter Tables'),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _loadTables),
          IconButton(
            icon: const Icon(Icons.receipt_long_outlined),
            tooltip: 'My Orders',
            onPressed: () {
              Navigator.of(context).push(
                MaterialPageRoute(
                  builder: (_) => WaiterMyOrdersScreen(
                    sessionService: widget.sessionService,
                    waiterService: widget.waiterService,
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
          : _error != null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(_error!, textAlign: TextAlign.center),
                        const SizedBox(height: 12),
                        ElevatedButton(onPressed: _loadTables, child: const Text('Retry')),
                      ],
                    ),
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _loadTables,
                  child: GridView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: _tables.length,
                    gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: 2,
                      childAspectRatio: 1.15,
                      crossAxisSpacing: 12,
                      mainAxisSpacing: 12,
                    ),
                    itemBuilder: (_, i) {
                      final t = _tables[i];
                      final color = _statusColor(t.status);
                      return InkWell(
                        onTap: () => _onTableTap(t),
                        borderRadius: BorderRadius.circular(16),
                        child: Container(
                          decoration: BoxDecoration(
                            color: AppColors.cardBackground,
                            borderRadius: BorderRadius.circular(16),
                            border: Border.all(color: color.withValues(alpha: 0.45), width: 1.5),
                          ),
                          padding: const EdgeInsets.all(14),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  Text(
                                    t.tableNumber,
                                    style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                                  ),
                                  const Spacer(),
                                  Container(
                                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                    decoration: BoxDecoration(
                                      color: color.withValues(alpha: 0.12),
                                      borderRadius: BorderRadius.circular(20),
                                    ),
                                    child: Text(
                                      t.statusText,
                                      style: TextStyle(fontSize: 11, color: color, fontWeight: FontWeight.w600),
                                    ),
                                  ),
                                ],
                              ),
                              const Spacer(),
                              Text('${t.capacity} seats', style: const TextStyle(color: AppColors.textMuted)),
                              if (t.activeOrderAmount != null)
                                Text(
                                  'Rs. ${t.activeOrderAmount!.toStringAsFixed(0)}',
                                  style: const TextStyle(fontWeight: FontWeight.bold, color: AppColors.primary),
                                ),
                            ],
                          ),
                        ),
                      );
                    },
                  ),
                ),
    );
  }
}
