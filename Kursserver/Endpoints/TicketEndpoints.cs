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
                    }).ToListAsync();

                    return Results.Ok(tickets);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch tickets: " + ex.Message, statusCode: 500);
                }
            });

            // POST /api/add-ticket
            app.MapPost("/api/add-ticket", [Authorize] async ([FromBody] AddTicketDto dto, ApplicationDbContext db, HttpContext context) =>
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

                    // Also remove replies
                    var replies = db.TicketReplies.Where(r => r.TicketId == id);
                    db.TicketReplies.RemoveRange(replies);
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
            app.MapPost("/api/add-ticket-reply", [Authorize] async ([FromBody] AddTicketReplyDto dto, ApplicationDbContext db, HttpContext context) =>
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
                    return Results.Created($"/api/fetch-ticket-replies/{reply.TicketId}", reply);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to add reply: " + ex.Message, statusCode: 500);
                }
            });
        }
    }
}
