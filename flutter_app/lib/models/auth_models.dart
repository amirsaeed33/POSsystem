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

class UserLoginInfoDto {
  final int id;
  final String? name;
  final String? surname;
  final String? userName;
  final String? emailAddress;
  final String? role;
  final int? branchId;
  final String? branchName;

  UserLoginInfoDto({
    required this.id,
    this.name,
    this.surname,
    this.userName,
    this.emailAddress,
    this.role,
    this.branchId,
    this.branchName,
  });

  String get displayName =>
      [name, surname].where((s) => s != null && s.isNotEmpty).join(' ').trim().isNotEmpty
          ? [name, surname].where((s) => s != null && s.isNotEmpty).join(' ').trim()
          : (userName ?? 'User');

  bool get isWaiter =>
      role?.toLowerCase().contains('waiter') == true ||
      userName?.toLowerCase().contains('waiter') == true;

  bool get isRider =>
      role?.toLowerCase().contains('rider') == true ||
      userName?.toLowerCase().contains('rider') == true;

  bool get isAdmin =>
      role?.toLowerCase().contains('admin') == true ||
      userName?.toLowerCase().contains('admin') == true;

  factory UserLoginInfoDto.fromJson(Map<String, dynamic> json) {
    return UserLoginInfoDto(
      id: asInt(json['id']),
      name: asStringOrNull(json['name']),
      surname: asStringOrNull(json['surname']),
      userName: asStringOrNull(json['userName']),
      emailAddress: asStringOrNull(json['emailAddress']),
      role: asStringOrNull(json['role']),
      branchId: asIntOrNull(json['branchId']),
      branchName: asStringOrNull(json['branchName']),
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'surname': surname,
        'userName': userName,
        'emailAddress': emailAddress,
        'role': role,
        'branchId': branchId,
        'branchName': branchName,
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

