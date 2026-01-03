using Azure.Core;
using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace Kursserver.Endpoints
{
    public static class ValidationEndpoints
    {
        public static readonly ConcurrentDictionary<string, int> passcodeStore = new();
        
        public static void MapValidationEndpoints(this WebApplication app, IConfiguration jwtSettings)
        {
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);


            app.MapPost("api/email-validation", async (ValidateEmailDto dto, [FromServices] ApplicationDbContext db) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);
                if (user == null) return Results.NotFound("Email not found.");
                else
                {
                    int passcode = Random.Shared.Next(100000, 999999);
                    passcodeStore[user.Email] = passcode;
                    return Results.Ok(passcode);
                }
            });

            app.MapPost("api/passcode-validation", async (ValidatePasscodeDto dto, [FromServices] ApplicationDbContext db) =>
            {
                if (passcodeStore.ContainsKey(dto.Email) && passcodeStore[dto.Email] == dto.Passcode)
                {
                    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                    if (user == null) return Results.NotFound();
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
                        expires: DateTime.UtcNow.AddHours(1),
                        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
                    );
                    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                    passcodeStore.TryRemove(dto.Email, out _);
                    return Results.Ok(new { token = tokenString });
                }
                else return Results.Problem("Passcode var incorrect");
            });
        }
    }
}
