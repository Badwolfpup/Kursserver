using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Kursserver.Endpoints
{
    public static class BookingEndpoints
    {
        public static void MapBookingEndpoints(this WebApplication app)
        {
            /// <summary>
            /// SCENARIO: Any authenticated user fetches bookings; admin=all, coach=own, student=own
            /// CALLS: useBookings() → bookingService.getBookings() (kurshemsida)
            /// SIDE EFFECTS: none (read-only)
            /// </summary>
            app.MapGet("/api/bookings", [Authorize] async (ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    var userId = int.Parse(context.User.FindFirst("id")!.Value);

                    if (string.IsNullOrEmpty(userRole)) return Results.Unauthorized();

                    if (!Enum.TryParse<Role>(userRole, out var role))
                        return Results.Unauthorized();

                    IQueryable<Booking> query = db.Bookings;

                    if (role == Role.Coach)
                        query = query.Where(b => b.CoachId == userId);
                    else if (role == Role.Student)
                        query = query.Where(b => b.StudentId == userId);
                    // Admin/Teacher see all

                    var bookings = await query.ToListAsync();
                    return Results.Ok(bookings);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch bookings: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Unified booking creation — role detected from JWT.
            ///   Admin: creates pending appointment (with coach or student)
            ///   Coach: books availability slot (accepted) or suggests meeting (pending)
            ///   Student: creates pending booking with admin
            /// CALLS: useCreateBooking() → bookingService.createBooking() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Creates Booking record
            ///   - For availability-based bookings: marks AdminAvailability.IsBooked = true if fully booked
            ///   - Sends email notification via BookingNotifier
            ///   - Returns 409 conflict if accepted booking overlaps; 409 warning if pending overlaps (Force=true bypasses warning)
            /// </summary>
            app.MapPost("/api/bookings", [Authorize] async (CreateBookingDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
            {
                try
                {
                    var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    var userId = int.Parse(context.User.FindFirst("id")!.Value);

                    if (string.IsNullOrEmpty(userRole) || !Enum.TryParse<Role>(userRole, out var role))
                        return Results.Unauthorized();

                    if (dto.StartTime >= dto.EndTime)
                        return Results.BadRequest("Invalid time range");

                    int bookingAdminId;
                    int? bookingCoachId = dto.CoachId;
                    int? bookingStudentId = dto.StudentId;
                    string status;

                    if (role == Role.Admin || role == Role.Teacher)
                    {
                        bookingAdminId = dto.AdminId ?? userId;
                        status = dto.AdminAvailabilityId.HasValue ? "accepted" : "pending";
                    }
                    else if (role == Role.Coach)
                    {
                        bookingCoachId = userId;

                        // Validate student belongs to coach
                        if (dto.StudentId.HasValue)
                        {
                            var student = await db.Users.FindAsync(dto.StudentId.Value);
                            if (student == null || student.CoachId != userId)
                                return Results.Forbid();
                        }

                        if (dto.AdminAvailabilityId.HasValue)
                        {
                            // Booking from availability slot → accepted
                            var avail = await db.AdminAvailabilities.FindAsync(dto.AdminAvailabilityId.Value);
                            if (avail == null) return Results.NotFound("Availability not found");
                            bookingAdminId = avail.AdminId;
                            status = "accepted";
                        }
                        else
                        {
                            // Suggesting meeting → pending
                            bookingAdminId = dto.AdminId ?? 0;
                            if (bookingAdminId == 0)
                                return Results.BadRequest("AdminId is required for suggested meetings");
                            status = "pending";
                        }
                    }
                    else if (role == Role.Student)
                    {
                        bookingAdminId = dto.AdminId ?? 0;
                        if (bookingAdminId == 0)
                            return Results.BadRequest("AdminId is required");
                        bookingStudentId = userId;
                        bookingCoachId = null;
                        status = "pending";
                    }
                    else
                    {
                        return Results.Forbid();
                    }

                    // Availability-based booking validation
                    AdminAvailability? availability = null;
                    if (dto.AdminAvailabilityId.HasValue)
                    {
                        availability = await db.AdminAvailabilities.FindAsync(dto.AdminAvailabilityId.Value);
                        if (availability == null) return Results.NotFound("Availability not found");

                        if (dto.StartTime < availability.StartTime || dto.EndTime > availability.EndTime)
                            return Results.BadRequest("Requested time is outside the availability window");

                        var hasOverlap = await db.Bookings.AnyAsync(b =>
                            b.AdminAvailabilityId == dto.AdminAvailabilityId &&
                            b.Status != "declined" &&
                            b.StartTime < dto.EndTime && b.EndTime > dto.StartTime);
                        if (hasOverlap) return Results.BadRequest("This time range overlaps with an existing booking");
                    }
                    else
                    {
                        // Standalone appointment — check conflicts
                        var conflicts = await ConflictDetection.CheckBookingConflicts(
                            db, dto.StartTime, dto.EndTime,
                            adminId: bookingAdminId,
                            coachId: bookingCoachId,
                            studentId: bookingStudentId);

                        if (conflicts.Any(b => b.Status == "accepted"))
                            return Results.Conflict(new { type = "conflict", bookings = conflicts.Where(b => b.Status == "accepted").ToList() });

                        if (!dto.Force && conflicts.Any(b => b.Status == "pending" || b.Status == "rescheduled"))
                            return Results.Conflict(new { type = "warning", bookings = conflicts.Where(b => b.Status != "declined").ToList() });
                    }

                    var booking = new Booking
                    {
                        AdminId = bookingAdminId,
                        CoachId = bookingCoachId,
                        StudentId = bookingStudentId,
                        AdminAvailabilityId = dto.AdminAvailabilityId,
                        Note = dto.Note,
                        MeetingType = dto.MeetingType,
                        StartTime = dto.StartTime,
                        EndTime = dto.EndTime,
                        BookedAt = DateTime.Now,
                        Seen = false,
                        Status = status
                    };

                    db.Bookings.Add(booking);
                    await db.SaveChangesAsync();

                    // Check if availability is now fully booked
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

                    // Notify
                    var notifier = new BookingNotifier(emailService, db);
                    await notifier.NotifyBookingCreated(booking, userRole);

                    return Results.Created($"/api/bookings/{booking.Id}", booking);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to create booking: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Accept or decline a booking — admin/teacher for any, coach for own pending/rescheduled, student for own pending/rescheduled
            /// CALLS: useUpdateBookingStatus() → bookingService.updateStatus() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Sets booking Status + Reason
            ///   - If declining: re-evaluates parent availability IsBooked
            ///   - Sends email via BookingNotifier
            /// </summary>
            app.MapPut("/api/bookings/{id}/status", [Authorize] async (int id, UpdateBookingStatusDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
            {
                try
                {
                    var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    var userId = int.Parse(context.User.FindFirst("id")!.Value);

                    if (string.IsNullOrEmpty(userRole)) return Results.Unauthorized();

                    var booking = await db.Bookings.FindAsync(id);
                    if (booking == null) return Results.NotFound("Booking not found");

                    if (userRole == "Coach")
                    {
                        if (booking.CoachId != userId) return Results.StatusCode(403);
                        if (booking.Status != "pending" && booking.Status != "rescheduled") return Results.StatusCode(403);
                    }
                    else if (userRole == "Student")
                    {
                        if (booking.StudentId != userId) return Results.StatusCode(403);
                        if (booking.Status != "pending" && booking.Status != "rescheduled") return Results.StatusCode(403);
                    }
                    else
                    {
                        var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                        if (accessCheck != null) return accessCheck;
                    }

                    if (dto.Status != "accepted" && dto.Status != "declined")
                        return Results.BadRequest("Status must be 'accepted' or 'declined'");

                    booking.Status = dto.Status;
                    booking.Reason = dto.Reason ?? "";
                    await db.SaveChangesAsync();

                    // Re-evaluate availability if declining
                    if (dto.Status == "declined" && booking.AdminAvailabilityId.HasValue)
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

                    var notifier = new BookingNotifier(emailService, db);
                    await notifier.NotifyStatusChanged(booking, userRole, dto.Status, dto.Reason);

                    return Results.Ok(booking);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to update booking status: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Cancel a booking — sets status to declined. Admin=any, coach=own, student=own
            /// CALLS: useCancelBooking() → bookingService.cancelBooking() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Sets booking Status = "declined"
            ///   - Re-evaluates parent availability IsBooked
            ///   - Sends email via BookingNotifier (student cancel followup → admin + coach notified)
            /// </summary>
            app.MapPut("/api/bookings/{id}/cancel", [Authorize] async (int id, UpdateBookingStatusDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
            {
                try
                {
                    var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    var userId = int.Parse(context.User.FindFirst("id")!.Value);

                    if (string.IsNullOrEmpty(userRole)) return Results.Unauthorized();

                    var booking = await db.Bookings.FindAsync(id);
                    if (booking == null) return Results.NotFound("Booking not found");

                    // Authorization: coach can cancel own, student can cancel own, admin can cancel any
                    if (userRole == "Coach" && booking.CoachId != userId)
                        return Results.StatusCode(403);
                    if (userRole == "Student" && booking.StudentId != userId)
                        return Results.StatusCode(403);
                    if (userRole != "Coach" && userRole != "Student")
                    {
                        var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                        if (accessCheck != null) return accessCheck;
                    }

                    booking.Status = "declined";
                    booking.Reason = dto.Reason ?? "";
                    await db.SaveChangesAsync();

                    // Re-evaluate availability
                    if (booking.AdminAvailabilityId.HasValue)
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

                    var notifier = new BookingNotifier(emailService, db);
                    await notifier.NotifyCancelled(booking, userRole, dto.Reason);

                    return Results.Ok(booking);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to cancel booking: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Transfer a booking to another teacher — admin/teacher only
            /// CALLS: useTransferBooking() → bookingService.transferBooking() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Updates booking.AdminId to TargetAdminId
            ///   - If booking was pending, sets status to "accepted"
            ///   - Sends transfer notification email via BookingNotifier.NotifyTransferred
            /// </summary>
            app.MapPut("/api/bookings/{id}/transfer", [Authorize] async (int id, TransferBookingDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var booking = await db.Bookings.FindAsync(id);
                    if (booking == null) return Results.NotFound("Booking not found");

                    var targetAdmin = await db.Users.FindAsync(dto.TargetAdminId);
                    if (targetAdmin == null || (targetAdmin.AuthLevel != Role.Admin && targetAdmin.AuthLevel != Role.Teacher))
                        return Results.BadRequest("Target user is not a valid admin/teacher");

                    // Check target teacher doesn't have a conflicting booking at this time
                    var conflicts = await ConflictDetection.CheckBookingConflicts(
                        db, booking.StartTime, booking.EndTime,
                        adminId: dto.TargetAdminId,
                        excludeBookingId: booking.Id);

                    if (conflicts.Any(b => b.Status == "accepted"))
                        return Results.Conflict("Target teacher already has a booking at this time");

                    var oldAdminId = booking.AdminId;
                    booking.AdminId = dto.TargetAdminId;

                    if (booking.Status == "pending")
                        booking.Status = "accepted";

                    await db.SaveChangesAsync();

                    var notifier = new BookingNotifier(emailService, db);
                    await notifier.NotifyTransferred(booking, oldAdminId);

                    return Results.Ok(booking);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to transfer booking: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Reschedule a booking — sets new times, status=rescheduled
            /// CALLS: useRescheduleBooking() → bookingService.rescheduleBooking() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Updates StartTime, EndTime, Reason, Status=rescheduled, RescheduledBy
            ///   - Sends email via BookingNotifier
            /// </summary>
            app.MapPut("/api/bookings/{id}/reschedule", [Authorize] async (int id, UpdateBookingTimesDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
            {
                try
                {
                    var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    var userId = int.Parse(context.User.FindFirst("id")!.Value);

                    if (string.IsNullOrEmpty(userRole)) return Results.Unauthorized();

                    var booking = await db.Bookings.FindAsync(id);
                    if (booking == null) return Results.NotFound("Booking not found");

                    if (userRole == "Coach" && booking.CoachId != userId)
                        return Results.StatusCode(403);
                    if (userRole != "Coach")
                    {
                        var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                        if (accessCheck != null) return accessCheck;
                    }

                    if (dto.StartTime >= dto.EndTime)
                        return Results.BadRequest("Invalid time range");

                    if (booking.AdminAvailabilityId.HasValue)
                    {
                        var availability = await db.AdminAvailabilities.FindAsync(booking.AdminAvailabilityId.Value);
                        if (availability != null && (dto.StartTime < availability.StartTime || dto.EndTime > availability.EndTime))
                            return Results.BadRequest("New times are outside the availability window");
                    }

                    booking.StartTime = dto.StartTime;
                    booking.EndTime = dto.EndTime;
                    booking.Reason = dto.Reason ?? "";
                    booking.Status = "rescheduled";
                    booking.RescheduledBy = dto.RescheduledBy;
                    await db.SaveChangesAsync();

                    var notifier = new BookingNotifier(emailService, db);
                    await notifier.NotifyRescheduled(booking, userRole);

                    return Results.Ok(booking);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to reschedule booking: " + ex.Message, statusCode: 500);
                }
            });
        }
    }
}
