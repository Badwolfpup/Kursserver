using Kursserver.Models;
using System.Security.Claims;

namespace Kursserver.Utils
{
    public static class HasAdminPriviligies
    {

        public static IResult? IsTeacher(HttpContext context, int affectedUserRole)
        {
            var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userRole) || !Enum.TryParse(userRole, out Role role)) return Results.Unauthorized();
            if ((Role)affectedUserRole == Role.Teacher && userRole != Role.Admin.ToString()) return Results.Unauthorized();
            if (role == Role.Admin || role == Role.Teacher) return null;
            return Results.StatusCode(403);
        }

        public static IResult? IsTeacher(HttpContext context, int affectedUserRole, int coach)
        {
            var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userRole) || !Enum.TryParse(userRole, out Role role)) return Results.Unauthorized();
            if ((Role)affectedUserRole == Role.Teacher && userRole != Role.Admin.ToString()) return Results.Unauthorized();
            if (role == Role.Admin || role == Role.Teacher || role == Role.Coach) return null;
            return Results.StatusCode(403);
        }
    }
}
