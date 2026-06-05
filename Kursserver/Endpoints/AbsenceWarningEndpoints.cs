using System.Net;
using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Endpoints
{
    [Authorize]
    public static class AbsenceWarningEndpoints
    {
        public static void MapAbsenceWarningEndpoints(this WebApplication app)
        {
            app.MapGet("/api/absence-warning/last-attended/{studentId}", [Authorize] async (int studentId, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 4);
                if (accessCheck != null) return accessCheck;

                var lastDate = await db.Attendances
                    .Where(a => a.UserId == studentId)
                    .MaxAsync(a => (DateTime?)a.Date);

                return Results.Ok(new { lastAttendedDate = lastDate?.ToString("yyyy-MM-dd") });
            });

            /// <summary>
            /// SCENARIO: Send absence warning email to a student's coach
            /// CALLS: useSendAbsenceWarning (React)
            /// SIDE EFFECTS:
            /// - Sends email to coach via EmailService.SendEmailFireAndForget (Resend API, fire-and-forget)
            /// - No-op in development mode
            /// </summary>
            app.MapPost("/api/absence-warning/send", [Authorize] async (SendAbsenceWarningDto dto, ApplicationDbContext db, HttpContext context, IEmailService emailService) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 4);
                if (accessCheck != null) return accessCheck;

                if (string.IsNullOrWhiteSpace(dto.CoachEmail))
                    return Results.BadRequest("Coach email is required.");

                // Restrict the recipient to a registered coach so the app's sending domain
                // can't be used to mail arbitrary addresses.
                var isCoachEmail = await db.Users.AnyAsync(u => u.Email == dto.CoachEmail && u.AuthLevel == Role.Coach);
                if (!isCoachEmail)
                    return Results.BadRequest("Recipient must be a registered coach.");

                var sanitizedBody = WebUtility.HtmlEncode(dto.Body);
                emailService.SendEmailFireAndForget(dto.CoachEmail, dto.Subject, sanitizedBody);
                return Results.Ok();
            });
        }
    }
}
