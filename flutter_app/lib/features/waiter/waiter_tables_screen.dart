import 'package:flutter/material.dart';
import '../../core/theme/staff_theme.dart';
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
          _error = 'No branch assigned. Please re-login.';
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
        return StaffTheme.waiterSage;
      case 1:
        return StaffTheme.waiterAccent;
      case 2:
        return const Color(0xFF3D6FA8);
      case 3:
        return const Color(0xFF7B4B8A);
      default:
        return Colors.grey;
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
          backgroundColor: StaffTheme.waiterCard,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
          title: Text('Open ${table.tableNumber}', style: StaffTheme.title()),
          content: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              IconButton(
                onPressed: () => setDialogState(() {
                  if (covers > 1) covers--;
                }),
                icon: const Icon(Icons.remove_circle_outline),
              ),
              Text('$covers guests', style: StaffTheme.title(size: 18)),
              IconButton(
                onPressed: () => setDialogState(() => covers++),
                icon: const Icon(Icons.add_circle_outline),
              ),
            ],
          ),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
            FilledButton(
              style: FilledButton.styleFrom(backgroundColor: StaffTheme.waiterAccent),
              onPressed: () => Navigator.pop(ctx, true),
              child: const Text('Seat guests'),
            ),
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
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Failed: $e')));
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
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('No active order')));
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
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('$e')));
    }
  }

  @override
  Widget build(BuildContext context) {
    final available = _tables.where((t) => t.isAvailable).length;
    final occupied = _tables.where((t) => t.isOccupied || t.isBilled).length;
    return Scaffold(
      backgroundColor: StaffTheme.waiterPaper,
      body: _isLoading
          ? const Center(child: CircularProgressIndicator(color: StaffTheme.waiterAccent))
          : RefreshIndicator(
              color: StaffTheme.waiterAccent,
              onRefresh: _loadTables,
              child: CustomScrollView(
                physics: const AlwaysScrollableScrollPhysics(),
                slivers: [
                  SliverToBoxAdapter(
                    child: Container(
                      padding: EdgeInsets.fromLTRB(20, MediaQuery.of(context).padding.top + 14, 20, 22),
                      decoration: StaffTheme.heroWash(colors: const [
                        Color(0xFF2A1810),
                        Color(0xFFC84B11),
                        Color(0xFFE89B3C),
                      ]),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Text('FLOOR', style: StaffTheme.body(color: Colors.white70, size: 11, weight: FontWeight.w700)),
                              const Spacer(),
                              IconButton(
                                onPressed: () => Navigator.of(context).push(
                                  MaterialPageRoute(
                                    builder: (_) => WaiterMyOrdersScreen(
                                      sessionService: widget.sessionService,
                                      waiterService: widget.waiterService,
                                    ),
                                  ),
                                ),
                                icon: const Icon(Icons.receipt_long, color: Colors.white),
                              ),
                              IconButton(
                                onPressed: () async {
                                  await widget.sessionService.logout();
                                  if (mounted) Navigator.of(context).pop();
                                },
                                icon: const Icon(Icons.logout, color: Colors.white),
                              ),
                            ],
                          ),
                          Text('Table service', style: StaffTheme.display(color: Colors.white, size: 32)),
                          Text(
                            widget.sessionService.currentUser?.displayName ?? 'Waiter station',
                            style: StaffTheme.body(color: Colors.white70),
                          ),
                          const SizedBox(height: 14),
                          Row(
                            children: [
                              _pill('$available free', StaffTheme.waiterSage),
                              const SizedBox(width: 8),
                              _pill('$occupied live', Colors.white24),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ),
                  if (_error != null)
                    SliverFillRemaining(
                      child: Center(
                        child: Padding(
                          padding: const EdgeInsets.all(24),
                          child: Column(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Text(_error!, textAlign: TextAlign.center),
                              const SizedBox(height: 12),
                              FilledButton(onPressed: _loadTables, child: const Text('Retry')),
                            ],
                          ),
                        ),
                      ),
                    )
                  else
                    SliverPadding(
                      padding: const EdgeInsets.all(16),
                      sliver: SliverGrid(
                        gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                          crossAxisCount: 2,
                          childAspectRatio: 1.12,
                          crossAxisSpacing: 12,
                          mainAxisSpacing: 12,
                        ),
                        delegate: SliverChildBuilderDelegate(
                          (_, i) {
                            final t = _tables[i];
                            final color = _statusColor(t.status);
                            return InkWell(
                              onTap: () => _onTableTap(t),
                              borderRadius: BorderRadius.circular(20),
                              child: Container(
                                decoration: StaffTheme.softCard(border: color.withValues(alpha: 0.35)),
                                padding: const EdgeInsets.all(14),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Row(
                                      children: [
                                        Text(t.tableNumber, style: StaffTheme.title(size: 18)),
                                        const Spacer(),
                                        Container(
                                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                          decoration: BoxDecoration(
                                            color: color.withValues(alpha: 0.12),
                                            borderRadius: BorderRadius.circular(20),
                                          ),
                                          child: Text(t.statusText,
                                              style: StaffTheme.body(color: color, size: 11, weight: FontWeight.w700)),
                                        ),
                                      ],
                                    ),
                                    const Spacer(),
                                    Text('${t.capacity} seats', style: StaffTheme.body(size: 12)),
                                    if (t.activeOrderAmount != null)
                                      Text(
                                        'Rs. ${t.activeOrderAmount!.toStringAsFixed(0)}',
                                        style: StaffTheme.title(color: StaffTheme.waiterAccent, size: 16),
                                      ),
                                  ],
                                ),
                              ),
                            );
                          },
                          childCount: _tables.length,
                        ),
                      ),
                    ),
                ],
              ),
            ),
    );
  }

  Widget _pill(String text, Color bg) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
        decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(20)),
        child: Text(text, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600, fontSize: 12)),
      );
}
