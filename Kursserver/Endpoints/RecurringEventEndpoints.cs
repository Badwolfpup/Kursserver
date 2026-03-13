using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Kursserver.Endpoints
{
    public static class RecurringEventEndpoints
    {
        public static void MapRecurringEventEndpoints(this WebApplication app)
        {
            /// <summary>
            /// SCENARIO: Any authenticated user fetches recurring event instances for a date range (NoClass days filtered out)
            /// CALLS: useRecurringEvents() → recurringEventService.getInstances() (kurshemsida)
            /// SIDE EFFECTS: none (read-only)
            /// </summary>
            app.MapGet("/api/recurring-events", [Authorize] async (DateTime from, DateTime to, ApplicationDbContext db) =>
            {
                try
                {
                    var events = await db.RecurringEvents.ToListAsync();
                    var exceptions = await db.RecurringEventExceptions.ToListAsync();
                    var noClassDates = await db.NoClasses.Select(n => n.Date.Date).ToListAsync();

                    var instances = RecurringEventExpander.ExpandAll(events, from, to, exceptions, noClassDates);
                    return Results.Ok(instances);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch recurring events: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher creates a recurring event (optional AdminId override for assigning another teacher)
            /// CALLS: useCreateRecurringEvent() → recurringEventService.create() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Creates RecurringEvent record with AdminId = dto.AdminId ?? JWT user
            ///   - Optional Classroom (positive integer) stored on the event
            /// </summary>
            app.MapPost("/api/recurring-events", [Authorize] async (CreateRecurringEventDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var userId = int.Parse(context.User.FindFirst("id")!.Value);

                    if (dto.StartTime >= dto.EndTime)
                        return Results.BadRequest("Start time must be before end time");

                    if (dto.Frequency != "weekly" && dto.Frequency != "biweekly")
                        return Results.BadRequest("Frequency must be 'weekly' or 'biweekly'");

                    if (dto.Classroom.HasValue && dto.Classroom.Value < 1)
                        return Results.BadRequest("Classroom must be a positive integer");

                    var ev = new RecurringEvent
                    {
                        Name = dto.Name,
                        Weekday = dto.Weekday,
                        StartTime = dto.StartTime,
                        EndTime = dto.EndTime,
                        Frequency = dto.Frequency,
                        StartDate = dto.StartDate,
                        AdminId = dto.AdminId ?? userId,
                        Classroom = dto.Classroom,
                        CreatedAt = DateTime.Now
                    };

                    db.RecurringEvents.Add(ev);
                    await db.SaveChangesAsync();

                    return Results.Created($"/api/recurring-events/{ev.Id}", ev);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to create recurring event: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher updates a recurring event definition. Admin(authLevel=1)=all, Teacher=own.
            /// CALLS: useUpdateRecurringEvent() → recurringEventService.update() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Updates RecurringEvent fields including optional Classroom
            /// </summary>
            app.MapPut("/api/recurring-events/{id}", [Authorize] async (int id, UpdateRecurringEventDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var userId = int.Parse(context.User.FindFirst("id")!.Value);
                    var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;

                    var ev = await db.RecurringEvents.FindAsync(id);
                    if (ev == null) return Results.NotFound("Recurring event not found");

                    // Teachers can only edit their own
                    if (userRole == "Teacher" && ev.AdminId != userId)
                        return Results.StatusCode(403);

                    if (dto.Name != null) ev.Name = dto.Name;
                    if (dto.Weekday.HasValue) ev.Weekday = dto.Weekday.Value;
                    if (dto.StartTime.HasValue) ev.StartTime = dto.StartTime.Value;
                    if (dto.EndTime.HasValue) ev.EndTime = dto.EndTime.Value;
                    if (dto.Frequency != null)
                    {
                        if (dto.Frequency != "weekly" && dto.Frequency != "biweekly")
                            return Results.BadRequest("Frequency must be 'weekly' or 'biweekly'");
                        ev.Frequency = dto.Frequency;
                    }

                    if (dto.Classroom.HasValue)
                    {
                        if (dto.Classroom.Value < 1)
                            return Results.BadRequest("Classroom must be a positive integer");
                        ev.Classroom = dto.Classroom.Value;
                    }

                    await db.SaveChangesAsync();
                    return Results.Ok(ev);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to update recurring event: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher deletes a recurring event (and all its exceptions via cascade)
            /// CALLS: useDeleteRecurringEvent() → recurringEventService.delete() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Removes RecurringEvent + all RecurringEventException records (cascade)
            /// </summary>
            app.MapDelete("/api/recurring-events/{id}", [Authorize] async (int id, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var userId = int.Parse(context.User.FindFirst("id")!.Value);
                    var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;

                    var ev = await db.RecurringEvents.FindAsync(id);
                    if (ev == null) return Results.NotFound("Recurring event not found");

                    // Teachers can only delete their own
                    if (userRole == "Teacher" && ev.AdminId != userId)
                        return Results.StatusCode(403);

                    db.RecurringEvents.Remove(ev);
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to delete recurring event: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher creates or updates a single-instance exception for a recurring event
            /// CALLS: useRecurringEventException() → recurringEventService.setException() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Creates or updates RecurringEventException for the given event + date
            /// </summary>
            app.MapPut("/api/recurring-events/{id}/exceptions/{date}", [Authorize] async (int id, DateTime date, RecurringEventExceptionDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var userId = int.Parse(context.User.FindFirst("id")!.Value);
                    var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;

                    var ev = await db.RecurringEvents.FindAsync(id);
                    if (ev == null) return Results.NotFound("Recurring event not found");

                    if (userRole == "Teacher" && ev.AdminId != userId)
                        return Results.StatusCode(403);

                    var existing = await db.RecurringEventExceptions
                        .FirstOrDefaultAsync(e => e.RecurringEventId == id && e.Date.Date == date.Date);

                    if (existing != null)
                    {
                        existing.IsDeleted = dto.IsDeleted;
                        existing.Name = dto.Name;
                        existing.StartTime = dto.StartTime;
                        existing.EndTime = dto.EndTime;
                    }
                    else
                    {
                        db.RecurringEventExceptions.Add(new RecurringEventException
                        {
                            RecurringEventId = id,
                            Date = date.Date,
                            IsDeleted = dto.IsDeleted,
                            Name = dto.Name,
                            StartTime = dto.StartTime,
                            EndTime = dto.EndTime
                        });
                    }

                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to update recurring event exception: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Admin/Teacher removes a single-instance exception (restores original occurrence)
            /// CALLS: useRemoveRecurringEventException() → recurringEventService.removeException() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Removes RecurringEventException for the given event + date
            /// </summary>
            app.MapDelete("/api/recurring-events/{id}/exceptions/{date}", [Authorize] async (int id, DateTime date, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var userId = int.Parse(context.User.FindFirst("id")!.Value);
                    var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;

                    var ev = await db.RecurringEvents.FindAsync(id);
                    if (ev == null) return Results.NotFound("Recurring event not found");

                    if (userRole == "Teacher" && ev.AdminId != userId)
                        return Results.StatusCode(403);

                    var exception = await db.RecurringEventExceptions
                        .FirstOrDefaultAsync(e => e.RecurringEventId == id && e.Date.Date == date.Date);

                    if (exception == null) return Results.NotFound("Exception not found");

                    db.RecurringEventExceptions.Remove(exception);
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to remove recurring event exception: " + ex.Message, statusCode: 500);
                }
            });
        }
    }
}
