using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Endpoints
{
    public static class NoClassEndpoints
    {
        public static void MapNoClassEndpoints(this WebApplication app)
        {
            app.MapGet("api/noclass", async (ApplicationDbContext db) =>
            {
                return await db.NoClasses.ToListAsync();
            });
            app.MapPost("api/noclass/{date}", async (DateTime date, ApplicationDbContext db) =>
            {
                try
                {
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
