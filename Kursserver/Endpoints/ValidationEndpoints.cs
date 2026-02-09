using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace Kursserver.Endpoints
{
    public static class ValidationEndpoints
    {
        public static readonly ConcurrentDictionary<string, int> passcodeStore = new();
        public static readonly ConcurrentDictionary<string, int> passcodeAttempts = new();
        public static readonly ConcurrentDictionary<string, DateTime> passcodeLockout = new();


        public static void MapValidationEndpoints(this WebApplication app, IConfiguration jwtSettings)
        {
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);


            app.MapPost("api/email-validation", async (ValidateEmailDto dto, ApplicationDbContext db, EmailService email) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);
                if (user == null && dto.Email != "guest@guest.com") return Results.NotFound("Email not found.");
                else
                {
                    if (app.Environment.IsDevelopment())
                    {
                        if (dto.Email == "guest@guest.com")
                        {
                            int freecode = Random.Shared.Next(100000, 999999);
                            passcodeStore[dto.Email] = 122334;
                            return Results.Ok("guest");
                        }
                        int passcode = Random.Shared.Next(100000, 999999);
                        passcodeStore[user.Email] = passcode;
                        return Results.Ok(passcode);
                    }
                    else
                    {
                        if (dto.Email == "guest@guest.com")
                        {
                            int freecode = Random.Shared.Next(100000, 999999);
                            passcodeStore[dto.Email] = 122334;
                            return Results.Ok();
                        }
                        if (user.AuthLevel == Role.Student) return Results.NotFound("Email not found.");
                        int passcode = Random.Shared.Next(100000, 999999);
                        passcodeStore[user.Email] = passcode;
                        passcodeAttempts[user.Email] = passcodeAttempts.ContainsKey(dto.Email) ? passcodeAttempts[dto.Email]++ : 1;
                        await email.SendEmailAsync(dto.Email, "Lösenkod CUL Programmering", $"Din lösenkod är : {passcode}");
                        return Results.Ok("Passcode sent to your email.");
                    }
                }
            });

            app.MapPost("api/passcode-validation", async (ValidatePasscodeDto dto, HttpContext httpContext, ApplicationDbContext db) =>
            {
                if (passcodeAttempts.ContainsKey(dto.Email) && passcodeAttempts[dto.Email] >= 10)
                {
                    if (passcodeLockout.ContainsKey(dto.Email))
                    {
                        var lockoutTime = passcodeLockout[dto.Email];
                        if ((DateTime.UtcNow - lockoutTime).TotalMinutes < 15)
                        {
                            return Results.Problem($"För många försök. Försök igen om {(DateTime.UtcNow - lockoutTime).TotalMinutes.ToString()}");
                        }
                        else
                        {
                            passcodeAttempts[dto.Email] = 0;
                            passcodeLockout.TryRemove(dto.Email, out _);
                        }
                    }
                    else
                    {
                        passcodeLockout[dto.Email] = DateTime.UtcNow;
                        return Results.Problem($"För många försök. Försök igen om 15 minuter");
                    }
                }
                if (passcodeStore.ContainsKey(dto.Email) && passcodeStore[dto.Email] == dto.Passcode)
                {
                    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                    if (user == null && dto.Email != "guest@guest.com") return Results.NotFound();
                    if (dto.Email != "guest@guest.com")
                    {
                        var claims = new[]
                        {
                        new Claim(JwtRegisteredClaimNames.Sub, dto.Email),
                        new Claim("id", user.Id.ToString()),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, user.AuthLevel.ToString())
                        };

                        var token = new JwtSecurityToken(
                            issuer: jwtSettings["Issuer"],
                            audience: jwtSettings["Audience"],
                            claims: claims,
                            expires: DateTime.UtcNow.AddDays((int)user.AuthLevel <= 2 ? 30 : 6),
                            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
                        );
                        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                        passcodeStore.TryRemove(dto.Email, out _);
                        var cookieOptions = new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict
                        };
                        if (dto.RememberMe)
                        {
                            cookieOptions.Expires = DateTimeOffset.UtcNow.AddDays((int)user.AuthLevel <= 2 ? 30 : 6);
                        }
                        httpContext.Response.Cookies.Append("jwt", tokenString, cookieOptions);
                        return Results.Ok(new
                        {
                            Id = user.Id,
                            Email = user.Email,
                            Role = user.AuthLevel
                        });
                    }
                    else
                    {
                        var claims = new[]
                        {
                        new Claim(JwtRegisteredClaimNames.Sub, dto.Email),
                        new Claim("id", "0"),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "Guest")
                        };

                        var token = new JwtSecurityToken(
                            issuer: jwtSettings["Issuer"],
                            audience: jwtSettings["Audience"],
                            claims: claims,
                            expires: DateTime.UtcNow.AddHours(1),
                            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
                        );
                        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                        passcodeStore.TryRemove(dto.Email, out _);
                        var cookieOptions = new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict
                        };

                        httpContext.Response.Cookies.Append("jwt", tokenString, cookieOptions);
                        return Results.Ok(new
                        {
                            Id = 0,
                            Email = dto.Email,
                            Role = Role.Guest
                        });
                    }
                }
                else
                {
                    if (passcodeAttempts.ContainsKey(dto.Email))
                    {
                        passcodeAttempts[dto.Email]++;
                    }
                    else
                    {
                        passcodeAttempts[dto.Email] = 1;
                    }
                    return Results.Problem($"Felaktig lösenkod. {(passcodeAttempts[dto.Email] > 5 ? (10 - passcodeAttempts[dto.Email]).ToString() + " försök kvar" : "")}");
                }
            });

            app.MapGet("api/me", [Authorize] async (HttpContext context) =>
            {
                foreach (var claim in context.User.Claims)
                {
                    Console.WriteLine($"Claim Type: {claim.Type}, Value: {claim.Value}");
                }

                var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == "id");
                var emailClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                var roleClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

                return Results.Ok(new
                {
                    Id = userIdClaim?.Value,
                    Email = emailClaim?.Value,
                    Role = roleClaim?.Value
                });
            });

            app.MapPost("api/logout", [Authorize] (HttpContext context) =>
            {
                context.Response.Cookies.Append("jwt", "", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(-1)  // Set to past date
                });

                return Results.Ok(new { success = true, message = "Logged out successfully" });
            });
        }
    }
}
