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
            app.MapGet("/api/busy-time", [Authorize] async (ApplicationDbContext db) =>
            {
                var busyTimes = await db.BusyTimes.ToListAsync();
                return Results.Ok(busyTimes);
            });

            app.MapPost("/api/busy-time", [Authorize] async (AddBusyTimeDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;

                if (dto.StartTime >= dto.EndTime)
                    return Results.BadRequest("Starttid måste vara före sluttid.");

                // Check for overlapping non-declined bookings on this admin
                var overlappingBookings = await db.Bookings
                    .Where(b => b.AdminId == dto.AdminId
                             && b.Status != "declined"
                             && b.StartTime < dto.EndTime
                             && b.EndTime > dto.StartTime)
                    .ToListAsync();

                if (overlappingBookings.Count > 0 && !dto.Force)
                {
                    // Return 409 with affected bookings so frontend can show confirmation
                    return Results.Conflict(new
                    {
                        type = "confirm",
                        bookings = overlappingBookings.Select(b => new
                        {
                            b.Id,
                            b.AdminId,
                            b.CoachId,
                            b.StudentId,
                            b.StartTime,
                            b.EndTime,
                            b.MeetingType,
                            b.Status,
                            b.Note
                        })
                    });
                }

                // Force=true or no overlapping bookings: proceed
                // Cancel overlapping bookings
                foreach (var booking in overlappingBookings)
                {
                    booking.Status = "declined";
                    booking.Reason = "Avbokad på grund av upptagen tid";
                }

                // Trim/split overlapping availability slots
                var overlappingAvails = await db.AdminAvailabilities
                    .Where(a => a.AdminId == dto.AdminId
                             && a.StartTime < dto.EndTime
                             && a.EndTime > dto.StartTime)
                    .ToListAsync();

                foreach (var avail in overlappingAvails)
                {
                    if (avail.StartTime >= dto.StartTime && avail.EndTime <= dto.EndTime)
                    {
                        // Fully covered — remove
                        db.AdminAvailabilities.Remove(avail);
                    }
                    else if (avail.StartTime < dto.StartTime && avail.EndTime > dto.EndTime)
                    {
                        // Busy time is in the middle — split into two
                        var newAvail = new AdminAvailability
                        {
                            AdminId = avail.AdminId,
                            StartTime = dto.EndTime,
                            EndTime = avail.EndTime,
                            IsBooked = false
                        };
                        avail.EndTime = dto.StartTime;

                        // Re-evaluate IsBooked for the trimmed left part
                        var leftBookings = await db.Bookings
                            .Where(b => b.AdminAvailabilityId == avail.Id
                                     && b.Status != "declined"
                                     && b.StartTime < avail.EndTime
                                     && b.EndTime > avail.StartTime)
                            .Select(b => new { b.StartTime, b.EndTime })
                            .ToListAsync();
                        avail.IsBooked = ScheduleHelpers.IsFullyBooked(
                            avail.StartTime, avail.EndTime,
                            leftBookings.Select(b => (b.StartTime, b.EndTime)));

                        db.AdminAvailabilities.Add(newAvail);
                    }
                    else if (avail.StartTime < dto.StartTime)
                    {
                        // Overlaps on the right — trim end
                        avail.EndTime = dto.StartTime;

                        var remainingBookings = await db.Bookings
                            .Where(b => b.AdminAvailabilityId == avail.Id
                                     && b.Status != "declined"
                                     && b.StartTime < avail.EndTime
                                     && b.EndTime > avail.StartTime)
                            .Select(b => new { b.StartTime, b.EndTime })
                            .ToListAsync();
                        avail.IsBooked = ScheduleHelpers.IsFullyBooked(
                            avail.StartTime, avail.EndTime,
                            remainingBookings.Select(b => (b.StartTime, b.EndTime)));
                    }
                    else
                    {
                        // Overlaps on the left — trim start
                        avail.StartTime = dto.EndTime;

                        var remainingBookings = await db.Bookings
                            .Where(b => b.AdminAvailabilityId == avail.Id
                                     && b.Status != "declined"
                                     && b.StartTime < avail.EndTime
                                     && b.EndTime > avail.StartTime)
                            .Select(b => new { b.StartTime, b.EndTime })
                            .ToListAsync();
                        avail.IsBooked = ScheduleHelpers.IsFullyBooked(
                            avail.StartTime, avail.EndTime,
                            remainingBookings.Select(b => (b.StartTime, b.EndTime)));
                    }
                }

                // Re-evaluate IsBooked on any availability that had its bookings cancelled
                foreach (var booking in overlappingBookings)
                {
                    if (booking.AdminAvailabilityId.HasValue)
                    {
                        var parentAvail = await db.AdminAvailabilities.FindAsync(booking.AdminAvailabilityId.Value);
                        if (parentAvail != null)
                        {
                            var activeBookings = await db.Bookings
                                .Where(b => b.AdminAvailabilityId == parentAvail.Id
                                         && b.Status != "declined"
                                         && b.StartTime < parentAvail.EndTime
                                         && b.EndTime > parentAvail.StartTime)
                                .Select(b => new { b.StartTime, b.EndTime })
                                .ToListAsync();
                            parentAvail.IsBooked = ScheduleHelpers.IsFullyBooked(
                                parentAvail.StartTime, parentAvail.EndTime,
                                activeBookings.Select(b => (b.StartTime, b.EndTime)));
                        }
                    }
                }

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

            app.MapPut("/api/busy-time/{id}", [Authorize] async (int id, UpdateBusyTimeDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;

                var busyTime = await db.BusyTimes.FindAsync(id);
                if (busyTime == null) return Results.NotFound("Busy time not found");

                if (dto.StartTime >= dto.EndTime)
                    return Results.BadRequest("Starttid måste vara före sluttid.");

                busyTime.StartTime = dto.StartTime;
                busyTime.EndTime = dto.EndTime;
                busyTime.Note = dto.Note;

                await db.SaveChangesAsync();
                return Results.Ok(busyTime);
            });

            app.MapDelete("/api/busy-time/{id}", [Authorize] async (int id, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                if (accessCheck != null) return accessCheck;

                var busyTime = await db.BusyTimes.FindAsync(id);
                if (busyTime == null) return Results.NotFound("Busy time not found");

                db.BusyTimes.Remove(busyTime);
                await db.SaveChangesAsync();

                return Results.Ok();
            });
        }
    }
}
