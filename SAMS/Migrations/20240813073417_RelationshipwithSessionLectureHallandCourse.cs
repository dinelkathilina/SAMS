using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMS.Migrations
{
    /// <inheritdoc />
    public partial class RelationshipwithSessionLectureHallandCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Session",
                columns: table => new
                {
                    SessionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseID = table.Column<int>(type: "int", nullable: false),
                    LectureHallID = table.Column<int>(type: "int", nullable: false),
                    SessionCode = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Session", x => x.SessionID);
                    table.ForeignKey(
                        name: "FK_Course_Course",
                        column: x => x.CourseID,
                        principalTable: "Course",
                        principalColumn: "CourseID");
                    table.ForeignKey(
                        name: "FK_Course_LecturerHall",
                        column: x => x.LectureHallID,
                        principalTable: "LectureHall",
                        principalColumn: "LectureHallID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Session_CourseID",
                table: "Session",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_Session_LectureHallID",
                table: "Session",
                column: "LectureHallID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Session");
        }
    }
}
