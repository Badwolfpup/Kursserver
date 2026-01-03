using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;

namespace Kursserver.Endpoints
{
    [Authorize]
    public static class AttendanceEndpoints
    {
        public static void MapAttendanceEndpoints(this WebApplication app)
        {
            app.MapGet("/api/weekly-attendance/{date}", async (string date, [FromKeyedServices] ApplicationDbContext db) =>
            {
                try
                {
                    DateTime monday = GetMonday(date);
                    var attendance = await db.Attendances.Where(x => x.Date >= monday && x.Date <= monday.AddDays(6)).GroupBy(x => x.UserId).ToListAsync();
                    return Results.Ok(attendance.Select(x => new
                    {
                        Id = x.Key,
                        AttendedDates = x.Select(a => a.Date).ToList()
                    }));
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch attendance: " + ex.Message, statusCode: 500);
                }
            });

            app.MapPut("/api/update-attendance/{date}", async (string date, [FromKeyedServices] ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var userId = int.Parse(context.User.FindFirst("id")?.Value ?? "0");
                    if (!DateTime.TryParse(date, out DateTime attendance)) return Results.Problem("No date or date in wrong format");
                    var hasDate = await db.Attendances.SingleOrDefaultAsync(x => x.UserId == userId && x.Date == attendance);
                    if (hasDate == null)
                    {
                        var newattendance = new Attendance
                        {
                            UserId = userId,
                            Date = attendance,
                        };
                        db.Attendances.Add(newattendance);
                        await db.SaveChangesAsync();
                        return Results.Ok(newattendance);
                    }
                    else
                    {
                        db.Remove(hasDate);
                        await db.SaveChangesAsync();
                        return Results.Ok();
                    }
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to update attendance: " + ex.Message, statusCode: 500);

                }
            });
        }



        private static DateTime GetMonday(string inputdate)
        {
            if (DateTime.TryParse(inputdate, out DateTime date))
            {
                int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
                return date.AddDays(-diff);
            }
            return DateTime.Today;
        }
    }
}

