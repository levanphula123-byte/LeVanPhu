import 'package:flutter/material.dart';

import '../services/api_service.dart';

class EntryField {
  final String keyName, label;
  final bool number, secret;
  final Map<String, String>? choices;
  const EntryField(
    this.keyName,
    this.label, {
    this.number = false,
    this.secret = false,
    this.choices,
  });
}

/// Form có nhãn rõ ràng; chuyển số và ngày giờ tại lớp giao diện.
Future<Map<String, dynamic>?> showEntryForm(
  BuildContext context,
  String title,
  List<EntryField> fields, {
  Map<String, dynamic>? initial,
}) async {
  final controllers = {
    for (final f in fields)
      f.keyName: TextEditingController(
        text:
            initial?[f.keyName]?.toString() ??
            f.choices?.keys.firstOrNull ??
            '',
      ),
  };
  final formKey = GlobalKey<FormState>();
  final result = await showDialog<Map<String, dynamic>>(
    context: context,
    builder: (ctx) => AlertDialog(
      title: Text(title),
      content: SizedBox(
        width: 440,
        child: Form(
          key: formKey,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: fields
                  .map(
                    (f) => Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: f.choices != null
                          ? DropdownButtonFormField<String>(
                              initialValue:
                                  f.choices!.containsKey(
                                    controllers[f.keyName]!.text,
                                  )
                                  ? controllers[f.keyName]!.text
                                  : f.choices!.keys.firstOrNull,
                              decoration: InputDecoration(labelText: f.label),
                              items: f.choices!.entries
                                  .map(
                                    (e) => DropdownMenuItem(
                                      value: e.key,
                                      child: Text(
                                        e.value,
                                        overflow: TextOverflow.ellipsis,
                                      ),
                                    ),
                                  )
                                  .toList(),
                              onChanged: (value) =>
                                  controllers[f.keyName]!.text = value ?? '',
                            )
                          : TextFormField(
                              controller: controllers[f.keyName],
                              obscureText: f.secret,
                              keyboardType: f.number
                                  ? TextInputType.number
                                  : TextInputType.text,
                              decoration: InputDecoration(labelText: f.label),
                              validator: (v) {
                                if (v == null || v.trim().isEmpty) {
                                  return 'Vui lòng nhập ${f.label.toLowerCase()}';
                                }
                                if (f.number && int.tryParse(v) == null) {
                                  return 'Nhập số nguyên';
                                }
                                if (f.keyName.endsWith('AtUtc') &&
                                    DateTime.tryParse(v) == null) {
                                  return 'Ví dụ 2026-10-21T07:00';
                                }
                                return null;
                              },
                            ),
                    ),
                  )
                  .toList(),
            ),
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(ctx),
          child: const Text('Hủy'),
        ),
        FilledButton(
          onPressed: () {
            if (!formKey.currentState!.validate()) return;
            final data = <String, dynamic>{};
            for (final f in fields) {
              final value = controllers[f.keyName]!.text.trim();
              if (value.isEmpty) return;
              data[f.keyName] = f.number
                  ? int.parse(value)
                  : f.keyName.endsWith('AtUtc')
                  ? DateTime.parse(value).toUtc().toIso8601String()
                  : value;
            }
            Navigator.pop(ctx, data);
          },
          child: const Text('Lưu'),
        ),
      ],
    ),
  );
  // Dialog animation may still reference its text controllers; defer disposal.
  Future<void>.delayed(const Duration(milliseconds: 350), () {
    for (final c in controllers.values) {
      c.dispose();
    }
  });
  return result;
}

class ManagementScreen extends StatefulWidget {
  final String title, endpoint, role;
  const ManagementScreen({
    super.key,
    required this.title,
    required this.endpoint,
    required this.role,
  });
  @override
  State<ManagementScreen> createState() => _ManagementScreenState();
}

class _ManagementScreenState extends State<ManagementScreen> {
  List<dynamic> items = [];
  bool loading = true;
  String? error;
  bool get canEdit => widget.role != 'Student' && widget.role != 'Collaborator';
  bool get canCreate =>
      canEdit ||
      widget.endpoint == '/api/students' && widget.role == 'Collaborator';
  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    try {
      final data = await ApiService.request('GET', widget.endpoint);
      if (mounted) {
        setState(() {
          items = data['data'] as List? ?? [];
          error = null;
        });
      }
    } catch (e) {
      if (mounted) setState(() => error = e.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<List<EntryField>> fields(bool editing) async {
    switch (widget.endpoint) {
      case '/api/students':
        return const [
          EntryField('studentCode', 'MSSV'),
          EntryField('fullName', 'Họ tên'),
          EntryField('email', 'Email liên kết'),
          EntryField('faculty', 'Khoa'),
        ];
      case '/api/accounts':
        return [
          const EntryField('fullName', 'Họ tên'),
          const EntryField('email', 'Email'),
          if (!editing) ...[
            EntryField(
              'role',
              'Vai trò',
              choices: widget.role == 'Admin'
                  ? {'Lecturer': 'Giảng viên'}
                  : {'Collaborator': 'Cộng tác viên'},
            ),
            const EntryField('password', 'Mật khẩu ban đầu', secret: true),
          ],
        ];
      case '/api/courses':
        return const [
          EntryField('code', 'Mã môn'),
          EntryField('name', 'Tên môn'),
          EntryField('credits', 'Số tín chỉ', number: true),
        ];
      case '/api/classes':
        final courses =
            (await ApiService.request('GET', '/api/courses'))['data'] as List;
        final result = [
          const EntryField('name', 'Tên lớp'),
          EntryField(
            'courseId',
            'Môn học',
            number: true,
            choices: {
              for (final c in courses)
                '${c['id']}': '${c['code']} - ${c['name']}',
            },
          ),
          const EntryField('room', 'Phòng học'),
        ];
        if (widget.role == 'Admin') {
          final accounts =
              (await ApiService.request('GET', '/api/accounts'))['data']
                  as List;
          result.add(
            EntryField(
              'lecturerUserId',
              'Giảng viên',
              choices: {
                for (final a in accounts.where(
                  (a) => (a['roles'] as List).contains('Lecturer'),
                ))
                  '${a['id']}': '${a['fullName']}',
              },
            ),
          );
        }
        return result;
      case '/api/schedules':
        final classes =
            (await ApiService.request('GET', '/api/classes'))['data'] as List;
        return [
          EntryField(
            'classGroupId',
            'Lớp',
            number: true,
            choices: {for (final c in classes) '${c['id']}': '${c['name']}'},
          ),
          const EntryField('startsAtUtc', 'Bắt đầu (giờ VN: YYYY-MM-DDTHH:mm)'),
          const EntryField('endsAtUtc', 'Kết thúc (giờ VN: YYYY-MM-DDTHH:mm)'),
          const EntryField('room', 'Phòng học'),
        ];
      default:
        return [];
    }
  }

  Future<void> edit([Map<String, dynamic>? item]) async {
    try {
      final definitions = await fields(item != null);
      if (definitions.any((f) => f.choices != null && f.choices!.isEmpty)) {
        throw Exception('Hãy tạo môn học/giảng viên/lớp trước.');
      }
      final initial = item == null ? null : Map<String, dynamic>.from(item);
      if (initial != null) {
        for (final key in ['startsAtUtc', 'endsAtUtc']) {
          if (initial[key] != null) {
            initial[key] = DateTime.parse(
              '${initial[key]}Z'.replaceAll('ZZ', 'Z'),
            ).toLocal().toIso8601String();
          }
        }
      }
      if (!mounted) return;
      final body = await showEntryForm(
        context,
        item == null ? 'Thêm ${widget.title}' : 'Sửa ${widget.title}',
        definitions,
        initial: initial,
      );
      if (body == null) return;
      await ApiService.request(
        item == null ? 'POST' : 'PUT',
        '${widget.endpoint}${item == null ? '' : '/${item['id']}'}',
        body: body,
      );
      await load();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.toString())));
      }
    }
  }

  Future<void> delete(Map<String, dynamic> item) async {
    final yes = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Xóa bản ghi?'),
        content: Text(title(item)),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Hủy'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Xóa'),
          ),
        ],
      ),
    );
    if (yes != true) return;
    try {
      await ApiService.request('DELETE', '${widget.endpoint}/${item['id']}');
      await load();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.toString())));
      }
    }
  }

  String title(Map<String, dynamic> item) =>
      '${item['fullName'] ?? item['name'] ?? (item['classGroupId'] != null ? 'Lớp #${item['classGroupId']}' : item['id'])}';
  String subtitle(Map<String, dynamic> item) {
    if (widget.endpoint == '/api/schedules') {
      return '${item['startsAtUtc']} UTC → ${item['endsAtUtc']} UTC · ${item['room']}';
    }
    return [
      item['studentCode'],
      item['code'],
      item['email'],
      item['room'],
      if (item['credits'] != null) '${item['credits']} tín chỉ',
      if (item['roles'] != null) (item['roles'] as List).join(', '),
    ].where((x) => x != null).join(' · ');
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: Text(widget.title),
      actions: [IconButton(onPressed: load, icon: const Icon(Icons.refresh))],
    ),
    floatingActionButton: canCreate
        ? FloatingActionButton(
            onPressed: () => edit(),
            child: const Icon(Icons.add),
          )
        : null,
    body: loading
        ? const Center(child: CircularProgressIndicator())
        : RefreshIndicator(
            onRefresh: load,
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                if (error != null) Text(error!),
                if (items.isEmpty)
                  const Text('Chưa có dữ liệu. Nhấn + để thêm.'),
                ...items.map((raw) {
                  final item = Map<String, dynamic>.from(raw as Map);
                  return Card(
                    child: ListTile(
                      title: Text(title(item)),
                      subtitle: Text(subtitle(item)),
                      trailing: canEdit
                          ? Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                IconButton(
                                  onPressed: () => edit(item),
                                  icon: const Icon(Icons.edit),
                                ),
                                IconButton(
                                  onPressed: () => delete(item),
                                  icon: const Icon(Icons.delete_outline),
                                ),
                              ],
                            )
                          : null,
                    ),
                  );
                }),
                const SizedBox(height: 80),
              ],
            ),
          ),
  );
}
