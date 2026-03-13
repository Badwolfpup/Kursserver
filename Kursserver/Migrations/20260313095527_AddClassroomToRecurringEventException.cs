using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kursserver.Migrations
{
    /// <inheritdoc />
    public partial class AddClassroomToRecurringEventException : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Classroom",
                table: "RecurringEventExceptions",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Classroom",
                table: "RecurringEventExceptions");
        }
    }
}
