import 'package:google_mlkit_face_detection/google_mlkit_face_detection.dart';

class FaceDetectorService {
  final FaceDetector _detector = FaceDetector(
    options: FaceDetectorOptions(
      performanceMode: FaceDetectorMode.accurate,
      enableLandmarks: true,
      enableClassification: true,
    ),
  );

  Future<List<Face>> detect(String imagePath) async {
    final inputImage = InputImage.fromFilePath(imagePath);

    return await _detector.processImage(inputImage);
  }

  Future<void> dispose() async {
    await _detector.close();
  }
}
