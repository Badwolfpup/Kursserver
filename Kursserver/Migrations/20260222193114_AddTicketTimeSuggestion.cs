using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kursserver.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketTimeSuggestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketTimeSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    SuggestedById = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeclineReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketTimeSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketTimeSuggestions_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketTimeSuggestions_Users_SuggestedById",
                        column: x => x.SuggestedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketTimeSuggestions_SuggestedById",
                table: "TicketTimeSuggestions",
                column: "SuggestedById");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTimeSuggestions_TicketId",
                table: "TicketTimeSuggestions",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketTimeSuggestions");
        }
    }
}
