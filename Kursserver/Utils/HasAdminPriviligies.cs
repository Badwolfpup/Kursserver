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

        /// <summary>
        /// Authorization gate for user-management actions (create / delete / activate).
        /// The caller must be staff (Admin or Teacher). Acting on a *privileged* account
        /// — one whose role is Admin or Teacher — additionally requires the caller to be Admin.
        /// Unlike <see cref="IsTeacher(HttpContext,int)"/>, this guards Admin targets too,
        /// so it must be passed the *actual* affected role (not a sentinel value).
        /// </summary>
        public static IResult? CanManageUser(HttpContext context, Role affectedRole)
        {
            var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userRole) || !Enum.TryParse(userRole, out Role role)) return Results.Unauthorized();

            // Only Admin and Teacher may manage users at all.
            if (role != Role.Admin && role != Role.Teacher) return Results.StatusCode(403);

            // Touching a privileged account (Admin or Teacher) requires Admin.
            if ((affectedRole == Role.Admin || affectedRole == Role.Teacher) && role != Role.Admin) return Results.Unauthorized();

            return null;
        }
    }
}
