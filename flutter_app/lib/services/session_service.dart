import 'package:flutter/foundation.dart';
import '../core/api/abp_api_client.dart';
import '../core/api/api_exceptions.dart';
import '../core/constants/app_endpoints.dart';
import '../core/storage/session_storage.dart';
import '../models/auth_models.dart';

class SessionService extends ChangeNotifier {
  final AbpApiClient _api;
  final SessionStorage _storage;

  String? _accessToken;
  int? _tenantId;
  String? _tenancyName;
  int? _branchId;
  String? _branchName;
  UserLoginInfoDto? _currentUser;
  bool _isInitialized = false;

  SessionService(this._api, this._storage);

  /// Shared ABP client for role screens created after login.
  AbpApiClient get apiClient => _api;

  bool get isInitialized => _isInitialized;
  bool get isAuthenticated => _accessToken != null && _accessToken!.isNotEmpty;
  UserLoginInfoDto? get currentUser => _currentUser;
  int? get tenantId => _tenantId;
  String? get tenancyName => _tenancyName;
  int? get branchId => _branchId;
  String? get branchName => _branchName;

  bool get isWaiter => _currentUser?.isWaiter ?? false;
  bool get isRider => _currentUser?.isRider ?? false;
  bool get isAdmin => _currentUser?.isAdmin ?? false;

  Future<void> init() async {
    await _storage.init();
    _accessToken = await _storage.getToken();
    _tenantId = _storage.getTenantId();
    _tenancyName = _storage.getTenancyName();
    _branchId = _storage.getBranchId();
    _currentUser = _storage.getUser();
    _isInitialized = true;
    notifyListeners();
  }

  Future<void> setBranch(int id, String name) async {
    _branchId = id;
    _branchName = name;
    await _storage.setBranchId(id, isGuest: !isAuthenticated);
    notifyListeners();
  }

  Future<void> setGuestTenant(int id, String name) async {
    _tenantId = id;
    _tenancyName = name;
    await _storage.setTenantId(id, isGuest: true);
    await _storage.setTenancyName(name, isGuest: true);
    notifyListeners();
  }

  Future<AuthenticateResultModel> login({
    String? tenancyName,
    required String userName,
    required String password,
    double? latitude,
    double? longitude,
  }) async {
    if (tenancyName != null && tenancyName.trim().isNotEmpty) {
      final tenantRes = await _api.post(
        AppEndpoints.isTenantAvailable,
        data: {'tenancyName': tenancyName.trim()},
        skipTenantHeader: true,
      );
      final tenantOutput = IsTenantAvailableOutput.fromJson(tenantRes as Map<String, dynamic>);
      if (!tenantOutput.isAvailable || tenantOutput.tenantId == null) {
        throw ApiException('Tenant "$tenancyName" is not found or inactive.');
      }
      _tenantId = tenantOutput.tenantId;
      _tenancyName = tenancyName.trim();
      await _storage.setTenantId(_tenantId!);
      await _storage.setTenancyName(_tenancyName!);
    }

    final authRes = await _api.post(
      AppEndpoints.authenticate,
      data: AuthenticateModel(
        tenancyName: tenancyName?.trim(),
        userNameOrEmailAddress: userName.trim(),
        password: password,
        latitude: latitude,
        longitude: longitude,
      ).toJson(),
      skipTenantHeader: tenancyName == null,
    );

    final auth = AuthenticateResultModel.fromJson(authRes as Map<String, dynamic>);
    _accessToken = auth.accessToken;
    await _storage.setToken(auth.accessToken);

    if (auth.tenantId != null) {
      _tenantId = auth.tenantId;
      _tenancyName = auth.tenancyName;
      await _storage.setTenantId(auth.tenantId!);
      if (auth.tenancyName != null) {
        await _storage.setTenancyName(auth.tenancyName!);
      }
    }

    try {
      final sessionRes = await _api.get(AppEndpoints.getCurrentLogin);
      final sessionOutput =
          GetCurrentLoginInformationsOutput.fromJson(sessionRes as Map<String, dynamic>);
      if (sessionOutput.user != null) {
        _currentUser = sessionOutput.user;
        await _storage.setUser(sessionOutput.user!);
        if (sessionOutput.user!.branchId != null) {
          _branchId = sessionOutput.user!.branchId;
          _branchName = sessionOutput.user!.branchName;
          await _storage.setBranchId(_branchId!);
        }
      }
    } catch (_) {}

    notifyListeners();
    return auth;
  }

  Future<void> logout() async {
    _accessToken = null;
    _currentUser = null;
    await _storage.clearAll();
    notifyListeners();
  }
}

