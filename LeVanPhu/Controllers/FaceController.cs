
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeVanPhu.Api.DTOs;
using LeVanPhu.Api.Models;

namespace LeVanPhu.Api.Controllers;

[ApiController]
[Route("api/face")]
[Authorize(Roles = "Student")]
public class FaceController(AppDbContext db) : ControllerBase
{
    // Kiểm tra vector khuôn mặt MobileFaceNet 192 chiều
    public static bool Valid(double[]? vector)
    {
        return vector is { Length: 192 }
            && vector.All(double.IsFinite)
            && double.IsFinite(vector.Sum(x => x * x))
            && vector.Sum(x => x * x) > 0.000001;
    }

    // GET: api/face
    // Kiểm tra sinh viên đã đăng ký khuôn mặt chưa
    [HttpGet]
    public async Task<IActionResult> Status()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new
            {
                message = "Không xác định được tài khoản."
            });
        }

        var registered = await db.FaceProfiles
            .AnyAsync(x => x.UserId == userId);

        return Ok(new
        {
            registered
        });
    }

    // PUT: api/face
    // Đăng ký hoặc cập nhật khuôn mặt
    [HttpPut]
    public async Task<IActionResult> Save(
        [FromBody] FaceRequest request)
    {
        if (!Valid(request.Embedding))
        {
            return BadRequest(new
            {
                message = "Vector khuôn mặt phải có 192 số hợp lệ từ MobileFaceNet."
            });
        }

        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new
            {
                message = "Không xác định được tài khoản."
            });
        }

        var student = await db.Students
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (student == null ||
            string.IsNullOrWhiteSpace(student.StudentCode))
        {
            return BadRequest(new
            {
                message = "Tài khoản chưa liên kết MSSV."
            });
        }

        var profile = await db.FaceProfiles
            .SingleOrDefaultAsync(x => x.UserId == userId);

        if (profile == null)
        {
            profile = new FaceProfile
            {
                UserId = userId
            };

            db.FaceProfiles.Add(profile);
        }

        profile.EmbeddingJson =
            JsonSerializer.Serialize(request.Embedding);

        profile.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok(new
        {
            message = "Đã đăng ký/cập nhật khuôn mặt.",
            registered = true
        });
    }

    // GET: api/face/debug-auth
    // Kiểm tra JWT và quyền Student
    // Chỉ sử dụng trong môi trường Development
    [HttpGet("debug-auth")]
    [Authorize]
    public IActionResult DebugAuth()
    {
        if (!HttpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsDevelopment())
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        var roles = User.FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .ToArray();

        return Ok(new
        {
            IsAuthenticated =
                User.Identity?.IsAuthenticated,

            UserId = userId,

            Roles = roles,

            IsStudent = User.IsInRole("Student"),

            AuthenticationType =
                User.Identity?.AuthenticationType
        });
    }
}
