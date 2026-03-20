using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Endpoints
{
    public static class SeatingEndpoints
    {
        public static void MapSeatingEndpoints(this WebApplication app)
        {
            /// <summary>
            /// SCENARIO: Admin/Teacher fetches all seating assignments for a classroom and day
            /// CALLS: useSeatingAssignments() → getSeatingAssignments() (kurshemsida)
            /// SIDE EFFECTS: none (read-only)
            /// </summary>
            app.MapGet("/api/seating", [Authorize] async (int classroomId, int dayOfWeek, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;

                var assignments = await db.SeatingAssignments
                    .Where(s => s.ClassroomId == classroomId && s.DayOfWeek == dayOfWeek)
                    .ToListAsync();

                return Results.Ok(assignments);
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher assigns a student to a seat (upsert — creates or updates)
            /// CALLS: useAssignSeat() → assignSeat() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Creates or updates SeatingAssignment record for the given classroom/day/period/row/column
            /// </summary>
            app.MapPut("/api/seating/assign", [Authorize] async (AssignSeatDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;

                var existing = await db.SeatingAssignments
                    .FirstOrDefaultAsync(s =>
                        s.ClassroomId == dto.ClassroomId &&
                        s.DayOfWeek == dto.DayOfWeek &&
                        s.Period == dto.Period &&
                        s.Row == dto.Row &&
                        s.Column == dto.Column);

                if (existing != null)
                {
                    existing.StudentId = dto.StudentId;
                }
                else
                {
                    db.SeatingAssignments.Add(new SeatingAssignment
                    {
                        ClassroomId = dto.ClassroomId,
                        DayOfWeek = dto.DayOfWeek,
                        Period = dto.Period,
                        Row = dto.Row,
                        Column = dto.Column,
                        StudentId = dto.StudentId,
                    });
                }

                await db.SaveChangesAsync();
                return Results.Ok();
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher removes a student from a seat
            /// CALLS: useClearSeat() → clearSeat() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Removes SeatingAssignment record for the given classroom/day/period/row/column
            /// </summary>
            app.MapDelete("/api/seating/clear", [Authorize] async (int classroomId, int dayOfWeek, string period, int row, int column, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;

                var existing = await db.SeatingAssignments
                    .FirstOrDefaultAsync(s =>
                        s.ClassroomId == classroomId &&
                        s.DayOfWeek == dayOfWeek &&
                        s.Period == period &&
                        s.Row == row &&
                        s.Column == column);

                if (existing != null)
                {
                    db.SeatingAssignments.Remove(existing);
                    await db.SaveChangesAsync();
                }

                return Results.Ok();
            });
        }
    }
}
