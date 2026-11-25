class ApiException implements Exception {
  final String message;
  final String? details;
  final int? statusCode;

  ApiException(this.message, {this.details, this.statusCode});

  @override
  String toString() => message;
}

