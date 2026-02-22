using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kursserver.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeedbackComment",
                table: "ProjectHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeedbackReason",
                table: "ProjectHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "ProjectHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPositive",
                table: "ProjectHistories",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SolutionCss",
                table: "ProjectHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SolutionHtml",
                table: "ProjectHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SolutionJs",
                table: "ProjectHistories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeedbackComment",
                table: "ProjectHistories");

            migrationBuilder.DropColumn(
                name: "FeedbackReason",
                table: "ProjectHistories");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "ProjectHistories");

            migrationBuilder.DropColumn(
                name: "IsPositive",
                table: "ProjectHistories");

            migrationBuilder.DropColumn(
                name: "SolutionCss",
                table: "ProjectHistories");

            migrationBuilder.DropColumn(
                name: "SolutionHtml",
                table: "ProjectHistories");

            migrationBuilder.DropColumn(
                name: "SolutionJs",
                table: "ProjectHistories");
        }
    }
}
