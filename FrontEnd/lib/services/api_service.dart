import 'dart:async';
import 'dart:convert';

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
      final request = http.Request(method, ApiConfig.endpoint(path));
      request.headers['Content-Type'] = 'application/json';
      if (auth) {
        final saved = await token;
        if (saved != null) request.headers['Authorization'] = 'Bearer $saved';
      }
      if (body != null) request.body = jsonEncode(body);
      final response = await http.Response.fromStream(
        await client.send(request).timeout(const Duration(seconds: 20)),
      ).timeout(const Duration(seconds: 20));
      dynamic data;
      try {
        data = response.body.isEmpty
            ? <String, dynamic>{}
            : jsonDecode(utf8.decode(response.bodyBytes));
      } catch (_) {
        data = response.body;
      }
      if (response.statusCode == 429) {
        throw Exception(
          'Đã yêu cầu quá nhiều lần. Hãy đợi một phút rồi thử lại.',
        );
      }
      if (response.statusCode == 401 && auth) {
        throw Exception(
          'Phiên đăng nhập hết hạn. Hãy đăng xuất và đăng nhập lại.',
        );
      }
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw Exception(
          data is Map
              ? (data['detail'] ??
                        data['message'] ??
                        data['title'] ??
                        data.toString())
                    .toString()
              : data.toString(),
        );
      }
      return data is Map<String, dynamic> ? data : {'data': data};
    } on TimeoutException {
      throw Exception(
        'API phản hồi quá lâu. Kiểm tra backend và kết nối điện thoại.',
      );
    } finally {
      client.close();
    }
  }

  static Future<void> saveAuth(Map<String, dynamic> data) async {
    final p = await SharedPreferences.getInstance();
    await p.setString('token', data['token'] as String);
    await p.setString('role', data['role'] as String? ?? 'Student');
    await p.setString('name', data['fullName'] as String? ?? '');
  }

  static Future<void> logout() async {
    final p = await SharedPreferences.getInstance();
    await p.remove('token');
    await p.remove('role');
    await p.remove('name');
  }
}
