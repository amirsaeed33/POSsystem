import '../core/api/abp_api_client.dart';
import '../core/api/json_helpers.dart';
import '../core/constants/app_endpoints.dart';

/// Admin dashboard APIs — mirrors MAUI `DashboardService`.
class DashboardService {
  final AbpApiClient _api;

  DashboardService(this._api);

  Future<DashboardDto?> getDashboard() async {
    try {
      final res = await _api.get(AppEndpoints.getDashboard);
      if (res is Map) {
        return DashboardDto.fromJson(Map<String, dynamic>.from(res));
      }
    } catch (_) {}
    return null;
  }

  Future<OnlineOrderPipelineDto?> getOnlineOrderPipeline({int? branchId}) async {
    try {
      final url = branchId != null && branchId > 0
          ? '${AppEndpoints.getOnlineOrderPipeline}?BranchId=$branchId'
          : AppEndpoints.getOnlineOrderPipeline;
      final res = await _api.get(url);
      if (res is Map) {
        return OnlineOrderPipelineDto.fromJson(Map<String, dynamic>.from(res));
      }
    } catch (_) {}
    return null;
  }
}

class DashboardDto {
  final String? userDisplayName;
  final double todaySales;
  final double todayProfit;
  final int totalProducts;
  final int lowStockCount;
  final int pendingOrdersCount;
  final int todaySalesCount;

  DashboardDto({
    this.userDisplayName,
    this.todaySales = 0,
    this.todayProfit = 0,
    this.totalProducts = 0,
    this.lowStockCount = 0,
    this.pendingOrdersCount = 0,
    this.todaySalesCount = 0,
  });

  factory DashboardDto.fromJson(Map<String, dynamic> json) {
    final qa = json['quickActions'] is Map ? Map<String, dynamic>.from(json['quickActions'] as Map) : null;
    return DashboardDto(
      userDisplayName: asStringOrNull(json['userDisplayName']),
      todaySales: asDouble(json['todaySales']),
      todayProfit: asDouble(json['todayProfit']),
      totalProducts: asInt(json['totalProducts']),
      lowStockCount: asInt(json['lowStockCount'] ?? qa?['lowStockCount']),
      pendingOrdersCount: asInt(qa?['pendingOrdersCount']),
      todaySalesCount: asInt(qa?['todaySalesCount']),
    );
  }
}

class OnlineOrderPipelineDto {
  final int placed;
  final int preparing;
  final int outForDelivery;
  final int delivered;

  OnlineOrderPipelineDto({
    this.placed = 0,
    this.preparing = 0,
    this.outForDelivery = 0,
    this.delivered = 0,
  });

  factory OnlineOrderPipelineDto.fromJson(Map<String, dynamic> json) => OnlineOrderPipelineDto(
        placed: asInt(json['placed'] ?? json['pending'] ?? json['newOrders']),
        preparing: asInt(json['preparing'] ?? json['inKitchen']),
        outForDelivery: asInt(json['outForDelivery'] ?? json['onRoute']),
        delivered: asInt(json['delivered'] ?? json['completed']),
      );
}
