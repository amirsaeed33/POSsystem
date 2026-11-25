class HeroSlideDto {
  final int id;
  final int branchId;
  final int sortOrder;
  final String? badge;
  final String? badgeIcon;
  final String? titleMain;
  final String? titleHighlight;
  final String? titleEnd;
  final String? description;
  final String? offerPill;
  final String? rating;
  final String? primaryBtnText;
  final String? secondaryBtnText;
  final String? imagePath;
  final String? imageUrl;

  HeroSlideDto({
    required this.id,
    required this.branchId,
    this.sortOrder = 0,
    this.badge,
    this.badgeIcon,
    this.titleMain,
    this.titleHighlight,
    this.titleEnd,
    this.description,
    this.offerPill,
    this.rating,
    this.primaryBtnText,
    this.secondaryBtnText,
    this.imagePath,
    this.imageUrl,
  });

  factory HeroSlideDto.fromJson(Map<String, dynamic> json) {
    return HeroSlideDto(
      id: json['id'] as int? ?? 0,
      branchId: json['branchId'] as int? ?? 0,
      sortOrder: json['sortOrder'] as int? ?? 0,
      badge: json['badge'] as String?,
      badgeIcon: json['badgeIcon'] as String?,
      titleMain: json['titleMain'] as String?,
      titleHighlight: json['titleHighlight'] as String?,
      titleEnd: json['titleEnd'] as String?,
      description: json['description'] as String?,
      offerPill: json['offerPill'] as String?,
      rating: json['rating'] as String?,
      primaryBtnText: json['primaryBtnText'] as String?,
      secondaryBtnText: json['secondaryBtnText'] as String?,
      imagePath: json['imagePath'] as String?,
      imageUrl: json['imageUrl'] as String?,
    );
  }
}

class DealDto {
  final int id;
  final int branchId;
  final String? branchName;
  final String name;
  final String? description;
  final double dealPrice;
  final double? originalPrice;
  final String? categoryName;
  final bool isActive;
  final int? productId;
  final String? imagePath;
  final String? imageUrl;

  DealDto({
    required this.id,
    required this.branchId,
    this.branchName,
    required this.name,
    this.description,
    required this.dealPrice,
    this.originalPrice,
    this.categoryName,
    this.isActive = true,
    this.productId,
    this.imagePath,
    this.imageUrl,
  });

  String get priceDisplay => 'Rs. ${dealPrice.toStringAsFixed(0)}';

  factory DealDto.fromJson(Map<String, dynamic> json) {
    return DealDto(
      id: json['id'] as int? ?? 0,
      branchId: json['branchId'] as int? ?? 0,
      branchName: json['branchName'] as String?,
      name: json['name'] as String? ?? 'Deal',
      description: json['description'] as String?,
      dealPrice: (json['dealPrice'] as num?)?.toDouble() ?? 0.0,
      originalPrice: (json['originalPrice'] as num?)?.toDouble(),
      categoryName: json['categoryName'] as String?,
      isActive: json['isActive'] as bool? ?? true,
      productId: json['productId'] as int?,
      imagePath: json['imagePath'] as String?,
      imageUrl: json['imageUrl'] as String?,
    );
  }
}

