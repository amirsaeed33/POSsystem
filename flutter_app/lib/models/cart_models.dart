enum OrderType {
  delivery,
  takeaway,
}

class CartSelectedModifier {
  final int optionId;
  final int groupId;
  final String groupName;
  final String optionName;
  final double price;

  CartSelectedModifier({
    required this.optionId,
    required this.groupId,
    required this.groupName,
    required this.optionName,
    required this.price,
  });

  Map<String, dynamic> toJson() => {
        'optionId': optionId,
        'groupId': groupId,
        'groupName': groupName,
        'optionName': optionName,
        'price': price,
      };

  factory CartSelectedModifier.fromJson(Map<String, dynamic> json) {
    return CartSelectedModifier(
      optionId: json['optionId'] as int? ?? 0,
      groupId: json['groupId'] as int? ?? 0,
      groupName: json['groupName'] as String? ?? '',
      optionName: json['optionName'] as String? ?? '',
      price: (json['price'] as num?)?.toDouble() ?? 0.0,
    );
  }
}

class CartItem {
  final String id; // Unique instance id in cart
  final int productId;
  final String productName;
  final double basePrice;
  final String? imageUrl;
  final List<CartSelectedModifier> modifiers;
  final String? notes;
  int quantity;

  CartItem({
    required this.id,
    required this.productId,
    required this.productName,
    required this.basePrice,
    this.imageUrl,
    this.modifiers = const [],
    this.notes,
    this.quantity = 1,
  });

  double get unitPrice {
    final modSum = modifiers.fold<double>(0.0, (sum, m) => sum + m.price);
    return basePrice + modSum;
  }

  double get totalPrice => unitPrice * quantity;

  String get modifierSummary {
    if (modifiers.isEmpty) return '';
    return modifiers.map((m) => m.optionName).join(', ');
  }
}

