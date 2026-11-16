import '../core/api/abp_api_client.dart';
import '../core/api/json_helpers.dart';
import '../core/constants/app_endpoints.dart';
import '../models/branch_models.dart';
import '../models/product_models.dart';
import '../models/deal_models.dart';
import '../models/order_models.dart';

/// Customer online-order APIs — mirrors MAUI `OnlineOrderService`.
class OnlineCatalogService {
  final AbpApiClient _api;

  OnlineCatalogService(this._api);

  Future<List<OnlineBranchDto>> getBranches() async {
    final res = await _api.get(AppEndpoints.getOnlineBranches);
    if (res is List) {
      return res.map((b) {
        final dto = OnlineBranchDto.fromJson(Map<String, dynamic>.from(b as Map));
        return OnlineBranchDto(
          id: dto.id,
          name: dto.name,
          isRestaurant: dto.isRestaurant,
          address: dto.address,
          imagePath: dto.imagePath,
          phone: dto.phone,
          email: dto.email,
          website: dto.website,
          code: dto.code,
          logoUrl: _api.resolveImageUrl(dto.imagePath),
        );
      }).toList();
    }
    return [];
  }

  Future<OnlineStoreHeaderDto?> getStoreHeader(int branchId) async {
    try {
      final res = await _api.get('${AppEndpoints.getOnlineStoreHeader}?branchId=$branchId');
      if (res is Map) {
        return OnlineStoreHeaderDto.fromJson(Map<String, dynamic>.from(res));
      }
    } catch (_) {}
    return null;
  }

  Future<List<OnlineProductDto>> getCatalog(int? branchId) async {
    final url = branchId != null
        ? '${AppEndpoints.getOnlineCatalog}?branchId=$branchId'
        : AppEndpoints.getOnlineCatalog;
    final res = await _api.get(url);
    if (res is List) {
      return res.map((p) => _mapProduct(Map<String, dynamic>.from(p as Map))).toList();
    }
    return [];
  }

  OnlineProductDto _mapProduct(Map<String, dynamic> json) {
    final dto = OnlineProductDto.fromJson(json);
    return OnlineProductDto(
      id: dto.id,
      name: dto.name,
      description: dto.description,
      categoryName: dto.categoryName,
      categoryId: dto.categoryId,
      price: dto.price,
      currencyFormat: dto.currencyFormat,
      imagePath: dto.imagePath,
      imageUrl: _api.resolveImageUrl(dto.imagePath),
      inStock: dto.inStock,
      stockQuantity: dto.stockQuantity,
      isFreeDelivery: dto.isFreeDelivery,
      isDeal: dto.isDeal,
      cardTag: dto.cardTag,
      dealId: dto.dealId,
      originalPrice: dto.originalPrice,
      dealItemsSummary: dto.dealItemsSummary,
    );
  }

  Future<List<HeroSlideDto>> getHeroSlides(int branchId) async {
    try {
      final res = await _api.get('${AppEndpoints.getHeroSlides}?branchId=$branchId');
      if (res is List) {
        return res.map((s) {
          final dto = HeroSlideDto.fromJson(Map<String, dynamic>.from(s as Map));
          return HeroSlideDto(
            id: dto.id,
            branchId: dto.branchId,
            sortOrder: dto.sortOrder,
            badge: dto.badge,
            badgeIcon: dto.badgeIcon,
            titleMain: dto.titleMain,
            titleHighlight: dto.titleHighlight,
            titleEnd: dto.titleEnd,
            description: dto.description,
            offerPill: dto.offerPill,
            rating: dto.rating,
            primaryBtnText: dto.primaryBtnText,
            secondaryBtnText: dto.secondaryBtnText,
            imagePath: dto.imagePath,
            imageUrl: _api.resolveImageUrl(dto.imagePath),
          );
        }).toList();
      }
    } catch (_) {}
    return [];
  }

  Future<List<DealDto>> getDeals(int branchId) async {
    try {
      final res = await _api.get('${AppEndpoints.getDeals}?branchId=$branchId');
      List raw = [];
      if (res is List) {
        raw = res;
      } else if (res is Map && res['items'] is List) {
        raw = res['items'] as List;
      }
      return raw.map((d) {
        final dto = DealDto.fromJson(Map<String, dynamic>.from(d as Map));
        return DealDto(
          id: dto.id,
          branchId: dto.branchId,
          branchName: dto.branchName,
          name: dto.name,
          description: dto.description,
          dealPrice: dto.dealPrice,
          originalPrice: dto.originalPrice,
          categoryName: dto.categoryName,
          isActive: dto.isActive,
          productId: dto.productId,
          imagePath: dto.imagePath,
          imageUrl: _api.resolveImageUrl(dto.imagePath),
        );
      }).toList();
    } catch (_) {}
    return [];
  }

  Future<List<ProductModifierGroupDto>> getProductModifiers(int productId) async {
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

  /// MAUI: `CustomerOrder/CreateOnlineOrder`
  Future<CustomerOrderDto> createCustomerOrder(CreateCustomerOrderDto dto) async {
    final res = await _api.post(AppEndpoints.createOnlineOrder, data: dto.toJson());
    if (res is Map) {
      return CustomerOrderDto.fromJson(Map<String, dynamic>.from(res));
    }
    if (res is int) {
      return CustomerOrderDto(
        id: res,
        branchId: dto.branchId ?? 0,
        orderDate: DateTime.now(),
        status: 0,
        subTotal: dto.subtotal,
        grandTotal: dto.grandTotal,
        orderNo: res.toString(),
      );
    }
    throw Exception('Unexpected create-order response');
  }

  /// MAUI: `GetOrdersByPhone?Phone=&BranchId=&MaxResultCount=`
  Future<List<CustomerOrderDto>> getOrdersByPhone(String phone, {int? branchId, int max = 30}) async {
    if (phone.trim().isEmpty) return [];
    try {
      final qs = StringBuffer(
        '${AppEndpoints.getOrdersByPhone}?Phone=${Uri.encodeQueryComponent(phone.trim())}&MaxResultCount=$max',
      );
      if (branchId != null && branchId > 0) {
        qs.write('&BranchId=$branchId');
      }
      final res = await _api.get(qs.toString());
      if (res is List) {
        return res
            .map((o) => CustomerOrderDto.fromJson(Map<String, dynamic>.from(o as Map)))
            .toList();
      }
    } catch (_) {}
    return [];
  }

  /// MAUI: POST `TrackOrder?orderNo=`
  Future<CustomerOrderDto?> trackOrder(String orderNo) async {
    try {
      final clean = orderNo.replaceAll('#', '').trim();
      final res = await _api.post(
        '${AppEndpoints.trackOrder}?orderNo=${Uri.encodeQueryComponent(clean)}',
      );
      if (res is Map) {
        return CustomerOrderDto.fromJson(Map<String, dynamic>.from(res));
      }
    } catch (_) {}
    return null;
  }

  Future<Set<int>> getFavoriteProductIds({int? branchId}) async {
    try {
      final url = branchId != null
          ? '${AppEndpoints.getFavoriteProductIds}?branchId=$branchId'
          : AppEndpoints.getFavoriteProductIds;
      final res = await _api.get(url);
      if (res is List) {
        return res.map((e) => asInt(e)).where((id) => id > 0).toSet();
      }
    } catch (_) {}
    return {};
  }

  Future<bool> toggleFavorite(int productId, {int? branchId}) async {
    try {
      final res = await _api.post(AppEndpoints.toggleFavorite, data: {
        'productId': productId,
        if (branchId != null) 'branchId': branchId,
      });
      if (res is Map) {
        return asBool(res['isFavorite'] ?? res['added'] ?? res['result']);
      }
      return true;
    } catch (_) {
      return false;
    }
  }
}
