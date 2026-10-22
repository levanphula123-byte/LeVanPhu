
import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

import '../config/config_api.dart';

class ApiService {
  static Future<String?> get token async =>
      (await SharedPreferences.getInstance()).getString('token');

  static Future<Map<String, dynamic>> request(
      String method,
      String path, {
        Map<String, dynamic>? body,
        bool auth = true,
      }) async {
    final client = http.Client();

    try {
      final uri = ApiConfig.endpoint(path);
      final request = http.Request(method, uri);

      request.headers['Content-Type'] = 'application/json';
      request.headers['Accept'] = 'application/json';

      if (auth) {
        final saved = await token;

        if (saved == null || saved.isEmpty) {
          throw Exception(
            'Chưa có phiên đăng nhập. Hãy đăng nhập lại.',
          );
        }

        request.headers['Authorization'] = 'Bearer $saved';
      }

      if (body != null) {
        request.body = jsonEncode(body);
      }

      debugPrint('[API] $method $uri');
      debugPrint('[API] Có JWT: ${request.headers.containsKey('Authorization')}');

      if (path == '/api/face' && body != null) {
        final embedding = body['embedding'];
        if (embedding is List) {
          debugPrint('[API] Embedding length: ${embedding.length}');
        }
      }

      final streamed = await client
          .send(request)
          .timeout(const Duration(seconds: 20));

      final response = await http.Response
          .fromStream(streamed)
          .timeout(const Duration(seconds: 20));

      final responseText = utf8.decode(
        response.bodyBytes,
        allowMalformed: true,
      );

      debugPrint('[API] HTTP status: ${response.statusCode}');
      debugPrint(
        '[API] Response: ${responseText.length > 1000 ? responseText.substring(0, 1000) : responseText}',
      );

      dynamic data;

      try {
        data = responseText.trim().isEmpty
            ? <String, dynamic>{}
            : jsonDecode(responseText);
      } catch (_) {
        data = responseText;
      }

      if (response.statusCode == 429) {
        throw Exception(
          'HTTP 429: Đã yêu cầu quá nhiều lần. Hãy đợi rồi thử lại.',
        );
      }

      if (response.statusCode == 401 && auth) {
        throw Exception(
          'HTTP 401: Phiên đăng nhập hết hạn hoặc JWT không hợp lệ.',
        );
      }

      if (response.statusCode < 200 || response.statusCode >= 300) {
        String message = '';

        if (data is Map) {
          message = (
              data['detail'] ??
                  data['message'] ??
                  data['title'] ??
                  ''
          ).toString();

          if (message.isEmpty && data['errors'] != null) {
            message = data['errors'].toString();
          }
        } else {
          message = data.toString();
        }

        if (message.trim().isEmpty || message == '{}') {
          message = 'Backend không cung cấp nội dung lỗi.';
        }

        throw Exception(
          'HTTP ${response.statusCode}: $message',
        );
      }

      return data is Map<String, dynamic>
          ? data
          : {'data': data};
    } on TimeoutException {
      throw Exception(
        'API phản hồi quá lâu. Kiểm tra Backend và kết nối điện thoại.',
      );
    } finally {
      client.close();
    }
  }

  static Future<void> saveAuth(Map<String, dynamic> data) async {
    final p = await SharedPreferences.getInstance();

    await p.setString('token', data['token'] as String);
    await p.setString(
      'role',
      data['role'] as String? ?? 'Student',
    );
    await p.setString(
      'name',
      data['fullName'] as String? ?? '',
    );
  }

  static Future<void> logout() async {
    final p = await SharedPreferences.getInstance();

    await p.remove('token');
    await p.remove('role');
    await p.remove('name');
  }
}
