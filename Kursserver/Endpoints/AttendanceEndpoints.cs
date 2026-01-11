using Kursserver.Dto;
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
            app.MapGet("/api/weekly-attendance/{date}", async (string date, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 4, 0);
                    if (accessCheck != null) return accessCheck;
                    DateTime monday = GetMonday(date);
                    var attendance = await db.Attendances.Where(x => x.Date >= monday && x.Date < monday.AddDays(7)).GroupBy(x => x.UserId).ToListAsync();
                    return Results.Ok(attendance.Select(x => new
                    {
                        Id = x.Key,
                        AttendedDays = x.Select(a => a.Date).ToList()
                    }));
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch attendance: " + ex.Message, statusCode: 500);
                }
            });

            app.MapPut("/api/update-attendance", async (UpdateAttendanceDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 4);
                    if (accessCheck != null) return accessCheck;
                    //var userId = int.Parse(context.User.FindFirst("id")?.Value ?? "0");
                    if (dto.Date == null) return Results.Problem("No date or date in wrong format");
                    if (dto.UserId == null || dto.UserId == 0) return Results.Problem("No userid supplied");
                    var hasDate = await db.Attendances.FirstOrDefaultAsync(x => x.UserId == dto.UserId && x.Date == dto.Date.Date);
                    if (hasDate == null)
                    {
                        var newattendance = new Attendance
                        {
                            UserId = dto.UserId,
                            Date = dto.Date.Date,
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
                return date.AddDays(-diff).Date;
            }
            return DateTime.Today;
        }
    }
}

