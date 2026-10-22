import 'dart:math' as math;

import 'package:flutter/foundation.dart';
import 'package:image/image.dart' as img;
import 'package:tflite_flutter/tflite_flutter.dart';

class FaceEmbeddingService {
  Interpreter? _interpreter;

  Future<void> initialize() async {
    _interpreter = await Interpreter.fromAsset(
      'assets/models/mobilefacenet.tflite',
    );

    final inputTensor = _interpreter!.getInputTensor(0);
    final outputTensor = _interpreter!.getOutputTensor(0);

    debugPrint('[TFLITE] Input shape: ${inputTensor.shape}');
    debugPrint('[TFLITE] Input type: ${inputTensor.type}');
    debugPrint('[TFLITE] Output shape: ${outputTensor.shape}');
    debugPrint('[TFLITE] Output type: ${outputTensor.type}');

    if (!listEquals(inputTensor.shape, [1, 112, 112, 3])) {
      throw Exception(
        'Input shape không hợp lệ: ${inputTensor.shape}',
      );
    }

    if (!listEquals(outputTensor.shape, [1, 192])) {
      throw Exception(
        'Output shape không hợp lệ: ${outputTensor.shape}',
      );
    }

    if (inputTensor.type != TensorType.float32 ||
        outputTensor.type != TensorType.float32) {
      throw Exception(
        'Mô hình cần input/output float32. '
            'Thực tế: ${inputTensor.type}, ${outputTensor.type}',
      );
    }

    debugPrint('[TFLITE] Khởi tạo mô hình thành công');
  }

  List<double> generateEmbedding(img.Image face) {
    final interpreter = _interpreter;

    if (interpreter == null) {
      throw StateError('Mô hình chưa được khởi tạo.');
    }

    final resized = img.copyResize(
      face,
      width: 112,
      height: 112,
    );

    final input = [
      List.generate(
        112,
            (y) => List.generate(112, (x) {
          final p = resized.getPixel(x, y);

          return <double>[
            (p.r / 127.5) - 1.0,
            (p.g / 127.5) - 1.0,
            (p.b / 127.5) - 1.0,
          ];
        }),
      ),
    ];

    final output = [
      List<double>.filled(192, 0.0),
    ];

    interpreter.run(input, output);

    final embedding = output.first;

    if (embedding.any((value) => !value.isFinite)) {
      throw Exception('Embedding chứa giá trị không hợp lệ.');
    }

    final norm = math.sqrt(
      embedding.fold<double>(
        0.0,
            (sum, value) => sum + value * value,
      ),
    );

    if (norm <= 1e-12) {
      throw Exception('Embedding có độ dài bằng 0.');
    }

    debugPrint('[TFLITE] Tạo embedding thành công');
    debugPrint('[TFLITE] Embedding length: ${embedding.length}');
    debugPrint('[TFLITE] Embedding norm: $norm');

    return embedding;
  }

  void dispose() {
    _interpreter?.close();
    _interpreter = null;
  }
}