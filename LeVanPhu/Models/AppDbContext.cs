using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace LeVanPhu.Api.Models;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Lecturer> Lecturers => Set<Lecturer>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<ClassGroup> Classes => Set<ClassGroup>();
    public DbSet<ClassMember> ClassMembers => Set<ClassMember>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<FaceProfile> FaceProfiles => Set<FaceProfile>();
    public DbSet<AttendanceChallenge> AttendanceChallenges => Set<AttendanceChallenge>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Student>().HasIndex(x => x.StudentCode).IsUnique();
        b.Entity<Student>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Student>().HasIndex(x => x.UserId).IsUnique();
        b.Entity<Lecturer>().HasIndex(x => x.UserId).IsUnique();
        b.Entity<Lecturer>().HasIndex(x => x.LecturerCode).IsUnique();
        b.Entity<Course>().HasIndex(x => x.Code).IsUnique();
        b.Entity<FaceProfile>().HasIndex(x => x.UserId).IsUnique();
        b.Entity<AttendanceSession>().HasIndex(x => x.QrToken).IsUnique();
        b.Entity<ClassMember>().HasIndex(x => new { x.ClassGroupId, x.StudentId }).IsUnique();
        b.Entity<AttendanceRecord>().HasIndex(x => new { x.SessionId, x.StudentId }).IsUnique();
        // Preserve attendance history: reject deletion of referenced parent records.
        b.Entity<Student>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Lecturer>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<FaceProfile>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ClassGroup>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.LecturerUserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ClassGroup>().HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ClassMember>().HasOne<ClassGroup>().WithMany().HasForeignKey(x => x.ClassGroupId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ClassMember>().HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Schedule>().HasOne<ClassGroup>().WithMany().HasForeignKey(x => x.ClassGroupId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AttendanceSession>().HasOne<ClassGroup>().WithMany().HasForeignKey(x => x.ClassGroupId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AttendanceRecord>().HasOne<AttendanceSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AttendanceRecord>().HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AttendanceChallenge>().HasOne<AttendanceSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AttendanceChallenge>().HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
    }
}
