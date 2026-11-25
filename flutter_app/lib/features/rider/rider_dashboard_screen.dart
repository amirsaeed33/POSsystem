import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../core/theme/staff_theme.dart';
import '../../models/rider_models.dart';
import '../../services/session_service.dart';
import '../../services/rider_service.dart';

/// Rider home — web-parity: stats, accept / decline / deliver, maps, 25s poll.
class RiderDashboardScreen extends StatefulWidget {
  final SessionService sessionService;
  final RiderService riderService;
  final Future<void> Function()? onLogout;
  final bool embedded;

  const RiderDashboardScreen({
    super.key,
    required this.sessionService,
    required this.riderService,
    this.onLogout,
    this.embedded = false,
  });

  @override
  State<RiderDashboardScreen> createState() => _RiderDashboardScreenState();
}

class _RiderDashboardScreenState extends State<RiderDashboardScreen> {
  static const _rejectPresets = [
    'Bike Breakdown / Flat Tyre',
    'Customer Address Too Far / Out of Reach',
    'Emergency / Unable to Deliver',
    'Order Package Damaged / Issue',
    'Other Reason',
  ];

  bool _isLoading = true;
  final Set<int> _busyIds = {};
  List<RiderOrderDto> _orders = [];
  RiderDashboardStatsDto? _stats;
  Timer? _poll;
  Timer? _clock;
  DateTime _now = DateTime.now();

  List<RiderOrderDto> get _newOrders => _orders.where((o) => o.isNewOrder).toList();
  List<RiderOrderDto> get _activeOrders => _orders.where((o) => !o.isNewOrder).toList();

  @override
  void initState() {
    super.initState();
    _load();
    _poll = Timer.periodic(const Duration(seconds: 25), (_) => _load(silent: true));
    _clock = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) setState(() => _now = DateTime.now());
    });
  }

  @override
  void dispose() {
    _poll?.cancel();
    _clock?.cancel();
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

  void _toast(String msg, {bool error = false}) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(msg),
        backgroundColor: error ? Colors.red.shade700 : StaffTheme.riderRoute,
      ),
    );
  }

  Future<void> _call(String phone) async {
    if (phone.trim().isEmpty) return;
    final uri = Uri.parse('tel:$phone');
    if (await canLaunchUrl(uri)) await launchUrl(uri);
  }

  Future<void> _openMaps(RiderOrderDto order) async {
    Uri? uri;
    if (order.hasCoords) {
      uri = Uri.parse(
        'https://www.google.com/maps/dir/?api=1&destination=${order.latitude},${order.longitude}',
      );
    } else if (order.deliveryAddress.trim().isNotEmpty) {
      uri = Uri.parse(
        'https://www.google.com/maps/dir/?api=1&destination=${Uri.encodeComponent(order.deliveryAddress)}',
      );
    }
    if (uri == null) {
      _toast('No location available', error: true);
      return;
    }
    if (await canLaunchUrl(uri)) {
      await launchUrl(uri, mode: LaunchMode.externalApplication);
    }
  }

  Future<void> _copyCoords(RiderOrderDto order) async {
    if (!order.hasCoords) return;
    await Clipboard.setData(ClipboardData(text: '${order.latitude},${order.longitude}'));
    if (!mounted) return;
    _toast('Coordinates copied');
  }

  Future<void> _accept(RiderOrderDto order) async {
    setState(() => _busyIds.add(order.id));
    final updated = await widget.riderService.acceptOrder(order.id);
    if (!mounted) return;
    setState(() => _busyIds.remove(order.id));
    _toast(
      updated != null
          ? 'Order ${order.orderNo} accepted. Ready for delivery!'
          : 'Could not accept this order.',
      error: updated == null,
    );
    await _load(silent: true);
  }

  Future<void> _showRejectDialog(RiderOrderDto order) async {
    var reason = '';
    final controller = TextEditingController();
    final confirmed = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) {
        return StatefulBuilder(
          builder: (ctx, setModal) {
            return Padding(
              padding: EdgeInsets.only(
                left: 20,
                right: 20,
                top: 16,
                bottom: MediaQuery.of(ctx).viewInsets.bottom + 20,
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text('Decline assignment', style: StaffTheme.title(size: 18)),
                  const SizedBox(height: 4),
                  Text(
                    'Order ${order.orderNo} will return to admin for reassignment.',
                    style: StaffTheme.body(size: 13),
                  ),
                  const SizedBox(height: 14),
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: _rejectPresets.map((preset) {
                      final selected = reason == preset;
                      return ChoiceChip(
                        label: Text(preset, style: TextStyle(fontSize: 12)),
                        selected: selected,
                        selectedColor: StaffTheme.riderRoute.withValues(alpha: 0.2),
                        onSelected: (_) {
                          setModal(() {
                            reason = preset;
                            controller.text = preset;
                          });
                        },
                      );
                    }).toList(),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: controller,
                    maxLines: 2,
                    decoration: const InputDecoration(
                      labelText: 'Reason',
                      hintText: 'Optional custom reason',
                    ),
                    onChanged: (v) => reason = v,
                  ),
                  const SizedBox(height: 16),
                  Row(
                    children: [
                      Expanded(
                        child: OutlinedButton(
                          onPressed: () => Navigator.pop(ctx, false),
                          child: const Text('Cancel'),
                        ),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: FilledButton(
                          style: FilledButton.styleFrom(backgroundColor: Colors.red.shade700),
                          onPressed: () => Navigator.pop(ctx, true),
                          child: const Text('Decline'),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            );
          },
        );
      },
    );

    if (confirmed != true || !mounted) return;
    final finalReason = reason.trim().isEmpty ? 'Declined by rider' : reason.trim();
    setState(() => _busyIds.add(order.id));
    final updated = await widget.riderService.rejectOrder(order.id, finalReason);
    if (!mounted) return;
    setState(() => _busyIds.remove(order.id));
    _toast(
      updated != null
          ? 'Assignment declined and returned to admin.'
          : 'Could not decline assignment.',
      error: updated == null,
    );
    await _load(silent: true);
  }

  Future<void> _showCompleteDialog(RiderOrderDto order) async {
    final notesCtrl = TextEditingController();
    int? rating;

    final confirmed = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) {
        return StatefulBuilder(
          builder: (ctx, setModal) {
            return Padding(
              padding: EdgeInsets.only(
                left: 20,
                right: 20,
                top: 16,
                bottom: MediaQuery.of(ctx).viewInsets.bottom + 20,
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text('Mark as delivered', style: StaffTheme.title(size: 18)),
                  const SizedBox(height: 4),
                  Text(
                    'Order ${order.orderNo} · Admin will complete payment.',
                    style: StaffTheme.body(size: 13),
                  ),
                  const SizedBox(height: 14),
                  Text('Delivery rating (optional)', style: StaffTheme.body(size: 12)),
                  const SizedBox(height: 6),
                  Row(
                    children: List.generate(5, (i) {
                      final star = i + 1;
                      return IconButton(
                        onPressed: () => setModal(() => rating = star),
                        icon: Icon(
                          rating != null && rating! >= star ? Icons.star : Icons.star_border,
                          color: const Color(0xFFE0A45A),
                        ),
                      );
                    }),
                  ),
                  TextField(
                    controller: notesCtrl,
                    maxLines: 2,
                    decoration: const InputDecoration(
                      labelText: 'Notes (optional)',
                      hintText: 'e.g. handed to guard',
                    ),
                  ),
                  const SizedBox(height: 16),
                  Row(
                    children: [
                      Expanded(
                        child: OutlinedButton(
                          onPressed: () => Navigator.pop(ctx, false),
                          child: const Text('Cancel'),
                        ),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: FilledButton(
                          style: FilledButton.styleFrom(backgroundColor: StaffTheme.riderWater),
                          onPressed: () => Navigator.pop(ctx, true),
                          child: const Text('Delivered'),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            );
          },
        );
      },
    );

    if (confirmed != true || !mounted) return;
    setState(() => _busyIds.add(order.id));
    final updated = await widget.riderService.completeDelivery(
      order.id,
      notes: notesCtrl.text,
      rating: rating,
    );
    if (!mounted) return;
    setState(() => _busyIds.remove(order.id));
    _toast(
      updated != null
          ? 'Order ${order.orderNo} marked as Delivered.'
          : 'Could not complete delivery.',
      error: updated == null,
    );
    await _load(silent: true);
  }

  String _elapsedLabel(RiderOrderDto order) {
    final mins = _now.difference(order.riderAssignedTime ?? order.orderDate).inMinutes;
    if (mins < 1) return 'Just now';
    if (mins < 60) return '${mins}m';
    final h = mins ~/ 60;
    final m = mins % 60;
    return '${h}h ${m}m';
  }

  @override
  Widget build(BuildContext context) {
    final riderName = _stats?.riderName ?? widget.sessionService.currentUser?.displayName ?? 'Rider';
    return Scaffold(
      backgroundColor: StaffTheme.riderPaper,
      body: _isLoading
          ? const Center(child: CircularProgressIndicator(color: StaffTheme.riderRoute))
          : RefreshIndicator(
              color: StaffTheme.riderRoute,
              onRefresh: _load,
              child: CustomScrollView(
                physics: const AlwaysScrollableScrollPhysics(),
                slivers: [
                  SliverToBoxAdapter(child: _mapHero(riderName)),
                  SliverPadding(
                    padding: const EdgeInsets.fromLTRB(16, 0, 16, 28),
                    sliver: SliverList(
                      delegate: SliverChildListDelegate([
                        Row(
                          children: [
                            Expanded(
                              child: StaffMetricTile(
                                label: 'Active deliveries',
                                value: '${_stats?.activeAssignedCount ?? _orders.length}',
                                icon: Icons.alt_route,
                                accent: StaffTheme.riderRoute,
                              ),
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: StaffMetricTile(
                                label: 'Delivered today',
                                value: '${_stats?.todayDeliveredCount ?? 0}',
                                icon: Icons.flag_circle_outlined,
                                accent: StaffTheme.riderWater,
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 10),
                        Row(
                          children: [
                            Expanded(
                              child: StaffMetricTile(
                                label: 'Collected today',
                                value:
                                    'Rs ${(_stats?.todayDeliveredAmount ?? 0).toStringAsFixed(0)}',
                                icon: Icons.payments_outlined,
                                accent: const Color(0xFFE0A45A),
                              ),
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: StaffMetricTile(
                                label: 'Pending amount',
                                value:
                                    'Rs ${(_stats?.activePendingAmount ?? 0).toStringAsFixed(0)}',
                                icon: Icons.hourglass_bottom,
                                accent: StaffTheme.riderInk,
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 20),
                        if (_orders.isEmpty)
                          _emptyState()
                        else ...[
                          if (_newOrders.isNotEmpty) ...[
                            Text('New assignments',
                                style: StaffTheme.title(color: StaffTheme.riderInk, size: 17)),
                            const SizedBox(height: 10),
                            ..._newOrders.map(_routeCard),
                            const SizedBox(height: 8),
                          ],
                          if (_activeOrders.isNotEmpty) ...[
                            Text('On the way',
                                style: StaffTheme.title(color: StaffTheme.riderInk, size: 17)),
                            const SizedBox(height: 10),
                            ..._activeOrders.map(_routeCard),
                          ],
                        ],
                      ]),
                    ),
                  ),
                ],
              ),
            ),
    );
  }

  Widget _emptyState() {
    return Container(
      padding: const EdgeInsets.all(28),
      decoration: StaffTheme.softCard(),
      child: Column(
        children: [
          Icon(Icons.map_outlined, size: 42, color: StaffTheme.riderWater.withValues(alpha: 0.7)),
          const SizedBox(height: 10),
          Text('No active deliveries', style: StaffTheme.title(size: 16)),
          Text('New assignments will appear here', style: StaffTheme.body(size: 13)),
        ],
      ),
    );
  }

  Widget _mapHero(String name) {
    return Container(
      padding: EdgeInsets.fromLTRB(20, MediaQuery.of(context).padding.top + 12, 20, 24),
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF1E3A34), Color(0xFF2A7F62), Color(0xFF5B8FA8)],
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Text(
                'DELIVERY PORTAL',
                style: StaffTheme.body(color: Colors.white70, size: 11, weight: FontWeight.w700),
              ),
              const Spacer(),
              if (widget.onLogout != null)
                IconButton(
                  onPressed: () => widget.onLogout?.call(),
                  icon: const Icon(Icons.logout, color: Colors.white),
                  tooltip: 'Logout',
                ),
            ],
          ),
          Text('Hi, $name', style: StaffTheme.display(color: Colors.white, size: 28)),
          const SizedBox(height: 6),
          Text(
            _stats?.branchName ??
                widget.sessionService.currentUser?.branchName ??
                'My Dashboard',
            style: StaffTheme.body(color: Colors.white70),
          ),
          const SizedBox(height: 14),
          Container(
            width: double.infinity,
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: Colors.white24),
            ),
            child: Text(
              'Live assignments · refreshes every 25s',
              style: StaffTheme.body(color: Colors.white, size: 12),
            ),
          ),
        ],
      ),
    );
  }

  Widget _routeCard(RiderOrderDto order) {
    final busy = _busyIds.contains(order.id);
    final late = order.isLate;

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      decoration: StaffTheme.softCard(),
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          Container(
            width: double.infinity,
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
            color: order.isNewOrder
                ? const Color(0xFFE8F5F0)
                : late
                    ? const Color(0xFFFFF0E8)
                    : const Color(0xFFEEF4F8),
            child: Row(
              children: [
                Icon(
                  Icons.place,
                  size: 18,
                  color: order.isNewOrder
                      ? StaffTheme.riderRoute
                      : late
                          ? Colors.deepOrange
                          : StaffTheme.riderWater,
                ),
                const SizedBox(width: 6),
                Expanded(
                  child: Text(order.orderNo, style: StaffTheme.title(size: 15)),
                ),
                Text(
                  _elapsedLabel(order),
                  style: StaffTheme.body(
                    size: 11,
                    color: late ? Colors.deepOrange : StaffTheme.riderInk.withValues(alpha: 0.6),
                    weight: FontWeight.w700,
                  ),
                ),
                const SizedBox(width: 8),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Text(
                    order.badgeText,
                    style: StaffTheme.body(size: 11, weight: FontWeight.w700),
                  ),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(14),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(order.customerName, style: StaffTheme.title(size: 16)),
                const SizedBox(height: 4),
                Text(order.deliveryAddress, style: StaffTheme.body(size: 13)),
                if (order.deliveryAreaName != null && order.deliveryAreaName!.isNotEmpty) ...[
                  const SizedBox(height: 2),
                  Text(order.areaLine, style: StaffTheme.body(size: 12, weight: FontWeight.w600)),
                ],
                const SizedBox(height: 6),
                Text(order.itemsSummary, style: StaffTheme.body(size: 12)),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Text(
                      order.priceDisplay,
                      style: StaffTheme.title(color: StaffTheme.riderRoute, size: 16),
                    ),
                    const SizedBox(width: 10),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                      decoration: BoxDecoration(
                        color: StaffTheme.riderPaper,
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Text(order.paymentLabel, style: StaffTheme.body(size: 11)),
                    ),
                    if (order.distanceKm != null) ...[
                      const Spacer(),
                      Text(
                        '${order.distanceKm!.toStringAsFixed(1)} km',
                        style: StaffTheme.body(size: 12, weight: FontWeight.w700),
                      ),
                    ],
                  ],
                ),
                const SizedBox(height: 12),
                if (busy)
                  const Center(
                    child: Padding(
                      padding: EdgeInsets.all(8),
                      child: SizedBox(
                        width: 22,
                        height: 22,
                        child: CircularProgressIndicator(strokeWidth: 2, color: StaffTheme.riderRoute),
                      ),
                    ),
                  )
                else if (order.isNewOrder)
                  Row(
                    children: [
                      Expanded(
                        child: OutlinedButton(
                          onPressed: () => _showRejectDialog(order),
                          style: OutlinedButton.styleFrom(foregroundColor: Colors.red.shade700),
                          child: const Text('Decline'),
                        ),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: FilledButton(
                          onPressed: () => _accept(order),
                          style: FilledButton.styleFrom(
                            backgroundColor: StaffTheme.riderRoute,
                            foregroundColor: Colors.white,
                          ),
                          child: const Text('Accept'),
                        ),
                      ),
                    ],
                  )
                else
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      OutlinedButton.icon(
                        onPressed: () => _call(order.customerPhone),
                        icon: const Icon(Icons.phone, size: 18),
                        label: const Text('Call'),
                      ),
                      OutlinedButton.icon(
                        onPressed: () => _openMaps(order),
                        icon: const Icon(Icons.directions, size: 18),
                        label: const Text('Directions'),
                      ),
                      if (order.hasCoords)
                        OutlinedButton.icon(
                          onPressed: () => _copyCoords(order),
                          icon: const Icon(Icons.copy, size: 16),
                          label: const Text('GPS'),
                        ),
                      FilledButton(
                        onPressed: () => _showCompleteDialog(order),
                        style: FilledButton.styleFrom(
                          backgroundColor: StaffTheme.riderWater,
                          foregroundColor: Colors.white,
                        ),
                        child: const Text('Delivered'),
                      ),
                    ],
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
