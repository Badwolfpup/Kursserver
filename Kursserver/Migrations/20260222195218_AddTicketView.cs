using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kursserver.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketViews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    LastViewedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketViews_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketViews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketViews_TicketId",
                table: "TicketViews",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketViews_UserId_TicketId",
                table: "TicketViews",
                columns: new[] { "UserId", "TicketId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketViews");
        }
    }
}
