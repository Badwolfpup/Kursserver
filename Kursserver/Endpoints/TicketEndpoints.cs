using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Endpoints
{
    public static class TicketEndpoints
    {
        public static void MapTicketEndpoints(this WebApplication app)
        {
            // GET /api/fetch-tickets — admin/teacher sees all, coach sees own
            app.MapGet("/api/fetch-tickets", [Authorize] async (ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var userId = new FromClaims().GetUserId(context);
                    var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                    IQueryable<Ticket> query = db.Tickets
                        .Include(t => t.Sender)
                        .Include(t => t.Recipient)
                        .OrderByDescending(t => t.CreatedAt);

                    // Admin/Teacher see all tickets, Coach/Student see only their own
                    if (userRole != Role.Admin.ToString() && userRole != Role.Teacher.ToString())
                    {
                        query = query.Where(t => t.SenderId == userId || t.RecipientId == userId);
                    }

                    var tickets = await query.Select(t => new
                    {
                        t.Id,
                        t.Subject,
                        t.Message,
                        t.Type,
                        t.Status,
                        t.SenderId,
                        SenderName = t.Sender != null ? t.Sender.FirstName + " " + t.Sender.LastName : "",
                        t.RecipientId,
                        RecipientName = t.Recipient != null ? t.Recipient.FirstName + " " + t.Recipient.LastName : "",
                        t.CreatedAt,
                        t.UpdatedAt,
                        AcceptedStartTime = db.TicketTimeSuggestions
                            .Where(s => s.TicketId == t.Id && s.Status == "accepted")
                            .Select(s => (DateTime?)s.StartTime)
                            .FirstOrDefault(),
                        AcceptedEndTime = db.TicketTimeSuggestions
                            .Where(s => s.TicketId == t.Id && s.Status == "accepted")
                            .Select(s => (DateTime?)s.EndTime)
                            .FirstOrDefault(),
                        HasPendingSuggestion = db.TicketTimeSuggestions
                            .Any(s => s.TicketId == t.Id && s.Status == "pending"),
                        HasUnread = !db.TicketViews.Any(v => v.UserId == userId && v.TicketId == t.Id)
                            ? db.TicketReplies.Any(r => r.TicketId == t.Id && r.SenderId != userId)
                              || db.TicketTimeSuggestions.Any(s => s.TicketId == t.Id && s.SuggestedById != userId)
                            : db.TicketReplies.Any(r => r.TicketId == t.Id && r.SenderId != userId
                                && r.CreatedAt > db.TicketViews.Where(v => v.UserId == userId && v.TicketId == t.Id).Select(v => v.LastViewedAt).First())
                              || db.TicketTimeSuggestions.Any(s => s.TicketId == t.Id && s.SuggestedById != userId
                                && s.CreatedAt > db.TicketViews.Where(v => v.UserId == userId && v.TicketId == t.Id).Select(v => v.LastViewedAt).First()),
                    }).ToListAsync();

                    return Results.Ok(tickets);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch tickets: " + ex.Message, statusCode: 500);
                }
            });

            // POST /api/add-ticket
            app.MapPost("/api/add-ticket", [Authorize] async ([FromBody] AddTicketDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
            {
                try
                {
                    var userId = new FromClaims().GetUserId(context);

                    // If no recipient specified, find first admin
                    int? recipientId = dto.RecipientId;
                    if (recipientId == null || recipientId == 0)
                    {
                        var admin = await db.Users.FirstOrDefaultAsync(u => u.AuthLevel == Role.Admin);
                        recipientId = admin?.Id;
                    }

                    var ticket = new Ticket
                    {
                        Subject = dto.Subject,
                        Message = dto.Message,
                        Type = dto.Type,
                        Status = "Open",
                        SenderId = userId,
                        RecipientId = recipientId,
                    };

                    db.Tickets.Add(ticket);
                    await db.SaveChangesAsync();

                    // Notify recipient
                    if (recipientId.HasValue)
                    {
                        var sender = await db.Users.FindAsync(userId);
                        var recipient = await db.Users.FindAsync(recipientId.Value);
                        if (recipient?.EmailNotifications == true)
                            emailService.SendEmailFireAndForget(recipient.Email, $"Nytt ärende: {dto.Subject}",
                                $"Du har fått ett nytt ärende från {sender?.FirstName} {sender?.LastName}.\n\n{dto.Message}");
                    }

                    return Results.Created($"/api/fetch-tickets/{ticket.Id}", ticket);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to add ticket: " + ex.Message, statusCode: 500);
                }
            });

            // PUT /api/update-ticket — update status or reassign
            app.MapPut("/api/update-ticket", [Authorize] async ([FromBody] UpdateTicketDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var ticket = await db.Tickets.FindAsync(dto.Id);
                    if (ticket == null) return Results.NotFound("Ticket not found");

                    if (!string.IsNullOrEmpty(dto.Status)) ticket.Status = dto.Status;
                    if (dto.RecipientId.HasValue) ticket.RecipientId = dto.RecipientId.Value;
                    ticket.UpdatedAt = DateTime.Now;

                    db.Tickets.Update(ticket);
                    await db.SaveChangesAsync();
                    return Results.Ok(ticket);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to update ticket: " + ex.Message, statusCode: 500);
                }
            });

            // DELETE /api/delete-ticket/{id}
            app.MapDelete("/api/delete-ticket/{id}", [Authorize] async (int id, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var ticket = await db.Tickets.FindAsync(id);
                    if (ticket == null) return Results.NotFound("Ticket not found");

                    // Also remove replies and time suggestions
                    var replies = db.TicketReplies.Where(r => r.TicketId == id);
                    db.TicketReplies.RemoveRange(replies);
                    var suggestions = db.TicketTimeSuggestions.Where(s => s.TicketId == id);
                    db.TicketTimeSuggestions.RemoveRange(suggestions);
                    db.Tickets.Remove(ticket);
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to delete ticket: " + ex.Message, statusCode: 500);
                }
            });

            // GET /api/fetch-ticket-replies/{ticketId}
            app.MapGet("/api/fetch-ticket-replies/{ticketId}", [Authorize] async (int ticketId, ApplicationDbContext db) =>
            {
                try
                {
                    var replies = await db.TicketReplies
                        .Where(r => r.TicketId == ticketId)
                        .Include(r => r.Sender)
                        .OrderBy(r => r.CreatedAt)
                        .Select(r => new
                        {
                            r.Id,
                            r.TicketId,
                            r.SenderId,
                            SenderName = r.Sender != null ? r.Sender.FirstName + " " + r.Sender.LastName : "",
                            r.Message,
                            r.CreatedAt,
                        }).ToListAsync();

                    return Results.Ok(replies);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch replies: " + ex.Message, statusCode: 500);
                }
            });

            // POST /api/add-ticket-reply
            app.MapPost("/api/add-ticket-reply", [Authorize] async ([FromBody] AddTicketReplyDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
            {
                try
                {
                    var userId = new FromClaims().GetUserId(context);

                    var reply = new TicketReply
                    {
                        TicketId = dto.TicketId,
                        SenderId = userId,
                        Message = dto.Message,
                    };

                    // Update ticket timestamp
                    var ticket = await db.Tickets.FindAsync(dto.TicketId);
                    if (ticket != null) ticket.UpdatedAt = DateTime.Now;

                    db.TicketReplies.Add(reply);
                    await db.SaveChangesAsync();

                    // Notify the other party
                    if (ticket != null)
                    {
                        var otherUserId = (userId == ticket.SenderId) ? ticket.RecipientId : ticket.SenderId;
                        if (otherUserId.HasValue)
                        {
                            var otherUser = await db.Users.FindAsync(otherUserId.Value);
                            if (otherUser?.EmailNotifications == true)
                                emailService.SendEmailFireAndForget(otherUser.Email, $"Nytt svar: {ticket.Subject}",
                                    $"Nytt svar på ärendet '{ticket.Subject}':\n\n{dto.Message}");
                        }
                    }

                    return Results.Created($"/api/fetch-ticket-replies/{reply.TicketId}", reply);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to add reply: " + ex.Message, statusCode: 500);
                }
            });

            // POST /api/ticket-time-suggestion — admin creates a time suggestion for a ticket
            app.MapPost("/api/ticket-time-suggestion", [Authorize] async ([FromBody] AddTicketTimeSuggestionDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var ticket = await db.Tickets.FindAsync(dto.TicketId);
                    if (ticket == null) return Results.NotFound("Ticket not found");

                    // Reject if there's already a pending suggestion for this ticket
                    var hasPending = await db.TicketTimeSuggestions
                        .AnyAsync(s => s.TicketId == dto.TicketId && s.Status == "pending");
                    if (hasPending) return Results.Conflict("A pending suggestion already exists for this ticket");

                    var userId = new FromClaims().GetUserId(context);

                    var suggestion = new TicketTimeSuggestion
                    {
                        TicketId = dto.TicketId,
                        SuggestedById = userId,
                        StartTime = dto.StartTime,
                        EndTime = dto.EndTime,
                        Status = "pending",
                    };

                    // Set ticket to InProgress
                    ticket.Status = "InProgress";
                    ticket.UpdatedAt = DateTime.Now;

                    db.TicketTimeSuggestions.Add(suggestion);
                    await db.SaveChangesAsync();

                    // Notify ticket sender
                    var sender = await db.Users.FindAsync(ticket.SenderId);
                    if (sender?.EmailNotifications == true)
                        emailService.SendEmailFireAndForget(sender.Email, "Tidförslag för ärende",
                            $"Du har fått ett tidförslag: {suggestion.StartTime:g}–{suggestion.EndTime:t}. Logga in för att svara.");

                    return Results.Created($"/api/ticket-time-suggestions/{dto.TicketId}", suggestion);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to create time suggestion: " + ex.Message, statusCode: 500);
                }
            });

            // PUT /api/ticket-time-suggestion/{id}/respond — student accepts or declines
            app.MapPut("/api/ticket-time-suggestion/{id}/respond", [Authorize] async (int id, [FromBody] RespondToTimeSuggestionDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
            {
                try
                {
                    var suggestion = await db.TicketTimeSuggestions
                        .Include(s => s.Ticket)
                        .FirstOrDefaultAsync(s => s.Id == id);
                    if (suggestion == null) return Results.NotFound("Suggestion not found");
                    if (suggestion.Status != "pending") return Results.BadRequest("Suggestion is no longer pending");

                    if (dto.Accept)
                    {
                        suggestion.Status = "accepted";
                        if (suggestion.Ticket != null)
                        {
                            suggestion.Ticket.Status = "Closed";
                            suggestion.Ticket.UpdatedAt = DateTime.Now;
                        }

                        // Create a Booking so the slot appears in the admin calendar
                        // and is blocked for coach scheduling.
                        var adminId = suggestion.Ticket?.RecipientId;
                        if (adminId.HasValue)
                        {
                            var booking = new Booking
                            {
                                AdminId = adminId.Value,
                                CoachId = adminId.Value,
                                StudentId = suggestion.Ticket!.SenderId,
                                AdminAvailabilityId = null,
                                Note = $"Handledning (ärende #{suggestion.TicketId})",
                                MeetingType = "session",
                                StartTime = suggestion.StartTime,
                                EndTime = suggestion.EndTime,
                                BookedAt = DateTime.Now,
                                Seen = false,
                                Status = "accepted",
                            };
                            db.Bookings.Add(booking);
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(dto.DeclineReason))
                            return Results.BadRequest("A reason is required when declining");

                        suggestion.Status = "declined";
                        suggestion.DeclineReason = dto.DeclineReason;
                        if (suggestion.Ticket != null)
                        {
                            suggestion.Ticket.UpdatedAt = DateTime.Now;
                        }
                    }

                    await db.SaveChangesAsync();

                    // Notify ticket recipient (admin) of the response
                    var recipientId = suggestion.Ticket?.RecipientId;
                    if (recipientId.HasValue)
                    {
                        var recipient = await db.Users.FindAsync(recipientId.Value);
                        if (recipient?.EmailNotifications == true)
                        {
                            if (dto.Accept)
                                emailService.SendEmailFireAndForget(recipient.Email, "Tidförslag accepterat",
                                    $"Tidförslaget {suggestion.StartTime:g}–{suggestion.EndTime:t} har accepterats.");
                            else
                                emailService.SendEmailFireAndForget(recipient.Email, "Tidförslag nekat",
                                    $"Tidförslaget {suggestion.StartTime:g}–{suggestion.EndTime:t} har nekats. Anledning: {dto.DeclineReason}");
                        }
                    }

                    return Results.Ok(suggestion);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to respond to suggestion: " + ex.Message, statusCode: 500);
                }
            });

            // GET /api/ticket-time-suggestions/{ticketId} — get all suggestions for a ticket
            app.MapGet("/api/ticket-time-suggestions/{ticketId}", [Authorize] async (int ticketId, ApplicationDbContext db) =>
            {
                try
                {
                    var suggestions = await db.TicketTimeSuggestions
                        .Where(s => s.TicketId == ticketId)
                        .Include(s => s.SuggestedBy)
                        .OrderByDescending(s => s.CreatedAt)
                        .Select(s => new
                        {
                            s.Id,
                            s.TicketId,
                            s.SuggestedById,
                            SuggestedByName = s.SuggestedBy != null ? s.SuggestedBy.FirstName + " " + s.SuggestedBy.LastName : "",
                            s.StartTime,
                            s.EndTime,
                            s.Status,
                            s.DeclineReason,
                            s.CreatedAt,
                        }).ToListAsync();

                    return Results.Ok(suggestions);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch suggestions: " + ex.Message, statusCode: 500);
                }
            });

            // POST /api/ticket-view/{ticketId} — upsert last viewed timestamp for a ticket
            app.MapPost("/api/ticket-view/{ticketId}", [Authorize] async (int ticketId, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var userId = new FromClaims().GetUserId(context);
                    var view = await db.TicketViews.FirstOrDefaultAsync(v => v.UserId == userId && v.TicketId == ticketId);
                    if (view == null)
                        db.TicketViews.Add(new TicketView { UserId = userId, TicketId = ticketId });
                    else
                        view.LastViewedAt = DateTime.Now;
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to mark ticket as viewed: " + ex.Message, statusCode: 500);
                }
            });
        }
    }
}
