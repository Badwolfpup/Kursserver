using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kursserver.Migrations
{
    /// <inheritdoc />
    public partial class AddComputers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Computers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OwnerStudentId = table.Column<int>(type: "int", nullable: true),
                    TakesHome = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Computers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Computers_Users_OwnerStudentId",
                        column: x => x.OwnerStudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ComputerAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComputerId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComputerAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComputerAssignments_Computers_ComputerId",
                        column: x => x.ComputerId,
                        principalTable: "Computers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComputerAssignments_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComputerAssignments_ComputerId_DayOfWeek_Period",
                table: "ComputerAssignments",
                columns: new[] { "ComputerId", "DayOfWeek", "Period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComputerAssignments_StudentId",
                table: "ComputerAssignments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Computers_Number",
                table: "Computers",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Computers_OwnerStudentId",
                table: "Computers",
                column: "OwnerStudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComputerAssignments");

            migrationBuilder.DropTable(
                name: "Computers");
        }
    }
}
