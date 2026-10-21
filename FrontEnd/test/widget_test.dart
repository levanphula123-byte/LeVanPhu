import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:levanphu/main.dart' as app;
import 'package:levanphu/config/config_api.dart';

void main() {
  testWidgets('Chưa đăng nhập hiển thị form liên kết MSSV và đăng nhập OTP', (
    tester,
  ) async {
    SharedPreferences.setMockInitialValues({});
    await app.main();
    await tester.pumpAndSettle();
    expect(ApiConfig.endpoint('/api/auth/login').path, '/api/auth/login');
    expect(find.text('Đăng nhập và gửi OTP'), findsOneWidget);
    await tester.tap(find.text('Sinh viên liên kết MSSV có sẵn'));
    await tester.pumpAndSettle();
    expect(find.text('MSSV đã có trong danh sách'), findsOneWidget);
    expect(find.text('Đăng ký và gửi OTP'), findsOneWidget);
  });
}
