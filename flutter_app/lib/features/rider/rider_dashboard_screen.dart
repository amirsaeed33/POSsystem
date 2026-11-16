import 'dart:async';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../core/theme/staff_theme.dart';
import '../../models/rider_models.dart';
import '../../services/session_service.dart';
import '../../services/rider_service.dart';
import 'rider_history_screen.dart';

/// Rider home — OSM / map-route inspired delivery board.
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
      SnackBar(
        content: Text(updated != null ? 'Route accepted — ride safe' : 'Could not accept'),
        backgroundColor: StaffTheme.riderRoute,
      ),
    );
    _load();
  }

  Future<void> _complete(RiderOrderDto order) async {
    final updated = await widget.riderService.completeDelivery(order.id);
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(updated != null ? 'Delivered. Great work!' : 'Complete failed')),
    );
    _load();
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
                    padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                    sliver: SliverList(
                      delegate: SliverChildListDelegate([
                        Row(
                          children: [
                            Expanded(
                              child: StaffMetricTile(
                                label: 'Active routes',
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
                        const SizedBox(height: 18),
                        Text('Assigned drops', style: StaffTheme.title(color: StaffTheme.riderInk, size: 18)),
                        const SizedBox(height: 10),
                        if (_orders.isEmpty)
                          Container(
                            padding: const EdgeInsets.all(28),
                            decoration: StaffTheme.softCard(),
                            child: Column(
                              children: [
                                Icon(Icons.map_outlined, size: 42, color: StaffTheme.riderWater.withValues(alpha: 0.7)),
                                const SizedBox(height: 10),
                                Text('No active deliveries', style: StaffTheme.title(size: 16)),
                                Text('New routes will appear here', style: StaffTheme.body(size: 13)),
                              ],
                            ),
                          )
                        else
                          ..._orders.map(_routeCard),
                      ]),
                    ),
                  ),
                ],
              ),
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
              Text('RIDER MAP', style: StaffTheme.body(color: Colors.white70, size: 11, weight: FontWeight.w700)),
              const Spacer(),
              IconButton(
                onPressed: () => Navigator.of(context).push(
                  MaterialPageRoute(
                    builder: (_) => RiderHistoryScreen(
                      sessionService: widget.sessionService,
                      riderService: widget.riderService,
                    ),
                  ),
                ),
                icon: const Icon(Icons.history, color: Colors.white),
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
          Text('Hi, $name', style: StaffTheme.display(color: Colors.white, size: 30)),
          const SizedBox(height: 6),
          Text(
            _stats?.branchName ?? 'OpenStreet-style route board · live assignments',
            style: StaffTheme.body(color: Colors.white70),
          ),
          const SizedBox(height: 16),
          // Decorative "map grid" strip
          Container(
            height: 64,
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: Colors.white24),
            ),
            child: CustomPaint(
              painter: _MiniMapPainter(),
              child: const Center(
                child: Text('Live routes refreshing every 25s', style: TextStyle(color: Colors.white, fontSize: 12)),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _routeCard(RiderOrderDto order) {
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      decoration: StaffTheme.softCard(),
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          Container(
            width: double.infinity,
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
            color: order.isNewOrder ? const Color(0xFFE8F5F0) : const Color(0xFFEEF4F8),
            child: Row(
              children: [
                Icon(Icons.place, size: 18, color: order.isNewOrder ? StaffTheme.riderRoute : StaffTheme.riderWater),
                const SizedBox(width: 6),
                Text(order.orderNo, style: StaffTheme.title(size: 15)),
                const Spacer(),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Text(order.badgeText, style: StaffTheme.body(size: 11, weight: FontWeight.w700)),
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
                const SizedBox(height: 8),
                Text(order.priceDisplay, style: StaffTheme.title(color: StaffTheme.riderRoute, size: 16)),
                const SizedBox(height: 12),
                Row(
                  children: [
                    OutlinedButton.icon(
                      onPressed: () => _call(order.customerPhone),
                      icon: const Icon(Icons.phone, size: 18),
                      label: const Text('Call'),
                      style: OutlinedButton.styleFrom(foregroundColor: StaffTheme.riderInk),
                    ),
                    const Spacer(),
                    FilledButton(
                      onPressed: () => order.isNewOrder ? _accept(order) : _complete(order),
                      style: FilledButton.styleFrom(
                        backgroundColor: order.isNewOrder ? StaffTheme.riderRoute : StaffTheme.riderWater,
                        foregroundColor: Colors.white,
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      ),
                      child: Text(order.isNewOrder ? 'Accept route' : 'Mark delivered'),
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

class _MiniMapPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final grid = Paint()
      ..color = Colors.white.withValues(alpha: 0.12)
      ..strokeWidth = 1;
    for (double x = 0; x < size.width; x += 18) {
      canvas.drawLine(Offset(x, 0), Offset(x, size.height), grid);
    }
    for (double y = 0; y < size.height; y += 18) {
      canvas.drawLine(Offset(0, y), Offset(size.width, y), grid);
    }
    final route = Paint()
      ..color = const Color(0xFFE0A45A)
      ..strokeWidth = 3
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round;
    final path = Path()
      ..moveTo(16, size.height * 0.7)
      ..quadraticBezierTo(size.width * 0.35, size.height * 0.2, size.width * 0.7, size.height * 0.55)
      ..quadraticBezierTo(size.width * 0.85, size.height * 0.7, size.width - 16, size.height * 0.35);
    canvas.drawPath(path, route);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
