import 'package:flutter/material.dart';
import '../../core/constants/app_colors.dart';
import '../../models/waiter_models.dart';
import '../../services/session_service.dart';
import '../../services/waiter_service.dart';

class WaiterOrderScreen extends StatefulWidget {
  final SessionService sessionService;
  final WaiterService waiterService;
  final RestaurantTableDto table;
  final RestaurantOrderDto order;

  const WaiterOrderScreen({
    super.key,
    required this.sessionService,
    required this.waiterService,
    required this.table,
    required this.order,
  });

  @override
  State<WaiterOrderScreen> createState() => _WaiterOrderScreenState();
}

class _WaiterOrderScreenState extends State<WaiterOrderScreen> {
  late RestaurantOrderDto _order;
  List<PosProductDto> _products = [];
  final List<_DraftLine> _draft = [];
  bool _loadingProducts = true;
  bool _busy = false;
  String _search = '';

  @override
  void initState() {
    super.initState();
    _order = widget.order;
    _loadProducts();
  }

  Future<void> _loadProducts() async {
    setState(() => _loadingProducts = true);
    final products = await widget.waiterService.getPosProducts();
    if (!mounted) return;
    setState(() {
      _products = products;
      _loadingProducts = false;
    });
  }

  List<PosProductDto> get _filtered {
    if (_search.trim().isEmpty) return _products;
    final q = _search.toLowerCase();
    return _products.where((p) => p.name.toLowerCase().contains(q)).toList();
  }

  void _addProduct(PosProductDto p) {
    setState(() {
      final existing = _draft.where((d) => d.productId == p.id).toList();
      if (existing.isNotEmpty) {
        existing.first.qty += 1;
      } else {
        _draft.add(_DraftLine(productId: p.id, name: p.name, unitPrice: p.price, qty: 1));
      }
    });
  }

  double get _draftTotal => _draft.fold(0.0, (s, d) => s + d.unitPrice * d.qty);

  Future<void> _saveAndKot() async {
    if (_draft.isEmpty && _order.items.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Add items first')));
      return;
    }
    setState(() => _busy = true);
    try {
      final lines = [
        ..._order.items.map((i) => {
              if (i.id != null) 'id': i.id,
              'productId': i.productId,
              'productName': i.productName,
              'quantity': i.quantity,
              'unitPrice': i.unitPrice,
              'totalPrice': i.totalPrice,
              if (i.notes != null) 'notes': i.notes,
              'isPrinted': i.isPrinted,
            }),
        ..._draft.map((d) => {
              'productId': d.productId,
              'productName': d.name,
              'quantity': d.qty,
              'unitPrice': d.unitPrice,
              'totalPrice': d.unitPrice * d.qty,
              'isPrinted': false,
            }),
      ];

      final saved = await widget.waiterService.saveOrder({
        'id': _order.id,
        'tableId': widget.table.id,
        'covers': _order.covers,
        'items': lines,
      });
      await widget.waiterService.sendToKitchen(saved.id);
      if (!mounted) return;
      setState(() {
        _order = saved;
        _draft.clear();
        _busy = false;
      });
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Sent to kitchen (KOT)')),
      );
    } catch (e) {
      if (!mounted) return;
      setState(() => _busy = false);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('KOT failed: $e'), backgroundColor: AppColors.badgeRed),
      );
    }
  }

  Future<void> _printBill() async {
    setState(() => _busy = true);
    try {
      await widget.waiterService.printBill(_order.id);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Bill sent to printer')));
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Print failed: $e'), backgroundColor: AppColors.badgeRed),
      );
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _settle() async {
    final paid = _order.grandTotal > 0 ? _order.grandTotal : _draftTotal;
    setState(() => _busy = true);
    try {
      await widget.waiterService.settleOrder(
        orderId: _order.id,
        paymentMethod: 'Cash',
        paidAmount: paid,
      );
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Order settled')));
      Navigator.of(context).pop();
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Settle failed: $e'), backgroundColor: AppColors.badgeRed),
      );
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.pageBackground,
      appBar: AppBar(
        title: Text('${widget.table.tableNumber} · ${_order.orderNo}'),
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
            child: TextField(
              decoration: const InputDecoration(
                hintText: 'Search POS products…',
                prefixIcon: Icon(Icons.search),
              ),
              onChanged: (v) => setState(() => _search = v),
            ),
          ),
          Expanded(
            child: _loadingProducts
                ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
                : ListView.builder(
                    padding: const EdgeInsets.symmetric(horizontal: 12),
                    itemCount: _filtered.length,
                    itemBuilder: (_, i) {
                      final p = _filtered[i];
                      return ListTile(
                        title: Text(p.name),
                        subtitle: Text('Rs. ${p.price.toStringAsFixed(0)}'),
                        trailing: IconButton(
                          icon: const Icon(Icons.add_circle, color: AppColors.primary),
                          onPressed: () => _addProduct(p),
                        ),
                      );
                    },
                  ),
          ),
          if (_draft.isNotEmpty)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              color: AppColors.primaryLight,
              child: Text(
                'New items: ${_draft.length} · Rs. ${_draftTotal.toStringAsFixed(0)}',
                style: const TextStyle(fontWeight: FontWeight.w600),
              ),
            ),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(12),
              child: Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _busy ? null : _printBill,
                      child: const Text('Bill'),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _busy ? null : _settle,
                      child: const Text('Settle'),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    flex: 2,
                    child: ElevatedButton(
                      onPressed: _busy ? null : _saveAndKot,
                      child: _busy
                          ? const SizedBox(
                              width: 18,
                              height: 18,
                              child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                            )
                          : const Text('Send KOT'),
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
}

class _DraftLine {
  final int productId;
  final String name;
  final double unitPrice;
  double qty;

  _DraftLine({
    required this.productId,
    required this.name,
    required this.unitPrice,
    required this.qty,
  });
}
