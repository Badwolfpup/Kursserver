using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kursserver.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoryDetailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssetsNeeded",
                table: "ProjectHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BonusChallenges",
                table: "ProjectHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DesignSpecs",
                table: "ProjectHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LearningGoals",
                table: "ProjectHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StarterHtml",
                table: "ProjectHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserStories",
                table: "ProjectHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Assumptions",
                table: "ExerciseHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Example",
                table: "ExerciseHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FunctionSignature",
                table: "ExerciseHistories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssetsNeeded",
                table: "ProjectHistories");

            migrationBuilder.DropColumn(
                name: "BonusChallenges",
                table: "ProjectHistories");

            migrationBuilder.DropColumn(
                name: "DesignSpecs",
                table: "ProjectHistories");

            migrationBuilder.DropColumn(
                name: "LearningGoals",
                table: "ProjectHistories");

            migrationBuilder.DropColumn(
                name: "StarterHtml",
                table: "ProjectHistories");

            migrationBuilder.DropColumn(
                name: "UserStories",
                table: "ProjectHistories");

            migrationBuilder.DropColumn(
                name: "Assumptions",
                table: "ExerciseHistories");

            migrationBuilder.DropColumn(
                name: "Example",
                table: "ExerciseHistories");

            migrationBuilder.DropColumn(
                name: "FunctionSignature",
                table: "ExerciseHistories");
        }
    }
}
