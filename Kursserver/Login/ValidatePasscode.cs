using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;

namespace Kursserver.Login
{
    public static class ValidatePasscode
    {
        public static void PasscodeValidationEndpoint(this WebApplication app, IConfiguration jwtSettings, string connectionString)
        {
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);


            app.MapPost("api/passcode-validation", async (context) =>
            {
                var request = await context.Request.ReadFromJsonAsync<ExtractPasscode>();
                if (request?.Passcode == null || request.Email == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "No passcode or email given." });
                    return;
                }
                bool validPasscode = request.Passcode == ValidateEmail.passcodeStore.GetValueOrDefault(request.Email);
                string role = await UserRole.GetUserRole(request.Email, connectionString);
                if (role == "")
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "User role not found." });
                    return;
                }
                if (validPasscode)
                {
                    var claims = new[]
                    {
                        new Claim(JwtRegisteredClaimNames.Sub, request.Email),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, role)
                    };

                    var token = new JwtSecurityToken(
                        issuer: jwtSettings["Issuer"],
                        audience: jwtSettings["Audience"],
                        claims: claims,
                        expires: DateTime.UtcNow.AddHours(1),
                        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
                    );

                    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                    Debug.WriteLine($"Passcode valid: {request.Passcode} for email: {request.Email}");
                    await context.Response.WriteAsJsonAsync(new { token = tokenString });
                }
                else
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Passcode wrong or decrapicated." });
                    return;
                }
            });

        }
    }
}
