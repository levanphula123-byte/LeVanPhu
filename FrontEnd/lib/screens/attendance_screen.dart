import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../services/api_service.dart';
import '../services/location_service.dart';
import 'face_camera_screen.dart';

class AttendanceScreen extends StatefulWidget {
  const AttendanceScreen({super.key});
  @override
  State<AttendanceScreen> createState() => _AttendanceScreenState();
}

class _AttendanceScreenState extends State<AttendanceScreen> {
  final scanner = MobileScannerController();
  bool busy = false, done = false;
  String message = 'Đưa QR giảng viên cung cấp vào camera.';
  @override
  void dispose() {
    scanner.dispose();
    super.dispose();
  }

  Future<void> mark(String token) async {
    if (busy || done) return;
    setState(() {
      busy = true;
      message = 'Đang kiểm tra vị trí và chuẩn bị camera khuôn mặt...';
    });
    try {
      await scanner.stop();
      final position = await LocationService.current();
      String challengeId = '';
      if (!mounted) return;
      final success = await Navigator.push<bool>(
        context,
        MaterialPageRoute(
          builder: (_) => FaceCameraScreen(
            onReady: () async {
              final result = await ApiService.request(
                'POST',
                '/api/attendance/begin',
                body: {
                  'qrToken': token,
                  'latitude': position.latitude,
                  'longitude': position.longitude,
                },
              );
              challengeId = result['challengeId'] as String;
              return result['seconds'] as int;
            },
            onEmbedding: (embedding) async {
              await ApiService.request(
                'POST',
                '/api/attendance/mark',
                body: {'challengeId': challengeId, 'embedding': embedding},
              );
            },
          ),
        ),
      );
      if (!mounted) return;
      setState(() {
        done = success == true;
        message = done
            ? 'Điểm danh thành công: QR + GPS + khuôn mặt.'
            : 'Chưa hoàn thành. Bấm thử lại để quét QR.';
      });
    } catch (e) {
      if (mounted) {
        setState(() => message = e.toString().replaceFirst('Exception: ', ''));
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Điểm danh')),
    body: Column(
      children: [
        Expanded(
          child: MobileScanner(
            controller: scanner,
            onDetect: (capture) {
              for (final code in capture.barcodes) {
                final value = code.rawValue;
                if (value != null && value.isNotEmpty) {
                  mark(value);
                  break;
                }
              }
            },
          ),
        ),
        Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            children: [
              if (busy) const CircularProgressIndicator(),
              Text(message, textAlign: TextAlign.center),
              if (!busy)
                FilledButton(
                  onPressed: () async {
                    if (done) {
                      Navigator.pop(context);
                    } else {
                      await scanner.start();
                    }
                  },
                  child: Text(done ? 'Hoàn tất' : 'Thử lại'),
                ),
            ],
          ),
        ),
      ],
    ),
  );
}
