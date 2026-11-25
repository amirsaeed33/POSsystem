import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import '../storage/session_storage.dart';
import '../constants/app_endpoints.dart';
import 'api_exceptions.dart';

/// ABP API client matching the production Swagger/curl pattern:
///
/// ```
/// curl -X POST 'https://aljannatapp.com/api/services/app/...' \
///   -H 'accept: text/plain' \
///   -H 'Content-Type: application/json' \
///   -H 'Authorization: Bearer <token>' \
///   -d '{ ... }'
/// ```
///
/// X-XSRF-TOKEN from Swagger is cookie antiforgery and is not required for
/// Bearer JWT mobile/web clients (same as MAUI).
class AbpApiClient {
  static const _skipTenantExtra = 'skipTenantHeader';

  final Dio _dio;
  final SessionStorage _storage;

  AbpApiClient(this._storage)
      : _dio = Dio(
          BaseOptions(
            baseUrl: _normalizeBaseUrl(
              _storage.getBaseUrl() ?? AppEndpoints.defaultBaseUrl,
            ),
            connectTimeout: const Duration(seconds: 20),
            receiveTimeout: const Duration(seconds: 30),
            // Match Swagger curl: accept: text/plain
            headers: const {
              'accept': 'text/plain',
            },
            contentType: Headers.jsonContentType,
            responseType: ResponseType.json,
          ),
        ) {
    if (kIsWeb) {
      // Keep browser requests credential-less so CORS + Bearer works cleanly.
      _dio.options.extra['withCredentials'] = false;
    }

    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          // Never leak internal flags as real HTTP headers (breaks CORS preflight).
          options.headers.remove('SkipTenantHeader');
          options.headers.remove(_skipTenantExtra);

          final method = options.method.toUpperCase();
          final hasBody = method == 'POST' || method == 'PUT' || method == 'PATCH';

          // Swagger curl headers
          options.headers['accept'] = 'text/plain';
          if (hasBody) {
            options.headers['Content-Type'] = 'application/json';
          } else {
            // GET/DELETE: avoid Content-Type (not a CORS-safelisted header).
            options.headers.remove('Content-Type');
            options.contentType = null;
          }

          // Authorization: Bearer <token>
          try {
            final token = await _storage.getToken();
            if (token != null && token.isNotEmpty) {
              options.headers['Authorization'] = 'Bearer $token';
            } else {
              options.headers.remove('Authorization');
            }
          } catch (_) {
            options.headers.remove('Authorization');
          }

          final skipTenant = options.extra[_skipTenantExtra] == true;

          // Abp.TenantId (same as MAUI)
          if (!skipTenant) {
            final tenantId = _storage.getTenantId();
            if (tenantId != null) {
              options.headers['Abp.TenantId'] = tenantId.toString();
            } else {
              options.headers.remove('Abp.TenantId');
            }
          } else {
            options.headers.remove('Abp.TenantId');
          }

          // SmartPos.BranchId (same as MAUI)
          final branchId = _storage.getBranchId();
          if (branchId != null) {
            options.headers['SmartPos.BranchId'] = branchId.toString();
          } else {
            options.headers.remove('SmartPos.BranchId');
          }

          return handler.next(options);
        },
        onError: (DioException e, handler) {
          final response = e.response;
          if (response?.data != null && response!.data is Map) {
            final data = Map<String, dynamic>.from(response.data as Map);
            final error = data['error'];
            if (error is Map) {
              final errMap = Map<String, dynamic>.from(error);
              final message = errMap['message'] ?? errMap['details'] ?? 'Request failed';
              return handler.reject(
                DioException(
                  requestOptions: e.requestOptions,
                  response: e.response,
                  type: e.type,
                  error: ApiException(
                    message.toString(),
                    details: errMap['details']?.toString(),
                  ),
                ),
              );
            }
          }
          return handler.next(e);
        },
      ),
    );
  }

  static String _normalizeBaseUrl(String url) {
    final trimmed = url.trim();
    if (trimmed.isEmpty) return '${AppEndpoints.defaultBaseUrl}/';
    return trimmed.endsWith('/') ? trimmed : '$trimmed/';
  }

  void updateBaseUrl(String newUrl) {
    _dio.options.baseUrl = _normalizeBaseUrl(newUrl);
  }

  String resolveImageUrl(String? path) {
    if (path == null || path.trim().isEmpty) return '';
    if (path.startsWith('http://') || path.startsWith('https://')) return path;
    final base = (_storage.getBaseUrl() ?? AppEndpoints.defaultBaseUrl).replaceAll(RegExp(r'/+$'), '');
    final cleanPath = path.startsWith('/') ? path : '/$path';
    return '$base$cleanPath';
  }

  dynamic _unwrapResponse(Response response) {
    final data = response.data;
    if (data is Map) {
      final map = Map<String, dynamic>.from(data);
      if (map.containsKey('success')) {
        final success = map['success'] == true;
        if (!success) {
          final err = map['error'] is Map ? Map<String, dynamic>.from(map['error'] as Map) : null;
          throw ApiException(
            err?['message']?.toString() ?? 'Request failed',
            details: err?['details']?.toString(),
          );
        }
        return map['result'];
      }
    }
    return data;
  }

  DioException _asApiException(DioException e) {
    if (e.error is ApiException) {
      return DioException(
        requestOptions: e.requestOptions,
        response: e.response,
        type: e.type,
        error: e.error,
      );
    }
    return e;
  }

  Future<dynamic> get(String path, {Map<String, dynamic>? queryParameters}) async {
    try {
      final response = await _dio.get(path, queryParameters: queryParameters);
      return _unwrapResponse(response);
    } on DioException catch (e) {
      final wrapped = _asApiException(e);
      if (wrapped.error is ApiException) throw wrapped.error as ApiException;
      throw ApiException(e.message ?? 'Network error occurred');
    }
  }

  /// POST matching Swagger curl: Content-Type application/json + JSON body.
  Future<dynamic> post(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    bool skipTenantHeader = false,
  }) async {
    try {
      final response = await _dio.post(
        path,
        data: data,
        queryParameters: queryParameters,
        options: Options(
          contentType: Headers.jsonContentType,
          headers: const {
            'accept': 'text/plain',
            'Content-Type': 'application/json',
          },
          extra: {
            _skipTenantExtra: skipTenantHeader,
          },
        ),
      );
      return _unwrapResponse(response);
    } on DioException catch (e) {
      final wrapped = _asApiException(e);
      if (wrapped.error is ApiException) throw wrapped.error as ApiException;
      throw ApiException(e.message ?? 'Network error occurred');
    }
  }

  Future<dynamic> put(String path, {dynamic data}) async {
    try {
      final response = await _dio.put(
        path,
        data: data,
        options: Options(
          contentType: Headers.jsonContentType,
          headers: const {
            'accept': 'text/plain',
            'Content-Type': 'application/json',
          },
        ),
      );
      return _unwrapResponse(response);
    } on DioException catch (e) {
      final wrapped = _asApiException(e);
      if (wrapped.error is ApiException) throw wrapped.error as ApiException;
      throw ApiException(e.message ?? 'Network error occurred');
    }
  }

  Future<dynamic> delete(String path) async {
    try {
      final response = await _dio.delete(path);
      return _unwrapResponse(response);
    } on DioException catch (e) {
      final wrapped = _asApiException(e);
      if (wrapped.error is ApiException) throw wrapped.error as ApiException;
      throw ApiException(e.message ?? 'Network error occurred');
    }
  }
}
