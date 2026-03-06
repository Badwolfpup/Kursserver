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

            /// <summary>
            /// SCENARIO: Admin/Teacher adds an availability slot; adjacent or overlapping slots for the same admin are merged into one
            /// CALLS: useAddAvailability() → adminAvailabilityService.add() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - If new slot overlaps or is adjacent to existing unbooked slot(s) for the same AdminId, all touching unbooked slots are deleted and replaced with one merged slot
            ///   - Slots with IsBooked = true (accepted bookings) are never touched by the merge
            ///   - If no unbooked overlap, inserts new slot as-is
            /// </summary>
            app.MapPost("/api/admin-availability/add", [Authorize] async (AddAvailabilityDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    // Find existing slots for this admin that overlap or are adjacent to the new slot
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

            /// <summary>
            /// SCENARIO: Admin/Teacher deletes an availability slot; any linked bookings are left as-is (orphaned)
            /// CALLS: useDeleteAvailability() → adminAvailabilityService.delete() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Removes the AdminAvailability record
            ///   - Does NOT modify linked Booking records — they remain in the database with their existing status
            /// </summary>
            app.MapDelete("/api/admin-availability/{id}", [Authorize] async (int id, ApplicationDbContext db, HttpContext context) =>
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

            /// <summary>
            /// SCENARIO: Coach, Teacher, or Admin books a time slot from an availability
            /// CALLS: useBookAvailability() → availabilityService.book() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Creates Booking record
            ///   - Marks AdminAvailability.IsBooked = true if fully booked (ScheduleHelpers.IsFullyBooked)
            ///   - Sends email to booked coach (EmailService)
            ///   - Coach callers: CoachId is enforced to caller's own ID; StudentId must belong to one of their students (403 otherwise)
            /// </summary>
            // POST book an availability — for coaches, or standalone appointment by admin
            app.MapPost("/api/admin-availability/book", [Authorize] async (BookAvailabilityDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1, 1);
                    if (accessCheck != null) return accessCheck;

                    var userId = int.Parse(context.User.FindFirst("id")!.Value);
                    var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                    // Coaches may only book as themselves and only for their own students
                    if (userRole == Role.Coach.ToString())
                    {
                        if (dto.CoachId != userId)
                            return Results.Forbid();
                        if (dto.StudentId.HasValue)
                        {
                            var student = await db.Users.FindAsync(dto.StudentId.Value);
                            if (student == null || student.CoachId != userId)
                                return Results.Forbid();
                        }
                    }

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
                            .Select(b => new { b.StartTime, b.EndTime })
                            .ToListAsync();

                        if (ScheduleHelpers.IsFullyBooked(availability.StartTime, availability.EndTime,
                            allBookings.Select(b => (b.StartTime, b.EndTime))))
                        {
                            availability.IsBooked = true;
                            await db.SaveChangesAsync();
                        }
                    }

                    // Notify coach
                    if (booking.CoachId > 0)
                    {
                        var coach = await db.Users.FindAsync(booking.CoachId);
                        if (coach != null)
                            emailService.SendEmailFireAndForget(coach.Email, "Ny bokning",
                                $"Du har blivit inbokad {booking.StartTime:g}–{booking.EndTime:t}. Mötestyp: {booking.MeetingType}.");
                    }

                    // Notify student (meeting is already accepted, so student should know)
                    if (booking.StudentId.HasValue)
                    {
                        var student = await db.Users.FindAsync(booking.StudentId.Value);
                        if (student?.EmailNotifications == true)
                            emailService.SendEmailFireAndForget(student.Email, "Nytt möte inbokat",
                                $"Ett möte har bokats in {booking.StartTime:g}–{booking.EndTime:t}. Logga in för att se detaljer.");
                    }

                    return Results.Created($"/api/admin-availability/bookings/{booking.Id}", booking);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to book availability: " + ex.Message, statusCode: 500);
                }
            });

            // POST create standalone appointment — for admin/teacher only
            app.MapPost("/api/admin-availability/appointments", [Authorize] async (AdminAppointmentDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
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

                    // Notify coach
                    if (booking.CoachId > 0)
                    {
                        var coach = await db.Users.FindAsync(booking.CoachId);
                        if (coach != null)
                            emailService.SendEmailFireAndForget(coach.Email, "Ny mötesförfrågan",
                                $"Du har fått en mötesförfrågan {booking.StartTime:g}–{booking.EndTime:t}. Logga in för att bekräfta.");
                    }

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
            app.MapPost("/api/admin-availability/bookings/{id}/status", [Authorize] async (int id, UpdateBookingStatusDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
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

                    // Send email notification
                    if (userRole == "Coach")
                    {
                        // Coach responded — notify admin
                        var admin = await db.Users.FindAsync(booking.AdminId);
                        if (admin?.EmailNotifications == true)
                        {
                            var verb = dto.Status == "accepted" ? "bekräftat" : "nekat";
                            emailService.SendEmailFireAndForget(admin.Email, "Svar på bokning",
                                $"Coach har {verb} bokning {booking.StartTime:g}.");
                        }
                    }
                    else
                    {
                        // Admin responded — notify coach (always send to coaches)
                        if (booking.CoachId > 0)
                        {
                            var coach = await db.Users.FindAsync(booking.CoachId);
                            if (coach != null)
                            {
                                if (dto.Status == "accepted")
                                    emailService.SendEmailFireAndForget(coach.Email, "Bokning bekräftad",
                                        $"Din bokning {booking.StartTime:g} har bekräftats.");
                                else
                                    emailService.SendEmailFireAndForget(coach.Email, "Bokning nekad",
                                        $"Din bokning {booking.StartTime:g} har nekats. Anledning: {dto.Reason}");
                            }
                        }
                    }

                    // If declining, re-evaluate whether the parent availability is still fully booked
                    if (dto.Status == "declined")
                    {
                        var availability = await db.AdminAvailabilities.FindAsync(booking.AdminAvailabilityId);
                        if (availability != null && availability.IsBooked)
                        {
                            var activeBookings = await db.Bookings
                                .Where(b => b.AdminAvailabilityId == booking.AdminAvailabilityId && b.Status != "declined")
                                .OrderBy(b => b.StartTime)
                                .Select(b => new { b.StartTime, b.EndTime })
                                .ToListAsync();

                            if (!ScheduleHelpers.IsFullyBooked(availability.StartTime, availability.EndTime,
                                activeBookings.Select(b => (b.StartTime, b.EndTime))))
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
            app.MapPost("/api/admin-availability/bookings/{id}/cancel", [Authorize] async (int id, UpdateBookingStatusDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
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

                    // Send email notification to the other party
                    if (userRole == "Coach")
                    {
                        var admin = await db.Users.FindAsync(booking.AdminId);
                        if (admin?.EmailNotifications == true)
                            emailService.SendEmailFireAndForget(admin.Email, "Bokning avbokad",
                                $"Coach har avbokat bokning {booking.StartTime:g}. Anledning: {dto.Reason}");
                    }
                    else
                    {
                        if (booking.CoachId > 0)
                        {
                            var coach = await db.Users.FindAsync(booking.CoachId);
                            if (coach != null)
                                emailService.SendEmailFireAndForget(coach.Email, "Bokning avbokad",
                                    $"Din bokning {booking.StartTime:g} har avbokats. Anledning: {dto.Reason}");
                        }
                    }

                    // Re-evaluate whether the parent availability is still fully booked
                    var availability = await db.AdminAvailabilities.FindAsync(booking.AdminAvailabilityId);
                    if (availability != null && availability.IsBooked)
                    {
                        var activeBookings = await db.Bookings
                            .Where(b => b.AdminAvailabilityId == booking.AdminAvailabilityId && b.Status != "declined")
                            .OrderBy(b => b.StartTime)
                            .Select(b => new { b.StartTime, b.EndTime })
                            .ToListAsync();

                        if (!ScheduleHelpers.IsFullyBooked(availability.StartTime, availability.EndTime,
                            activeBookings.Select(b => (b.StartTime, b.EndTime))))
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
            app.MapPut("/api/admin-availability/bookings/{id}/reschedule", [Authorize] async (int id, UpdateBookingTimesDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
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

                    // Send email notification to the other party
                    if (userRole == "Coach")
                    {
                        var admin = await db.Users.FindAsync(booking.AdminId);
                        if (admin?.EmailNotifications == true)
                            emailService.SendEmailFireAndForget(admin.Email, "Bokning ombokas",
                                $"Coach begär ombokning till {booking.StartTime:g}–{booking.EndTime:t}. Logga in för att godkänna.");
                    }
                    else
                    {
                        if (booking.CoachId > 0)
                        {
                            var coach = await db.Users.FindAsync(booking.CoachId);
                            if (coach != null)
                                emailService.SendEmailFireAndForget(coach.Email, "Bokning ombokas",
                                    $"Din bokning har ombokats till {booking.StartTime:g}–{booking.EndTime:t}. Logga in för att godkänna.");
                        }
                    }

                    return Results.Ok(booking);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to reschedule booking: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Coach suggests a meeting with an admin for a time outside of admin availability
            /// CALLS: createCoachAppointment() → BookingService (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Creates Booking with Status = "pending", CoachId = caller, AdminId = dto.AdminId
            ///   - Returns 409 conflict if accepted booking overlaps; 409 warning if pending booking overlaps (unless Force=true)
            ///   - Sends email to the admin if EmailNotifications = true
            /// </summary>
            app.MapPost("/api/admin-availability/coach-appointments", [Authorize] async (CoachAppointmentDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1, 3);
                    if (accessCheck != null) return accessCheck;

                    if (dto.StartTime >= dto.EndTime)
                        return Results.BadRequest("Invalid time range");

                    var coachId = int.Parse(context.User.FindFirst("id")!.Value);

                    // Check admin conflicts
                    var adminConflicts = await db.Bookings
                        .Where(b => b.AdminId == dto.AdminId
                                 && b.Status != "declined"
                                 && b.StartTime < dto.EndTime
                                 && b.EndTime > dto.StartTime)
                        .ToListAsync();

                    // Check coach's own conflicts
                    var coachConflicts = await db.Bookings
                        .Where(b => b.CoachId == coachId
                                 && b.Status != "declined"
                                 && b.StartTime < dto.EndTime
                                 && b.EndTime > dto.StartTime)
                        .ToListAsync();

                    var allConflicts = adminConflicts.Union(coachConflicts).DistinctBy(b => b.Id).ToList();

                    if (allConflicts.Any(b => b.Status == "accepted"))
                        return Results.Conflict(new { type = "conflict", bookings = allConflicts.Where(b => b.Status == "accepted").ToList() });

                    if (!dto.Force && allConflicts.Any(b => b.Status == "pending" || b.Status == "rescheduled"))
                        return Results.Conflict(new { type = "warning", bookings = allConflicts.Where(b => b.Status != "declined").ToList() });

                    var booking = new Booking
                    {
                        AdminId = dto.AdminId,
                        CoachId = coachId,
                        StudentId = dto.StudentId,
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

                    // Notify admin
                    if (!app.Environment.IsDevelopment())
                    {
                        var admin = await db.Users.FindAsync(dto.AdminId);
                        if (admin?.EmailNotifications == true)
                            emailService.SendEmailFireAndForget(admin.Email, "Ny mötesförfrågan från coach",
                                $"Du har fått en mötesförfrågan {booking.StartTime:g}–{booking.EndTime:t}. Logga in för att bekräfta.");
                    }

                    return Results.Created($"/api/admin-availability/bookings/{booking.Id}", booking);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to create coach appointment: " + ex.Message, statusCode: 500);
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
