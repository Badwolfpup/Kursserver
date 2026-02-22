using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kursserver.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Asserts",
                table: "ExerciseHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeedbackComment",
                table: "ExerciseHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeedbackReason",
                table: "ExerciseHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "ExerciseHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPositive",
                table: "ExerciseHistories",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Solution",
                table: "ExerciseHistories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Asserts",
                table: "ExerciseHistories");

            migrationBuilder.DropColumn(
                name: "FeedbackComment",
                table: "ExerciseHistories");

            migrationBuilder.DropColumn(
                name: "FeedbackReason",
                table: "ExerciseHistories");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "ExerciseHistories");

            migrationBuilder.DropColumn(
                name: "IsPositive",
                table: "ExerciseHistories");

            migrationBuilder.DropColumn(
                name: "Solution",
                table: "ExerciseHistories");
        }
    }
}
