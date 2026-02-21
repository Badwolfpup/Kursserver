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

            // GET single availability by id — for coaches (needed when rescheduling a rescheduled booking)
            app.MapGet("/api/admin-availability/{id}", [Authorize] async (int id, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1, 1);
                    if (accessCheck != null) return accessCheck;

                    var availability = await db.AdminAvailabilities.FindAsync(id);
                    if (availability == null) return Results.NotFound("Availability not found");
                    return Results.Ok(availability);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch availability: " + ex.Message, statusCode: 500);
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

            // DELETE availability — for admin/teacher
            // Refuses if there are accepted bookings; declines pending/rescheduled ones first
            app.MapDelete("/api/admin-availability/{id}", [Authorize] async (int id, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var availability = await db.AdminAvailabilities.FindAsync(id);
                    if (availability == null) return Results.NotFound("Availability not found");

                    var relatedBookings = await db.Bookings
                        .Where(b => b.AdminAvailabilityId == id)
                        .ToListAsync();

                    if (relatedBookings.Any(b => b.Status == "accepted"))
                        return Results.BadRequest("Kan inte ta bort: det finns godkända bokningar");

                    // Decline any pending/rescheduled bookings
                    foreach (var b in relatedBookings.Where(b => b.Status != "declined"))
                    {
                        b.Status = "declined";
                        b.Reason = "Tillgängligheten togs bort";
                    }

                    db.AdminAvailabilities.Remove(availability);
                    await db.SaveChangesAsync();

                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to delete availability: " + ex.Message, statusCode: 500);
                }
            });

            // POST book an availability — for coaches, or standalone appointment by admin
            app.MapPost("/api/admin-availability/book", [Authorize] async (BookAvailabilityDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1, 1);
                    if (accessCheck != null) return accessCheck;

                    if (dto.StartTime >= dto.EndTime)
                        return Results.BadRequest("Invalid time range");

                    AdminAvailability? availability = null;

                    if (dto.AdminAvailabilityId.HasValue)
                    {
                        availability = await db.AdminAvailabilities.FindAsync(dto.AdminAvailabilityId.Value);
                        if (availability == null) return Results.NotFound("Availability not found");

                        // Validate the requested time fits within the availability
                        if (dto.StartTime < availability.StartTime || dto.EndTime > availability.EndTime)
                            return Results.BadRequest("Requested time is outside the availability window");

                        // Check for overlapping bookings on the same availability
                        var hasOverlap = await db.Bookings.AnyAsync(b =>
                            b.AdminAvailabilityId == dto.AdminAvailabilityId &&
                            b.Status != "declined" &&
                            b.StartTime < dto.EndTime && b.EndTime > dto.StartTime);
                        if (hasOverlap) return Results.BadRequest("This time range overlaps with an existing booking");
                    }

                    var userId = int.Parse(context.User.FindFirst("id")!.Value);

                    var booking = new Booking
                    {
                        AdminId = availability?.AdminId ?? userId,
                        CoachId = dto.CoachId,
                        StudentId = dto.StudentId,
                        AdminAvailabilityId = dto.AdminAvailabilityId,
                        Note = dto.Note,
                        MeetingType = dto.MeetingType,
                        StartTime = dto.StartTime,
                        EndTime = dto.EndTime,
                        BookedAt = DateTime.Now,
                        Seen = false,
                        Status = "accepted"
                    };

                    db.Bookings.Add(booking);
                    await db.SaveChangesAsync();

                    // Check if the entire availability is now fully booked (only for availability-linked bookings)
                    if (availability != null)
                    {
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
                    }

                    return Results.Created($"/api/admin-availability/bookings/{booking.Id}", booking);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to book availability: " + ex.Message, statusCode: 500);
                }
            });

            // POST create standalone appointment — for admin/teacher only
            app.MapPost("/api/admin-availability/appointments", [Authorize] async (AdminAppointmentDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    if (dto.StartTime >= dto.EndTime)
                        return Results.BadRequest("Invalid time range");

                    var userId = int.Parse(context.User.FindFirst("id")!.Value);

                    // Check coach conflicts
                    var coachConflicts = await db.Bookings
                        .Where(b => b.CoachId == dto.CoachId
                                 && b.Status != "declined"
                                 && b.StartTime < dto.EndTime
                                 && b.EndTime > dto.StartTime)
                        .ToListAsync();

                    // Check admin's own calendar conflicts
                    var adminConflicts = await db.Bookings
                        .Where(b => b.AdminId == userId
                                 && b.Status != "declined"
                                 && b.StartTime < dto.EndTime
                                 && b.EndTime > dto.StartTime)
                        .ToListAsync();

                    var allConflicts = coachConflicts.Union(adminConflicts).DistinctBy(b => b.Id).ToList();

                    if (allConflicts.Any(b => b.Status == "accepted"))
                        return Results.Conflict(new { type = "conflict", bookings = allConflicts.Where(b => b.Status == "accepted").ToList() });

                    if (!dto.Force && allConflicts.Any(b => b.Status == "pending" || b.Status == "rescheduled"))
                        return Results.Conflict(new { type = "warning", bookings = allConflicts.Where(b => b.Status != "declined").ToList() });

                    var booking = new Booking
                    {
                        AdminId = userId,
                        CoachId = dto.CoachId,
                        StudentId = null,
                        AdminAvailabilityId = null,
                        Note = dto.Note,
                        MeetingType = dto.MeetingType,
                        StartTime = dto.StartTime,
                        EndTime = dto.EndTime,
                        BookedAt = DateTime.Now,
                        Seen = false,
                        Status = "pending"
                    };

                    db.Bookings.Add(booking);
                    await db.SaveChangesAsync();

                    return Results.Created($"/api/admin-availability/bookings/{booking.Id}", booking);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to create appointment: " + ex.Message, statusCode: 500);
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

            // POST update booking status (accept/decline) — for admin/teacher, or coach responding to a rescheduled booking
            app.MapPost("/api/admin-availability/bookings/{id}/status", [Authorize] async (int id, UpdateBookingStatusDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                    var userId = context.User.FindFirst("id")?.Value;

                    // Coaches may only respond to rescheduled bookings that belong to them
                    if (userRole == "Coach")
                    {
                        var bookingForCheck = await db.Bookings.FindAsync(id);
                        if (bookingForCheck == null) return Results.NotFound("Booking not found");
                        if (bookingForCheck.CoachId.ToString() != userId) return Results.StatusCode(403);
                        if (bookingForCheck.Status != "rescheduled") return Results.StatusCode(403);
                    }
                    else
                    {
                        var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                        if (accessCheck != null) return accessCheck;
                    }

                    var booking = await db.Bookings.FindAsync(id);
                    if (booking == null) return Results.NotFound("Booking not found");

                    if (dto.Status != "accepted" && dto.Status != "declined")
                        return Results.BadRequest("Status must be 'accepted' or 'declined'");

                    booking.Status = dto.Status;
                    booking.Reason = dto.Reason ?? "";
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

            // POST cancel a booking — accessible by coach (own bookings) or admin/teacher
            app.MapPost("/api/admin-availability/bookings/{id}/cancel", [Authorize] async (int id, UpdateBookingStatusDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1, 1);
                    if (accessCheck != null) return accessCheck;

                    var booking = await db.Bookings.FindAsync(id);
                    if (booking == null) return Results.NotFound("Booking not found");

                    // Coaches can only cancel their own bookings
                    var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                    var userId = context.User.FindFirst("id")?.Value;
                    if (userRole == "Coach" && booking.CoachId.ToString() != userId)
                        return Results.StatusCode(403);

                    booking.Status = "declined";
                    booking.Reason = dto.Reason ?? "";
                    await db.SaveChangesAsync();

                    // Re-evaluate whether the parent availability is still fully booked
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

                    return Results.Ok(booking);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to cancel booking: " + ex.Message, statusCode: 500);
                }
            });

            // PUT reschedule a booking — accessible by coach (own) or admin/teacher
            app.MapPut("/api/admin-availability/bookings/{id}/reschedule", [Authorize] async (int id, UpdateBookingTimesDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1, 1);
                    if (accessCheck != null) return accessCheck;

                    var booking = await db.Bookings.FindAsync(id);
                    if (booking == null) return Results.NotFound("Booking not found");

                    var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                    var userId = context.User.FindFirst("id")?.Value;
                    if (userRole == "Coach" && booking.CoachId.ToString() != userId)
                        return Results.StatusCode(403);

                    if (dto.StartTime >= dto.EndTime)
                        return Results.BadRequest("Invalid time range");

                    if (booking.AdminAvailabilityId.HasValue)
                    {
                        var availability = await db.AdminAvailabilities.FindAsync(booking.AdminAvailabilityId.Value);
                        if (availability == null) return Results.NotFound("Parent availability not found");
                        if (dto.StartTime < availability.StartTime || dto.EndTime > availability.EndTime)
                            return Results.BadRequest("New times are outside the availability window");
                    }

                    booking.StartTime = dto.StartTime;
                    booking.EndTime = dto.EndTime;
                    booking.Reason = dto.Reason ?? "";
                    booking.Status = "rescheduled";
                    booking.RescheduledBy = dto.RescheduledBy;
                    await db.SaveChangesAsync();

                    return Results.Ok(booking);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to reschedule booking: " + ex.Message, statusCode: 500);
                }
            });

            // GET bookings visible to coaches (all future bookings on availabilities they can see)
            app.MapGet("/api/admin-availability/bookings/visible", [Authorize] async (ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1, 1);
                    if (accessCheck != null) return accessCheck;

                    var bookings = await db.Bookings.ToListAsync();
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
