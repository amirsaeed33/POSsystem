import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../core/theme/staff_theme.dart';
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
    final name = _dash?.userDisplayName ?? widget.sessionService.currentUser?.displayName ?? 'Admin';
    return Scaffold(
      backgroundColor: StaffTheme.adminPaper,
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: StaffTheme.adminGold))
          : RefreshIndicator(
              color: StaffTheme.adminGold,
              onRefresh: _load,
              child: CustomScrollView(
                physics: const AlwaysScrollableScrollPhysics(),
                slivers: [
                  SliverToBoxAdapter(child: _hero(name)),
                  SliverPadding(
                    padding: const EdgeInsets.fromLTRB(20, 0, 20, 28),
                    sliver: SliverList(
                      delegate: SliverChildListDelegate([
                        _kpiGrid(),
                        const SizedBox(height: 22),
                        _salesChart(),
                        const SizedBox(height: 22),
                        _pipelineCard(),
                        const SizedBox(height: 22),
                        _stockStrip(),
                      ]),
                    ),
                  ),
                ],
              ),
            ),
    );
  }

  Widget _hero(String name) {
    return Container(
      padding: EdgeInsets.fromLTRB(20, MediaQuery.of(context).padding.top + 18, 20, 28),
      decoration: StaffTheme.heroWash(colors: const [
        Color(0xFF2A211C),
        Color(0xFF171310),
        Color(0xFFC84B11),
      ]),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Text('COMMAND', style: GoogleFonts.manrope(
                color: StaffTheme.adminGold,
                letterSpacing: 3,
                fontWeight: FontWeight.w700,
                fontSize: 12,
              )),
              const Spacer(),
              IconButton(
                onPressed: _load,
                icon: const Icon(Icons.refresh, color: StaffTheme.adminCream),
              ),
              IconButton(
                onPressed: () async {
                  await widget.sessionService.logout();
                  if (mounted) Navigator.of(context).pop();
                },
                icon: const Icon(Icons.logout, color: StaffTheme.adminCream),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Text('Good evening,', style: StaffTheme.body(color: StaffTheme.adminCream.withValues(alpha: 0.7))),
          Text(name, style: StaffTheme.display(color: StaffTheme.adminCream, size: 34)),
          const SizedBox(height: 8),
          Text(
            'Your kitchen, counters, and riders — one calm pulse.',
            style: StaffTheme.body(color: StaffTheme.adminCream.withValues(alpha: 0.72)),
          ),
          const SizedBox(height: 18),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: 0.08),
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: Colors.white.withValues(alpha: 0.12)),
            ),
            child: Row(
              children: [
                const Icon(Icons.trending_up, color: StaffTheme.adminGold),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    _dash == null
                        ? 'Waiting for live metrics…'
                        : '${_dash!.isGrowthPositive ? '+' : ''}${_dash!.growthPercentage.toStringAsFixed(1)}% vs yesterday',
                    style: StaffTheme.body(color: StaffTheme.adminCream, weight: FontWeight.w600),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _kpiGrid() {
    final d = _dash;
    return GridView.count(
      crossAxisCount: 2,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      mainAxisSpacing: 12,
      crossAxisSpacing: 12,
      childAspectRatio: 1.25,
      children: [
        StaffMetricTile(
          label: "Today's sales",
          value: 'Rs ${_fmt(d?.todaySales)}',
          icon: Icons.payments_outlined,
          accent: StaffTheme.adminGold,
          background: StaffTheme.adminCard,
          foreground: StaffTheme.adminCream,
        ),
        StaffMetricTile(
          label: 'Profit',
          value: 'Rs ${_fmt(d?.todayProfit)}',
          icon: Icons.auto_graph,
          accent: const Color(0xFF7DCFB6),
          background: StaffTheme.adminCard,
          foreground: StaffTheme.adminCream,
        ),
        StaffMetricTile(
          label: 'Orders',
          value: '${d?.todaySalesCount ?? 0}',
          icon: Icons.receipt_long,
          accent: const Color(0xFFFF8A65),
          background: StaffTheme.adminCard,
          foreground: StaffTheme.adminCream,
        ),
        StaffMetricTile(
          label: 'Pending pipeline',
          value: '${d?.pendingOrdersCount ?? 0}',
          icon: Icons.hourglass_top,
          accent: const Color(0xFF90CAF9),
          background: StaffTheme.adminCard,
          foreground: StaffTheme.adminCream,
        ),
      ],
    );
  }

  Widget _salesChart() {
    final days = _dash?.last7Days ?? [];
    final spots = <FlSpot>[];
    for (var i = 0; i < days.length; i++) {
      spots.add(FlSpot(i.toDouble(), days[i].sales));
    }
    final maxY = spots.isEmpty ? 100.0 : spots.map((s) => s.y).reduce((a, b) => a > b ? a : b) * 1.2;

    return Container(
      padding: const EdgeInsets.fromLTRB(16, 18, 16, 12),
      decoration: BoxDecoration(
        color: StaffTheme.adminCard,
        borderRadius: BorderRadius.circular(22),
        border: Border.all(color: StaffTheme.adminLine),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('7-day sales rhythm', style: StaffTheme.title(color: StaffTheme.adminCream, size: 17)),
          const SizedBox(height: 4),
          Text('Feel the week at a glance', style: StaffTheme.body(color: StaffTheme.adminCream.withValues(alpha: 0.55), size: 12)),
          const SizedBox(height: 18),
          SizedBox(
            height: 200,
            child: spots.isEmpty
                ? Center(child: Text('No chart data yet', style: StaffTheme.body(color: StaffTheme.adminCream.withValues(alpha: 0.5))))
                : LineChart(
                    LineChartData(
                      minY: 0,
                      maxY: maxY <= 0 ? 100 : maxY,
                      gridData: FlGridData(
                        show: true,
                        drawVerticalLine: false,
                        getDrawingHorizontalLine: (_) => FlLine(color: StaffTheme.adminLine, strokeWidth: 1),
                      ),
                      titlesData: FlTitlesData(
                        rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                        topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                        leftTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                        bottomTitles: AxisTitles(
                          sideTitles: SideTitles(
                            showTitles: true,
                            getTitlesWidget: (v, _) {
                              final i = v.toInt();
                              if (i < 0 || i >= days.length) return const SizedBox.shrink();
                              final label = days[i].dayLabel ?? ['S', 'M', 'T', 'W', 'T', 'F', 'S'][days[i].date.weekday % 7];
                              return Padding(
                                padding: const EdgeInsets.only(top: 8),
                                child: Text(label, style: StaffTheme.body(color: StaffTheme.adminCream.withValues(alpha: 0.55), size: 11)),
                              );
                            },
                          ),
                        ),
                      ),
                      borderData: FlBorderData(show: false),
                      lineBarsData: [
                        LineChartBarData(
                          spots: spots,
                          isCurved: true,
                          barWidth: 3.5,
                          color: StaffTheme.adminGold,
                          dotData: const FlDotData(show: false),
                          belowBarData: BarAreaData(
                            show: true,
                            gradient: LinearGradient(
                              colors: [
                                StaffTheme.adminGold.withValues(alpha: 0.35),
                                StaffTheme.adminGold.withValues(alpha: 0.02),
                              ],
                              begin: Alignment.topCenter,
                              end: Alignment.bottomCenter,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
          ),
        ],
      ),
    );
  }

  Widget _pipelineCard() {
    final p = _pipeline;
    final rows = [
      ('Placed', p?.placed ?? 0, const Color(0xFFFFCC80)),
      ('Preparing', p?.preparing ?? 0, const Color(0xFFFF8A65)),
      ('On route', p?.outForDelivery ?? 0, const Color(0xFF80CBC4)),
      ('Delivered', p?.delivered ?? 0, const Color(0xFFA5D6A7)),
    ];
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: StaffTheme.adminCard,
        borderRadius: BorderRadius.circular(22),
        border: Border.all(color: StaffTheme.adminLine),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Online order pipeline', style: StaffTheme.title(color: StaffTheme.adminCream, size: 17)),
          const SizedBox(height: 14),
          ...rows.map((r) => Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: Row(
                  children: [
                    Container(width: 10, height: 10, decoration: BoxDecoration(color: r.$3, shape: BoxShape.circle)),
                    const SizedBox(width: 10),
                    Expanded(child: Text(r.$1, style: StaffTheme.body(color: StaffTheme.adminCream))),
                    Text('${r.$2}', style: StaffTheme.title(color: StaffTheme.adminCream, size: 16)),
                  ],
                ),
              )),
        ],
      ),
    );
  }

  Widget _stockStrip() {
    final d = _dash;
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        gradient: const LinearGradient(colors: [Color(0xFF3A2A22), Color(0xFFC84B11)]),
        borderRadius: BorderRadius.circular(22),
      ),
      child: Row(
        children: [
          Expanded(child: _mini('In stock', '${d?.inStockCount ?? 0}')),
          Expanded(child: _mini('Low', '${d?.lowStockCount ?? 0}')),
          Expanded(child: _mini('Out', '${d?.outOfStockCount ?? 0}')),
        ],
      ),
    );
  }

  Widget _mini(String label, String value) => Column(
        children: [
          Text(value, style: StaffTheme.title(color: Colors.white, size: 20)),
          Text(label, style: StaffTheme.body(color: Colors.white70, size: 12)),
        ],
      );

  String _fmt(double? v) {
    final n = v ?? 0;
    if (n >= 1000) return '${(n / 1000).toStringAsFixed(1)}k';
    return n.toStringAsFixed(0);
  }
}
