using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMS.Migrations
{
    /// <inheritdoc />
    public partial class cHANGEsTUDENTANDlECTUREIDSNAMEANDUserandAttendanceRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Student",
                newName: "StudentID");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Lecturer",
                newName: "LecturerID");

            migrationBuilder.CreateTable(
                name: "Attendance",
                columns: table => new
                {
                    AttendanceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendance", x => x.AttendanceID);
                    table.ForeignKey(
                        name: "FK_Course_Session",
                        column: x => x.SessionID,
                        principalTable: "Session",
                        principalColumn: "SessionID");
                    table.ForeignKey(
                        name: "FK_User_userID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_SessionID",
                table: "Attendance",
                column: "SessionID");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_UserID",
                table: "Attendance",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendance");

            migrationBuilder.RenameColumn(
                name: "StudentID",
                table: "Student",
                newName: "UserID");

            migrationBuilder.RenameColumn(
                name: "LecturerID",
                table: "Lecturer",
                newName: "UserID");
        }
    }
}
