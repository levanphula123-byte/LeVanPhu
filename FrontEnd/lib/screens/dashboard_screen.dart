import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../services/api_service.dart';
import 'attendance_screen.dart';
import 'face_camera_screen.dart';
import 'login_screen.dart';
import 'management_screen.dart';
import 'teaching_screen.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});
  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  String role = '', name = '';
  bool faceRegistered = false;
  String? error;
  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    final preferences = await SharedPreferences.getInstance();
    if (!mounted) return;
    setState(() {
      role = preferences.getString('role') ?? '';
      name = preferences.getString('name') ?? '';
    });
    if (role == 'Student') {
      try {
        final status = await ApiService.request('GET', '/api/face');
        if (mounted) {
          setState(() => faceRegistered = status['registered'] == true);
        }
      } catch (e) {
        if (mounted) setState(() => error = e.toString());
      }
    }
  }

  void open(Widget page) =>
      Navigator.push(context, MaterialPageRoute(builder: (_) => page));
  Widget tile(String title, String subtitle, IconData icon, VoidCallback tap) =>
      Card(
        child: InkWell(
          onTap: tap,
          borderRadius: BorderRadius.circular(20),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 15),
            child: Row(
              children: [
                Container(
                  width: 52,
                  height: 52,
                  decoration: BoxDecoration(
                    color: const Color(0xFFE4F5F4),
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: Icon(icon, color: const Color(0xFF087F8C)),
                ),
                const SizedBox(width: 14),
                Expanded(child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(title, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 15)),
                    const SizedBox(height: 4),
                    Text(subtitle, style: const TextStyle(color: Color(0xFF66788A), fontSize: 12)),
                  ],
                )),
                const Icon(Icons.arrow_forward_ios_rounded, size: 16, color: Color(0xFF90A0AD)),
              ],
            ),
          ),
        ),
      );
  void manage(String title, String endpoint) =>
      open(ManagementScreen(title: title, endpoint: endpoint, role: role));
  Future<void> registerFace() async {
    final success = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => FaceCameraScreen(
          onEmbedding: (embedding) async {
            debugPrint(
              '[REGISTER FACE] Embedding length: ${embedding.length}',
            );

            try {
              debugPrint('[REGISTER FACE] Gửi PUT /api/face');

              final result = await ApiService.request(
                'PUT',
                '/api/face',
                body: {'embedding': embedding},
              );

              debugPrint('[REGISTER FACE] Thành công: $result');
            } catch (e, stackTrace) {
              debugPrint('[REGISTER FACE] Lỗi: $e');
              debugPrintStack(stackTrace: stackTrace);
              rethrow;
            }
          },
        ),
      ),
    );

    if (!mounted) return;

    if (success == true) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Đăng ký khuôn mặt thành công!'),
        ),
      );
    }

    await load();
  }

  Future<void> history() async {
    try {
      final rows =
          (await ApiService.request('GET', '/api/attendance/mine'))['data']
              as List;
      if (!mounted) return;
      await showDialog<void>(
        context: context,
        builder: (ctx) => AlertDialog(
          title: const Text('Lịch sử điểm danh'),
          content: SizedBox(
            width: 400,
            child: ListView(
              shrinkWrap: true,
              children: rows.isEmpty
                  ? [const Text('Chưa có lượt điểm danh.')]
                  : rows
                        .map(
                          (r) => ListTile(
                            title: Text('Phiên #${r['sessionId']}'),
                            subtitle: Text(
                              '${r['attendedAtUtc']} UTC · ${r['method']}',
                            ),
                          ),
                        )
                        .toList(),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(ctx),
              child: const Text('Đóng'),
            ),
          ],
        ),
      );
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.toString())));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final roleName =
        {
          'Admin': 'Quản trị viên',
          'Lecturer': 'Giảng viên',
          'Collaborator': 'Cộng tác viên',
          'Student': 'Sinh viên',
        }[role] ??
        'Đang tải';
    return Scaffold(
      appBar: AppBar(
        title: const Text('LeVanPhu', style: TextStyle(fontWeight: FontWeight.w800)),
        actions: [
          IconButton(
            onPressed: () async {
              await ApiService.logout();
              if (context.mounted) {
                Navigator.pushAndRemoveUntil(
                  context,
                  MaterialPageRoute(
                    builder: (_) => LoginScreen(onLoggedIn: () {}),
                  ),
                  (_) => false,
                );
              }
            },
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Container(
            padding: const EdgeInsets.all(24),
            decoration: BoxDecoration(
              gradient: const LinearGradient(colors: [Color(0xFF087F8C), Color(0xFF145C78)]),
              borderRadius: BorderRadius.circular(24),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Icon(Icons.auto_awesome_rounded, color: Colors.white70, size: 28),
                const SizedBox(height: 18),
                Text('Xin chào, ${name.isEmpty ? "bạn" : name}',
                  style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: Colors.white)),
                const SizedBox(height: 6),
                Text('Vai trò: $roleName', style: const TextStyle(color: Color(0xFFD8F5F4))),
                const SizedBox(height: 10),
                const Text('Quản lý học tập và điểm danh thuận tiện hơn mỗi ngày.',
                  style: TextStyle(color: Colors.white)),
              ],
            ),
          ),
          const SizedBox(height: 24),
          const Text('Tiện ích của bạn', style: TextStyle(fontSize: 19, fontWeight: FontWeight.w800)),
          const SizedBox(height: 10),
          if (error != null) Text(error!),
          if (role == 'Student') ...[
            tile(
              faceRegistered ? 'Cập nhật khuôn mặt' : 'Đăng ký khuôn mặt',
              faceRegistered
                  ? 'Đã có mẫu nhận diện'
                  : 'Cần đăng ký trước lần điểm danh đầu tiên',
              Icons.face,
              registerFace,
            ),
            tile(
              'Quét QR điểm danh',
              'GPS trong 150m · Khuôn mặt trong 10 giây',
              Icons.qr_code_scanner,
              () => open(const AttendanceScreen()),
            ),
            tile(
              'Lịch sử điểm danh',
              'Xem kết quả của bạn',
              Icons.history,
              history,
            ),
          ],
          if (role == 'Admin' || role == 'Lecturer') ...[
            tile(
              role == 'Admin'
                  ? 'Quản lý tài khoản giảng viên'
                  : 'Quản lý cộng tác viên',
              'Thêm, xem, sửa, xóa tài khoản',
              Icons.manage_accounts,
              () => manage('Tài khoản', '/api/accounts'),
            ),
            tile(
              'Môn học',
              'Thêm, sửa, xóa môn học',
              Icons.menu_book,
              () => manage('Môn học', '/api/courses'),
            ),
            tile(
              'Quản lý lớp',
              'Tạo lớp và phân công giảng viên',
              Icons.school,
              () => manage('Lớp học', '/api/classes'),
            ),
            tile(
              'Danh sách lớp và bảng điểm danh',
              'Ghi danh sinh viên, mở QR, xem và sửa kết quả',
              Icons.fact_check,
              () => open(const TeachingScreen()),
            ),
          ],
          if (role == 'Admin' || role == 'Lecturer' || role == 'Collaborator')
            tile(
              'Hồ sơ sinh viên',
              role == 'Collaborator'
                  ? 'Thêm hồ sơ MSSV và email cho sinh viên liên kết'
                  : 'Thêm, xem, sửa, xóa hồ sơ',
              Icons.people,
              () => manage('Sinh viên', '/api/students'),
            ),
          if (role.isNotEmpty)
            tile(
              'Lịch học',
              'Xem thời gian và phòng học',
              Icons.calendar_month,
              () => manage('Lịch học', '/api/schedules'),
            ),
        ],
      ),
    );
  }
}
