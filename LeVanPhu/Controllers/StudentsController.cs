using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeVanPhu.Api.DTOs;
using LeVanPhu.Api.Models;
namespace LeVanPhu.Api.Controllers;

[ApiController, Route("api/students"), Authorize(Roles = "Admin,Collaborator,Lecturer")]
public class StudentsController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.Students.AsNoTracking().OrderBy(x => x.StudentCode).ToListAsync());
    private async Task<string?> Validate(StudentRequest r, int id = 0)
    {
        if (string.IsNullOrWhiteSpace(r.StudentCode) || string.IsNullOrWhiteSpace(r.FullName) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(r.Email)) return "MSSV, họ tên, email hợp lệ là bắt buộc.";
        if (await db.Students.AnyAsync(x => x.Id != id && (x.StudentCode == r.StudentCode.Trim() || x.Email == r.Email.Trim().ToLowerInvariant()))) return "MSSV hoặc email đã tồn tại.";
        return null;
    }
    [HttpPost]
    public async Task<IActionResult> Create(StudentRequest r)
    {
        var error = await Validate(r); if (error != null) return BadRequest(error);
        var student = new Student { StudentCode = r.StudentCode.Trim(), FullName = r.FullName.Trim(), Email = r.Email.Trim().ToLowerInvariant(), Faculty = r.Faculty };
        db.Students.Add(student); await db.SaveChangesAsync(); return Ok(student);
    }
    [HttpPut("{id:int}"), Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Update(int id, StudentRequest r)
    {
        var student = await db.Students.FindAsync(id); if (student == null) return NotFound();
        var error = await Validate(r, id); if (error != null) return BadRequest(error);
        if (student.UserId != null && (student.Email != r.Email.Trim().ToLowerInvariant() || student.StudentCode != r.StudentCode.Trim())) return BadRequest("Không đổi MSSV/email sau khi liên kết tài khoản.");
        student.StudentCode = r.StudentCode.Trim(); student.FullName = r.FullName.Trim(); student.Email = r.Email.Trim().ToLowerInvariant(); student.Faculty = r.Faculty;
        await db.SaveChangesAsync(); return Ok(student);
    }
    [HttpDelete("{id:int}"), Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await db.Students.FindAsync(id); if (student == null) return NotFound();
        if (student.UserId != null || await db.ClassMembers.AnyAsync(x => x.StudentId == id) || await db.AttendanceRecords.AnyAsync(x => x.StudentId == id)) return Conflict("Chỉ xóa hồ sơ chưa liên kết và chưa có lớp/lịch sử điểm danh.");
        db.Students.Remove(student); await db.SaveChangesAsync(); return NoContent();
    }
}
