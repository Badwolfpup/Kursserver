using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Endpoints
{
    public static class BusyTimeEndpoints
    {
        public static void MapBusyTimeEndpoints(this WebApplication app)
        {
            /// <summary>
            /// SCENARIO: Any authenticated user fetches all busy time blocks
            /// CALLS: useBusyTimes() → getBusyTimes() (kurshemsida)
            /// SIDE EFFECTS: none (read-only)
            /// </summary>
            app.MapGet("/api/busy-time", [Authorize] async (ApplicationDbContext db) =>
            {
                var busyTimes = await db.BusyTimes.ToListAsync();
                return Results.Ok(busyTimes);
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher creates a busy time block. No conflict checks — admin resolves
            ///   any visual overlap with bookings manually (per UX decision).
            /// CALLS: useAddBusyTime() → addBusyTime() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Creates a BusyTime record
            /// </summary>
            app.MapPost("/api/busy-time", [Authorize] async (AddBusyTimeDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;

                var userId = int.Parse(context.User.FindFirst("id")!.Value);
                if (dto.AdminId != userId)
                    return Results.StatusCode(403);

                if (dto.StartTime >= dto.EndTime)
                    return Results.BadRequest("Starttid måste vara före sluttid.");

                var busyTime = new BusyTime
                {
                    AdminId = dto.AdminId,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    Note = dto.Note
                };

                db.BusyTimes.Add(busyTime);
                await db.SaveChangesAsync();

                return Results.Created($"/api/busy-time/{busyTime.Id}", busyTime);
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher updates a busy time block's time or note. No conflict checks.
            /// CALLS: useUpdateBusyTime() → updateBusyTime() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Updates StartTime, EndTime, Note on the BusyTime record
            /// </summary>
            app.MapPut("/api/busy-time/{id}", [Authorize] async (int id, UpdateBusyTimeDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;

                var userId = int.Parse(context.User.FindFirst("id")!.Value);
                var busyTime = await db.BusyTimes.FindAsync(id);
                if (busyTime == null) return Results.NotFound("Busy time not found");
                if (busyTime.AdminId != userId) return Results.StatusCode(403);

                if (dto.StartTime >= dto.EndTime)
                    return Results.BadRequest("Starttid måste vara före sluttid.");

                busyTime.StartTime = dto.StartTime;
                busyTime.EndTime = dto.EndTime;
                busyTime.Note = dto.Note;

                await db.SaveChangesAsync();
                return Results.Ok(busyTime);
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher deletes a busy time block
            /// CALLS: useDeleteBusyTime() → deleteBusyTime() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Removes the BusyTime record
            /// </summary>
            app.MapDelete("/api/busy-time/{id}", [Authorize] async (int id, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;

                var userId = int.Parse(context.User.FindFirst("id")!.Value);
                var busyTime = await db.BusyTimes.FindAsync(id);
                if (busyTime == null) return Results.NotFound("Busy time not found");
                if (busyTime.AdminId != userId) return Results.StatusCode(403);

                db.BusyTimes.Remove(busyTime);
                await db.SaveChangesAsync();

                return Results.Ok();
            });
        }
    }
}
