using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeVanPhu.Api.DTOs;
using LeVanPhu.Api.Models;
namespace LeVanPhu.Api.Controllers;

[ApiController, Route("api/face"), Authorize(Roles = "Student")]
public class FaceController(AppDbContext db) : ControllerBase
{
    public static bool Valid(double[]? vector) => vector is { Length: 192 } && vector.All(double.IsFinite) && double.IsFinite(vector.Sum(x => x * x)) && vector.Sum(x => x * x) > 0.000001;
    [HttpGet]
    public async Task<IActionResult> Status() => Ok(new { registered = await db.FaceProfiles.AnyAsync(x => x.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier)) });
    [HttpPut]
    public async Task<IActionResult> Save(FaceRequest r)
    {
        if (!Valid(r.Embedding)) return BadRequest("Vector khuôn mặt phải có 192 số hợp lệ từ MobileFaceNet.");
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await db.FaceProfiles.SingleOrDefaultAsync(x => x.UserId == uid);
        if (profile == null) { profile = new FaceProfile { UserId = uid }; db.FaceProfiles.Add(profile); }
        profile.EmbeddingJson = JsonSerializer.Serialize(r.Embedding);
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { message = "Đã đăng ký/cập nhật khuôn mặt." });
    }
}
