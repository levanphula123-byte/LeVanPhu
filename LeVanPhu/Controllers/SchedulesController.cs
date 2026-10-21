using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeVanPhu.Api.DTOs;
using LeVanPhu.Api.Models;
namespace LeVanPhu.Api.Controllers;

[ApiController, Route("api/schedules"), Authorize]
public class SchedulesController(AppDbContext db) : ControllerBase
{
    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private IQueryable<ClassGroup> Visible => db.Classes.Where(c => User.IsInRole("Admin") || c.LecturerUserId == Uid || db.ClassMembers.Any(m => m.ClassGroupId == c.Id && db.Students.Any(s => s.Id == m.StudentId && s.UserId == Uid)) || User.IsInRole("Collaborator") && db.Users.Any(u => u.Id == Uid && u.CreatedByUserId == c.LecturerUserId));
    private async Task<bool> Manage(int classId) => User.IsInRole("Admin") || await db.Classes.AnyAsync(x => x.Id == classId && x.LecturerUserId == Uid);
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.Schedules.Where(s => Visible.Any(c => c.Id == s.ClassGroupId)).OrderBy(x => x.StartsAtUtc).ToListAsync());
    [HttpPost, Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Create(ScheduleRequest r)
    {
        if (!await Manage(r.ClassGroupId)) return Forbid();
        if (r.EndsAtUtc <= r.StartsAtUtc) return BadRequest("Giờ kết thúc phải sau giờ bắt đầu.");
        var s = new Schedule { ClassGroupId = r.ClassGroupId, StartsAtUtc = r.StartsAtUtc.ToUniversalTime(), EndsAtUtc = r.EndsAtUtc.ToUniversalTime(), Room = r.Room };
        db.Schedules.Add(s); await db.SaveChangesAsync(); return Ok(s);
    }
    [HttpPut("{id:int}"), Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Update(int id, ScheduleRequest r)
    {
        var s = await db.Schedules.FindAsync(id); if (s == null) return NotFound();
        if (!await Manage(s.ClassGroupId) || !await Manage(r.ClassGroupId)) return Forbid();
        if (r.EndsAtUtc <= r.StartsAtUtc) return BadRequest("Giờ kết thúc phải sau giờ bắt đầu.");
        s.ClassGroupId = r.ClassGroupId; s.StartsAtUtc = r.StartsAtUtc.ToUniversalTime(); s.EndsAtUtc = r.EndsAtUtc.ToUniversalTime(); s.Room = r.Room;
        await db.SaveChangesAsync(); return Ok(s);
    }
    [HttpDelete("{id:int}"), Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await db.Schedules.FindAsync(id); if (s == null) return NotFound();
        if (!await Manage(s.ClassGroupId)) return Forbid();
        db.Schedules.Remove(s); await db.SaveChangesAsync(); return NoContent();
    }
}
