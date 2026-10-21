import 'package:google_mlkit_face_detection/google_mlkit_face_detection.dart';
import 'package:image/image.dart' as img;

class FaceCropService {
  img.Image cropFace(img.Image image, Face face) {
    final box = face.boundingBox;

    int left = box.left.round();
    int top = box.top.round();
    int width = box.width.round();
    int height = box.height.round();

    left = left.clamp(0, image.width - 1);

    top = top.clamp(0, image.height - 1);

    width = width.clamp(1, image.width - left);

    height = height.clamp(1, image.height - top);

    return img.copyCrop(image, x: left, y: top, width: width, height: height);
  }
}
