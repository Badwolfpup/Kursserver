using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Kursserver.Endpoints
{
    public static class MessageEndpoints
    {
        public static void MapMessageEndpoints(this WebApplication app)
        {
            /// <summary>
            /// SCENARIO: User fetches all their threads with last message and unread flag
            /// CALLS: useThreads() → messageService.getThreads() (kurshemsida)
            /// SIDE EFFECTS: none (read-only)
            /// </summary>
            app.MapGet("/api/threads", [Authorize] async (ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var userId = new FromClaims().GetUserId(context);
                    var role = context.User.FindFirst(ClaimTypes.Role)?.Value;

                    var query = db.Threads
                        .Include(t => t.User1)
                        .Include(t => t.User2)
                        .Include(t => t.StudentContext)
                        .Where(t => t.User1Id == userId || t.User2Id == userId);

                    query = await ApplyThreadVisibilityFilter(query, userId, role, db);

                    var threads = await query
                        .OrderByDescending(t => t.UpdatedAt)
                        .Select(t => new
                        {
                            t.Id,
                            t.User1Id,
                            User1Name = t.User1 != null ? t.User1.FirstName + " " + t.User1.LastName : "",
                            User1Role = t.User1 != null ? t.User1.AuthLevel.ToString() : "",
                            t.User2Id,
                            User2Name = t.User2 != null ? t.User2.FirstName + " " + t.User2.LastName : "",
                            User2Role = t.User2 != null ? t.User2.AuthLevel.ToString() : "",
                            t.StudentContextId,
                            StudentContextName = t.StudentContext != null
                                ? t.StudentContext.FirstName + " " + t.StudentContext.LastName
                                : (string?)null,
                            t.CreatedAt,
                            t.UpdatedAt,
                            LastMessage = db.Messages
                                .Where(m => m.ThreadId == t.Id)
                                .OrderByDescending(m => m.CreatedAt)
                                .Select(m => new
                                {
                                    m.Content,
                                    m.SenderId,
                                    SenderName = m.Sender != null ? m.Sender.FirstName : "",
                                    m.CreatedAt,
                                })
                                .FirstOrDefault(),
                            HasUnread = db.Messages.Any(m => m.ThreadId == t.Id) &&
                                (!db.ThreadViews.Any(v => v.UserId == userId && v.ThreadId == t.Id)
                                    ? db.Messages.Any(m => m.ThreadId == t.Id && m.SenderId != userId)
                                    : db.Messages.Any(m => m.ThreadId == t.Id && m.SenderId != userId
                                        && m.CreatedAt > db.ThreadViews
                                            .Where(v => v.UserId == userId && v.ThreadId == t.Id)
                                            .Select(v => v.LastViewedAt)
                                            .First())),
                        })
                        .ToListAsync();

                    return Results.Ok(threads);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch threads: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: User fetches paginated messages for a thread
            /// CALLS: useThreadMessages() → messageService.getThreadMessages() (kurshemsida)
            /// SIDE EFFECTS: none (read-only)
            /// </summary>
            app.MapGet("/api/threads/{id}/messages", [Authorize] async (int id, [FromQuery] int take, [FromQuery] int skip, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var userId = new FromClaims().GetUserId(context);
                    var thread = await db.Threads.FindAsync(id);
                    if (thread == null) return Results.NotFound("Thread not found");
                    if (thread.User1Id != userId && thread.User2Id != userId)
                        return Results.Forbid();

                    var messages = await db.Messages
                        .Where(m => m.ThreadId == id)
                        .OrderByDescending(m => m.CreatedAt)
                        .Skip(skip)
                        .Take(take)
                        .Select(m => new
                        {
                            m.Id,
                            m.ThreadId,
                            m.SenderId,
                            SenderName = m.Sender != null ? m.Sender.FirstName + " " + m.Sender.LastName : "",
                            m.Content,
                            m.CreatedAt,
                        })
                        .ToListAsync();

                    return Results.Ok(messages);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch messages: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: User sends a message to another user, creating thread lazily
            /// CALLS: useSendMessage() → messageService.sendMessage() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Creates Thread if first message between users
            ///   - Creates Message record
            ///   - Updates Thread.UpdatedAt
            ///   - Sends email to recipient if EmailNotifications = true (EmailService)
            /// </summary>
            app.MapPost("/api/messages", [Authorize] async ([FromBody] SendMessageDto dto, ApplicationDbContext db, HttpContext context, EmailService emailService) =>
            {
                try
                {
                    var senderId = new FromClaims().GetUserId(context);
                    if (string.IsNullOrWhiteSpace(dto.Content))
                        return Results.BadRequest("Message content cannot be empty");
                    if (senderId == dto.RecipientId)
                        return Results.BadRequest("Cannot message yourself");

                    // Enforce User1Id < User2Id for uniqueness
                    var user1Id = Math.Min(senderId, dto.RecipientId);
                    var user2Id = Math.Max(senderId, dto.RecipientId);

                    // Find or create thread
                    var thread = await db.Threads.FirstOrDefaultAsync(t =>
                        t.User1Id == user1Id &&
                        t.User2Id == user2Id &&
                        t.StudentContextId == dto.StudentContextId);

                    if (thread == null)
                    {
                        thread = new Models.Thread
                        {
                            User1Id = user1Id,
                            User2Id = user2Id,
                            StudentContextId = dto.StudentContextId,
                        };
                        db.Threads.Add(thread);
                        await db.SaveChangesAsync();
                    }

                    var message = new Message
                    {
                        ThreadId = thread.Id,
                        SenderId = senderId,
                        Content = dto.Content,
                    };

                    thread.UpdatedAt = DateTime.UtcNow;
                    db.Messages.Add(message);
                    await db.SaveChangesAsync();

                    // Notify recipient (skip in dev)
                    if (!app.Environment.IsDevelopment())
                    {
                        var recipient = await db.Users.FindAsync(dto.RecipientId);
                        if (recipient?.EmailNotifications == true)
                        {
                            var sender = await db.Users.FindAsync(senderId);
                            var senderName = sender != null ? $"{sender.FirstName} {sender.LastName}" : "Okänd";
                            emailService.SendEmailFireAndForget(
                                recipient.Email,
                                $"Nytt meddelande från {senderName}",
                                $"Du har fått ett nytt meddelande från {senderName}:\n\n{dto.Content}");
                        }
                    }

                    return Results.Ok(new { ThreadId = thread.Id, MessageId = message.Id });
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to send message: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: User marks a thread as viewed (upsert ThreadView.LastViewedAt)
            /// CALLS: useMarkThreadViewed() → messageService.markThreadViewed() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Upserts ThreadView.LastViewedAt
            /// </summary>
            app.MapPost("/api/threads/{id}/view", [Authorize] async (int id, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var userId = new FromClaims().GetUserId(context);
                    var thread = await db.Threads.FindAsync(id);
                    if (thread == null) return Results.NotFound("Thread not found");
                    if (thread.User1Id != userId && thread.User2Id != userId)
                        return Results.Forbid();

                    var view = await db.ThreadViews
                        .FirstOrDefaultAsync(v => v.UserId == userId && v.ThreadId == id);

                    if (view == null)
                        db.ThreadViews.Add(new Models.ThreadView
                        {
                            UserId = userId,
                            ThreadId = id,
                        });
                    else
                        view.LastViewedAt = DateTime.UtcNow;

                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to mark thread as viewed: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: User fetches total unread thread count for sidebar badge
            /// CALLS: useUnreadCount() → messageService.getUnreadCount() (kurshemsida)
            /// SIDE EFFECTS: none (read-only)
            /// </summary>
            app.MapGet("/api/threads/unread-count", [Authorize] async (ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var userId = new FromClaims().GetUserId(context);
                    var role = context.User.FindFirst(ClaimTypes.Role)?.Value;

                    var query = db.Threads
                        .Where(t => t.User1Id == userId || t.User2Id == userId);

                    query = await ApplyThreadVisibilityFilter(query, userId, role, db);

                    var unreadCount = await query.CountAsync(t =>
                        db.Messages.Any(m => m.ThreadId == t.Id) &&
                        (!db.ThreadViews.Any(v => v.UserId == userId && v.ThreadId == t.Id)
                            ? db.Messages.Any(m => m.ThreadId == t.Id && m.SenderId != userId)
                            : db.Messages.Any(m => m.ThreadId == t.Id && m.SenderId != userId
                                && m.CreatedAt > db.ThreadViews
                                    .Where(v => v.UserId == userId && v.ThreadId == t.Id)
                                    .Select(v => v.LastViewedAt)
                                    .First())));

                    return Results.Ok(new { count = unreadCount });
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch unread count: " + ex.Message, statusCode: 500);
                }
            });
        }

        private static async Task<IQueryable<Models.Thread>> ApplyThreadVisibilityFilter(
            IQueryable<Models.Thread> query, int userId, string? role, ApplicationDbContext db)
        {
            if (role == Role.Student.ToString())
                query = query.Where(t => t.StudentContextId == null);
            else if (role == Role.Coach.ToString())
            {
                var studentIds = await db.Users
                    .Where(u => u.CoachId == userId && u.AuthLevel == Role.Student)
                    .Select(u => u.Id)
                    .ToListAsync();

                query = query.Where(t =>
                    t.StudentContextId == null ||
                    studentIds.Contains(t.StudentContextId.Value));
            }
            return query;
        }
    }
}
