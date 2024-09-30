using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMS.Migrations
{
    /// <inheritdoc />
    public partial class ClaudeCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Course_Session",
                table: "Attendance");

            migrationBuilder.DropForeignKey(
                name: "FK_User_userID",
                table: "Attendance");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_Session",
                table: "Attendance",
                column: "SessionID",
                principalTable: "Session",
                principalColumn: "SessionID");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_User",
                table: "Attendance",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_Session",
                table: "Attendance");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_User",
                table: "Attendance");

            migrationBuilder.AddForeignKey(
                name: "FK_Course_Session",
                table: "Attendance",
                column: "SessionID",
                principalTable: "Session",
                principalColumn: "SessionID");

            migrationBuilder.AddForeignKey(
                name: "FK_User_userID",
                table: "Attendance",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
