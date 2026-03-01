using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Endpoints
{
    public static class NoClassEndpoints
    {
        public static void MapNoClassEndpoints(this WebApplication app)
        {
            /// <summary>
            /// SCENARIO: Admin, Teacher, or Coach fetches all no-class dates
            /// CALLS: useNoClass() → noClassService.fetchDates() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Returns 403 if caller is not Admin, Teacher, or Coach
            /// </summary>
            app.MapGet("api/noclass", [Authorize] async (ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1, 1);
                if (accessCheck != null) return accessCheck;
                return Results.Ok(await db.NoClasses.ToListAsync());
            });

            /// <summary>
            /// SCENARIO: Admin or Teacher toggles a no-class date (adds if absent, removes if present)
            /// CALLS: useNoClass() → noClassService.toggleDate() (kurshemsida)
            /// SIDE EFFECTS:
            ///   - Creates or removes NoClass record for the given date
            ///   - Returns 403 if caller is not Admin or Teacher
            /// </summary>
            app.MapPost("api/noclass/{date}", [Authorize] async (DateTime date, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;

                    var hasDate = db.NoClasses.FirstOrDefault(x => x.Date == date);
                    if (hasDate != null) db.NoClasses.Remove(hasDate);
                    else db.NoClasses.Add(new NoClass { Date = date });
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

        }
    }
}
