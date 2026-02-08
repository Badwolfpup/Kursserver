using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kursserver.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedProjectHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectHistories_UserId_Topic_TechStack",
                table: "ProjectHistories");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "ProjectHistories");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectHistories_UserId_TechStack",
                table: "ProjectHistories",
                columns: new[] { "UserId", "TechStack" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectHistories_UserId_TechStack",
                table: "ProjectHistories");

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "ProjectHistories",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectHistories_UserId_Topic_TechStack",
                table: "ProjectHistories",
                columns: new[] { "UserId", "Topic", "TechStack" });
        }
    }
}
