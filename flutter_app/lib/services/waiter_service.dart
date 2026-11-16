import '../core/api/abp_api_client.dart';
import '../core/api/json_helpers.dart';
import '../core/constants/app_endpoints.dart';
import '../models/waiter_models.dart';
import '../models/product_models.dart';

/// Waiter/restaurant APIs — mirrors MAUI `RestaurantService`.
class WaiterService {
  final AbpApiClient _api;

  WaiterService(this._api);

  Future<List<RestaurantTableDto>> getTables(int branchId) async {
    final res = await _api.get('${AppEndpoints.getTables}?branchId=$branchId');
    if (res is List) {
      return res
          .map((t) => RestaurantTableDto.fromJson(Map<String, dynamic>.from(t as Map)))
          .toList();
    }
    return [];
  }

  Future<void> assignWaiter(int tableId, int waiterId) async {
    await _api.post(AppEndpoints.assignWaiter, data: {
      'tableId': tableId,
      'waiterId': waiterId,
    });
  }

  Future<void> changeTableStatus(int tableId, int status) async {
    await _api.post(AppEndpoints.changeTableStatus, data: {
      'tableId': tableId,
      'status': status,
    });
  }

  Future<List<RestaurantOrderDto>> getActiveOrders(int branchId) async {
    final res = await _api.get('${AppEndpoints.getActiveOrders}?branchId=$branchId');
    if (res is List) {
      return res
          .map((o) => RestaurantOrderDto.fromJson(Map<String, dynamic>.from(o as Map)))
          .toList();
    }
    return [];
  }

  Future<RestaurantOrderDto?> getOrder(int id) async {
    try {
      final res = await _api.get('${AppEndpoints.getOrder}?Id=$id');
      if (res is Map) return RestaurantOrderDto.fromJson(Map<String, dynamic>.from(res));
    } catch (_) {}
    return null;
  }

  Future<RestaurantOrderDto?> getOrderByTable(int tableId) async {
    try {
      final res = await _api.get('${AppEndpoints.getOrderByTable}?tableId=$tableId');
      if (res is Map) return RestaurantOrderDto.fromJson(Map<String, dynamic>.from(res));
    } catch (_) {}
    return null;
  }

  Future<RestaurantOrderDto> openTableSession({
    required int tableId,
    required int covers,
    int? customerId,
  }) async {
    final res = await _api.post(AppEndpoints.openTableSession, data: {
      'tableId': tableId,
      'covers': covers,
      if (customerId != null) 'customerId': customerId,
    });
    return RestaurantOrderDto.fromJson(Map<String, dynamic>.from(res as Map));
  }

  Future<RestaurantOrderDto> saveOrder(Map<String, dynamic> input) async {
    final res = await _api.post(AppEndpoints.saveOrder, data: input);
    return RestaurantOrderDto.fromJson(Map<String, dynamic>.from(res as Map));
  }

  Future<void> sendToKitchen(int orderId) async {
    await _api.post('${AppEndpoints.sendToKitchen}?orderId=$orderId');
  }

  Future<void> printBill(int orderId) async {
    await _api.post('${AppEndpoints.printBill}?orderId=$orderId');
  }

  Future<void> settleOrder({
    required int orderId,
    required String paymentMethod,
    required double paidAmount,
  }) async {
    await _api.post(AppEndpoints.settleOrder, data: {
      'orderId': orderId,
      'paymentMethod': paymentMethod,
      'paidAmount': paidAmount,
    });
  }

  Future<void> cancelOrder(int orderId) async {
    await _api.post('${AppEndpoints.cancelOrder}?orderId=$orderId');
  }

  Future<List<PosProductDto>> getPosProducts({int? categoryId}) async {
    try {
      final url = categoryId != null
          ? '${AppEndpoints.getPosProducts}?categoryId=$categoryId'
          : AppEndpoints.getPosProducts;
      final res = await _api.get(url);
      List raw = [];
      if (res is List) {
        raw = res;
      } else if (res is Map && res['items'] is List) {
        raw = res['items'] as List;
      }
      return raw.map((p) => PosProductDto.fromJson(Map<String, dynamic>.from(p as Map))).toList();
    } catch (_) {
      return [];
    }
  }

  Future<List<ProductModifierGroupDto>> getModifiersForProduct(int productId) async {
    try {
      final res = await _api.get('${AppEndpoints.getModifiersForProduct}?productId=$productId');
      if (res is List) {
        return res
            .map((g) => ProductModifierGroupDto.fromJson(Map<String, dynamic>.from(g as Map)))
            .toList();
      }
    } catch (_) {}
    return [];
  }
}

class PosProductDto {
  final int id;
  final String name;
  final double price;
  final String? categoryName;
  final int? categoryId;
  final String? imagePath;

  PosProductDto({
    required this.id,
    required this.name,
    required this.price,
    this.categoryName,
    this.categoryId,
    this.imagePath,
  });

  factory PosProductDto.fromJson(Map<String, dynamic> json) => PosProductDto(
        id: asInt(json['id']),
        name: json['name']?.toString() ?? 'Item',
        price: asDouble(json['price'] ?? json['salesPrice'] ?? json['unitPrice']),
        categoryName: asStringOrNull(json['categoryName']),
        categoryId: asIntOrNull(json['categoryId']),
        imagePath: asStringOrNull(json['imagePath']),
      );
}
