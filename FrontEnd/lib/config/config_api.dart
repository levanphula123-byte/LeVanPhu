import 'package:flutter_dotenv/flutter_dotenv.dart';

class ApiConfig {
  /// Swagger: https://littlebrasspencil10.conveyor.cloud/swagger/index.html
  /// API base phải là domain gốc, không phải đường dẫn Swagger.
  static String get baseURL {
    final value = dotenv.env['API_BASE_URL']?.trim() ?? '';
    final uri = Uri.tryParse(value);
    if (uri == null ||
        !uri.hasAuthority ||
        uri.host.isEmpty ||
        !['http', 'https'].contains(uri.scheme) ||
        uri.hasQuery ||
        uri.hasFragment) {
      throw StateError(
        'API_BASE_URL trong .env phải là URL HTTP/HTTPS hợp lệ.',
      );
    }
    return value.replaceFirst(RegExp(r'/+$'), '');
  }

  /// Tất cả endpoint dùng chung baseURL, ví dụ /api/auth/login.
  static Uri endpoint(String path) =>
      Uri.parse('$baseURL/${path.replaceFirst(RegExp(r'^/+'), '')}');
}
