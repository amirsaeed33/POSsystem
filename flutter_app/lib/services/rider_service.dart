import '../core/api/abp_api_client.dart';
import '../core/constants/app_endpoints.dart';
import '../models/rider_models.dart';

/// Rider APIs — mirrors Angular `RiderService` / MAUI `RiderOrderService`.
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

  Future<PagedRiderOrderResult> getOrderHistory({
    int skip = 0,
    int max = 20,
    String? keyword,
    DateTime? fromDate,
    DateTime? toDate,
    int status = RiderOrderStatus.delivered,
  }) async {
    try {
      final params = <String, String>{
        'SkipCount': '$skip',
        'MaxResultCount': '$max',
        'Status': '$status',
      };
      if (keyword != null && keyword.trim().isNotEmpty) {
        params['Keyword'] = keyword.trim();
      }
      if (fromDate != null) {
        params['FromDate'] = _toDateString(fromDate);
      }
      if (toDate != null) {
        params['ToDate'] = _toDateString(toDate);
      }

      final query = params.entries
          .map((e) => '${e.key}=${Uri.encodeQueryComponent(e.value)}')
          .join('&');
      final res = await _api.get('${AppEndpoints.getRiderHistory}?$query');

      if (res is Map) {
        final map = Map<String, dynamic>.from(res);
        final raw = map['items'] is List ? map['items'] as List : <dynamic>[];
        return PagedRiderOrderResult(
          totalCount: (map['totalCount'] as num?)?.toInt() ?? raw.length,
          items: raw
              .map((o) => RiderOrderDto.fromJson(Map<String, dynamic>.from(o as Map)))
              .toList(),
        );
      }
      if (res is List) {
        final items = res
            .map((o) => RiderOrderDto.fromJson(Map<String, dynamic>.from(o as Map)))
            .toList();
        return PagedRiderOrderResult(totalCount: items.length, items: items);
      }
    } catch (_) {}
    return PagedRiderOrderResult();
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

  Future<RiderOrderDto?> completeDelivery(
    int orderId, {
    String? notes,
    int? rating,
  }) async {
    try {
      final res = await _api.post(AppEndpoints.completeDelivery, data: {
        'orderId': orderId,
        if (notes != null && notes.trim().isNotEmpty) 'notes': notes.trim(),
        if (rating != null && rating >= 1 && rating <= 5) 'rating': rating,
      });
      if (res is Map) return RiderOrderDto.fromJson(Map<String, dynamic>.from(res));
    } catch (_) {}
    return null;
  }

  static String _toDateString(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';
}
