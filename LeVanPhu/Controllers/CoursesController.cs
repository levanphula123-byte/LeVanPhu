using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeVanPhu.Api.DTOs;
using LeVanPhu.Api.Models;
namespace LeVanPhu.Api.Controllers;

[ApiController, Route("api/courses"), Authorize]
public class CoursesController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await db.Courses.AsNoTracking().OrderBy(x => x.Code).ToListAsync());
    private async Task<bool> Valid(CourseRequest r, int id = 0) => !string.IsNullOrWhiteSpace(r.Code) && !string.IsNullOrWhiteSpace(r.Name) && r.Credits is >= 1 and <= 20 && !await db.Courses.AnyAsync(x => x.Id != id && x.Code == r.Code.Trim());
    [HttpPost, Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Create(CourseRequest r)
    {
        if (!await Valid(r)) return BadRequest("Mã môn phải duy nhất; tên bắt buộc; tín chỉ từ 1 đến 20.");
        var c = new Course { Code = r.Code.Trim(), Name = r.Name.Trim(), Credits = r.Credits }; db.Courses.Add(c); await db.SaveChangesAsync(); return Ok(c);
    }
    [HttpPut("{id:int}"), Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Update(int id, CourseRequest r)
    {
        var c = await db.Courses.FindAsync(id); if (c == null) return NotFound();
        if (!await Valid(r, id)) return BadRequest("Mã môn phải duy nhất; tên bắt buộc; tín chỉ từ 1 đến 20.");
        c.Code = r.Code.Trim(); c.Name = r.Name.Trim(); c.Credits = r.Credits; await db.SaveChangesAsync(); return Ok(c);
    }
    [HttpDelete("{id:int}"), Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Courses.FindAsync(id); if (c == null) return NotFound();
        if (await db.Classes.AnyAsync(x => x.CourseId == id)) return Conflict("Môn học đang được sử dụng trong lớp.");
        db.Courses.Remove(c); await db.SaveChangesAsync(); return NoContent();
    }
}
