using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeVanPhu.Api.DTOs;
using LeVanPhu.Api.Models;
namespace LeVanPhu.Api.Controllers;

[ApiController, Route("api/classes"), Authorize]
public class ClassesController(AppDbContext db) : ControllerBase
{
    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private async Task<bool> Manage(int id) => User.IsInRole("Admin") || User.IsInRole("Lecturer") && await db.Classes.AnyAsync(x => x.Id == id && x.LecturerUserId == Uid);
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var query = db.Classes.AsNoTracking().Include(x => x.Course).AsQueryable();
        if (User.IsInRole("Lecturer")) query = query.Where(x => x.LecturerUserId == Uid);
        if (User.IsInRole("Student")) query = query.Where(x => db.ClassMembers.Any(m => m.ClassGroupId == x.Id && db.Students.Any(s => s.Id == m.StudentId && s.UserId == Uid)));
        if (User.IsInRole("Collaborator")) query = query.Where(x => db.Users.Any(u => u.Id == Uid && u.CreatedByUserId == x.LecturerUserId));
        return Ok(await query.ToListAsync());
    }
    [HttpPost, Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Create(ClassRequest r)
    {
        var owner = User.IsInRole("Admin") ? r.LecturerUserId : Uid;
        if (!await Valid(r, owner)) return BadRequest("Tên lớp, môn học hoặc giảng viên không hợp lệ.");
        var c = new ClassGroup { Name = r.Name.Trim(), CourseId = r.CourseId, Room = r.Room, LecturerUserId = owner! };
        db.Classes.Add(c); await db.SaveChangesAsync(); return Ok(c);
    }
    private async Task<bool> Valid(ClassRequest r, string? owner) => !string.IsNullOrWhiteSpace(r.Name) && await db.Courses.AnyAsync(x => x.Id == r.CourseId) && await db.Lecturers.AnyAsync(x => x.UserId == owner);
    [HttpPut("{id:int}"), Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Update(int id, ClassRequest r)
    {
        if (!await Manage(id)) return Forbid();
        var c = await db.Classes.FindAsync(id); if (c == null) return NotFound();
        var owner = User.IsInRole("Admin") ? r.LecturerUserId : Uid;
        if (!await Valid(r, owner)) return BadRequest("Tên lớp, môn học hoặc giảng viên không hợp lệ.");
        c.Name = r.Name.Trim(); c.CourseId = r.CourseId; c.Room = r.Room; c.LecturerUserId = owner!;
        await db.SaveChangesAsync(); return Ok(c);
    }
    [HttpGet("{id:int}/students"), Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Members(int id)
    {
        if (!await Manage(id)) return Forbid();
        return Ok(await db.ClassMembers.Where(x => x.ClassGroupId == id).Join(db.Students, x => x.StudentId, s => s.Id, (x, s) => s).ToListAsync());
    }
    [HttpPost("{classId:int}/students/{studentId:int}"), Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Add(int classId, int studentId)
    {
        if (!await Manage(classId)) return Forbid();
        if (!await db.Students.AnyAsync(x => x.Id == studentId)) return NotFound();
        if (!await db.ClassMembers.AnyAsync(x => x.ClassGroupId == classId && x.StudentId == studentId))
        { db.ClassMembers.Add(new ClassMember { ClassGroupId = classId, StudentId = studentId }); await db.SaveChangesAsync(); }
        return Ok(new { message = "Đã thêm sinh viên vào lớp." });
    }
    [HttpDelete("{classId:int}/students/{studentId:int}"), Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Remove(int classId, int studentId)
    {
        if (!await Manage(classId)) return Forbid();
        if (await db.AttendanceRecords.AnyAsync(x => x.StudentId == studentId && db.AttendanceSessions.Any(s => s.Id == x.SessionId && s.ClassGroupId == classId))) return Conflict("Sinh viên đã có lịch sử điểm danh trong lớp.");
        await db.ClassMembers.Where(x => x.ClassGroupId == classId && x.StudentId == studentId).ExecuteDeleteAsync(); return NoContent();
    }
    [HttpDelete("{id:int}"), Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await Manage(id)) return Forbid();
        if (await db.AttendanceSessions.AnyAsync(x => x.ClassGroupId == id)) return Conflict("Lớp đã có phiên điểm danh.");
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.ClassMembers.Where(x => x.ClassGroupId == id).ExecuteDeleteAsync();
        await db.Schedules.Where(x => x.ClassGroupId == id).ExecuteDeleteAsync();
        await db.Classes.Where(x => x.Id == id).ExecuteDeleteAsync();
        await tx.CommitAsync(); return NoContent();
    }
}
