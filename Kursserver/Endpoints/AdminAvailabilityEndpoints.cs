using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Endpoints
{
    public static class AdminAvailabilityEndpoints
    {
        public static void MapAdminAvailabilityEndpoints(this WebApplication app)
        {
            // GET free (unbooked) availabilities — for coaches
            app.MapGet("/api/admin-availability/free", [Authorize] async (ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1, 1);
                    if (accessCheck != null) return accessCheck;

                    var availabilities = await db.AdminAvailabilities
                        .Where(a => a.EndTime > DateTime.Now)
                        .ToListAsync();

                    return Results.Ok(availabilities);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch availabilities: " + ex.Message, statusCode: 500);
                }
            });

            // GET all availabilities — for admin/teacher
            app.MapGet("/api/admin-availability/all", [Authorize] async (ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var availabilities = await db.AdminAvailabilities.ToListAsync();
                    return Results.Ok(availabilities);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch availabilities: " + ex.Message, statusCode: 500);
                }
            });

            // POST add availability — for admin/teacher
            app.MapPost("/api/admin-availability/add", [Authorize] async (AddAvailabilityDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var availability = new AdminAvailability
                    {
                        AdminId = dto.AdminId,
                        StartTime = dto.StartTime,
                        EndTime = dto.EndTime,
                        IsBooked = false
                    };

                    db.AdminAvailabilities.Add(availability);
                    await db.SaveChangesAsync();

                    return Results.Created($"/api/admin-availability/{availability.Id}", availability);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to add availability: " + ex.Message, statusCode: 500);
                }
            });

            // POST update availability — for admin/teacher
            app.MapPost("/api/admin-availability/update", [Authorize] async (UpdateAvailabilityDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var availability = await db.AdminAvailabilities.FindAsync(dto.Id);
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

            // POST book an availability — for coaches
            // Splits the availability around the booked time range
            app.MapPost("/api/admin-availability/book", [Authorize] async (BookAvailabilityDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1, 1);
                    if (accessCheck != null) return accessCheck;

                    var availability = await db.AdminAvailabilities.FindAsync(dto.AdminAvailabilityId);
                    if (availability == null) return Results.NotFound("Availability not found");

                    // Validate the requested time fits within the availability
                    if (dto.StartTime < availability.StartTime || dto.EndTime > availability.EndTime)
                        return Results.BadRequest("Requested time is outside the availability window");
                    if (dto.StartTime >= dto.EndTime)
                        return Results.BadRequest("Invalid time range");

                    // Check for overlapping bookings on the same availability
                    var hasOverlap = await db.Bookings.AnyAsync(b =>
                        b.AdminAvailabilityId == dto.AdminAvailabilityId &&
                        b.Status != "declined" &&
                        b.StartTime < dto.EndTime && b.EndTime > dto.StartTime);
                    if (hasOverlap) return Results.BadRequest("This time range overlaps with an existing booking");

                    var booking = new Booking
                    {
                        AdminId = availability.AdminId,
                        CoachId = dto.CoachId,
                        StudentId = dto.StudentId,
                        AdminAvailabilityId = dto.AdminAvailabilityId,
                        Note = dto.Note,
                        MeetingType = dto.MeetingType,
                        StartTime = dto.StartTime,
                        EndTime = dto.EndTime,
                        BookedAt = DateTime.Now,
                        Seen = false
                    };

                    db.Bookings.Add(booking);

                    // Check if the entire availability is now fully booked
                    // by loading all bookings for this availability and checking coverage
                    await db.SaveChangesAsync();

                    // After saving, check if the availability is fully covered
                    var allBookings = await db.Bookings
                        .Where(b => b.AdminAvailabilityId == dto.AdminAvailabilityId && b.Status != "declined")
                        .OrderBy(b => b.StartTime)
                        .ToListAsync();

                    var coveredStart = availability.StartTime;
                    var fullyCovered = true;
                    foreach (var b in allBookings)
                    {
                        if (b.StartTime > coveredStart) { fullyCovered = false; break; }
                        if (b.EndTime > coveredStart) coveredStart = b.EndTime;
                    }
                    if (coveredStart < availability.EndTime) fullyCovered = false;

                    if (fullyCovered)
                    {
                        availability.IsBooked = true;
                        await db.SaveChangesAsync();
                    }

                    return Results.Created($"/api/admin-availability/bookings/{booking.Id}", booking);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to book availability: " + ex.Message, statusCode: 500);
                }
            });

            // GET all bookings — for admin/teacher
            app.MapGet("/api/admin-availability/bookings", [Authorize] async (ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var bookings = await db.Bookings.ToListAsync();
                    return Results.Ok(bookings);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch bookings: " + ex.Message, statusCode: 500);
                }
            });

            // POST update booking status (accept/decline) — for admin/teacher
            app.MapPost("/api/admin-availability/bookings/{id}/status", [Authorize] async (int id, UpdateBookingStatusDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var booking = await db.Bookings.FindAsync(id);
                    if (booking == null) return Results.NotFound("Booking not found");

                    if (dto.Status != "accepted" && dto.Status != "declined")
                        return Results.BadRequest("Status must be 'accepted' or 'declined'");

                    booking.Status = dto.Status;
                    await db.SaveChangesAsync();

                    // If declining, re-evaluate whether the parent availability is still fully booked
                    if (dto.Status == "declined")
                    {
                        var availability = await db.AdminAvailabilities.FindAsync(booking.AdminAvailabilityId);
                        if (availability != null && availability.IsBooked)
                        {
                            var activeBookings = await db.Bookings
                                .Where(b => b.AdminAvailabilityId == booking.AdminAvailabilityId && b.Status != "declined")
                                .OrderBy(b => b.StartTime)
                                .ToListAsync();

                            var coveredStart = availability.StartTime;
                            var fullyCovered = true;
                            foreach (var b in activeBookings)
                            {
                                if (b.StartTime > coveredStart) { fullyCovered = false; break; }
                                if (b.EndTime > coveredStart) coveredStart = b.EndTime;
                            }
                            if (coveredStart < availability.EndTime) fullyCovered = false;

                            if (!fullyCovered)
                            {
                                availability.IsBooked = false;
                                await db.SaveChangesAsync();
                            }
                        }
                    }

                    return Results.Ok(booking);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to update booking status: " + ex.Message, statusCode: 500);
                }
            });

            // GET bookings visible to coaches (all future bookings on availabilities they can see)
            app.MapGet("/api/admin-availability/bookings/visible", [Authorize] async (ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1, 1);
                    if (accessCheck != null) return accessCheck;

                    var bookings = await db.Bookings
                        .Where(b => b.EndTime > DateTime.Now)
                        .ToListAsync();
                    return Results.Ok(bookings);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch bookings: " + ex.Message, statusCode: 500);
                }
            });
        }
    }
}
