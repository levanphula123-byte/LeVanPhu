using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeVanPhu.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CorrectedAtUtc",
                table: "AttendanceRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectedByUserId",
                table: "AttendanceRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectionReason",
                table: "AttendanceRecords",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrectedAtUtc",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CorrectedByUserId",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CorrectionReason",
                table: "AttendanceRecords");
        }
    }
}
