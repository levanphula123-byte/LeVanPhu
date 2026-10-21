using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using LeVanPhu.Api.DTOs;
using LeVanPhu.Api.Models;
using LeVanPhu.Api.Services;

namespace LeVanPhu.Api.Controllers;

[ApiController, Route("api/auth"), EnableRateLimiting("otp")]
public class AuthController(UserManager<AppUser> users, AppDbContext db, IConfiguration config, EmailService email) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest r)
    {
        var address = r.Email.Trim().ToLowerInvariant();
        var student = await db.Students.SingleOrDefaultAsync(x => x.StudentCode == r.StudentCode.Trim() && x.Email == address);
        if (student == null || student.UserId != null)
            return BadRequest("MSSV và email phải trùng hồ sơ do giảng viên/cộng tác viên tạo và chưa liên kết.");
        if (await users.FindByEmailAsync(address) != null) return Conflict("Email đã được đăng ký. Dùng chức năng gửi lại OTP nếu chưa xác minh.");
        var user = new AppUser { UserName = address, Email = address, FullName = student.FullName };
        await using var transaction = await db.Database.BeginTransactionAsync();
        var result = await users.CreateAsync(user, r.Password);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(x => x.Description));
        result = await users.AddToRoleAsync(user, "Student");
        if (!result.Succeeded) return BadRequest(result.Errors.Select(x => x.Description));
        student.UserId = user.Id;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await SendCode(user, "registration");
    }

    [HttpPost("resend-email")]
    public async Task<IActionResult> Resend(VerifyOtpRequest r)
    {
        var user = await FindByLoginAsync(r.Email);
        if (user == null) return BadRequest("Không tìm thấy tài khoản đã liên kết. Kiểm tra MSSV, mã GV hoặc email.");
        if (user.EmailConfirmed) return BadRequest("Email đã xác minh. Quay lại đăng nhập bằng mã và mật khẩu, rồi dùng OTP đăng nhập mới gửi tới Gmail.");
        return await SendCode(user, "registration");
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> Verify(VerifyOtpRequest r)
    {
        var user = await FindByLoginAsync(r.Email);
        if (user == null || user.EmailConfirmed || !await users.VerifyUserTokenAsync(user, "Email", "registration", r.Otp))
            return BadRequest("OTP không đúng hoặc đã hết hạn.");
        user.EmailConfirmed = true;
        await users.UpdateAsync(user);
        await users.UpdateSecurityStampAsync(user);
        return Ok(new { message = "Liên kết email với MSSV thành công. Hãy đăng nhập và đăng ký khuôn mặt." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest r)
    {
        var user = await FindByLoginAsync(r.Email);
        if (user == null) return Unauthorized("Mã/email hoặc mật khẩu không đúng. Sinh viên chưa liên kết cần đăng ký trước.");
        if (await users.IsLockedOutAsync(user)) return Unauthorized("Tài khoản tạm khóa do nhập sai nhiều lần. Đợi 5 phút rồi thử lại.");
        if (!await users.CheckPasswordAsync(user, r.Password))
        {
            await users.AccessFailedAsync(user);
            return Unauthorized("Mật khẩu không đúng. Dùng mật khẩu tài khoản ứng dụng, không dùng mật khẩu Gmail hoặc App Password.");
        }
        if (!user.EmailConfirmed) return Unauthorized("Email chưa được xác minh. Hãy gửi lại OTP đăng ký.");
        await users.ResetAccessFailedCountAsync(user);
        return await SendCode(user, "login");
    }

    [HttpPost("verify-login")]
    public async Task<IActionResult> VerifyLogin(VerifyOtpRequest r)
    {
        var user = await FindByLoginAsync(r.Email);
        if (user == null || !user.EmailConfirmed || await users.IsLockedOutAsync(user)) return Unauthorized();
        if (!await users.VerifyUserTokenAsync(user, "Email", "login", r.Otp))
        {
            await users.AccessFailedAsync(user);
            return Unauthorized("OTP không đúng hoặc hết hạn.");
        }
        await users.UpdateSecurityStampAsync(user); // OTP becomes unusable after success.
        await users.ResetAccessFailedCountAsync(user);
        var roles = await users.GetRolesAsync(user);
        // A shared admin/lecturer identity enters the requested role when using a code.
        var identifier = r.Email.Trim().ToUpperInvariant();
        if (identifier.Contains('@') && roles.Contains("Admin"))
            roles = roles.OrderBy(role => role == "Admin" ? 0 : 1).ToList();
        if (!identifier.Contains('@'))
        {
            if (roles.Contains("Lecturer") && await db.Lecturers.AnyAsync(x => x.UserId == user.Id && x.LecturerCode == identifier))
                roles = new List<string> { "Lecturer" };
            else if (roles.Contains("Student") && await db.Students.AnyAsync(x => x.UserId == user.Id && x.StudentCode == identifier))
                roles = new List<string> { "Student" };
        }
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id), new(ClaimTypes.Email, user.Email!), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var jwt = config.GetSection("Jwt");
        var token = new JwtSecurityToken(jwt["Issuer"], jwt["Audience"], claims, expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"]!)), SecurityAlgorithms.HmacSha256));
        return Ok(new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), roles.FirstOrDefault() ?? "Student", user.FullName, user.Email!));
    }

    // Email remains supported for administrators, collaborators and existing clients.
    // Resolve codes for every OTP step so the client can keep using the same identifier.
    private async Task<AppUser?> FindByLoginAsync(string identifier)
    {
        var value = identifier.Trim();
        if (string.IsNullOrEmpty(value)) return null;
        if (value.Contains('@')) return await users.FindByEmailAsync(value);
        var code = value.ToUpperInvariant();
        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(x => x.StudentCode == code);
        if (student != null)
            return student.UserId == null ? null : await users.FindByIdAsync(student.UserId);
        var lecturer = await db.Lecturers.AsNoTracking().SingleOrDefaultAsync(x => x.LecturerCode == code);
        return lecturer == null ? null : await users.FindByIdAsync(lecturer.UserId);
    }
    private async Task<IActionResult> SendCode(AppUser user, string purpose)
    {
        var code = await users.GenerateUserTokenAsync(user, "Email", purpose);
        try { await email.SendOtpAsync(user.Email!, code); }
        catch (Exception) { return Problem(statusCode: 503, title: "Chưa gửi được OTP. Kiểm tra cấu hình Gmail SMTP; tài khoản đã tạo có thể gửi lại OTP."); }
        return Ok(new { message = "Đã gửi OTP tới email.", email = user.Email, requiresOtp = true });
    }
}
