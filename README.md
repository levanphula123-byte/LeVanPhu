# LeVanPhu — ASP.NET Core Web API (.NET 8)

Dự án mới, tổ chức lại từ mã nguồn backend được cung cấp, giữ các controller, entity, JWT/Identity, điểm danh và nhận diện khuôn mặt.

## Yêu cầu
- .NET 8 SDK (không chỉ runtime)
- SQL Server / SQL Server Express
- Visual Studio hỗ trợ .NET 8

## Mở project
Mở **LeVanPhu.slnx** bằng File > Open > Project/Solution. Không mở bằng Folder View.

## Thiết lập bí mật (trong thư mục LeVanPhu)
```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\\SQLEXPRESS;Database=LeVanPhu;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Secret" "THAY_BANG_CHUOI_NGAU_NHIEN_DU_DAI_IT_NHAT_32_KY_TU"
```
Tùy nhu cầu email: `dotnet user-secrets set "Smtp:User" "..."`, `dotnet user-secrets set "Smtp:Password" "..."`, `dotnet user-secrets set "Smtp:From" "..."`. Không commit mật khẩu.

## Restore và chạy
```powershell
cd LeVanPhu
dotnet restore
dotnet build
dotnet run
```
Swagger (môi trường Development): `/swagger`; health: `/api/health`.

## EF Core 8
Đã bao gồm Microsoft.EntityFrameworkCore.Tools và Design 8.0.20.
```powershell
dotnet tool install --global dotnet-ef --version 8.0.20
dotnet ef migrations list
```
Nếu dotnet-ef đã có, thay `install` bằng `update`. Mở đúng solution và chọn Default project = LeVanPhu nếu dùng Package Manager Console.

**Chú ý:** Các migrations được kế thừa từ dự án cũ, có thể đã tạo bằng EF Core 10. Cần kiểm tra với EF Core 8 và sao lưu dữ liệu trước khi áp dụng. Program.cs hiện tự gọi `MigrateAsync()` khi khởi động; điều này sẽ thay đổi database. Nếu chưa sẵn sàng áp dụng migrations, tạm vô hiệu hóa đoạn khởi tạo database trong Program.cs.

## Giới hạn kiểm thử
Môi trường tạo gói không có dotnet SDK/SQL Server nên chưa thực hiện được build, migrations hay integration tests. Đây là dự án tái tạo từ mã nguồn hiện có, không phải bản viết lại toàn bộ nghiệp vụ từ đầu.
