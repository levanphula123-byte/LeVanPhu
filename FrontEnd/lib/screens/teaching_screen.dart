import 'package:flutter/material.dart';
import 'package:qr_flutter/qr_flutter.dart';

import '../services/api_service.dart';
import '../services/location_service.dart';
import 'management_screen.dart';

class TeachingScreen extends StatefulWidget {
  const TeachingScreen({super.key});
  @override
  State<TeachingScreen> createState() => _TeachingScreenState();
}

class _TeachingScreenState extends State<TeachingScreen> {
  List<dynamic> classes = [], sessions = [];
  String? error;
  bool busy = false;
  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    try {
      final results = await Future.wait([
        ApiService.request('GET', '/api/classes'),
        ApiService.request('GET', '/api/attendance/sessions'),
      ]);
      if (mounted) {
        setState(() {
          classes = results[0]['data'] as List;
          sessions = results[1]['data'] as List;
          error = null;
        });
      }
    } catch (e) {
      if (mounted) setState(() => error = e.toString());
    }
  }

  void notify(Object e) {
    if (mounted) {
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(e.toString())));
    }
  }

  Future<void> createSession(Map<String, dynamic> c) async {
    if (busy) return;
    setState(() => busy = true);
    try {
      final position = await LocationService.current();
      final session = await ApiService.request(
        'POST',
        '/api/attendance/sessions',
        body: {
          'classGroupId': c['id'],
          'latitude': position.latitude,
          'longitude': position.longitude,
          'durationMinutes': 10,
        },
      );
      await load();
      if (mounted) await showQr(session);
    } catch (e) {
      notify(e);
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> showQr(Map<String, dynamic> session) async {
    await showDialog<void>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('QR phiên #${session['id']}'),
        content: SizedBox(
          width: 300,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              QrImageView(data: session['qrToken'] as String, size: 260),
              const Text(
                'Sinh viên trong lớp, cách vị trí mở phiên ≤150m. Sau kiểm tra GPS có 10 giây xác thực khuôn mặt.',
              ),
              Text('Hết hạn: ${session['expiresAtUtc']} UTC'),
            ],
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
  }

  Future<void> members(Map<String, dynamic> c) async {
    try {
      final enrolled =
          (await ApiService.request(
                'GET',
                '/api/classes/${c['id']}/students',
              ))['data']
              as List;
      final all =
          (await ApiService.request('GET', '/api/students'))['data'] as List;
      if (!mounted) return;
      await showDialog<void>(
        context: context,
        builder: (ctx) => AlertDialog(
          title: Text('Danh sách ${c['name']}'),
          content: SizedBox(
            width: 440,
            child: ListView(
              shrinkWrap: true,
              children: all.map((s) {
                final exists = enrolled.any((e) => e['id'] == s['id']);
                return ListTile(
                  title: Text('${s['studentCode']} · ${s['fullName']}'),
                  subtitle: Text(exists ? 'Đã ghi danh' : 'Chưa ghi danh'),
                  trailing: IconButton(
                    icon: Icon(exists ? Icons.person_remove : Icons.person_add),
                    onPressed: () async {
                      try {
                        await ApiService.request(
                          exists ? 'DELETE' : 'POST',
                          '/api/classes/${c['id']}/students/${s['id']}',
                        );
                        if (ctx.mounted) Navigator.pop(ctx);
                      } catch (e) {
                        notify(e);
                      }
                    },
                  ),
                );
              }).toList(),
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
      notify(e);
    }
  }

  Future<void> records(Map<String, dynamic> session) async {
    try {
      final rows =
          (await ApiService.request(
                'GET',
                '/api/attendance/sessions/${session['id']}/records',
              ))['data']
              as List;
      if (!mounted) return;
      await showDialog<void>(
        context: context,
        builder: (ctx) => AlertDialog(
          title: Text('Bảng điểm danh #${session['id']}'),
          content: SizedBox(
            width: 440,
            child: ListView(
              shrinkWrap: true,
              children: rows
                  .map(
                    (s) => ListTile(
                      title: Text('${s['studentCode']} · ${s['fullName']}'),
                      subtitle: Text(
                        s['record'] == null ||
                                s['record']['method'] == 'Manual:Absent'
                            ? 'Vắng / chưa điểm danh'
                            : 'Có mặt · ${s['record']['method']}',
                      ),
                      trailing: IconButton(
                        icon: const Icon(Icons.edit_note),
                        onPressed: () async {
                          Navigator.pop(ctx);
                          final form = await showEntryForm(
                            context,
                            'Điều chỉnh điểm danh',
                            const [
                              EntryField(
                                'present',
                                'Trạng thái',
                                choices: {'true': 'Có mặt', 'false': 'Vắng'},
                              ),
                              EntryField('reason', 'Lý do điều chỉnh'),
                            ],
                          );
                          if (form == null) return;
                          try {
                            await ApiService.request(
                              'PUT',
                              '/api/attendance/sessions/${session['id']}/records/${s['studentId']}',
                              body: {
                                'present': form['present'] == 'true',
                                'reason': form['reason'],
                              },
                            );
                          } catch (e) {
                            notify(e);
                          }
                        },
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
      notify(e);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('Lớp và phiên điểm danh'),
      actions: [IconButton(onPressed: load, icon: const Icon(Icons.refresh))],
    ),
    body: ListView(
      padding: const EdgeInsets.all(16),
      children: [
        if (error != null) Text(error!),
        if (busy) const LinearProgressIndicator(),
        const Text(
          'Lớp phụ trách',
          style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
        ),
        ...classes.map((raw) {
          final c = Map<String, dynamic>.from(raw as Map);
          return Card(
            child: ListTile(
              title: Text('${c['name']}'),
              subtitle: Text('Phòng ${c['room']}'),
              trailing: Wrap(
                children: [
                  IconButton(
                    tooltip: 'Danh sách lớp',
                    onPressed: () => members(c),
                    icon: const Icon(Icons.groups),
                  ),
                  IconButton(
                    tooltip: 'Mở phiên 10 phút tại vị trí hiện tại',
                    onPressed: busy ? null : () => createSession(c),
                    icon: const Icon(Icons.qr_code),
                  ),
                ],
              ),
            ),
          );
        }),
        const SizedBox(height: 20),
        const Text(
          'Các phiên điểm danh',
          style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
        ),
        ...sessions.map((raw) {
          final s = Map<String, dynamic>.from(raw as Map);
          return Card(
            child: Column(
              children: [
                ListTile(
                  title: Text('Phiên #${s['id']} · Lớp #${s['classGroupId']}'),
                  subtitle: Text(
                    'Hết hạn ${s['expiresAtUtc']} UTC · ${s['isOpen'] == true ? 'Mở' : 'Đóng'}',
                  ),
                ),
                Wrap(
                  children: [
                    TextButton(
                      onPressed: () => showQr(s),
                      child: const Text('Hiện QR'),
                    ),
                    TextButton(
                      onPressed: () => records(s),
                      child: const Text('Bảng điểm danh'),
                    ),
                    if (s['isOpen'] == true)
                      TextButton(
                        onPressed: () async {
                          try {
                            await ApiService.request(
                              'PUT',
                              '/api/attendance/sessions/${s['id']}/close',
                            );
                            await load();
                          } catch (e) {
                            notify(e);
                          }
                        },
                        child: const Text('Đóng phiên'),
                      ),
                  ],
                ),
              ],
            ),
          );
        }),
      ],
    ),
  );
}
