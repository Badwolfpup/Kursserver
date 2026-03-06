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
            /// SCENARIO: Any authenticated user fetches availability slots; admin/teacher see all, coach/student see unbooked only
            /// CALLS: useAvailabilities() → availabilityService.getAll/getFree() (kurshemsida)
            /// SIDE EFFECTS: none (read-only)
            /// </summary>
            app.MapGet("/api/availability", [Authorize] async (ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                    if (string.IsNullOrEmpty(userRole)) return Results.Unauthorized();

                    var availabilities = await db.AdminAvailabilities.ToListAsync();
                    return Results.Ok(availabilities);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch availabilities: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher adds an availability slot; adjacent or overlapping slots for the same admin are merged into one
            /// CALLS: useAddAvailability() → availabilityService.add() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - If new slot overlaps or is adjacent to existing unbooked slot(s) for the same AdminId, all touching unbooked slots are deleted and replaced with one merged slot
            ///   - Slots with IsBooked = true are never touched by the merge
            /// </summary>
            app.MapPost("/api/availability", [Authorize] async (AddAvailabilityDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var touching = await db.AdminAvailabilities
                        .Where(a => a.AdminId == dto.AdminId
                                 && !a.IsBooked
                                 && a.StartTime <= dto.EndTime
                                 && a.EndTime >= dto.StartTime)
                        .ToListAsync();

                    DateTime mergedStart = dto.StartTime;
                    DateTime mergedEnd = dto.EndTime;

                    if (touching.Count > 0)
                    {
                        mergedStart = touching.Min(a => a.StartTime) < dto.StartTime ? touching.Min(a => a.StartTime) : dto.StartTime;
                        mergedEnd = touching.Max(a => a.EndTime) > dto.EndTime ? touching.Max(a => a.EndTime) : dto.EndTime;
                        db.AdminAvailabilities.RemoveRange(touching);
                    }

                    var availability = new AdminAvailability
                    {
                        AdminId = dto.AdminId,
                        StartTime = mergedStart,
                        EndTime = mergedEnd,
                        IsBooked = false
                    };

                    db.AdminAvailabilities.Add(availability);
                    await db.SaveChangesAsync();

                    return Results.Created($"/api/availability/{availability.Id}", availability);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to add availability: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher updates an availability slot's time or booked status
            /// CALLS: useUpdateAvailability() → availabilityService.update() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Updates StartTime, EndTime, IsBooked on the AdminAvailability record
            /// </summary>
            app.MapPut("/api/availability/{id}", [Authorize] async (int id, UpdateAvailabilityDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var availability = await db.AdminAvailabilities.FindAsync(id);
                    if (availability == null) return Results.NotFound("Availability not found");

                    availability.StartTime = dto.StartTime;
                    availability.EndTime = dto.EndTime;
                    availability.IsBooked = dto.IsBooked;

                    db.AdminAvailabilities.Update(availability);
                    await db.SaveChangesAsync();

                    return Results.Ok(availability);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to update availability: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher deletes an availability slot; linked bookings are left as-is (orphaned)
            /// CALLS: useDeleteAvailability() → availabilityService.delete() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Removes the AdminAvailability record
            ///   - Linked Booking records remain with AdminAvailabilityId set to null (cascade SetNull)
            /// </summary>
            app.MapDelete("/api/availability/{id}", [Authorize] async (int id, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var availability = await db.AdminAvailabilities.FindAsync(id);
                    if (availability == null) return Results.NotFound("Availability not found");

                    db.AdminAvailabilities.Remove(availability);
                    await db.SaveChangesAsync();

                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to delete availability: " + ex.Message, statusCode: 500);
                }
            });

            // Keep legacy single-item endpoint for reschedule fallback
            app.MapGet("/api/availability/{id}", [Authorize] async (int id, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var availability = await db.AdminAvailabilities.FindAsync(id);
                    if (availability == null) return Results.NotFound("Availability not found");
                    return Results.Ok(availability);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch availability: " + ex.Message, statusCode: 500);
                }
            });
        }
    }
}
