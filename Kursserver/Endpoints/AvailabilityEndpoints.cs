using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Endpoints
{
    public static class AvailabilityEndpoints
    {
        public static void MapAvailabilityEndpoints(this WebApplication app)
        {
            /// <summary>
            /// SCENARIO: Any authenticated user fetches availability overlays (decorative calendar rectangles).
            /// CALLS: useAvailabilities() → availabilityService.getAll() (kurshemsida)
            /// SIDE EFFECTS: none (read-only)
            /// </summary>
            app.MapGet("/api/availability", [Authorize] async (ApplicationDbContext db) =>
            {
                var availabilities = await db.AdminAvailabilities.ToListAsync();
                return Results.Ok(availabilities);
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher adds an availability overlay (decorative rectangle on the calendar).
            /// CALLS: useAddAvailability() → availabilityService.add() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Creates a new AdminAvailability record with no interaction with bookings or busy times
            /// </summary>
            app.MapPost("/api/availability", [Authorize] async (AddAvailabilityDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;

                // A staff member may only create availability for themselves.
                var userId = int.Parse(context.User.FindFirst("id")!.Value);
                if (dto.AdminId != userId)
                    return Results.StatusCode(403);

                if (dto.StartTime >= dto.EndTime)
                    return Results.BadRequest("Starttid måste vara före sluttid.");

                var availability = new AdminAvailability
                {
                    AdminId = dto.AdminId,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime
                };

                db.AdminAvailabilities.Add(availability);
                await db.SaveChangesAsync();

                return Results.Created($"/api/availability/{availability.Id}", availability);
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher updates an availability overlay's time window.
            /// CALLS: useUpdateAvailability() → availabilityService.update() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Updates StartTime, EndTime on the AdminAvailability record
            /// </summary>
            app.MapPut("/api/availability/{id}", [Authorize] async (int id, UpdateAvailabilityDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;

                var availability = await db.AdminAvailabilities.FindAsync(id);
                if (availability == null) return Results.NotFound("Availability not found");

                // A staff member may only modify their own availability.
                var userId = int.Parse(context.User.FindFirst("id")!.Value);
                if (availability.AdminId != userId)
                    return Results.StatusCode(403);

                if (dto.StartTime >= dto.EndTime)
                    return Results.BadRequest("Starttid måste vara före sluttid.");

                availability.StartTime = dto.StartTime;
                availability.EndTime = dto.EndTime;

                await db.SaveChangesAsync();
                return Results.Ok(availability);
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher deletes an availability overlay. Bookings are independent of overlays.
            /// CALLS: useDeleteAvailability() → availabilityService.delete() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Removes the AdminAvailability record
            /// </summary>
            app.MapDelete("/api/availability/{id}", [Authorize] async (int id, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;

                var availability = await db.AdminAvailabilities.FindAsync(id);
                if (availability == null) return Results.NotFound("Availability not found");

                // A staff member may only modify their own availability.
                var userId = int.Parse(context.User.FindFirst("id")!.Value);
                if (availability.AdminId != userId)
                    return Results.StatusCode(403);

                db.AdminAvailabilities.Remove(availability);
                await db.SaveChangesAsync();

                return Results.Ok();
            });
        }
    }
}
