import '../core/api/json_helpers.dart';

class DeliveryAreaDto {
  final int id;
  final int branchId;
  final String name;
  final double deliveryFee;
  final double minimumOrderAmount;
  final String? estimatedDeliveryTime;
  final bool isActive;

  DeliveryAreaDto({
    required this.id,
    required this.branchId,
    required this.name,
    this.deliveryFee = 0,
    this.minimumOrderAmount = 0,
    this.estimatedDeliveryTime,
    this.isActive = true,
  });

  String get feeLabel => deliveryFee <= 0 ? 'Free' : 'Rs. ${deliveryFee.toStringAsFixed(0)}';
  String get etaLabel => estimatedDeliveryTime?.trim().isNotEmpty == true
      ? estimatedDeliveryTime!
      : '30-45 min';

  factory DeliveryAreaDto.fromJson(Map<String, dynamic> json) => DeliveryAreaDto(
        id: asInt(json['id']),
        branchId: asInt(json['branchId']),
        name: json['name']?.toString() ?? 'Area',
        deliveryFee: asDouble(json['deliveryFee']),
        minimumOrderAmount: asDouble(json['minimumOrderAmount']),
        estimatedDeliveryTime: asStringOrNull(json['estimatedDeliveryTime']),
        isActive: asBool(json['isActive'], true),
      );
}

class CustomerSavedAddressDto {
  final int id;
  final String? label;
  final String address;
  final String? landmark;
  final int? deliveryAreaId;
  final String? deliveryAreaName;
  final double? latitude;
  final double? longitude;
  final bool isDefault;

  CustomerSavedAddressDto({
    required this.id,
    this.label,
    required this.address,
    this.landmark,
    this.deliveryAreaId,
    this.deliveryAreaName,
    this.latitude,
    this.longitude,
    this.isDefault = false,
  });

  String get displayLabel => (label?.trim().isNotEmpty == true) ? label!.trim() : 'Saved address';

  factory CustomerSavedAddressDto.fromJson(Map<String, dynamic> json) => CustomerSavedAddressDto(
        id: asInt(json['id']),
        label: asStringOrNull(json['label']),
        address: json['address']?.toString() ?? '',
        landmark: asStringOrNull(json['landmark']),
        deliveryAreaId: asIntOrNull(json['deliveryAreaId']),
        deliveryAreaName: asStringOrNull(json['deliveryAreaName']),
        latitude: json['latitude'] == null ? null : asDouble(json['latitude']),
        longitude: json['longitude'] == null ? null : asDouble(json['longitude']),
        isDefault: asBool(json['isDefault']),
      );
}

class PosCustomerDto {
  final int id;
  final String name;
  final String? phone;
  final String? email;

  PosCustomerDto({required this.id, required this.name, this.phone, this.email});

  factory PosCustomerDto.fromJson(Map<String, dynamic> json) => PosCustomerDto(
        id: asInt(json['id']),
        name: json['name']?.toString() ?? 'Customer',
        phone: asStringOrNull(json['phone'] ?? json['mobile']),
        email: asStringOrNull(json['email']),
      );
}

class CatalogPageResult {
  final List items;
  final int totalCount;

  CatalogPageResult({required this.items, required this.totalCount});
}
