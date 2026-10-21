using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeVanPhu.Migrations
{
    /// <inheritdoc />
    public partial class ReferentialIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Courses_CourseId",
                table: "Classes");

            migrationBuilder.AlterColumn<string>(
                name: "LecturerUserId",
                table: "Classes",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_ClassGroupId",
                table: "Schedules",
                column: "ClassGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassMembers_StudentId",
                table: "ClassMembers",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_LecturerUserId",
                table: "Classes",
                column: "LecturerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_ClassGroupId",
                table: "AttendanceSessions",
                column: "ClassGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentId",
                table: "AttendanceRecords",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceChallenges_SessionId",
                table: "AttendanceChallenges",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceChallenges_StudentId",
                table: "AttendanceChallenges",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceChallenges_AttendanceSessions_SessionId",
                table: "AttendanceChallenges",
                column: "SessionId",
                principalTable: "AttendanceSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceChallenges_Students_StudentId",
                table: "AttendanceChallenges",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_AttendanceSessions_SessionId",
                table: "AttendanceRecords",
                column: "SessionId",
                principalTable: "AttendanceSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Students_StudentId",
                table: "AttendanceRecords",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceSessions_Classes_ClassGroupId",
                table: "AttendanceSessions",
                column: "ClassGroupId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_AspNetUsers_LecturerUserId",
                table: "Classes",
                column: "LecturerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Courses_CourseId",
                table: "Classes",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassMembers_Classes_ClassGroupId",
                table: "ClassMembers",
                column: "ClassGroupId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassMembers_Students_StudentId",
                table: "ClassMembers",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FaceProfiles_AspNetUsers_UserId",
                table: "FaceProfiles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Lecturers_AspNetUsers_UserId",
                table: "Lecturers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Classes_ClassGroupId",
                table: "Schedules",
                column: "ClassGroupId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_AspNetUsers_UserId",
                table: "Students",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceChallenges_AttendanceSessions_SessionId",
                table: "AttendanceChallenges");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceChallenges_Students_StudentId",
                table: "AttendanceChallenges");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_AttendanceSessions_SessionId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Students_StudentId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceSessions_Classes_ClassGroupId",
                table: "AttendanceSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Classes_AspNetUsers_LecturerUserId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Courses_CourseId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassMembers_Classes_ClassGroupId",
                table: "ClassMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassMembers_Students_StudentId",
                table: "ClassMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_FaceProfiles_AspNetUsers_UserId",
                table: "FaceProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Lecturers_AspNetUsers_UserId",
                table: "Lecturers");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Classes_ClassGroupId",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_AspNetUsers_UserId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_ClassGroupId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_ClassMembers_StudentId",
                table: "ClassMembers");

            migrationBuilder.DropIndex(
                name: "IX_Classes_LecturerUserId",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceSessions_ClassGroupId",
                table: "AttendanceSessions");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_StudentId",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceChallenges_SessionId",
                table: "AttendanceChallenges");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceChallenges_StudentId",
                table: "AttendanceChallenges");

            migrationBuilder.AlterColumn<string>(
                name: "LecturerUserId",
                table: "Classes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Courses_CourseId",
                table: "Classes",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
