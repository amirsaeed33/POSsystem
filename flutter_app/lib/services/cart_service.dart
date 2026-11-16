import 'package:flutter/foundation.dart';
import 'package:uuid/uuid.dart';
import '../models/cart_models.dart';

class CartService extends ChangeNotifier {
  final List<CartItem> _items = [];
  OrderType _orderType = OrderType.delivery;
  double _baseDeliveryFee = 150.0;
  double _freeDeliveryThreshold = 0.0;

  String _customerName = '';
  String _customerPhone = '';
  String _deliveryAddress = '';
  String _deliveryLandmark = '';
  String _orderNotes = '';

  List<CartItem> get items => List.unmodifiable(_items);
  OrderType get orderType => _orderType;
  double get baseDeliveryFee => _baseDeliveryFee;
  double get freeDeliveryThreshold => _freeDeliveryThreshold;

  String get customerName => _customerName;
  String get customerPhone => _customerPhone;
  String get deliveryAddress => _deliveryAddress;
  String get deliveryLandmark => _deliveryLandmark;
  String get orderNotes => _orderNotes;

  int get itemCount => _items.fold<int>(0, (sum, i) => sum + i.quantity);

  double get subtotal => _items.fold<double>(0.0, (sum, i) => sum + i.totalPrice);

  double get deliveryFee {
    if (_orderType == OrderType.takeaway) return 0.0;
    if (_freeDeliveryThreshold > 0 && subtotal >= _freeDeliveryThreshold) return 0.0;
    return _baseDeliveryFee;
  }

  double get grandTotal => subtotal + deliveryFee;

  void setDeliveryConfig({required double baseFee, required double freeThreshold}) {
    _baseDeliveryFee = baseFee;
    _freeDeliveryThreshold = freeThreshold;
    notifyListeners();
  }

  void setOrderType(OrderType type) {
    _orderType = type;
    notifyListeners();
  }

  void setCustomerDetails({
    String? name,
    String? phone,
    String? address,
    String? landmark,
    String? notes,
  }) {
    if (name != null) _customerName = name;
    if (phone != null) _customerPhone = phone;
    if (address != null) _deliveryAddress = address;
    if (landmark != null) _deliveryLandmark = landmark;
    if (notes != null) _orderNotes = notes;
    notifyListeners();
  }

  void addItem({
    required int productId,
    required String productName,
    required double basePrice,
    String? imageUrl,
    List<CartSelectedModifier> modifiers = const [],
    String? notes,
    int quantity = 1,
  }) {
    // Check if identical item already exists (same productId and same modifiers)
    final existingIndex = _items.indexWhere((i) {
      if (i.productId != productId) return false;
      if (i.modifiers.length != modifiers.length) return false;
      final existingModIds = i.modifiers.map((m) => m.optionId).toSet();
      final newModIds = modifiers.map((m) => m.optionId).toSet();
      return existingModIds.containsAll(newModIds);
    });

    if (existingIndex >= 0) {
      _items[existingIndex].quantity += quantity;
    } else {
      _items.add(
        CartItem(
          id: const Uuid().v4(),
          productId: productId,
          productName: productName,
          basePrice: basePrice,
          imageUrl: imageUrl,
          modifiers: modifiers,
          notes: notes,
          quantity: quantity,
        ),
      );
    }
    notifyListeners();
  }

  void updateQuantity(String cartItemId, int delta) {
    final idx = _items.indexWhere((i) => i.id == cartItemId);
    if (idx >= 0) {
      _items[idx].quantity += delta;
      if (_items[idx].quantity <= 0) {
        _items.removeAt(idx);
      }
      notifyListeners();
    }
  }

  void removeItem(String cartItemId) {
    _items.removeWhere((i) => i.id == cartItemId);
    notifyListeners();
  }

  void clearCart() {
    _items.clear();
    notifyListeners();
  }

  int getQuantityForProduct(int productId) {
    return _items
        .where((i) => i.productId == productId)
        .fold<int>(0, (sum, i) => sum + i.quantity);
  }
}

