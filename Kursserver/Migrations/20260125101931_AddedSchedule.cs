using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kursserver.Migrations
{
    /// <inheritdoc />
    public partial class AddedSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ScheduledMonAm",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ScheduledMonPm",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ScheduledThuAm",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ScheduledThuPm",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ScheduledTueAm",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ScheduledTuePm",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ScheduledWedAm",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ScheduledWedPm",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledMonAm",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ScheduledMonPm",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ScheduledThuAm",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ScheduledThuPm",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ScheduledTueAm",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ScheduledTuePm",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ScheduledWedAm",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ScheduledWedPm",
                table: "Users");
        }
    }
}
