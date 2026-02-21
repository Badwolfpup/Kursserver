using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kursserver.Migrations
{
    /// <inheritdoc />
    public partial class NullableAvailabilityId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AdminAvailabilities_AdminAvailabilityId",
                table: "Bookings");

            migrationBuilder.AlterColumn<int>(
                name: "AdminAvailabilityId",
                table: "Bookings",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AdminAvailabilities_AdminAvailabilityId",
                table: "Bookings",
                column: "AdminAvailabilityId",
                principalTable: "AdminAvailabilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AdminAvailabilities_AdminAvailabilityId",
                table: "Bookings");

            migrationBuilder.AlterColumn<int>(
                name: "AdminAvailabilityId",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AdminAvailabilities_AdminAvailabilityId",
                table: "Bookings",
                column: "AdminAvailabilityId",
                principalTable: "AdminAvailabilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
