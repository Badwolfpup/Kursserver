using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Data;


namespace Kursserver.Endpoints
{
    [Authorize]
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app) 
        {
            app.MapPost("api/add-user", async (AddUserDto dto, [FromServices] ApplicationDbContext db) =>
            {
                var user = new User
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    AuthLevel = (Role)dto.AuthLevel,
                    Course = dto.Course,
                };
                try
                {
                    if (!db.Users.Any(x => x.Email == user.Email))
                    {
                        db.Users.Add(user);
                        await db.SaveChangesAsync();
                        return Results.Ok(user);
                    }
                    return Results.Problem("Email already exists", statusCode: 500);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to add user: " + ex.Message, statusCode: 500);
                }
            });

            app.MapDelete("api/delete-user/", async ([FromServices] ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var userId = int.Parse(context.User.FindFirst("id")?.Value ?? "0");
                    var user = await db.Users.FindAsync(userId);
                    if (user == null) return Results.NotFound();
                    db.Users.Remove(user);
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to delete user: " + ex.Message, statusCode: 500);
                }
            });

            

            app.MapPut("api/update-user", async (UpdateUserDto dto, [FromServices] ApplicationDbContext db) =>
            {
                var user = db.Users.FirstOrDefault(x => x.Id == dto.Id) as User;
                if (user == null) return Results.NotFound();
                try
                {
                    if (!string.IsNullOrEmpty(dto.Email)) user.Email = dto.Email;
                    if (!string.IsNullOrEmpty(dto.FirstName)) user.FirstName = dto.FirstName;
                    if (!string.IsNullOrEmpty(dto.LastName)) user.LastName = dto.LastName;
                    if (dto.Course != null && dto.Course > 0) user.Course = dto.Course.Value;
                    if (dto.CoachId != null && dto.CoachId > 0) user.CoachId = dto.CoachId.Value;
                    if (dto.AuthLevel.HasValue) user.AuthLevel = dto.AuthLevel.Value;
                    if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;

                    db.Update(user);
                    await db.SaveChangesAsync();
                    return Results.Ok(user);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to update user: " + ex.Message, statusCode: 500);
                }
            });

            app.MapPost("api/fetch-users", async ([AsParameters] FetchUsersDto dto, [FromServices] ApplicationDbContext db) =>
            {
                try
                {
                    var users = dto.AuthLevel == null ? await db.Users.ToListAsync() : await db.Users.Where(x => x.AuthLevel == dto.AuthLevel).ToListAsync();
                    return Results.Ok(users);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch user: " + ex.Message, statusCode: 500);
                }
            });

            app.MapPut("api/update-activity", async (UpdateActivityDto dto, [FromServices] ApplicationDbContext db) => {
                var user = db.Users.FirstOrDefault(x => x.Id == dto.Id);
                if (user == null) return Results.NotFound();
                try
                {
                    user.IsActive = !user.IsActive;
                    db.Update(user);
                    await db.SaveChangesAsync(); 
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to update user's activity status: " + ex.Message, statusCode: 500);
                }
            });

        }

    }
}
