import '../core/api/json_helpers.dart';

class ModifierOptionDto {
  final int id;
  final int groupId;
  final String name;
  final double price;
  final bool allowMultiple;
  final bool isRequired;
  final int maxSelection;

  ModifierOptionDto({
    required this.id,
    required this.groupId,
    required this.name,
    this.price = 0.0,
    this.allowMultiple = false,
    this.isRequired = false,
    this.maxSelection = 1,
  });

  bool get hasPrice => price > 0;
  String get priceLabel => price > 0 ? '+Rs. ${price.toStringAsFixed(0)}' : 'Included';

  factory ModifierOptionDto.fromJson(Map<String, dynamic> json) {
    return ModifierOptionDto(
      id: json['id'] as int? ?? 0,
      groupId: json['groupId'] as int? ?? 0,
      name: json['name'] as String? ?? '',
      price: (json['price'] as num?)?.toDouble() ?? 0.0,
      allowMultiple: json['allowMultiple'] as bool? ?? false,
      isRequired: json['isRequired'] as bool? ?? false,
      maxSelection: json['maxSelection'] as int? ?? 1,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'groupId': groupId,
        'name': name,
        'price': price,
      };
}

class ProductModifierGroupDto {
  final int id;
  final String name;
  final bool isRequired;
  final bool allowMultiple;
  final int maxSelection;
  final List<ModifierOptionDto> options;

  ProductModifierGroupDto({
    required this.id,
    required this.name,
    this.isRequired = false,
    this.allowMultiple = false,
    this.maxSelection = 1,
    this.options = const [],
  });

  String get requirementLabel => isRequired ? '(Required)' : '(Optional)';
  String get selectionHint => allowMultiple
      ? (maxSelection > 1 ? 'Choose up to $maxSelection' : 'Multiple choices')
      : 'Choose 1';

  bool get usePillLayout => options.isNotEmpty && options.every((o) => o.price <= 0);

  factory ProductModifierGroupDto.fromJson(Map<String, dynamic> json) {
    return ProductModifierGroupDto(
      id: json['id'] as int? ?? 0,
      name: json['name'] as String? ?? 'Options',
      isRequired: json['isRequired'] as bool? ?? false,
      allowMultiple: json['allowMultiple'] as bool? ?? false,
      maxSelection: json['maxSelection'] as int? ?? 1,
      options: (json['options'] as List<dynamic>?)
              ?.map((o) => ModifierOptionDto.fromJson(o as Map<String, dynamic>))
              .toList() ??
          [],
    );
  }
}

class OnlineProductDto {
  final int id;
  final String name;
  final String? description;
  final String? categoryName;
  final int? categoryId;
  final double price;
  final String currencyFormat;
  final String? imagePath;
  final String? imageUrl;
  final bool inStock;
  final double stockQuantity;
  final bool isFreeDelivery;
  final bool isDeal;
  final String? cardTag;
  final int? dealId;
  final double? originalPrice;
  final String? dealItemsSummary;

  OnlineProductDto({
    required this.id,
    required this.name,
    this.description,
    this.categoryName,
    this.categoryId,
    required this.price,
    this.currencyFormat = 'Rs.',
    this.imagePath,
    this.imageUrl,
    this.inStock = true,
    this.stockQuantity = 999.0,
    this.isFreeDelivery = false,
    this.isDeal = false,
    this.cardTag,
    this.dealId,
    this.originalPrice,
    this.dealItemsSummary,
  });

  bool get hasOriginalPrice => originalPrice != null && originalPrice! > price;
  String get originalPriceDisplay =>
      originalPrice != null ? 'Rs. ${originalPrice!.toStringAsFixed(0)}' : '';

  String get dealDiscountBadge {
    if (originalPrice != null && originalPrice! > price && originalPrice! > 0) {
      final pct = (((originalPrice! - price) / originalPrice!) * 100).round();
      if (pct > 0) return 'SAVE $pct%';
    }
    return 'HOT DEAL';
  }

  String get priceDisplay => 'Rs. ${price.toStringAsFixed(0)}';

  String get displayTag {
    if (cardTag != null && cardTag!.trim().isNotEmpty) return cardTag!;
    if (isDeal) return 'DEAL';
    if (isFreeDelivery) return 'FREE DELIVERY';
    return '';
  }

  bool get hasTag => displayTag.isNotEmpty;

  String get culinarySubtitle {
    if (description != null && description!.trim().isNotEmpty) {
      final t = description!.trim();
      return t.length <= 42 ? t : '${t.substring(0, 39)}…';
    }
    return categoryName?.trim() ?? 'Gulberg Kitchen';
  }

  String get initials {
    final parts = name.trim().split(RegExp(r'\s+'));
    if (parts.length >= 2 && parts[0].isNotEmpty && parts[1].isNotEmpty) {
      return '${parts[0][0]}${parts[1][0]}'.toUpperCase();
    }
    return name.length >= 2 ? name.substring(0, 2).toUpperCase() : name.toUpperCase();
  }

  factory OnlineProductDto.fromJson(Map<String, dynamic> json) {
    return OnlineProductDto(
      id: asInt(json['id']),
      name: json['name']?.toString() ?? '',
      description: asStringOrNull(json['description']),
      categoryName: asStringOrNull(json['categoryName']),
      categoryId: asIntOrNull(json['categoryId']),
      price: asDouble(json['price']),
      currencyFormat: json['currencyFormat']?.toString() ?? 'Rs.',
      imagePath: asStringOrNull(json['imagePath']),
      imageUrl: asStringOrNull(json['imageUrl']),
      inStock: asBool(json['inStock'], true),
      stockQuantity: asDouble(json['stockQuantity'], 999.0),
      isFreeDelivery: asBool(json['isFreeDelivery']),
      isDeal: asBool(json['isDeal']),
      cardTag: asStringOrNull(json['cardTag']),
      dealId: asIntOrNull(json['dealId']),
      originalPrice: json['originalPrice'] == null ? null : asDouble(json['originalPrice']),
      dealItemsSummary: asStringOrNull(json['dealItemsSummary']),
    );
  }
}

