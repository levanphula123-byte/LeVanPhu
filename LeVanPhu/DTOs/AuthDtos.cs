namespace LeVanPhu.Api.DTOs;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string StudentCode
);

public record VerifyOtpRequest(
    string Email,
    string Otp
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string Token,
    string Role,
    string FullName,
    string Email
);

public record CreateSessionRequest(
    int ClassGroupId,
    double Latitude,
    double Longitude,
    int DurationMinutes = 10
);

public record MarkAttendanceRequest(
    string ChallengeId,
    double[] Embedding
);

public record CourseRequest(
    string Code,
    string Name,
    int Credits
);

public record ClassRequest(
    string Name,
    int CourseId,
    string Room,
    string? LecturerUserId = null
);

public record StudentRequest(
    string? UserId,
    string StudentCode,
    string FullName,
    string Email,
    string? Faculty
);

public record BeginAttendanceRequest(string QrToken, double Latitude, double Longitude);
public record FaceRequest(double[] Embedding);
public record AccountRequest(string Email, string FullName, string Role, string Password);
public record AccountUpdateRequest(string FullName, string Email);
public record ScheduleRequest(int ClassGroupId, DateTime StartsAtUtc, DateTime EndsAtUtc, string Room);
public record CorrectAttendanceRequest(bool Present, string Reason);
