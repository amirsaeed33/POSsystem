import 'dart:convert';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../models/auth_models.dart';

class SessionStorage {
  static const _tokenKey = 'sp.accessToken';
  static const _tenantIdKey = 'sp.tenantId';
  static const _tenancyNameKey = 'sp.tenancyName';
  static const _branchIdKey = 'sp.branchId';
  static const _userJsonKey = 'sp.userJson';
  static const _guestTenantIdKey = 'sp.guestTenantId';
  static const _guestTenancyNameKey = 'sp.guestTenancyName';
  static const _guestBranchIdKey = 'sp.guestBranchId';
  static const _apiBaseUrlKey = 'sp.apiBaseUrl';

  final FlutterSecureStorage _secure = const FlutterSecureStorage();
  late SharedPreferences _prefs;

  Future<void> init() async {
    _prefs = await SharedPreferences.getInstance();
  }

  // Token
  Future<String?> getToken() => _secure.read(key: _tokenKey);
  Future<void> setToken(String token) => _secure.write(key: _tokenKey, value: token);
  Future<void> clearToken() => _secure.delete(key: _tokenKey);

  // Tenant ID
  int? getTenantId() {
    final val = _prefs.getInt(_tenantIdKey);
    return val ?? _prefs.getInt(_guestTenantIdKey);
  }

  Future<void> setTenantId(int tenantId, {bool isGuest = false}) async {
    final key = isGuest ? _guestTenantIdKey : _tenantIdKey;
    await _prefs.setInt(key, tenantId);
  }

  // Tenancy Name
  String? getTenancyName() {
    return _prefs.getString(_tenancyNameKey) ?? _prefs.getString(_guestTenancyNameKey);
  }

  Future<void> setTenancyName(String tenancyName, {bool isGuest = false}) async {
    final key = isGuest ? _guestTenancyNameKey : _tenancyNameKey;
    await _prefs.setString(key, tenancyName);
  }

  // Branch ID
  int? getBranchId() {
    return _prefs.getInt(_branchIdKey) ?? _prefs.getInt(_guestBranchIdKey);
  }

  Future<void> setBranchId(int branchId, {bool isGuest = false}) async {
    final key = isGuest ? _guestBranchIdKey : _branchIdKey;
    await _prefs.setInt(key, branchId);
  }

  Future<void> clearBranch() async {
    await _prefs.remove(_branchIdKey);
    await _prefs.remove(_guestBranchIdKey);
  }

  // User info
  UserLoginInfoDto? getUser() {
    final jsonStr = _prefs.getString(_userJsonKey);
    if (jsonStr == null || jsonStr.isEmpty) return null;
    try {
      return UserLoginInfoDto.fromJson(jsonDecode(jsonStr));
    } catch (_) {
      return null;
    }
  }

  Future<void> setUser(UserLoginInfoDto user) async {
    await _prefs.setString(_userJsonKey, jsonEncode(user.toJson()));
  }

  // Base URL
  String? getBaseUrl() => _prefs.getString(_apiBaseUrlKey);
  Future<void> setBaseUrl(String url) => _prefs.setString(_apiBaseUrlKey, url);

  // Clear all
  Future<void> clearAll() async {
    await _secure.deleteAll();
    await _prefs.remove(_tenantIdKey);
    await _prefs.remove(_tenancyNameKey);
    await _prefs.remove(_branchIdKey);
    await _prefs.remove(_userJsonKey);
  }
}

