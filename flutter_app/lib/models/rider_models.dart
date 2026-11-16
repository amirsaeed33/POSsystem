import '../core/api/json_helpers.dart';

class RiderOrderDto {
  final int id;
  final String orderNo;
  final String customerName;
  final String customerPhone;
  final String deliveryAddress;
  final double grandTotal;
  final int status;
  final String statusName;
  final DateTime orderDate;
  final String? notes;
  final bool isAcceptedByRider;

  RiderOrderDto({
    required this.id,
    required this.orderNo,
    required this.customerName,
    required this.customerPhone,
    required this.deliveryAddress,
    required this.grandTotal,
    required this.status,
    required this.statusName,
    required this.orderDate,
    this.notes,
    this.isAcceptedByRider = false,
  });

  bool get isNewOrder => !isAcceptedByRider;
  String get priceDisplay => 'Rs. ${grandTotal.toStringAsFixed(0)}';
  String get badgeText => isNewOrder ? 'NEW' : statusName;

  factory RiderOrderDto.fromJson(Map<String, dynamic> json) {
    final total = asDouble(
      json['totalAmount'] ?? json['grandTotal'] ?? json['subTotal'],
    );
    return RiderOrderDto(
      id: asInt(json['id']),
      orderNo: json['orderNo']?.toString() ?? '#${asInt(json['id'])}',
      customerName: json['customerName']?.toString() ?? 'Customer',
      customerPhone: (json['customerPhone'] ?? '').toString(),
      deliveryAddress: (json['customerAddress'] ?? json['deliveryAddress'] ?? '').toString(),
      grandTotal: total,
      status: asInt(json['status']),
      statusName: (json['statusText'] ?? json['statusName'] ?? 'Pending').toString(),
      orderDate: json['orderDate'] != null
          ? DateTime.tryParse(json['orderDate'].toString()) ?? DateTime.now()
          : DateTime.now(),
      notes: asStringOrNull(json['notes']),
      isAcceptedByRider: asBool(json['isAcceptedByRider']),
    );
  }
}
