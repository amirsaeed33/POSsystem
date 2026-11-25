import '../core/api/json_helpers.dart';

class OnlineBranchDto {
  final int id;
  final String name;
  final bool isRestaurant;
  final String? address;
  final String? imagePath;
  final String? phone;
  final String? email;
  final String? website;
  final String? code;
  final String? logoUrl;

  OnlineBranchDto({
    required this.id,
    required this.name,
    this.isRestaurant = true,
    this.address,
    this.imagePath,
    this.phone,
    this.email,
    this.website,
    this.code,
    this.logoUrl,
  });

  bool get hasLogo => logoUrl != null && logoUrl!.trim().isNotEmpty;
  bool get hasAddress => address != null && address!.trim().isNotEmpty;
  bool get hasPhone => phone != null && phone!.trim().isNotEmpty;
  String get businessTypeLabel => isRestaurant ? 'Restaurant' : 'Store';

  String get initials {
    final parts = name.trim().split(RegExp(r'\s+'));
    if (parts.length >= 2 && parts[0].isNotEmpty && parts[1].isNotEmpty) {
      return '${parts[0][0]}${parts[1][0]}'.toUpperCase();
    }
    return name.length >= 2 ? name.substring(0, 2).toUpperCase() : name.toUpperCase();
  }

  factory OnlineBranchDto.fromJson(Map<String, dynamic> json) {
    return OnlineBranchDto(
      id: asInt(json['id']),
      name: json['name']?.toString() ?? 'Branch',
      isRestaurant: asBool(json['isRestaurant'], true),
      address: asStringOrNull(json['address']),
      imagePath: asStringOrNull(json['imagePath']),
      phone: asStringOrNull(json['phone']),
      email: asStringOrNull(json['email']),
      website: asStringOrNull(json['website']),
      code: asStringOrNull(json['code']),
      logoUrl: asStringOrNull(json['logoUrl']),
    );
  }
}

class OnlineStoreHeaderDto {
  final int branchId;
  final String branchName;
  final String? address;
  final String? phone;
  final String currencyFormat;
  final double baseDeliveryFee;
  final bool isDeliveryEnabled;
  final double freeDeliveryThreshold;
  final String? deliveryAreaNote;
  final bool isRestaurant;
  final List<String> categories;

  OnlineStoreHeaderDto({
    required this.branchId,
    required this.branchName,
    this.address,
    this.phone,
    this.currencyFormat = 'Rs.',
    this.baseDeliveryFee = 0.0,
    this.isDeliveryEnabled = true,
    this.freeDeliveryThreshold = 0.0,
    this.deliveryAreaNote,
    this.isRestaurant = true,
    this.categories = const [],
  });

  factory OnlineStoreHeaderDto.fromJson(Map<String, dynamic> json) {
    return OnlineStoreHeaderDto(
      branchId: asInt(json['branchId']),
      branchName: json['branchName']?.toString() ?? 'Gulberg Kitchen',
      address: asStringOrNull(json['address']),
      phone: asStringOrNull(json['phone']),
      currencyFormat: json['currencyFormat']?.toString() ?? 'Rs.',
      baseDeliveryFee: asDouble(json['baseDeliveryFee']),
      isDeliveryEnabled: asBool(json['isDeliveryEnabled'], true),
      freeDeliveryThreshold: asDouble(json['freeDeliveryThreshold']),
      deliveryAreaNote: asStringOrNull(json['deliveryAreaNote']),
      isRestaurant: asBool(json['isRestaurant'], true),
      categories: (json['categories'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          [],
    );
  }
}

