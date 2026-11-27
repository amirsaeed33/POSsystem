import '../core/api/abp_api_client.dart';
import '../core/api/json_helpers.dart';
import '../core/constants/app_endpoints.dart';
import '../models/branch_models.dart';
import '../models/product_models.dart';
import '../models/deal_models.dart';
import '../models/order_models.dart';
import '../models/checkout_models.dart';

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

  /// Storefront categories with optional uploaded images (matches web GetOnlineCategories).
  Future<List<OnlineCategoryDto>> getOnlineCategories({int? branchId}) async {
    try {
      final url = branchId != null
          ? '${AppEndpoints.getOnlineCategories}?branchId=$branchId'
          : AppEndpoints.getOnlineCategories;
      final res = await _api.get(url);
      if (res is! List) return [];

      final items = <OnlineCategoryDto>[];
      for (final raw in res) {
        if (raw is String) {
          final name = raw.trim();
          if (name.isNotEmpty) items.add(OnlineCategoryDto(name: name));
          continue;
        }
        if (raw is Map) {
          final dto = OnlineCategoryDto.fromJson(Map<String, dynamic>.from(raw));
          if (dto.name.isEmpty) continue;
          items.add(OnlineCategoryDto(
            name: dto.name,
            imagePath: dto.imagePath,
            imageUrl: _api.resolveImageUrl(dto.imagePath),
          ));
        }
      }
      return items;
    } catch (_) {
      return [];
    }
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
      List raw = [];
      if (res is List) {
        raw = res;
      } else if (res is Map) {
        final items = res['items'] ?? res['Items'];
        if (items is List) raw = items;
      }

      final defaultsRe = RegExp(r'OnlineOrderHeroImages/defaults', caseSensitive: false);
      final slides = <HeroSlideDto>[];
      for (final s in raw) {
        if (s is! Map) continue;
        final dto = HeroSlideDto.fromJson(Map<String, dynamic>.from(s));
        final path = (dto.bgImagePath ?? dto.imagePath ?? '').trim();
        if (path.isEmpty || defaultsRe.hasMatch(path)) continue;

        final imageUrl = _api.resolveImageUrl(dto.imagePath);
        final bgImageUrl = _api.resolveImageUrl(dto.bgImagePath?.isNotEmpty == true ? dto.bgImagePath : dto.imagePath);
        if (bgImageUrl.isEmpty && imageUrl.isEmpty) continue;

        slides.add(HeroSlideDto(
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
          bgImagePath: dto.bgImagePath,
          imageUrl: imageUrl,
          bgImageUrl: bgImageUrl.isNotEmpty ? bgImageUrl : imageUrl,
        ));
      }
      slides.sort((a, b) => a.sortOrder.compareTo(b.sortOrder));
      return slides;
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

  /// MAUI paged catalog with legacy fallback.
  Future<({List<OnlineProductDto> items, int totalCount})> getCatalogPage({
    int? branchId,
    int skip = 0,
    int take = 30,
    String? categoryName,
    String? keyword,
  }) async {
    try {
      final qs = StringBuffer(
        '${AppEndpoints.getOnlineCatalogPaged}?SkipCount=$skip&MaxResultCount=$take',
      );
      if (branchId != null && branchId > 0) qs.write('&BranchId=$branchId');
      if (categoryName != null && categoryName.trim().isNotEmpty && categoryName != 'All') {
        qs.write('&CategoryName=${Uri.encodeQueryComponent(categoryName.trim())}');
      }
      if (keyword != null && keyword.trim().isNotEmpty) {
        qs.write('&Keyword=${Uri.encodeQueryComponent(keyword.trim())}');
      }
      final res = await _api.get(qs.toString());
      if (res is Map) {
        final itemsRaw = res['items'] is List ? res['items'] as List : <dynamic>[];
        final items = itemsRaw
            .map((p) => _mapProduct(Map<String, dynamic>.from(p as Map)))
            .toList();
        return (items: items, totalCount: asInt(res['totalCount'], items.length));
      }
    } catch (_) {
      // fall through to legacy
    }

    final all = await getCatalog(branchId);
    var filtered = all;
    if (categoryName != null && categoryName != 'All') {
      filtered = filtered.where((p) => p.categoryName == categoryName).toList();
    }
    if (keyword != null && keyword.trim().isNotEmpty) {
      final q = keyword.toLowerCase();
      filtered = filtered
          .where((p) =>
              p.name.toLowerCase().contains(q) ||
              (p.description?.toLowerCase().contains(q) ?? false))
          .toList();
    }
    final page = filtered.skip(skip).take(take).toList();
    return (items: page, totalCount: filtered.length);
  }

  Future<List<DeliveryAreaDto>> getDeliveryAreas(int branchId) async {
    try {
      final res = await _api.get('${AppEndpoints.getDeliveryAreas}?branchId=$branchId');
      if (res is List) {
        return res
            .map((a) => DeliveryAreaDto.fromJson(Map<String, dynamic>.from(a as Map)))
            .where((a) => a.isActive)
            .toList();
      }
    } catch (_) {}
    return [];
  }

  Future<List<CustomerSavedAddressDto>> getSavedAddresses(String phone, {int? branchId}) async {
    if (phone.trim().isEmpty) return [];
    try {
      final qs = StringBuffer(
        '${AppEndpoints.getSavedAddresses}?Phone=${Uri.encodeQueryComponent(phone.trim())}',
      );
      if (branchId != null && branchId > 0) qs.write('&BranchId=$branchId');
      final res = await _api.get(qs.toString());
      if (res is List) {
        return res
            .map((a) => CustomerSavedAddressDto.fromJson(Map<String, dynamic>.from(a as Map)))
            .toList();
      }
    } catch (_) {}
    return [];
  }

  Future<CustomerSavedAddressDto?> saveSavedAddress(Map<String, dynamic> input) async {
    try {
      final res = await _api.post(AppEndpoints.saveSavedAddress, data: input);
      if (res is Map) {
        return CustomerSavedAddressDto.fromJson(Map<String, dynamic>.from(res));
      }
    } catch (_) {}
    return null;
  }
}
