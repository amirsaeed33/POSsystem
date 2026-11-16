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
  final double todayPurchases;
  final double todayExpenses;
  final double growthPercentage;
  final bool isGrowthPositive;
  final int totalProducts;
  final int inStockCount;
  final int lowStockCount;
  final int outOfStockCount;
  final int pendingOrdersCount;
  final int todaySalesCount;
  final List<DailyOverviewDto> last7Days;

  DashboardDto({
    this.userDisplayName,
    this.todaySales = 0,
    this.todayProfit = 0,
    this.todayPurchases = 0,
    this.todayExpenses = 0,
    this.growthPercentage = 0,
    this.isGrowthPositive = true,
    this.totalProducts = 0,
    this.inStockCount = 0,
    this.lowStockCount = 0,
    this.outOfStockCount = 0,
    this.pendingOrdersCount = 0,
    this.todaySalesCount = 0,
    this.last7Days = const [],
  });

  factory DashboardDto.fromJson(Map<String, dynamic> json) {
    final qa = json['quickActions'] is Map ? Map<String, dynamic>.from(json['quickActions'] as Map) : null;
    final days = (json['last7Days'] as List?)
            ?.map((e) => DailyOverviewDto.fromJson(Map<String, dynamic>.from(e as Map)))
            .toList() ??
        [];
    return DashboardDto(
      userDisplayName: asStringOrNull(json['userDisplayName']),
      todaySales: asDouble(json['todaySales']),
      todayProfit: asDouble(json['todayProfit']),
      todayPurchases: asDouble(json['todayPurchases']),
      todayExpenses: asDouble(json['todayExpenses']),
      growthPercentage: asDouble(json['growthPercentage']),
      isGrowthPositive: asBool(json['isGrowthPositive'], true),
      totalProducts: asInt(json['totalProducts']),
      inStockCount: asInt(json['inStockCount']),
      lowStockCount: asInt(json['lowStockCount'] ?? qa?['lowStockCount']),
      outOfStockCount: asInt(json['outOfStockCount']),
      pendingOrdersCount: asInt(qa?['pendingOrdersCount']),
      todaySalesCount: asInt(qa?['todaySalesCount']),
      last7Days: days,
    );
  }
}

class DailyOverviewDto {
  final DateTime date;
  final String? dayLabel;
  final double sales;
  final double purchases;
  final double profit;

  DailyOverviewDto({
    required this.date,
    this.dayLabel,
    this.sales = 0,
    this.purchases = 0,
    this.profit = 0,
  });

  factory DailyOverviewDto.fromJson(Map<String, dynamic> json) => DailyOverviewDto(
        date: DateTime.tryParse(json['date']?.toString() ?? '') ?? DateTime.now(),
        dayLabel: asStringOrNull(json['dayLabel']),
        sales: asDouble(json['sales']),
        purchases: asDouble(json['purchases']),
        profit: asDouble(json['profit']),
      );
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
