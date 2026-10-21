using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeVanPhu.Api.DTOs;
using LeVanPhu.Api.Models;
namespace LeVanPhu.Api.Controllers;

[ApiController, Route("api/accounts"), Authorize(Roles = "Admin,Lecturer")]
public class AccountsController(UserManager<AppUser> users, AppDbContext db) : ControllerBase
{
    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = new List<object>();
        foreach (var u in await db.Users.Where(x => User.IsInRole("Admin") || x.CreatedByUserId == Uid).AsNoTracking().ToListAsync())
        {
            var roles = await users.GetRolesAsync(u);
            if (roles.Contains("Lecturer") || roles.Contains("Collaborator"))
                result.Add(new { u.Id, u.Email, u.FullName, roles });
        }
        return Ok(result);
    }
    [HttpPost]
    public async Task<IActionResult> Create(AccountRequest r)
    {
        if (r.Role is not ("Lecturer" or "Collaborator")) return BadRequest("Sinh viên được thêm qua hồ sơ MSSV. Chỉ tạo Lecturer hoặc Collaborator ở đây.");
        if (r.Role == "Lecturer" && !User.IsInRole("Admin")) return Forbid();
        if (r.Role == "Collaborator" && !User.IsInRole("Lecturer")) return Forbid();
        if (string.IsNullOrWhiteSpace(r.FullName) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(r.Email)) return BadRequest("Họ tên/email không hợp lệ.");
        await using var tx = await db.Database.BeginTransactionAsync();
        var user = new AppUser { UserName = r.Email.Trim().ToLowerInvariant(), Email = r.Email.Trim().ToLowerInvariant(), FullName = r.FullName.Trim(), CreatedByUserId = Uid };
        var result = await users.CreateAsync(user, r.Password);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(x => x.Description));
        result = await users.AddToRoleAsync(user, r.Role);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(x => x.Description));
        if (r.Role == "Lecturer") db.Lecturers.Add(new Lecturer { UserId = user.Id, LecturerCode = "GV" + Guid.NewGuid().ToString("N")[..8], FullName = user.FullName, Email = user.Email });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { user.Id, user.Email, user.FullName, role = r.Role, message = "Tài khoản đã tạo. Người dùng gửi lại OTP ở màn hình đăng ký để xác minh email trước khi đăng nhập." });
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, AccountUpdateRequest r)
    {
        var user = await users.FindByIdAsync(id);
        if (user == null) return NotFound();
        if (!User.IsInRole("Admin") && user.CreatedByUserId != Uid) return Forbid();
        if (!await users.IsInRoleAsync(user, "Lecturer") && !await users.IsInRoleAsync(user, "Collaborator")) return BadRequest("Endpoint này chỉ quản lý giảng viên và cộng tác viên.");
        if (string.IsNullOrWhiteSpace(r.FullName)) return BadRequest("Họ tên bắt buộc.");
        // Email is the verified login identity; keep it fixed and update the display name.
        if (!string.Equals(user.Email, r.Email.Trim(), StringComparison.OrdinalIgnoreCase)) return BadRequest("MVP không đổi email của tài khoản đã tạo.");
        user.FullName = r.FullName.Trim();
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(x => x.Description));
        var lecturer = await db.Lecturers.SingleOrDefaultAsync(x => x.UserId == id);
        if (lecturer != null) { lecturer.FullName = user.FullName; await db.SaveChangesAsync(); }
        return Ok(new { user.Id, user.FullName, user.Email });
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await users.FindByIdAsync(id);
        if (user == null) return NotFound();
        if (id == Uid || await users.IsInRoleAsync(user, "Admin")) return BadRequest("Không xóa tài khoản quản trị/đang sử dụng.");
        if (!User.IsInRole("Admin") && user.CreatedByUserId != Uid) return Forbid();
        if (!await users.IsInRoleAsync(user, "Lecturer") && !await users.IsInRoleAsync(user, "Collaborator")) return BadRequest("Endpoint này chỉ quản lý giảng viên và cộng tác viên.");
        if (await db.Classes.AnyAsync(x => x.LecturerUserId == id)) return Conflict("Giảng viên còn phụ trách lớp.");
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Lecturers.Where(x => x.UserId == id).ExecuteDeleteAsync();
        var result = await users.DeleteAsync(user);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(x => x.Description));
        await tx.CommitAsync();
        return NoContent();
    }
}
