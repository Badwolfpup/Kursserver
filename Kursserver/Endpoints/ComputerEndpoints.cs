using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Endpoints
{
    public static class ComputerEndpoints
    {
        private static readonly HashSet<string> ValidPeriods = new() { "am", "pm" };

        // Who may be assigned a computer (as owner or per-slot). Students borrow their own
        // machine; staff (admins/teachers) may also borrow and — unlike students — are not
        // limited to a single computer (that "one per student" rule lives in the frontend).
        private static readonly HashSet<Role> BorrowerRoles = new() { Role.Student, Role.Teacher, Role.Admin };

        public static void MapComputerEndpoints(this WebApplication app)
        {
            /// <summary>
            /// SCENARIO: Admin/Teacher fetches all computers
            /// CALLS: useComputers() → getComputers() (kurshemsida)
            /// SIDE EFFECTS: none (read-only)
            /// </summary>
            app.MapGet("/api/computers", [Authorize] async (ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;
                var computers = await db.Computers.OrderBy(c => c.Number).ToListAsync();
                return Results.Ok(computers);
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher fetches all shared-computer day/period assignments
            /// CALLS: useComputerAssignments() → getComputerAssignments() (kurshemsida)
            /// SIDE EFFECTS: none (read-only)
            /// </summary>
            app.MapGet("/api/computer-assignments", [Authorize] async (ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;
                var assignments = await db.ComputerAssignments.ToListAsync();
                return Results.Ok(assignments);
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher adds a computer to the pool (by its id number)
            /// CALLS: useAddComputer() → addComputer() (kurshemsida)
            /// SIDE EFFECTS: creates a Computer record (Number must be unique)
            /// </summary>
            app.MapPost("/api/computers", [Authorize] async (AddComputerDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;
                if (await db.Computers.AnyAsync(c => c.Number == dto.Number))
                    return Results.Conflict("En dator med det numret finns redan");
                var computer = new Computer { Number = dto.Number };
                db.Computers.Add(computer);
                await db.SaveChangesAsync();
                return Results.Ok(computer);
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher removes a computer from the pool
            /// CALLS: useRemoveComputer() → removeComputer() (kurshemsida)
            /// SIDE EFFECTS: deletes the Computer (cascades its ComputerAssignment slots)
            /// </summary>
            app.MapDelete("/api/computers/{id}", [Authorize] async (int id, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;
                var computer = await db.Computers.FindAsync(id);
                if (computer == null) return Results.NotFound();
                db.Computers.Remove(computer);
                await db.SaveChangesAsync();
                return Results.Ok();
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher dedicates a whole computer to one borrower — a student or a
            ///           staff member (admin/teacher) — with optional take-home, or clears the
            ///           owner to make it shared again
            /// CALLS: useSetComputerOwner() → setComputerOwner() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Sets/clears Computer.OwnerStudentId and TakesHome
            ///   - When an owner is set, removes that computer's shared slot assignments
            /// </summary>
            app.MapPut("/api/computers/owner", [Authorize] async (SetComputerOwnerDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;
                var computer = await db.Computers.FindAsync(dto.ComputerId);
                if (computer == null) return Results.NotFound();

                if (dto.StudentId.HasValue)
                {
                    var user = await db.Users.FindAsync(dto.StudentId.Value);
                    if (user == null || !BorrowerRoles.Contains(user.AuthLevel))
                        return Results.BadRequest("Invalid borrower");
                    computer.OwnerStudentId = dto.StudentId.Value;
                    computer.TakesHome = dto.TakesHome;
                    // A dedicated computer is no longer shared per slot.
                    var slots = db.ComputerAssignments.Where(a => a.ComputerId == computer.Id);
                    db.ComputerAssignments.RemoveRange(slots);
                }
                else
                {
                    computer.OwnerStudentId = null;
                    computer.TakesHome = false;
                }

                await db.SaveChangesAsync();
                return Results.Ok(computer);
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher assigns a borrower (student or staff member) to a shared
            ///           computer for a day/period (upsert)
            /// CALLS: useAssignComputerSlot() → assignComputerSlot() (kurshemsida)
            /// SIDE EFFECTS: creates or updates a ComputerAssignment for the computer/day/period
            /// </summary>
            app.MapPut("/api/computer-assignments/assign", [Authorize] async (AssignComputerDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;
                if (!ValidPeriods.Contains(dto.Period))
                    return Results.BadRequest("Period must be 'am' or 'pm'");
                if (dto.DayOfWeek < 1 || dto.DayOfWeek > 4)
                    return Results.BadRequest("DayOfWeek must be 1-4");

                var user = await db.Users.FindAsync(dto.StudentId);
                if (user == null || !BorrowerRoles.Contains(user.AuthLevel))
                    return Results.BadRequest("Invalid borrower");

                try
                {
                    var existing = await db.ComputerAssignments.FirstOrDefaultAsync(a =>
                        a.ComputerId == dto.ComputerId && a.DayOfWeek == dto.DayOfWeek && a.Period == dto.Period);
                    if (existing != null)
                    {
                        existing.StudentId = dto.StudentId;
                    }
                    else
                    {
                        db.ComputerAssignments.Add(new ComputerAssignment
                        {
                            ComputerId = dto.ComputerId,
                            DayOfWeek = dto.DayOfWeek,
                            Period = dto.Period,
                            StudentId = dto.StudentId,
                        });
                    }
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (DbUpdateException)
                {
                    return Results.Conflict("Datorplatsen är redan tilldelad");
                }
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher clears a shared computer's day/period slot
            /// CALLS: useClearComputerSlot() → clearComputerSlot() (kurshemsida)
            /// SIDE EFFECTS: removes the matching ComputerAssignment record
            /// </summary>
            app.MapDelete("/api/computer-assignments/clear", [Authorize] async (int computerId, int dayOfWeek, string period, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;
                var existing = await db.ComputerAssignments.FirstOrDefaultAsync(a =>
                    a.ComputerId == computerId && a.DayOfWeek == dayOfWeek && a.Period == period);
                if (existing != null)
                {
                    db.ComputerAssignments.Remove(existing);
                    await db.SaveChangesAsync();
                }
                return Results.Ok();
            });
        }
    }
}
