import 'dart:async';
import 'dart:io';

import 'package:camera/camera.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:image/image.dart' as img;

import '../services/face_crop_service.dart';
import '../services/face_detector_service.dart';
import '../services/face_embedding_service.dart';

/// Tự chụp và tạo embedding. Callback gửi backend để backend so khớp.
class FaceCameraScreen extends StatefulWidget {
  final Future<void> Function(List<double>) onEmbedding;
  final Future<int> Function()? onReady;
  const FaceCameraScreen({super.key, required this.onEmbedding, this.onReady});
  @override
  State<FaceCameraScreen> createState() => _FaceCameraScreenState();
}

class _FaceCameraScreenState extends State<FaceCameraScreen> {
  final detector = FaceDetectorService();
  final model = FaceEmbeddingService();
  CameraController? camera;
  Timer? timer;
  late Future<void> initialization;
  DateTime? deadline;
  bool loading = true, processing = false, finished = false, disposed = false;
  String message = 'Đang khởi động camera trước...';
  int remaining = 0;
  @override
  void initState() {
    super.initState();
    initialization = initialize();
  }

  Future<void> initialize() async {
    try {
      await model.initialize();
      if (disposed) {
        return;
      }
      final cameras = await availableCameras();
      if (disposed) return;
      final front = cameras.where(
        (c) => c.lensDirection == CameraLensDirection.front,
      );
      if (front.isEmpty) throw Exception('Không tìm thấy camera trước.');
      camera = CameraController(
        front.first,
        ResolutionPreset.medium,
        enableAudio: false,
      );
      await camera!.initialize();
      if (disposed) return;
      await camera!.lockCaptureOrientation(DeviceOrientation.portraitUp);
      if (!mounted) return;
      if (widget.onReady != null) {
        final seconds = await widget.onReady!();
        deadline = DateTime.now().add(Duration(seconds: seconds));
        remaining = seconds;
      }
      if (!mounted) return;
      setState(() {
        loading = false;
        message = 'Nhìn thẳng camera, giữ mặt đủ sáng.';
      });
      timer = Timer.periodic(const Duration(milliseconds: 600), (_) {
        if (!mounted || finished) return;
        if (deadline != null) {
          remaining =
              (deadline!.difference(DateTime.now()).inMilliseconds / 1000)
                  .ceil();
          if (remaining <= 0) {
            timer?.cancel();
            setState(() {
              finished = true;
              message = 'Đã hết 10 giây. Quay lại và quét lại QR.';
            });
            return;
          }
          setState(() {});
        }
        scan();
      });
    } catch (e) {
      if (mounted) {
        setState(() {
          loading = false;
          finished = true;
          message = e.toString();
        });
      }
    }
  }

  Future<void> scan() async {
    if (processing || finished || camera == null) return;
    processing = true;
    XFile? photo;
    try {
      photo = await camera!.takePicture();
      // Bake EXIF orientation before both ML Kit and cropping, so coordinates agree.
      final decoded = img.decodeImage(await File(photo.path).readAsBytes());
      if (decoded == null) throw Exception('Không đọc được ảnh.');
      final upright = img.bakeOrientation(decoded);
      await File(photo.path).writeAsBytes(img.encodeJpg(upright));
      final faces = await detector.detect(photo.path);
      if (!mounted || finished) return;
      if (faces.length != 1) {
        setState(
          () => message = faces.isEmpty
              ? 'Chưa thấy mặt. Hãy nhìn thẳng camera.'
              : 'Chỉ để một khuôn mặt trong khung.',
        );
        return;
      }
      final embedding = model.generateEmbedding(
        FaceCropService().cropFace(upright, faces.first),
      );
      if (deadline != null && !DateTime.now().isBefore(deadline!)) return;
      await widget.onEmbedding(embedding);
      if (mounted) {
        finished = true;
        timer?.cancel();
        Navigator.pop(context, true);
      }
    } catch (e) {
      if (mounted) {
        setState(() => message = e.toString().replaceFirst('Exception: ', ''));
      }
    } finally {
      if (photo != null) {
        try {
          await File(photo.path).delete();
        } catch (_) {}
      }
      processing = false;
    }
  }

  @override
  void dispose() {
    disposed = true;
    timer?.cancel();
    // Do not close native resources while a capture/inference is in flight.
    release();
    super.dispose();
  }

  Future<void> release() async {
    await initialization;
    while (processing) {
      await Future<void>.delayed(const Duration(milliseconds: 100));
    }
    await camera?.dispose();
    await detector.dispose();
    model.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: Text(
        widget.onReady == null
            ? 'Đăng ký / cập nhật khuôn mặt'
            : 'Xác thực khuôn mặt',
      ),
    ),
    body: Column(
      children: [
        Expanded(
          child: loading
              ? const Center(child: CircularProgressIndicator())
              : camera?.value.isInitialized == true
              ? Center(child: CameraPreview(camera!))
              : const Center(child: Icon(Icons.no_photography, size: 80)),
        ),
        if (deadline != null)
          Text(
            '$remaining giây',
            style: Theme.of(context).textTheme.headlineMedium,
          ),
        Padding(
          padding: const EdgeInsets.all(20),
          child: Text(message, textAlign: TextAlign.center),
        ),
        if (finished)
          Padding(
            padding: const EdgeInsets.all(12),
            child: FilledButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Quay lại'),
            ),
          ),
      ],
    ),
  );
}
