import 'package:flutter/material.dart';
import '../../core/constants/app_colors.dart';
import '../../models/rider_models.dart';
import '../../services/session_service.dart';
import '../../services/rider_service.dart';

class RiderHistoryScreen extends StatefulWidget {
  final SessionService sessionService;
  final RiderService riderService;

  const RiderHistoryScreen({
    super.key,
    required this.sessionService,
    required this.riderService,
  });

  @override
  State<RiderHistoryScreen> createState() => _RiderHistoryScreenState();
}

class _RiderHistoryScreenState extends State<RiderHistoryScreen> {
  bool _loading = true;
  List<RiderOrderDto> _orders = [];
  final _search = TextEditingController();

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

  Future<void> _load() async {
    setState(() => _loading = true);
    final orders = await widget.riderService.getOrderHistory(keyword: _search.text);
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
      appBar: AppBar(title: const Text('Delivery History')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(12),
            child: TextField(
              controller: _search,
              decoration: InputDecoration(
                hintText: 'Search by order / customer',
                prefixIcon: const Icon(Icons.search),
                suffixIcon: IconButton(icon: const Icon(Icons.arrow_forward), onPressed: _load),
              ),
              onSubmitted: (_) => _load(),
            ),
          ),
          Expanded(
            child: _loading
                ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
                : _orders.isEmpty
                    ? const Center(child: Text('No history found'))
                    : ListView.separated(
                        padding: const EdgeInsets.all(16),
                        itemCount: _orders.length,
                        separatorBuilder: (_, __) => const SizedBox(height: 10),
                        itemBuilder: (_, i) {
                          final o = _orders[i];
                          return ListTile(
                            tileColor: AppColors.cardBackground,
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(12),
                              side: const BorderSide(color: AppColors.borderSubtle),
                            ),
                            title: Text(o.orderNo, style: const TextStyle(fontWeight: FontWeight.bold)),
                            subtitle: Text('${o.customerName}\n${o.statusName}'),
                            isThreeLine: true,
                            trailing: Text(o.priceDisplay, style: const TextStyle(color: AppColors.primary)),
                          );
                        },
                      ),
          ),
        ],
      ),
    );
  }
}
