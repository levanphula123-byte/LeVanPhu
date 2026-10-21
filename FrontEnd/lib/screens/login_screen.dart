import 'package:flutter/material.dart';

import '../services/api_service.dart';
import 'dashboard_screen.dart';

class LoginScreen extends StatefulWidget {
  final VoidCallback onLoggedIn;
  const LoginScreen({super.key, required this.onLoggedIn});
  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final email = TextEditingController(),
      password = TextEditingController(),
      code = TextEditingController(),
      studentCode = TextEditingController();
  bool register = false, otpSent = false, busy = false;
  String? message;
  @override
  void dispose() {
    email.dispose();
    password.dispose();
    code.dispose();
    studentCode.dispose();
    super.dispose();
  }

  Future<void> run() async {
    setState(() {
      busy = true;
      message = null;
    });
    try {
      final address = email.text.trim();
      if (register) {
        if (!otpSent) {
          await ApiService.request(
            'POST',
            '/api/auth/register',
            auth: false,
            body: {
              'email': address,
              'password': password.text,
              'fullName': '',
              'studentCode': studentCode.text.trim(),
            },
          );
          if (mounted) {
            setState(() {
              otpSent = true;
              message = 'Đã gửi OTP đăng ký tới email.';
            });
          }
        } else {
          await ApiService.request(
            'POST',
            '/api/auth/verify-email',
            auth: false,
            body: {'email': address, 'otp': code.text.trim()},
          );
          if (mounted) {
            setState(() {
              register = false;
              otpSent = false;
              code.clear();
              message = 'Đã xác minh email. Hãy đăng nhập.';
            });
          }
        }
      } else if (!otpSent) {
        await ApiService.request(
          'POST',
          '/api/auth/login',
          auth: false,
          body: {'email': address, 'password': password.text},
        );
        if (mounted) {
          setState(() {
            otpSent = true;
            message = 'Đã gửi OTP đăng nhập tới email.';
          });
        }
      } else {
        final data = await ApiService.request(
          'POST',
          '/api/auth/verify-login',
          auth: false,
          body: {'email': address, 'otp': code.text.trim()},
        );
        await ApiService.saveAuth(data);
        if (!mounted) return;
        Navigator.pushAndRemoveUntil(
          context,
          MaterialPageRoute(builder: (_) => const DashboardScreen()),
          (_) => false,
        );
      }
    } catch (e) {
      if (mounted) {
        setState(() => message = e.toString().replaceFirst('Exception: ', ''));
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> resend() async {
    setState(() => busy = true);
    try {
      await ApiService.request(
        'POST',
        '/api/auth/resend-email',
        auth: false,
        body: {'email': email.text.trim(), 'otp': ''},
      );
      if (mounted) {
        setState(() {
          register = true;
          otpSent = true;
          message = 'Đã gửi lại OTP xác minh email.';
        });
      }
    } catch (e) {
      if (mounted) setState(() => message = e.toString());
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> resendLogin() async {
    setState(() {
      otpSent = false;
      code.clear();
    });
    await run();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    body: Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 440),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Container(
                width: 84, height: 84,
                decoration: BoxDecoration(
                  color: const Color(0xFFE4F5F4), borderRadius: BorderRadius.circular(24)),
                child: const Icon(Icons.school_rounded, size: 44, color: Color(0xFF087F8C)),
              ),
              const SizedBox(height: 16),
              Text(
                register ? 'Tạo tài khoản học tập' : 'Chào mừng đến LeVanPhu',
                style: Theme.of(context).textTheme.headlineSmall,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              const Text('Đăng nhập để quản lý lớp học và điểm danh',
                textAlign: TextAlign.center,
                style: TextStyle(color: Color(0xFF66788A))),
              const SizedBox(height: 28),
              TextField(
                controller: email,
                enabled: !busy && !otpSent,
                keyboardType: register
                    ? TextInputType.emailAddress
                    : TextInputType.text,
                decoration: InputDecoration(
                  labelText: register
                      ? 'Email'
                      : 'Mã sinh viên / Mã giảng viên / Email',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 12),
              if (register && !otpSent) ...[
                TextField(
                  controller: studentCode,
                  decoration: const InputDecoration(
                    labelText: 'MSSV đã có trong danh sách',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
              ],
              if (!otpSent)
                TextField(
                  controller: password,
                  obscureText: true,
                  decoration: const InputDecoration(
                    labelText: 'Mật khẩu (8 ký tự, hoa, thường, số, ký hiệu)',
                    border: OutlineInputBorder(),
                  ),
                ),
              if (otpSent)
                TextField(
                  controller: code,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(
                    labelText: 'OTP email',
                    border: OutlineInputBorder(),
                  ),
                ),
              if (message != null)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 12),
                  child: Text(message!),
                ),
              const SizedBox(height: 16),
              FilledButton(
                onPressed: busy ? null : run,
                child: Text(
                  busy
                      ? 'Đang xử lý...'
                      : otpSent
                      ? 'Xác minh OTP'
                      : register
                      ? 'Đăng ký và gửi OTP'
                      : 'Đăng nhập và gửi OTP',
                ),
              ),
              TextButton(
                onPressed: busy
                    ? null
                    : () => setState(() {
                        register = otpSent ? false : !register;
                        otpSent = false;
                        code.clear();
                        message = null;
                      }),
                child: Text(
                  otpSent
                      ? 'Quay lại đăng nhập'
                      : register
                      ? 'Quay lại đăng nhập'
                      : 'Sinh viên liên kết MSSV có sẵn',
                ),
              ),
              TextButton(
                onPressed: busy
                    ? null
                    : (otpSent && !register ? resendLogin : resend),
                child: Text(
                  otpSent && !register
                      ? 'Gửi lại OTP đăng nhập'
                      : 'Tài khoản chưa xác minh? Gửi lại OTP email',
                ),
              ),
            ],
          ),
        ),
      ),
    ),
  );
}
