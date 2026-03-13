using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kursserver.Migrations
{
    /// <inheritdoc />
    public partial class AddClassroomToRecurringEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Classroom",
                table: "RecurringEvents",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Classroom",
                table: "RecurringEvents");
        }
    }
}
