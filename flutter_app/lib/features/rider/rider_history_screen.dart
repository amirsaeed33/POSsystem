import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../core/theme/staff_theme.dart';
import '../../models/rider_models.dart';
import '../../services/session_service.dart';
import '../../services/rider_service.dart';

class RiderHistoryScreen extends StatefulWidget {
  final SessionService sessionService;
  final RiderService riderService;
  final bool embedded;

  const RiderHistoryScreen({
    super.key,
    required this.sessionService,
    required this.riderService,
    this.embedded = false,
  });

  @override
  State<RiderHistoryScreen> createState() => _RiderHistoryScreenState();
}

class _RiderHistoryScreenState extends State<RiderHistoryScreen> {
  bool _loading = true;
  List<RiderOrderDto> _orders = [];
  int _totalCount = 0;
  int _skip = 0;
  final int _pageSize = 20;
  String _filter = 'today';
  final _search = TextEditingController();

  double get _collected =>
      _orders.fold<double>(0, (sum, o) => sum + o.grandTotal);

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  (DateTime?, DateTime?) _dateRange() {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    switch (_filter) {
      case 'today':
        return (today, today);
      case 'yesterday':
        final y = today.subtract(const Duration(days: 1));
        return (y, y);
      case 'week':
        return (today.subtract(const Duration(days: 7)), today);
      case 'month':
        return (DateTime(now.year, now.month, 1), today);
      default:
        return (null, null);
    }
  }

  Future<void> _load({bool resetPage = true}) async {
    if (resetPage) {
      _skip = 0;
      setState(() => _loading = true);
    }
    final range = _dateRange();
    final result = await widget.riderService.getOrderHistory(
      skip: _skip,
      max: _pageSize,
      keyword: _search.text,
      fromDate: range.$1,
      toDate: range.$2,
      status: RiderOrderStatus.delivered,
    );
    if (!mounted) return;
    setState(() {
      if (resetPage) {
        _orders = result.items;
      } else {
        _orders = [..._orders, ...result.items];
      }
      _totalCount = result.totalCount;
      _loading = false;
    });
  }

  Future<void> _loadMore() async {
    if (!_hasMore || _loading) return;
    _skip += _pageSize;
    await _load(resetPage: false);
  }

  void _applyFilter(String filter) {
    if (_filter == filter) return;
    setState(() => _filter = filter);
    _load();
  }

  void _showDetails(RiderOrderDto order) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) {
        return Padding(
          padding: const EdgeInsets.fromLTRB(20, 16, 20, 28),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(order.orderNo, style: StaffTheme.title(size: 18)),
              const SizedBox(height: 4),
              Text(
                DateFormat('dd MMM yyyy, hh:mm a').format(order.orderDate.toLocal()),
                style: StaffTheme.body(size: 12),
              ),
              const SizedBox(height: 12),
              Text(order.customerName, style: StaffTheme.title(size: 15)),
              Text(order.customerPhone, style: StaffTheme.body(size: 13)),
              const SizedBox(height: 6),
              Text(order.deliveryAddress, style: StaffTheme.body(size: 13)),
              const SizedBox(height: 10),
              Text(order.itemsSummary, style: StaffTheme.body(size: 12)),
              if (order.notes != null && order.notes!.trim().isNotEmpty) ...[
                const SizedBox(height: 8),
                Text('Note: ${order.notes}', style: StaffTheme.body(size: 12)),
              ],
              const SizedBox(height: 12),
              Row(
                children: [
                  Text(order.priceDisplay, style: StaffTheme.title(color: StaffTheme.riderRoute)),
                  const Spacer(),
                  if (order.deliveryRating != null)
                    Row(
                      children: [
                        const Icon(Icons.star, size: 16, color: Color(0xFFE0A45A)),
                        Text(' ${order.deliveryRating}', style: StaffTheme.body(size: 13)),
                      ],
                    ),
                ],
              ),
            ],
          ),
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final topPad = MediaQuery.of(context).padding.top;
    return Scaffold(
      backgroundColor: StaffTheme.riderPaper,
      body: Column(
        children: [
          Container(
            width: double.infinity,
            padding: EdgeInsets.fromLTRB(20, topPad + 12, 20, 16),
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [Color(0xFF1E3A34), Color(0xFF2A7F62)],
              ),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (!widget.embedded)
                  Row(
                    children: [
                      IconButton(
                        onPressed: () => Navigator.of(context).pop(),
                        icon: const Icon(Icons.arrow_back, color: Colors.white),
                      ),
                    ],
                  ),
                Text('Order History', style: StaffTheme.display(color: Colors.white, size: 26)),
                const SizedBox(height: 4),
                Text(
                  '$_totalCount delivered · Rs ${_collected.toStringAsFixed(0)} on this page',
                  style: StaffTheme.body(color: Colors.white70, size: 13),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 12, 12, 0),
            child: TextField(
              controller: _search,
              decoration: InputDecoration(
                hintText: 'Search order / customer',
                prefixIcon: const Icon(Icons.search),
                suffixIcon: IconButton(
                  icon: const Icon(Icons.arrow_forward),
                  onPressed: () => _load(),
                ),
                filled: true,
                fillColor: Colors.white,
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(14),
                  borderSide: BorderSide.none,
                ),
              ),
              onSubmitted: (_) => _load(),
            ),
          ),
          SizedBox(
            height: 48,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              children: [
                _chip('today', 'Today'),
                _chip('yesterday', 'Yesterday'),
                _chip('week', 'Last 7 days'),
                _chip('month', 'This month'),
                _chip('all', 'All time'),
              ],
            ),
          ),
          Expanded(
            child: _loading
                ? const Center(child: CircularProgressIndicator(color: StaffTheme.riderRoute))
                : _orders.isEmpty
                    ? Center(
                        child: Text(
                          'No delivered orders for this filter',
                          style: StaffTheme.body(),
                        ),
                      )
                    : RefreshIndicator(
                        color: StaffTheme.riderRoute,
                        onRefresh: _load,
                        child: ListView.separated(
                          padding: const EdgeInsets.fromLTRB(16, 4, 16, 24),
                          itemCount: _orders.length + (_hasMore ? 1 : 0),
                          separatorBuilder: (_, __) => const SizedBox(height: 10),
                          itemBuilder: (_, i) {
                            if (i == _orders.length) {
                              return TextButton(
                                onPressed: _loadMore,
                                child: const Text('Load more'),
                              );
                            }
                            return _historyCard(_orders[i]);
                          },
                        ),
                      ),
          ),
        ],
      ),
    );
  }

  bool get _hasMore => (_skip + _orders.length) < _totalCount;

  Widget _chip(String key, String label) {
    final selected = _filter == key;
    return Padding(
      padding: const EdgeInsets.only(right: 8),
      child: ChoiceChip(
        label: Text(label),
        selected: selected,
        selectedColor: StaffTheme.riderRoute.withValues(alpha: 0.2),
        onSelected: (_) => _applyFilter(key),
      ),
    );
  }

  Widget _historyCard(RiderOrderDto order) {
    return InkWell(
      onTap: () => _showDetails(order),
      borderRadius: BorderRadius.circular(16),
      child: Container(
        padding: const EdgeInsets.all(14),
        decoration: StaffTheme.softCard(),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(child: Text(order.orderNo, style: StaffTheme.title(size: 15))),
                Text(order.priceDisplay, style: StaffTheme.title(color: StaffTheme.riderRoute, size: 15)),
              ],
            ),
            const SizedBox(height: 4),
            Text(order.customerName, style: StaffTheme.body(size: 13, weight: FontWeight.w600)),
            Text(order.deliveryAddress, style: StaffTheme.body(size: 12), maxLines: 2, overflow: TextOverflow.ellipsis),
            const SizedBox(height: 6),
            Row(
              children: [
                Text(
                  DateFormat('dd MMM, hh:mm a').format(order.orderDate.toLocal()),
                  style: StaffTheme.body(size: 11),
                ),
                const Spacer(),
                if (order.deliveryRating != null) ...[
                  const Icon(Icons.star, size: 14, color: Color(0xFFE0A45A)),
                  Text(' ${order.deliveryRating}', style: StaffTheme.body(size: 12)),
                ],
                const SizedBox(width: 8),
                Text('Details', style: StaffTheme.body(size: 12, color: StaffTheme.riderWater, weight: FontWeight.w700)),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
