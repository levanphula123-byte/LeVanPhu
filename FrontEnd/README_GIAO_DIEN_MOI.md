# LeVanPhu — FrontEnd Flutter thiết kế lại

- Giữ nguyên các màn hình và luồng API: đăng nhập/OTP, sinh viên, lớp, môn học, lịch học, điểm danh QR/GPS, khuôn mặt, quản trị.
- Giao diện mới: xanh teal, thẻ bo góc, trang chủ có banner, nút và form Material 3.
- Swagger: https://littlebrasspencil10.conveyor.cloud/swagger/index.html
- API_BASE_URL trong `.env`: https://littlebrasspencil10.conveyor.cloud (không thêm `/swagger/index.html` vì đó là trang tài liệu, không phải gốc API).

## Chạy

```bash
flutter pub get
flutter run
```

## Lưu ý

Backend phải đang chạy, Conveyor phải còn hiệu lực và các route API phải khớp backend. Chưa xác thực kết nối thực tế hoặc build Flutter trong môi trường này.

Tên ứng dụng, package Flutter và Android đã đổi thành LeVanPhu / levanphu.
