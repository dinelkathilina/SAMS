using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMS.Migrations
{
    /// <inheritdoc />
    public partial class LectureHallEntityadded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LectureHall",
                columns: table => new
                {
                    LectureHallID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    MinLatitude = table.Column<float>(type: "real", nullable: false),
                    MaxLatitude = table.Column<float>(type: "real", nullable: false),
                    MinLongitude = table.Column<float>(type: "real", nullable: false),
                    MaxLongitude = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LectureHall", x => x.LectureHallID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LectureHall");
        }
    }
}
