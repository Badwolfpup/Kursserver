using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kursserver.Utils
{
    [Authorize]
    public class FromClaims: ControllerBase
    {
        public int GetUserId(HttpContext context)
        {
            var claims = context.User.FindFirst("id");
            if (claims == null) return 0;
            int userID = 0;
            if (int.TryParse(claims.Value, out userID))return userID;
            return userID;
        }
    }
}
