import 'package:geolocator/geolocator.dart';

class LocationService {
  static Future<Position> current() async {
    if (!await Geolocator.isLocationServiceEnabled()) {
      throw Exception('Hãy bật GPS.');
    }
    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }
    if (permission == LocationPermission.denied ||
        permission == LocationPermission.deniedForever) {
      throw Exception('Cần cấp quyền vị trí trong cài đặt ứng dụng.');
    }
    final position = await Geolocator.getCurrentPosition(
      locationSettings: const LocationSettings(
        accuracy: LocationAccuracy.high,
        timeLimit: Duration(seconds: 20),
      ),
    );
    if (position.isMocked) throw Exception('Không chấp nhận vị trí giả lập.');
    if (position.accuracy > 100) {
      throw Exception('GPS chưa chính xác. Hãy ra vị trí thoáng và thử lại.');
    }
    return position;
  }
}
