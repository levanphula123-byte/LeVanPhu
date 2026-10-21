using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeVanPhu.Api.DTOs;
using LeVanPhu.Api.Models;
namespace LeVanPhu.Api.Controllers;

[ApiController, Route("api/attendance"), Authorize]
public class AttendanceController(AppDbContext db) : ControllerBase
{
    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private async Task<bool> Owns(int classId) => User.IsInRole("Admin") || await db.Classes.AnyAsync(x => x.Id == classId && x.LecturerUserId == Uid);
    private static bool Coordinates(double lat, double lon) => double.IsFinite(lat) && double.IsFinite(lon) && lat is >= -90 and <= 90 && lon is >= -180 and <= 180;
    public static double Distance(double lat1, double lon1, double lat2, double lon2)
    {
        var a = Math.Pow(Math.Sin((lat2 - lat1) * Math.PI / 360), 2) + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * Math.Pow(Math.Sin((lon2 - lon1) * Math.PI / 360), 2);
        return 6371000 * 2 * Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));
    }
    [HttpGet("sessions"), Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Sessions() => Ok(await db.AttendanceSessions.Where(x => User.IsInRole("Admin") || x.LecturerUserId == Uid).OrderByDescending(x => x.Id).ToListAsync());
    [HttpPost("sessions"), Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Create(CreateSessionRequest r)
    {
        if (!await Owns(r.ClassGroupId)) return Forbid();
        if (!Coordinates(r.Latitude, r.Longitude) || r.DurationMinutes is < 1 or > 60) return BadRequest("Tọa độ/thời lượng không hợp lệ.");
        var session = new AttendanceSession { ClassGroupId = r.ClassGroupId, LecturerUserId = Uid, Latitude = r.Latitude, Longitude = r.Longitude, ExpiresAtUtc = DateTime.UtcNow.AddMinutes(r.DurationMinutes) };
        db.AttendanceSessions.Add(session);
        await db.SaveChangesAsync();
        return Ok(session);
    }
    [HttpPut("sessions/{id:int}/close"), Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Close(int id)
    {
        var session = await db.AttendanceSessions.FindAsync(id);
        if (session == null) return NotFound();
        if (!await Owns(session.ClassGroupId)) return Forbid();
        session.IsOpen = false;
        await db.SaveChangesAsync();
        return Ok(new { message = "Đã đóng phiên điểm danh." });
    }
    [HttpPost("begin"), Authorize(Roles = "Student")]
    public async Task<IActionResult> Begin(BeginAttendanceRequest r)
    {
        if (!Coordinates(r.Latitude, r.Longitude)) return BadRequest("Tọa độ không hợp lệ.");
        var student = await db.Students.SingleOrDefaultAsync(x => x.UserId == Uid);
        if (student == null) return BadRequest("Chưa liên kết MSSV.");
        if (!await db.FaceProfiles.AnyAsync(x => x.UserId == Uid)) return BadRequest("Hãy đăng ký khuôn mặt trước.");
        var session = await db.AttendanceSessions.SingleOrDefaultAsync(x => x.QrToken == r.QrToken);
        if (session == null || !session.IsOpen || session.ExpiresAtUtc <= DateTime.UtcNow) return BadRequest("QR không hợp lệ hoặc phiên đã đóng/hết hạn.");
        if (!await db.ClassMembers.AnyAsync(x => x.StudentId == student.Id && x.ClassGroupId == session.ClassGroupId)) return Forbid();
        var distance = Distance(r.Latitude, r.Longitude, session.Latitude, session.Longitude);
        if (distance > 150) return BadRequest($"Bạn cách giảng viên {distance:0}m. Giới hạn 150m.");
        if (await db.AttendanceRecords.AnyAsync(x => x.SessionId == session.Id && x.StudentId == student.Id)) return Conflict("Bạn đã điểm danh.");
        var previous = await db.AttendanceChallenges.Where(x => x.StudentId == student.Id && !x.Used).ToListAsync();
        previous.ForEach(x => x.Used = true);
        var challenge = new AttendanceChallenge { SessionId = session.Id, StudentId = student.Id, Latitude = r.Latitude, Longitude = r.Longitude, ExpiresAtUtc = DateTime.UtcNow.AddSeconds(10) };
        db.AttendanceChallenges.Add(challenge);
        await db.SaveChangesAsync();
        return Ok(new { challengeId = challenge.Id, expiresAtUtc = challenge.ExpiresAtUtc, seconds = 10 });
    }
    [HttpPost("mark"), Authorize(Roles = "Student")]
    public async Task<IActionResult> Mark(MarkAttendanceRequest r)
    {
        if (!FaceController.Valid(r.Embedding)) return BadRequest("Dữ liệu khuôn mặt không hợp lệ.");
        var student = await db.Students.SingleOrDefaultAsync(x => x.UserId == Uid);
        if (student == null) return Forbid();
        var challenge = await db.AttendanceChallenges.SingleOrDefaultAsync(x => x.Id == r.ChallengeId && x.StudentId == student.Id);
        if (challenge == null || challenge.Used || challenge.ExpiresAtUtc <= DateTime.UtcNow) return BadRequest("Đã quá 10 giây hoặc lượt quét đã dùng. Hãy quét lại QR.");
        var session = await db.AttendanceSessions.FindAsync(challenge.SessionId);
        if (session == null || !session.IsOpen || session.ExpiresAtUtc <= DateTime.UtcNow) return BadRequest("Phiên đã đóng/hết hạn.");
        if (!await db.ClassMembers.AnyAsync(x => x.StudentId == student.Id && x.ClassGroupId == session.ClassGroupId)) return Forbid();
        var profile = await db.FaceProfiles.SingleOrDefaultAsync(x => x.UserId == Uid);
        if (profile == null) return BadRequest("Chưa đăng ký khuôn mặt.");
        var saved = JsonSerializer.Deserialize<double[]>(profile.EmbeddingJson)!;
        if (!FaceController.Valid(saved)) return Problem(statusCode: 500, title: "Hồ sơ khuôn mặt đã lưu không hợp lệ.");
        var similarity = saved.Zip(r.Embedding).Sum(x => x.First * x.Second) / Math.Sqrt(saved.Sum(x => x * x) * r.Embedding.Sum(x => x * x));
        if (similarity < 0.70) return BadRequest("Khuôn mặt không khớp. Hãy nhìn thẳng camera và thử lại.");
        await using var tx = await db.Database.BeginTransactionAsync();
        var consumed = await db.AttendanceChallenges.Where(x => x.Id == challenge.Id && !x.Used && x.ExpiresAtUtc > DateTime.UtcNow).ExecuteUpdateAsync(s => s.SetProperty(x => x.Used, true));
        if (consumed == 0) return Conflict("Lượt quét đã dùng hoặc hết hạn.");
        if (await db.AttendanceRecords.AnyAsync(x => x.SessionId == session.Id && x.StudentId == student.Id)) return Conflict("Bạn đã điểm danh.");
        db.AttendanceRecords.Add(new AttendanceRecord { SessionId = session.Id, StudentId = student.Id, Latitude = challenge.Latitude, Longitude = challenge.Longitude, Method = "QR+GPS+Face" });
        try { await db.SaveChangesAsync(); await tx.CommitAsync(); }
        catch (DbUpdateException) { return Conflict("Đã có bản ghi điểm danh. Hãy tải lại lịch sử."); }
        return Ok(new { message = "Điểm danh thành công: QR + GPS + khuôn mặt.", similarity = Math.Round(similarity, 3) });
    }
    [HttpGet("sessions/{id:int}/records"), Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Records(int id)
    {
        var session = await db.AttendanceSessions.FindAsync(id);
        if (session == null) return NotFound();
        if (!await Owns(session.ClassGroupId)) return Forbid();
        return Ok(await db.ClassMembers.Where(x => x.ClassGroupId == session.ClassGroupId).Join(db.Students, m => m.StudentId, s => s.Id, (m, s) => s)
            .Select(s => new { studentId = s.Id, s.StudentCode, s.FullName, record = db.AttendanceRecords.FirstOrDefault(a => a.SessionId == id && a.StudentId == s.Id) }).ToListAsync());
    }
    [HttpGet("mine"), Authorize(Roles = "Student")]
    public async Task<IActionResult> Mine() => Ok(await db.AttendanceRecords.Where(x => db.Students.Any(s => s.Id == x.StudentId && s.UserId == Uid)).OrderByDescending(x => x.AttendedAtUtc).ToListAsync());
    [HttpPut("sessions/{id:int}/records/{studentId:int}"), Authorize(Roles = "Admin,Lecturer")]
    public async Task<IActionResult> Correct(int id, int studentId, CorrectAttendanceRequest r)
    {
        var session = await db.AttendanceSessions.FindAsync(id);
        if (session == null) return NotFound();
        if (!await Owns(session.ClassGroupId)) return Forbid();
        if (string.IsNullOrWhiteSpace(r.Reason)) return BadRequest("Cần ghi lý do điều chỉnh.");
        if (!await db.ClassMembers.AnyAsync(x => x.ClassGroupId == session.ClassGroupId && x.StudentId == studentId)) return BadRequest("Sinh viên không thuộc lớp.");
        var record = await db.AttendanceRecords.SingleOrDefaultAsync(x => x.SessionId == id && x.StudentId == studentId);
        if (record == null)
        {
            record = new AttendanceRecord { SessionId = id, StudentId = studentId };
            db.AttendanceRecords.Add(record);
        }
        record.Method = r.Present ? "Manual:Present" : "Manual:Absent";
        record.CorrectionReason = r.Reason.Trim();
        record.CorrectedByUserId = Uid;
        record.CorrectedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(record);
    }
}
