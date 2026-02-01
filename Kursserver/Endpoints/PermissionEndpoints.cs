using Kursserver.Dto;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;

namespace Kursserver.Endpoints
{
    public static class PermissionEndpoints
    {
        public static void MapPermissionEndpoints(this WebApplication app)
        {
            app.MapPost("api/fetch-user-permissions", [Authorize] async (FetchPermissionDto dto, ApplicationDbContext db) =>
            {
                var user = db.Users.FirstOrDefault(x => x.Email == dto.Email);
                if (user == null) return Results.NotFound("User not found");
                try
                {
                    var permissions = db.Permissions.SingleOrDefault(x => x.UserId == user.Id);
                    if (permissions == null) return Results.NotFound("Permissions not found");
                    return Results.Ok(permissions);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch user: " + ex.Message, statusCode: 500);
                }

            });

            app.MapPut("api/update-user-permissions", [Authorize] async (UpdatePermissionDto dto, ApplicationDbContext db) =>
            {
                var user = await db.Users.FindAsync(dto.UserId);
                if (user == null) return Results.NotFound("User not found");
                try
                {
                    var permissions = db.Permissions.SingleOrDefault(x => x.UserId == user.Id);
                    if (permissions == null) return Results.NotFound("Permissions not found");

                    if (dto.Loops.HasValue) permissions.Loops = dto.Loops.Value;
                    if (dto.Css.HasValue) permissions.Css = dto.Css.Value;
                    if (dto.Objects.HasValue) permissions.Objects = dto.Objects.Value;
                    if (dto.Javascript.HasValue) permissions.Javascript = dto.Javascript.Value;
                    if (dto.Arrays.HasValue) permissions.Arrays = dto.Arrays.Value;
                    if (dto.Conditionals.HasValue) permissions.Conditionals = dto.Conditionals.Value;
                    if (dto.Variable.HasValue) permissions.Variable = dto.Variable.Value;
                    if (dto.Functions.HasValue) permissions.Functions = dto.Functions.Value;
                    if (dto.Html.HasValue) permissions.Html = dto.Html.Value;

                    db.Update(permissions);
                    await db.SaveChangesAsync();

                    return Results.Ok(permissions);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch user: " + ex.Message, statusCode: 500);
                }
            });
        }
    }
}
