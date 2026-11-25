import '../core/api/json_helpers.dart';

class RestaurantTableDto {
  final int id;
  final String tableNumber;
  final int capacity;
  final int status; // 0=Available, 1=Occupied, 2=Reserved, 3=Billed
  final int? waiterId;
  final String? waiterName;
  final int? activeOrderId;
  final double? activeOrderAmount;

  RestaurantTableDto({
    required this.id,
    required this.tableNumber,
    this.capacity = 4,
    this.status = 0,
    this.waiterId,
    this.waiterName,
    this.activeOrderId,
    this.activeOrderAmount,
  });

  bool get isAvailable => status == 0;
  bool get isOccupied => status == 1;
  bool get isReserved => status == 2;
  bool get isBilled => status == 3;

  String get statusText {
    switch (status) {
      case 0:
        return 'Available';
      case 1:
        return 'Occupied';
      case 2:
        return 'Reserved';
      case 3:
        return 'Billed';
      default:
        return 'Available';
    }
  }

  factory RestaurantTableDto.fromJson(Map<String, dynamic> json) {
    return RestaurantTableDto(
      id: asInt(json['id']),
      tableNumber: json['tableNumber']?.toString() ?? 'T-${asInt(json['id'])}',
      capacity: asInt(json['capacity'], 4),
      status: asInt(json['status']),
      waiterId: asIntOrNull(json['waiterId']),
      waiterName: asStringOrNull(json['waiterName']),
      activeOrderId: asIntOrNull(json['activeOrderId']),
      activeOrderAmount:
          json['activeOrderAmount'] == null ? null : asDouble(json['activeOrderAmount']),
    );
  }
}

class RestaurantOrderItemDto {
  final int? id;
  final int productId;
  final String productName;
  final double quantity;
  final double unitPrice;
  final double totalPrice;
  final String? notes;
  final bool isPrinted;

  RestaurantOrderItemDto({
    this.id,
    required this.productId,
    required this.productName,
    required this.quantity,
    required this.unitPrice,
    required this.totalPrice,
    this.notes,
    this.isPrinted = false,
  });

  factory RestaurantOrderItemDto.fromJson(Map<String, dynamic> json) {
    return RestaurantOrderItemDto(
      id: asIntOrNull(json['id']),
      productId: asInt(json['productId']),
      productName: json['productName']?.toString() ?? 'Item',
      quantity: asDouble(json['quantity'], 1),
      unitPrice: asDouble(json['unitPrice']),
      totalPrice: asDouble(json['totalPrice']),
      notes: asStringOrNull(json['notes']),
      isPrinted: asBool(json['isPrinted']),
    );
  }

  Map<String, dynamic> toJson() => {
        if (id != null) 'id': id,
        'productId': productId,
        'productName': productName,
        'quantity': quantity,
        'unitPrice': unitPrice,
        'totalPrice': totalPrice,
        if (notes != null) 'notes': notes,
        'isPrinted': isPrinted,
      };
}

class RestaurantOrderDto {
  final int id;
  final String orderNo;
  final int? tableId;
  final String? tableName;
  final int covers;
  final double subTotal;
  final double grandTotal;
  final int status;
  final List<RestaurantOrderItemDto> items;

  RestaurantOrderDto({
    required this.id,
    required this.orderNo,
    this.tableId,
    this.tableName,
    this.covers = 1,
    required this.subTotal,
    required this.grandTotal,
    this.status = 0,
    this.items = const [],
  });

  factory RestaurantOrderDto.fromJson(Map<String, dynamic> json) {
    return RestaurantOrderDto(
      id: asInt(json['id']),
      orderNo: json['orderNo']?.toString() ?? '#${asInt(json['id'])}',
      tableId: asIntOrNull(json['tableId']),
      tableName: asStringOrNull(json['tableName']),
      covers: asInt(json['covers'], 1),
      subTotal: asDouble(json['subTotal']),
      grandTotal: asDouble(json['grandTotal']),
      status: asInt(json['status']),
      items: (json['items'] as List<dynamic>?)
              ?.map((i) => RestaurantOrderItemDto.fromJson(Map<String, dynamic>.from(i as Map)))
              .toList() ??
          [],
    );
  }
}

