namespace LeVanPhu.Api.Models;

public class Student
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string StudentCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Faculty { get; set; }
}

public class Lecturer
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public string LecturerCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
}

public class Course
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Credits { get; set; }
}

public class ClassGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int CourseId { get; set; }
    public string LecturerUserId { get; set; } = "";
    public string Room { get; set; } = "";
    public Course? Course { get; set; }
}

public class ClassMember
{
    public int Id { get; set; }
    public int ClassGroupId { get; set; }
    public int StudentId { get; set; }
}

public class AttendanceSession
{
    public int Id { get; set; }
    public int ClassGroupId { get; set; }
    public string LecturerUserId { get; set; } = "";
    public string QrToken { get; set; } = Guid.NewGuid().ToString("N");
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsOpen { get; set; } = true;
}

public class AttendanceRecord
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int StudentId { get; set; }
    public DateTime AttendedAtUtc { get; set; } = DateTime.UtcNow;
    public string Method { get; set; } = "QR+GPS+Face";
    public string? CorrectionReason { get; set; }
    public string? CorrectedByUserId { get; set; }
    public DateTime? CorrectedAtUtc { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class FaceProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public string EmbeddingJson { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AttendanceChallenge
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int SessionId { get; set; }
    public int StudentId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool Used { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class Schedule
{
    public int Id { get; set; }
    public int ClassGroupId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public string Room { get; set; } = "";
}


