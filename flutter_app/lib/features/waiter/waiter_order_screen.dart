import 'package:flutter/material.dart';
import '../../core/theme/staff_theme.dart';
import '../../models/checkout_models.dart';
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
  PosCustomerDto? _customer;
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

  Future<void> _pickCustomer() async {
    final customers = await widget.waiterService.getPosCustomers();
    if (!mounted) return;
    final selected = await showModalBottomSheet<PosCustomerDto>(
      context: context,
      isScrollControlled: true,
      backgroundColor: StaffTheme.waiterCard,
      shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(22))),
      builder: (ctx) {
        var q = '';
        return StatefulBuilder(
          builder: (context, setModal) {
            final filtered = customers
                .where((c) =>
                    q.isEmpty ||
                    c.name.toLowerCase().contains(q) ||
                    (c.phone?.contains(q) ?? false))
                .toList();
            return SizedBox(
              height: MediaQuery.of(context).size.height * 0.7,
              child: Column(
                children: [
                  const SizedBox(height: 10),
                  Container(width: 40, height: 4, decoration: BoxDecoration(color: Colors.black12, borderRadius: BorderRadius.circular(4))),
                  Padding(
                    padding: const EdgeInsets.all(16),
                    child: TextField(
                      decoration: const InputDecoration(
                        hintText: 'Search customers…',
                        prefixIcon: Icon(Icons.person_search),
                      ),
                      onChanged: (v) => setModal(() => q = v.toLowerCase()),
                    ),
                  ),
                  Expanded(
                    child: ListView.builder(
                      itemCount: filtered.length,
                      itemBuilder: (_, i) {
                        final c = filtered[i];
                        return ListTile(
                          leading: CircleAvatar(
                            backgroundColor: StaffTheme.waiterAccent.withValues(alpha: 0.12),
                            child: Text(c.name.isNotEmpty ? c.name[0].toUpperCase() : '?',
                                style: const TextStyle(color: StaffTheme.waiterAccent)),
                          ),
                          title: Text(c.name),
                          subtitle: Text(c.phone ?? ''),
                          onTap: () => Navigator.pop(ctx, c),
                        );
                      },
                    ),
                  ),
                ],
              ),
            );
          },
        );
      },
    );
    if (selected == null) return;
    try {
      await widget.waiterService.setOrderCustomer(_order.id, selected.id);
      if (!mounted) return;
      setState(() => _customer = selected);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Customer set: ${selected.name}')),
      );
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Failed: $e')));
    }
  }

  Future<void> _splitBill() async {
    int splits = 2;
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (context, setD) => AlertDialog(
          title: Text('Split bill', style: StaffTheme.title()),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Text('Split equally across how many checks?'),
              const SizedBox(height: 12),
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  IconButton(onPressed: () => setD(() { if (splits > 2) splits--; }), icon: const Icon(Icons.remove_circle_outline)),
                  Text('$splits', style: StaffTheme.title(size: 22)),
                  IconButton(onPressed: () => setD(() => splits++), icon: const Icon(Icons.add_circle_outline)),
                ],
              ),
            ],
          ),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
            FilledButton(
              style: FilledButton.styleFrom(backgroundColor: StaffTheme.waiterAccent),
              onPressed: () => Navigator.pop(ctx, true),
              child: const Text('Split'),
            ),
          ],
        ),
      ),
    );
    if (ok != true) return;
    setState(() => _busy = true);
    final result = await widget.waiterService.splitBill(
      sourceOrderId: _order.id,
      numberOfSplits: splits,
      splitEqually: true,
    );
    if (!mounted) return;
    setState(() => _busy = false);
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(result != null ? 'Bill split into $splits' : 'Split failed')),
    );
    if (result != null) setState(() => _order = result);
  }

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
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('KOT sent to kitchen')));
    } catch (e) {
      if (!mounted) return;
      setState(() => _busy = false);
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('KOT failed: $e')));
    }
  }

  Future<void> _printBill() async {
    setState(() => _busy = true);
    try {
      await widget.waiterService.printBill(_order.id);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Bill printed')));
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Print failed: $e')));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _settle() async {
    final paid = _order.grandTotal > 0 ? _order.grandTotal : _draftTotal;
    setState(() => _busy = true);
    try {
      await widget.waiterService.settleOrder(orderId: _order.id, paymentMethod: 'Cash', paidAmount: paid);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Settled — table freed')));
      Navigator.of(context).pop();
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Settle failed: $e')));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: StaffTheme.waiterPaper,
      appBar: AppBar(
        backgroundColor: StaffTheme.waiterPaper,
        foregroundColor: StaffTheme.waiterInk,
        elevation: 0,
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('${widget.table.tableNumber} · ${_order.orderNo}', style: StaffTheme.title(size: 16)),
            Text(
              _customer?.name ?? 'Walk-in guest · ${_order.covers} covers',
              style: StaffTheme.body(size: 12),
            ),
          ],
        ),
        actions: [
          IconButton(tooltip: 'Customer', onPressed: _pickCustomer, icon: const Icon(Icons.person_add_alt)),
          IconButton(tooltip: 'Split bill', onPressed: _busy ? null : _splitBill, icon: const Icon(Icons.call_split)),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
            child: TextField(
              decoration: InputDecoration(
                hintText: 'Search kitchen menu…',
                prefixIcon: const Icon(Icons.search),
                filled: true,
                fillColor: Colors.white,
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide.none),
              ),
              onChanged: (v) => setState(() => _search = v),
            ),
          ),
          Expanded(
            child: _loadingProducts
                ? const Center(child: CircularProgressIndicator(color: StaffTheme.waiterAccent))
                : ListView.builder(
                    padding: const EdgeInsets.symmetric(horizontal: 8),
                    itemCount: _filtered.length,
                    itemBuilder: (_, i) {
                      final p = _filtered[i];
                      return Card(
                        elevation: 0,
                        color: Colors.white,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(14),
                          side: const BorderSide(color: Color(0xFFEAE2DA)),
                        ),
                        child: ListTile(
                          title: Text(p.name, style: StaffTheme.title(size: 15)),
                          subtitle: Text('Rs. ${p.price.toStringAsFixed(0)} · ${p.categoryName ?? 'Menu'}'),
                          trailing: IconButton(
                            icon: const Icon(Icons.add_circle, color: StaffTheme.waiterAccent, size: 28),
                            onPressed: () => _addProduct(p),
                          ),
                        ),
                      );
                    },
                  ),
          ),
          if (_draft.isNotEmpty)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              color: const Color(0xFFFDF1EB),
              child: Text(
                'Tray: ${_draft.length} new · Rs. ${_draftTotal.toStringAsFixed(0)}',
                style: StaffTheme.title(size: 14, color: StaffTheme.waiterAccent),
              ),
            ),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(12, 8, 12, 12),
              child: Row(
                children: [
                  Expanded(
                    child: OutlinedButton(onPressed: _busy ? null : _printBill, child: const Text('Bill')),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton(onPressed: _busy ? null : _settle, child: const Text('Settle')),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    flex: 2,
                    child: FilledButton(
                      style: FilledButton.styleFrom(
                        backgroundColor: StaffTheme.waiterAccent,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      ),
                      onPressed: _busy ? null : _saveAndKot,
                      child: _busy
                          ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
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

  _DraftLine({required this.productId, required this.name, required this.unitPrice, required this.qty});
}
