import '../core/api/json_helpers.dart';

class CreateCustomerOrderLineDto {
  final int productId;
  final double quantity;
  final double unitPrice;

  CreateCustomerOrderLineDto({
    required this.productId,
    required this.quantity,
    required this.unitPrice,
  });

  Map<String, dynamic> toJson() => {
        'productId': productId,
        'quantity': quantity,
        'unitPrice': unitPrice,
      };
}

class CreateCustomerOrderDto {
  final int? branchId;
  final String customerName;
  final String customerMobile;
  final String? deliveryAddress;
  final String? deliveryLandmark;
  final String? notes;
  final String paymentMethod;
  final double? latitude;
  final double? longitude;
  final double subtotal;
  final double deliveryCharges;
  final double grandTotal;
  final List<CreateCustomerOrderLineDto> lines;

  CreateCustomerOrderDto({
    this.branchId,
    required this.customerName,
    required this.customerMobile,
    this.deliveryAddress,
    this.deliveryLandmark,
    this.notes,
    this.paymentMethod = 'CashOnDelivery',
    this.latitude,
    this.longitude,
    required this.subtotal,
    required this.deliveryCharges,
    required this.grandTotal,
    required this.lines,
  });

  Map<String, dynamic> toJson() => {
        if (branchId != null) 'branchId': branchId,
        'customerName': customerName,
        'customerMobile': customerMobile,
        'orderDate': DateTime.now().toIso8601String(),
        if (deliveryAddress != null) 'deliveryAddress': deliveryAddress,
        if (deliveryLandmark != null) 'deliveryLandmark': deliveryLandmark,
        if (notes != null) 'notes': notes,
        'paymentMethod': paymentMethod,
        if (latitude != null) 'latitude': latitude,
        if (longitude != null) 'longitude': longitude,
        'subtotal': subtotal,
        'deliveryCharges': deliveryCharges,
        'grandTotal': grandTotal,
        'lines': lines.map((l) => l.toJson()).toList(),
      };
}

class CustomerOrderDto {
  final int id;
  final int branchId;
  final String? branchName;
  final String? customerName;
  final String? customerPhone;
  final DateTime orderDate;
  final String? orderNo;
  final int status;
  final String? statusName;
  final double subTotal;
  final double? deliveryCharges;
  final double grandTotal;
  final String? riderName;
  final String? riderPhone;

  CustomerOrderDto({
    required this.id,
    required this.branchId,
    this.branchName,
    this.customerName,
    this.customerPhone,
    required this.orderDate,
    this.orderNo,
    required this.status,
    this.statusName,
    required this.subTotal,
    this.deliveryCharges,
    required this.grandTotal,
    this.riderName,
    this.riderPhone,
  });

  String get displayOrderNo =>
      orderNo != null && orderNo!.isNotEmpty ? (orderNo!.startsWith('#') ? orderNo! : '#$orderNo') : '#$id';

  String get priceDisplay => 'Rs. ${grandTotal.toStringAsFixed(0)}';

  String get statusText {
    if (statusName != null && statusName!.isNotEmpty) return statusName!;
    switch (status) {
      case 0:
        return 'Placed';
      case 1:
        return 'Confirmed';
      case 2:
        return 'Preparing';
      case 3:
        return 'Out for Delivery';
      case 4:
        return 'Delivered';
      case 5:
        return 'Cancelled';
      default:
        return 'Processing';
    }
  }

  factory CustomerOrderDto.fromJson(Map<String, dynamic> json) {
    final grand = asDouble(
      json['totalAmount'] ?? json['grandTotal'] ?? json['subTotal'],
    );
    return CustomerOrderDto(
      id: asInt(json['id']),
      branchId: asInt(json['branchId']),
      branchName: asStringOrNull(json['branchName']),
      customerName: asStringOrNull(json['customerName']),
      customerPhone: asStringOrNull(json['customerPhone'] ?? json['customerMobile']),
      orderDate: json['orderDate'] != null
          ? DateTime.tryParse(json['orderDate'].toString()) ?? DateTime.now()
          : DateTime.now(),
      orderNo: asStringOrNull(json['orderNo']),
      status: asInt(json['status']),
      statusName: asStringOrNull(json['statusName'] ?? json['statusText']),
      subTotal: asDouble(json['subTotal'] ?? json['subtotal']),
      deliveryCharges: json['deliveryFee'] != null || json['deliveryCharges'] != null
          ? asDouble(json['deliveryFee'] ?? json['deliveryCharges'])
          : null,
      grandTotal: grand,
      riderName: asStringOrNull(json['riderName']),
      riderPhone: asStringOrNull(json['riderPhone']),
    );
  }
}

class OrderTrackingDto {
  final int orderId;
  final String orderNo;
  final int status;
  final String statusText;
  final String? riderName;
  final String? riderPhone;
  final int estimatedMinutes;

  OrderTrackingDto({
    required this.orderId,
    required this.orderNo,
    required this.status,
    required this.statusText,
    this.riderName,
    this.riderPhone,
    this.estimatedMinutes = 30,
  });

  factory OrderTrackingDto.fromOrder(CustomerOrderDto order) => OrderTrackingDto(
        orderId: order.id,
        orderNo: order.displayOrderNo,
        status: order.status.clamp(0, 4),
        statusText: order.statusText,
        riderName: order.riderName,
        riderPhone: order.riderPhone,
      );

  factory OrderTrackingDto.fromJson(Map<String, dynamic> json) {
    return OrderTrackingDto(
      orderId: asInt(json['orderId'] ?? json['id']),
      orderNo: (json['orderNo'] ?? '').toString(),
      status: asInt(json['status']),
      statusText: (json['statusText'] ?? json['statusName'] ?? 'Processing').toString(),
      riderName: asStringOrNull(json['riderName']),
      riderPhone: asStringOrNull(json['riderPhone']),
      estimatedMinutes: asInt(json['estimatedMinutes'], 30),
    );
  }
}
