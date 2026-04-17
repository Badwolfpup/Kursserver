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
            /// SCENARIO: Authenticated user fetches bookings.
            ///   Admin/Teacher: all bookings.
            ///   Coach: own bookings (all statuses) + other coaches' accepted bookings masked as opaque busy blocks.
            /// CALLS: useBookings() → bookingService.getBookings() (kurshemsida)
            /// SIDE EFFECTS: none (read-only)
            /// </summary>
            app.MapGet("/api/bookings", [Authorize] async (ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    var userId = int.Parse(context.User.FindFirst("id")!.Value);

                    if (string.IsNullOrEmpty(userRole) || !Enum.TryParse<Role>(userRole, out var role))
                        return Results.Unauthorized();

                    if (role == Role.Coach)
                    {
                        var own = await db.Bookings
                            .Where(b => b.CoachId == userId)
                            .ToListAsync();

                        var othersAccepted = await db.Bookings
                            .Where(b => b.CoachId != userId
                                     && b.Status == BookingStatus.Accepted)
                            .Select(b => new Booking
                            {
                                Id = b.Id,
                                AdminId = b.AdminId,
                                StartTime = b.StartTime,
                                EndTime = b.EndTime,
                                Status = b.Status
                            })
                            .ToListAsync();

                        return Results.Ok(own.Concat(othersAccepted));
                    }

                    var bookings = await db.Bookings.ToListAsync();
                    return Results.Ok(bookings);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch bookings: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Any admin/teacher or coach creates a booking as a pending suggestion.
            ///   Status is always Pending regardless of creator — the non-creating party approves.
            ///   Returns 409 only if the time overlaps an existing Accepted booking for the same admin/coach (B1 hard block).
            ///   Overlaps with busy-time or recurring events are not blocked by the backend (frontend warns coaches).
            /// CALLS: useCreateBooking() → bookingService.createBooking() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Creates Booking with Status = Pending, CreatedByRole = creator's role
            ///   - Sends email notification via BookingNotifier
            ///   - Returns 409 on accepted-booking overlap
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
                    BookingActor creator;

                    if (role == Role.Admin || role == Role.Teacher)
                    {
                        bookingAdminId = dto.AdminId ?? userId;
                        creator = BookingActor.Admin;
                    }
                    else if (role == Role.Coach)
                    {
                        bookingCoachId = userId;

                        if (dto.StudentId.HasValue)
                        {
                            var student = await db.Users.FindAsync(dto.StudentId.Value);
                            if (student == null || student.CoachId != userId)
                                return Results.Forbid();
                        }

                        bookingAdminId = dto.AdminId ?? 0;
                        if (bookingAdminId == 0)
                            return Results.BadRequest("AdminId is required");
                        creator = BookingActor.Coach;
                    }
                    else
                    {
                        return Results.Forbid();
                    }

                    var conflicts = await ConflictDetection.CheckAcceptedBookingConflicts(
                        db, dto.StartTime, dto.EndTime,
                        adminId: bookingAdminId,
                        coachId: bookingCoachId);

                    if (conflicts.Count > 0)
                        return Results.Conflict(new { bookings = conflicts });

                    var booking = new Booking
                    {
                        AdminId = bookingAdminId,
                        CoachId = bookingCoachId,
                        StudentId = bookingStudentId,
                        Note = dto.Note,
                        MeetingType = dto.MeetingType,
                        StartTime = dto.StartTime,
                        EndTime = dto.EndTime,
                        BookedAt = DateTime.Now,
                        Seen = false,
                        Status = BookingStatus.Pending,
                        CreatedByRole = creator
                    };

                    db.Bookings.Add(booking);
                    await db.SaveChangesAsync();

                    var notifier = new BookingNotifier(emailService, db);
                    await notifier.NotifyBookingCreated(booking, creator);

                    return Results.Created($"/api/bookings/{booking.Id}", booking);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to create booking: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher or Coach responds to a pending booking — accept or decline.
            /// CALLS: useUpdateBookingStatus() → bookingService.updateStatus() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Sets booking Status + Reason
            ///   - Sends email via BookingNotifier
            /// </summary>
            app.MapPut("/api/bookings/{id}/status", [Authorize] async (int id, UpdateBookingStatusDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
            {
                try
                {
                    var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    var userId = int.Parse(context.User.FindFirst("id")!.Value);

                    if (string.IsNullOrEmpty(userRole) || !Enum.TryParse<Role>(userRole, out var role))
                        return Results.Unauthorized();

                    var booking = await db.Bookings.FindAsync(id);
                    if (booking == null) return Results.NotFound("Booking not found");

                    BookingActor responder;
                    if (role == Role.Coach)
                    {
                        if (booking.CoachId != userId) return Results.StatusCode(403);
                        if (booking.Status != BookingStatus.Pending) return Results.StatusCode(403);
                        responder = BookingActor.Coach;
                    }
                    else
                    {
                        var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                        if (accessCheck != null) return accessCheck;
                        responder = BookingActor.Admin;
                    }

                    if (dto.Status != BookingStatus.Accepted && dto.Status != BookingStatus.Declined)
                        return Results.BadRequest("Status must be Accepted or Declined");

                    booking.Status = dto.Status;
                    booking.Reason = dto.Reason ?? "";
                    await db.SaveChangesAsync();

                    var notifier = new BookingNotifier(emailService, db);
                    await notifier.NotifyStatusChanged(booking, responder, dto.Status, dto.Reason);

                    return Results.Ok(booking);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to update booking status: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Cancel a booking — sets Status = Declined. Admin any, Coach own.
            /// CALLS: useCancelBooking() → bookingService.cancelBooking() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Sets booking Status = Declined
            ///   - Sends email via BookingNotifier
            /// </summary>
            app.MapPut("/api/bookings/{id}/cancel", [Authorize] async (int id, UpdateBookingStatusDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
            {
                try
                {
                    var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    var userId = int.Parse(context.User.FindFirst("id")!.Value);

                    if (string.IsNullOrEmpty(userRole) || !Enum.TryParse<Role>(userRole, out var role))
                        return Results.Unauthorized();

                    var booking = await db.Bookings.FindAsync(id);
                    if (booking == null) return Results.NotFound("Booking not found");

                    BookingActor canceller;
                    if (role == Role.Coach)
                    {
                        if (booking.CoachId != userId) return Results.StatusCode(403);
                        canceller = BookingActor.Coach;
                    }
                    else
                    {
                        var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                        if (accessCheck != null) return accessCheck;
                        canceller = BookingActor.Admin;
                    }

                    booking.Status = BookingStatus.Declined;
                    booking.Reason = dto.Reason ?? "";
                    await db.SaveChangesAsync();

                    var notifier = new BookingNotifier(emailService, db);
                    await notifier.NotifyCancelled(booking, canceller, dto.Reason);

                    return Results.Ok(booking);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to cancel booking: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher transfers a booking to another teacher.
            /// CALLS: useTransferBooking() → bookingService.transferBooking() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Updates booking.AdminId to TargetAdminId
            ///   - If booking was pending, sets status to Accepted
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

                    var conflicts = await ConflictDetection.CheckAcceptedBookingConflicts(
                        db, booking.StartTime, booking.EndTime,
                        adminId: dto.TargetAdminId,
                        excludeBookingId: booking.Id);

                    if (conflicts.Count > 0)
                        return Results.Conflict("Target teacher already has a booking at this time");

                    var oldAdminId = booking.AdminId;
                    booking.AdminId = dto.TargetAdminId;

                    if (booking.Status == BookingStatus.Pending)
                        booking.Status = BookingStatus.Accepted;

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
            /// SCENARIO: Reschedule a booking — sets new times, resets status to Pending for re-approval.
            /// CALLS: useRescheduleBooking() → bookingService.rescheduleBooking() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Updates StartTime, EndTime, Reason
            ///   - Sets Status = Pending and RescheduledBy = caller's role
            ///   - Sends email via BookingNotifier
            /// </summary>
            app.MapPut("/api/bookings/{id}/reschedule", [Authorize] async (int id, UpdateBookingTimesDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
            {
                try
                {
                    var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    var userId = int.Parse(context.User.FindFirst("id")!.Value);

                    if (string.IsNullOrEmpty(userRole) || !Enum.TryParse<Role>(userRole, out var role))
                        return Results.Unauthorized();

                    var booking = await db.Bookings.FindAsync(id);
                    if (booking == null) return Results.NotFound("Booking not found");

                    BookingActor rescheduler;
                    if (role == Role.Coach)
                    {
                        if (booking.CoachId != userId) return Results.StatusCode(403);
                        rescheduler = BookingActor.Coach;
                    }
                    else
                    {
                        var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                        if (accessCheck != null) return accessCheck;
                        rescheduler = BookingActor.Admin;
                    }

                    if (dto.StartTime >= dto.EndTime)
                        return Results.BadRequest("Invalid time range");

                    booking.StartTime = dto.StartTime;
                    booking.EndTime = dto.EndTime;
                    booking.Reason = dto.Reason ?? "";
                    booking.Status = BookingStatus.Pending;
                    booking.RescheduledBy = rescheduler;
                    await db.SaveChangesAsync();

                    var notifier = new BookingNotifier(emailService, db);
                    await notifier.NotifyRescheduled(booking, rescheduler);

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
