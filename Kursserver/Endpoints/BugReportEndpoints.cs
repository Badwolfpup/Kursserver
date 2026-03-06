using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Endpoints
{
    public static class BugReportEndpoints
    {
        public static void MapBugReportEndpoints(this WebApplication app)
        {
            /// <summary>
            /// SCENARIO: User submits a bug report or idea
            /// CALLS: useSubmitBugReport() → bugReportService.submitBugReport() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Creates BugReport record
            /// </summary>
            app.MapPost("/api/bug-reports", [Authorize] async ([FromBody] AddBugReportDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var userId = new FromClaims().GetUserId(context);

                    var report = new BugReport
                    {
                        Type = dto.Type,
                        Content = dto.Content,
                        SenderId = userId,
                    };

                    db.BugReports.Add(report);
                    await db.SaveChangesAsync();

                    return Results.Created($"/api/bug-reports/{report.Id}", report);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to submit bug report: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Admin or teacher fetches all bug reports and ideas
            /// CALLS: useBugReports() → bugReportService.getBugReports() (kurshemsida)
            /// SIDE EFFECTS: none (read-only)
            /// </summary>
            app.MapGet("/api/bug-reports", [Authorize] async (ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var reports = await db.BugReports
                        .Include(b => b.Sender)
                        .OrderByDescending(b => b.CreatedAt)
                        .Select(b => new
                        {
                            b.Id,
                            b.Type,
                            b.Content,
                            b.SenderId,
                            SenderName = b.Sender != null ? b.Sender.FirstName + " " + b.Sender.LastName : "",
                            b.CreatedAt,
                        })
                        .ToListAsync();

                    return Results.Ok(reports);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch bug reports: " + ex.Message, statusCode: 500);
                }
            });
        }
    }
}
