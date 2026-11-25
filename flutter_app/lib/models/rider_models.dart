import '../core/api/json_helpers.dart';

/// Delivered = 4 — same as web `RiderOrderStatus.Delivered`.
class RiderOrderStatus {
  static const int pending = 0;
  static const int approved = 1;
  static const int rejected = 2;
  static const int inProgress = 3;
  static const int delivered = 4;
  static const int completed = 5;
}

class RiderOrderItemDto {
  final int productId;
  final String productName;
  final double quantity;
  final double unitPrice;
  final double lineTotal;
  final bool isDeal;

  RiderOrderItemDto({
    this.productId = 0,
    this.productName = '',
    this.quantity = 0,
    this.unitPrice = 0,
    this.lineTotal = 0,
    this.isDeal = false,
  });

  factory RiderOrderItemDto.fromJson(Map<String, dynamic> json) => RiderOrderItemDto(
        productId: asInt(json['productId']),
        productName: (json['productName'] ?? '').toString(),
        quantity: asDouble(json['quantity']),
        unitPrice: asDouble(json['unitPrice']),
        lineTotal: asDouble(json['lineTotal']),
        isDeal: asBool(json['isDeal']),
      );
}

class RiderOrderDto {
  final int id;
  final String orderNo;
  final String customerName;
  final String customerPhone;
  final String deliveryAddress;
  final String? deliveryAreaName;
  final double grandTotal;
  final double subTotal;
  final double deliveryFee;
  final int status;
  final String statusName;
  final DateTime orderDate;
  final DateTime? riderAssignedTime;
  final String? notes;
  final bool isAcceptedByRider;
  final String? paymentMethod;
  final double? latitude;
  final double? longitude;
  final double? distanceKm;
  final int? deliveryRating;
  final int? customerRating;
  final List<RiderOrderItemDto> items;

  RiderOrderDto({
    required this.id,
    required this.orderNo,
    required this.customerName,
    required this.customerPhone,
    required this.deliveryAddress,
    this.deliveryAreaName,
    required this.grandTotal,
    this.subTotal = 0,
    this.deliveryFee = 0,
    required this.status,
    required this.statusName,
    required this.orderDate,
    this.riderAssignedTime,
    this.notes,
    this.isAcceptedByRider = false,
    this.paymentMethod,
    this.latitude,
    this.longitude,
    this.distanceKm,
    this.deliveryRating,
    this.customerRating,
    this.items = const [],
  });

  bool get isNewOrder => !isAcceptedByRider;
  String get priceDisplay => 'Rs. ${grandTotal.toStringAsFixed(0)}';
  String get badgeText => isNewOrder ? 'NEW' : 'IN TRANSIT';

  String get paymentLabel {
    final p = (paymentMethod ?? '').toLowerCase();
    if (p.contains('online') || p.contains('card') || p.contains('jazz') || p.contains('easypaisa')) {
      return 'Online';
    }
    return 'Cash on Delivery';
  }

  bool get hasCoords => latitude != null && longitude != null;

  String get itemsSummary {
    if (items.isEmpty) return 'No items listed';
    final names = items
        .where((i) => i.productName.trim().isNotEmpty)
        .take(3)
        .map((i) => '${i.quantity.toStringAsFixed(i.quantity == i.quantity.roundToDouble() ? 0 : 1)}× ${i.productName}');
    final text = names.join(' · ');
    return items.length > 3 ? '$text +${items.length - 3}' : text;
  }

  String get areaLine =>
      (deliveryAreaName != null && deliveryAreaName!.trim().isNotEmpty)
          ? deliveryAreaName!.trim()
          : 'Delivery';

  Duration get elapsedSinceAssigned {
    final start = riderAssignedTime ?? orderDate;
    return DateTime.now().difference(start);
  }

  bool get isLate => elapsedSinceAssigned.inMinutes >= 45;

  factory RiderOrderDto.fromJson(Map<String, dynamic> json) {
    final total = asDouble(
      json['totalAmount'] ?? json['grandTotal'] ?? json['subTotal'],
    );
    final rawItems = json['items'];
    final items = <RiderOrderItemDto>[];
    if (rawItems is List) {
      for (final item in rawItems) {
        if (item is Map) {
          items.add(RiderOrderItemDto.fromJson(Map<String, dynamic>.from(item)));
        }
      }
    }

    return RiderOrderDto(
      id: asInt(json['id']),
      orderNo: json['orderNo']?.toString() ?? '#${asInt(json['id'])}',
      customerName: json['customerName']?.toString() ?? 'Customer',
      customerPhone: (json['customerPhone'] ?? '').toString(),
      deliveryAddress: (json['customerAddress'] ?? json['deliveryAddress'] ?? '').toString(),
      deliveryAreaName: asStringOrNull(json['deliveryAreaName']),
      grandTotal: total,
      subTotal: asDouble(json['subTotal']),
      deliveryFee: asDouble(json['deliveryFee']),
      status: asInt(json['status']),
      statusName: (json['statusText'] ?? json['statusName'] ?? 'Pending').toString(),
      orderDate: json['orderDate'] != null
          ? DateTime.tryParse(json['orderDate'].toString()) ?? DateTime.now()
          : DateTime.now(),
      riderAssignedTime: json['riderAssignedTime'] != null
          ? DateTime.tryParse(json['riderAssignedTime'].toString())
          : null,
      notes: asStringOrNull(json['notes']),
      isAcceptedByRider: asBool(json['isAcceptedByRider']),
      paymentMethod: asStringOrNull(json['paymentMethod']),
      latitude: json['latitude'] != null ? asDouble(json['latitude']) : null,
      longitude: json['longitude'] != null ? asDouble(json['longitude']) : null,
      distanceKm: json['distanceKm'] != null ? asDouble(json['distanceKm']) : null,
      deliveryRating: asIntOrNull(json['deliveryRating']),
      customerRating: asIntOrNull(json['customerRating']),
      items: items,
    );
  }
}

class RiderDashboardStatsDto {
  final int todayDeliveredCount;
  final int activeAssignedCount;
  final double todayDeliveredAmount;
  final double activePendingAmount;
  final int totalAssignedToday;
  final String? riderName;
  final String? riderPhone;
  final String? branchName;

  RiderDashboardStatsDto({
    this.todayDeliveredCount = 0,
    this.activeAssignedCount = 0,
    this.todayDeliveredAmount = 0,
    this.activePendingAmount = 0,
    this.totalAssignedToday = 0,
    this.riderName,
    this.riderPhone,
    this.branchName,
  });

  factory RiderDashboardStatsDto.fromJson(Map<String, dynamic> json) => RiderDashboardStatsDto(
        todayDeliveredCount: asInt(json['todayDeliveredCount']),
        activeAssignedCount: asInt(json['activeAssignedCount']),
        todayDeliveredAmount: asDouble(json['todayDeliveredAmount']),
        activePendingAmount: asDouble(json['activePendingAmount']),
        totalAssignedToday: asInt(json['totalAssignedToday']),
        riderName: asStringOrNull(json['riderName']),
        riderPhone: asStringOrNull(json['riderPhone']),
        branchName: asStringOrNull(json['branchName']),
      );
}

class PagedRiderOrderResult {
  final int totalCount;
  final List<RiderOrderDto> items;

  PagedRiderOrderResult({this.totalCount = 0, this.items = const []});
}
