import 'package:image/image.dart' as img;
import 'package:tflite_flutter/tflite_flutter.dart';

class FaceEmbeddingService {
  Interpreter? _interpreter;
  Future<void> initialize() async {
    _interpreter = await Interpreter.fromAsset(
      'assets/models/mobilefacenet.tflite',
    );
    final shape = _interpreter!.getOutputTensor(0).shape;
    if (shape.last != 192) throw Exception('Mô hình phải trả về 192 chiều.');
  }

  List<double> generateEmbedding(img.Image face) {
    final resized = img.copyResize(face, width: 112, height: 112);
    final input = [
      List.generate(
        112,
        (y) => List.generate(112, (x) {
          final p = resized.getPixel(x, y);
          return [
            (p.r / 127.5) - 1.0,
            (p.g / 127.5) - 1.0,
            (p.b / 127.5) - 1.0,
          ];
        }),
      ),
    ];
    final output = [List<double>.filled(192, 0)];
    _interpreter!.run(input, output);
    return output.first;
  }

  void dispose() => _interpreter?.close();
}
