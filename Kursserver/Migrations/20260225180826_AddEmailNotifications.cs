using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kursserver.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailNotifications",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("UPDATE Users SET EmailNotifications = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailNotifications",
                table: "Users");
        }
    }
}
