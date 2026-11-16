import '../core/api/abp_api_client.dart';
import '../core/api/json_helpers.dart';
import '../core/constants/app_endpoints.dart';
import '../models/rider_models.dart';

/// Rider APIs — mirrors MAUI `RiderOrderService`.
class RiderService {
  final AbpApiClient _api;

  RiderService(this._api);

  Future<RiderDashboardStatsDto?> getDashboardStats() async {
    try {
      final res = await _api.get(AppEndpoints.getRiderDashboardStats);
      if (res is Map) {
        return RiderDashboardStatsDto.fromJson(Map<String, dynamic>.from(res));
      }
    } catch (_) {}
    return null;
  }

  Future<List<RiderOrderDto>> getAssignedOrders() async {
    try {
      final res = await _api.get(AppEndpoints.getRiderOrders);
      if (res is List) {
        return res
            .map((o) => RiderOrderDto.fromJson(Map<String, dynamic>.from(o as Map)))
            .toList();
      }
    } catch (_) {}
    return [];
  }

  Future<List<RiderOrderDto>> getOrderHistory({int skip = 0, int max = 20, String? keyword}) async {
    try {
      var url =
          '${AppEndpoints.getRiderHistory}?SkipCount=$skip&MaxResultCount=$max';
      if (keyword != null && keyword.trim().isNotEmpty) {
        url += '&Keyword=${Uri.encodeQueryComponent(keyword.trim())}';
      }
      final res = await _api.get(url);
      List raw = [];
      if (res is List) {
        raw = res;
      } else if (res is Map && res['items'] is List) {
        raw = res['items'] as List;
      }
      return raw
          .map((o) => RiderOrderDto.fromJson(Map<String, dynamic>.from(o as Map)))
          .toList();
    } catch (_) {}
    return [];
  }

  Future<RiderOrderDto?> acceptOrder(int orderId) async {
    try {
      final res = await _api.post(AppEndpoints.acceptRiderOrder, data: {'id': orderId});
      if (res is Map) return RiderOrderDto.fromJson(Map<String, dynamic>.from(res));
    } catch (_) {}
    return null;
  }

  Future<RiderOrderDto?> rejectOrder(int orderId, String reason) async {
    try {
      final res = await _api.post(AppEndpoints.rejectRiderOrder, data: {
        'orderId': orderId,
        'reason': reason,
      });
      if (res is Map) return RiderOrderDto.fromJson(Map<String, dynamic>.from(res));
    } catch (_) {}
    return null;
  }

  Future<RiderOrderDto?> completeDelivery(int orderId) async {
    try {
      final res = await _api.post(AppEndpoints.completeDelivery, data: {'id': orderId});
      if (res is Map) return RiderOrderDto.fromJson(Map<String, dynamic>.from(res));
    } catch (_) {}
    return null;
  }
}

class RiderDashboardStatsDto {
  final int todayDeliveredCount;
  final int activeAssignedCount;
  final double todayDeliveredAmount;
  final double activePendingAmount;
  final int totalAssignedToday;
  final String? riderName;
  final String? branchName;

  RiderDashboardStatsDto({
    this.todayDeliveredCount = 0,
    this.activeAssignedCount = 0,
    this.todayDeliveredAmount = 0,
    this.activePendingAmount = 0,
    this.totalAssignedToday = 0,
    this.riderName,
    this.branchName,
  });

  factory RiderDashboardStatsDto.fromJson(Map<String, dynamic> json) => RiderDashboardStatsDto(
        todayDeliveredCount: asInt(json['todayDeliveredCount']),
        activeAssignedCount: asInt(json['activeAssignedCount']),
        todayDeliveredAmount: asDouble(json['todayDeliveredAmount']),
        activePendingAmount: asDouble(json['activePendingAmount']),
        totalAssignedToday: asInt(json['totalAssignedToday']),
        riderName: asStringOrNull(json['riderName']),
        branchName: asStringOrNull(json['branchName']),
      );
}
