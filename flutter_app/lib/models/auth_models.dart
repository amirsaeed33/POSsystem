import '../core/api/json_helpers.dart';

class AuthenticateModel {
  final String? tenancyName;
  final String userNameOrEmailAddress;
  final String password;
  final bool rememberClient;
  final double? latitude;
  final double? longitude;

  AuthenticateModel({
    this.tenancyName,
    required this.userNameOrEmailAddress,
    required this.password,
    this.rememberClient = true,
    this.latitude,
    this.longitude,
  });

  Map<String, dynamic> toJson() => {
        if (tenancyName != null && tenancyName!.isNotEmpty) 'tenancyName': tenancyName,
        'userNameOrEmailAddress': userNameOrEmailAddress,
        'password': password,
        'rememberClient': rememberClient,
        if (latitude != null) 'latitude': latitude,
        if (longitude != null) 'longitude': longitude,
      };
}

class AuthenticateResultModel {
  final String accessToken;
  final String? encryptedAccessToken;
  final int expireInSeconds;
  final int userId;
  final int? tenantId;
  final String? tenancyName;
  final String? role;
  final List<String> userRoles;

  AuthenticateResultModel({
    required this.accessToken,
    this.encryptedAccessToken,
    required this.expireInSeconds,
    required this.userId,
    this.tenantId,
    this.tenancyName,
    this.role,
    this.userRoles = const [],
  });

  factory AuthenticateResultModel.fromJson(Map<String, dynamic> json) {
    return AuthenticateResultModel(
      accessToken: json['accessToken']?.toString() ?? '',
      encryptedAccessToken: asStringOrNull(json['encryptedAccessToken']),
      expireInSeconds: asInt(json['expireInSeconds']),
      userId: asInt(json['userId']),
      tenantId: asIntOrNull(json['tenantId']),
      tenancyName: asStringOrNull(json['tenancyName']),
      role: asStringOrNull(json['role']),
      userRoles: (json['userRoles'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          [],
    );
  }
}

/// Matches ABP `UserLoginInfoDto` + MAUI role helpers.
class UserLoginInfoDto {
  final int id;
  final String? name;
  final String? surname;
  final String? userName;
  final String? emailAddress;
  final String? role;
  final List<String> roles;
  final int? branchId;
  final String? branchName;
  final bool isDeliveryRider;
  final bool isWaiterFlag;
  final int? riderStaffId;
  final String? designation;

  UserLoginInfoDto({
    required this.id,
    this.name,
    this.surname,
    this.userName,
    this.emailAddress,
    this.role,
    this.roles = const [],
    this.branchId,
    this.branchName,
    this.isDeliveryRider = false,
    this.isWaiterFlag = false,
    this.riderStaffId,
    this.designation,
  });

  String get displayName =>
      [name, surname].where((s) => s != null && s.isNotEmpty).join(' ').trim().isNotEmpty
          ? [name, surname].where((s) => s != null && s.isNotEmpty).join(' ').trim()
          : (userName ?? 'User');

  String get _designationLower => (designation ?? '').toLowerCase();

  bool get isWaiter {
    if (isWaiterFlag) return true;
    if (roles.any((r) => r.toLowerCase().contains('waiter'))) return true;
    if (role?.toLowerCase().contains('waiter') == true) return true;
    if (_designationLower.contains('waiter') || _designationLower.contains('server')) {
      return true;
    }
    return userName?.toLowerCase().contains('waiter') == true;
  }

  /// Same rules as web `AuthService.isDeliveryRider()` / MAUI `IsRider`.
  bool get isRider {
    if (isDeliveryRider) return true;
    if (roles.any((r) => r.toLowerCase().contains('rider'))) return true;
    if (role?.toLowerCase().contains('rider') == true) return true;
    if (_designationLower.contains('rider') || _designationLower.contains('delivery')) {
      return true;
    }
    return userName?.toLowerCase().contains('rider') == true;
  }

  bool get isAdmin {
    bool match(String value) {
      final v = value.toLowerCase();
      return v.contains('admin') ||
          v.contains('manager') ||
          v.contains('cashier') ||
          v.contains('owner');
    }

    if (roles.any(match)) return true;
    if (role != null && match(role!)) return true;
    if (_designationLower.isNotEmpty && match(_designationLower)) return true;
    return userName?.toLowerCase().contains('admin') == true;
  }

  factory UserLoginInfoDto.fromJson(Map<String, dynamic> json) {
    final rolesList = <String>[];
    final rawRoles = json['roles'] ?? json['roleNames'];
    if (rawRoles is List) {
      rolesList.addAll(rawRoles.map((e) => e.toString()));
    }

    return UserLoginInfoDto(
      id: asInt(json['id']),
      name: asStringOrNull(json['name']),
      surname: asStringOrNull(json['surname']),
      userName: asStringOrNull(json['userName']),
      emailAddress: asStringOrNull(json['emailAddress']),
      role: asStringOrNull(json['role']),
      roles: rolesList,
      branchId: asIntOrNull(json['branchId']),
      branchName: asStringOrNull(json['branchName']),
      isDeliveryRider: asBool(json['isDeliveryRider']),
      isWaiterFlag: asBool(json['isWaiter']),
      riderStaffId: asIntOrNull(json['riderStaffId']),
      designation: asStringOrNull(json['designation']),
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'surname': surname,
        'userName': userName,
        'emailAddress': emailAddress,
        'role': role,
        'roles': roles,
        'branchId': branchId,
        'branchName': branchName,
        'isDeliveryRider': isDeliveryRider,
        'isWaiter': isWaiterFlag,
        'riderStaffId': riderStaffId,
        'designation': designation,
      };
}

class GetCurrentLoginInformationsOutput {
  final UserLoginInfoDto? user;
  final Map<String, dynamic>? tenant;
  final Map<String, dynamic>? application;

  GetCurrentLoginInformationsOutput({this.user, this.tenant, this.application});

  factory GetCurrentLoginInformationsOutput.fromJson(Map<String, dynamic> json) {
    return GetCurrentLoginInformationsOutput(
      user: json['user'] != null
          ? UserLoginInfoDto.fromJson(json['user'] as Map<String, dynamic>)
          : null,
      tenant: json['tenant'] as Map<String, dynamic>?,
      application: json['application'] as Map<String, dynamic>?,
    );
  }
}

class IsTenantAvailableOutput {
  final int state;
  final int? tenantId;

  IsTenantAvailableOutput({required this.state, this.tenantId});

  bool get isAvailable => state == 1; // 1 = Available in ABP

  factory IsTenantAvailableOutput.fromJson(Map<String, dynamic> json) {
    return IsTenantAvailableOutput(
      state: asInt(json['state']),
      tenantId: asIntOrNull(json['tenantId']),
    );
  }
}
