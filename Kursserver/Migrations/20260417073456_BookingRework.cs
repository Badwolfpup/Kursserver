using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kursserver.Migrations
{
    /// <inheritdoc />
    public partial class BookingRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- Step 1: Make RescheduledBy nullable FIRST so the backfill below can set NULLs ---
            migrationBuilder.AlterColumn<string>(
                name: "RescheduledBy",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // --- Step 2: Data backfill: normalize string values to match new enum names (PascalCase) ---

            // Booking.Status: pending/accepted/declined/rescheduled → Pending/Accepted/Declined
            // Rescheduled collapses into Pending (reschedules now require fresh approval)
            migrationBuilder.Sql(@"
                UPDATE Bookings SET Status = CASE LOWER(Status)
                    WHEN 'pending' THEN 'Pending'
                    WHEN 'accepted' THEN 'Accepted'
                    WHEN 'declined' THEN 'Declined'
                    WHEN 'rescheduled' THEN 'Pending'
                    ELSE 'Pending'
                END;");

            // Booking.MeetingType: intro/followup/session/other → Intro/Followup/Other (session dropped)
            migrationBuilder.Sql(@"
                UPDATE Bookings SET MeetingType = CASE LOWER(MeetingType)
                    WHEN 'intro' THEN 'Intro'
                    WHEN 'followup' THEN 'Followup'
                    WHEN 'session' THEN 'Other'
                    WHEN 'other' THEN 'Other'
                    ELSE 'Other'
                END;");

            // Booking.CreatedByRole: Admin/Teacher/Coach/Student → Admin/Coach (Teacher/Student map to Admin)
            migrationBuilder.Sql(@"
                UPDATE Bookings SET CreatedByRole = CASE LOWER(CreatedByRole)
                    WHEN 'coach' THEN 'Coach'
                    ELSE 'Admin'
                END;");

            // Booking.RescheduledBy: admin/coach/empty → Admin/Coach/NULL (column is now nullable)
            migrationBuilder.Sql(@"
                UPDATE Bookings SET RescheduledBy = CASE LOWER(RescheduledBy)
                    WHEN 'admin' THEN 'Admin'
                    WHEN 'teacher' THEN 'Admin'
                    WHEN 'coach' THEN 'Coach'
                    ELSE NULL
                END;");

            // RecurringEvent.Frequency: weekly/biweekly → Weekly/Biweekly
            migrationBuilder.Sql(@"
                UPDATE RecurringEvents SET Frequency = CASE LOWER(Frequency)
                    WHEN 'biweekly' THEN 'Biweekly'
                    ELSE 'Weekly'
                END;");

            // --- Step 3: Schema drops ---

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AdminAvailabilities_AdminAvailabilityId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_AdminAvailabilityId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AdminAvailabilityId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsBooked",
                table: "AdminAvailabilities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RescheduledBy",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdminAvailabilityId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBooked",
                table: "AdminAvailabilities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_AdminAvailabilityId",
                table: "Bookings",
                column: "AdminAvailabilityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AdminAvailabilities_AdminAvailabilityId",
                table: "Bookings",
                column: "AdminAvailabilityId",
                principalTable: "AdminAvailabilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
