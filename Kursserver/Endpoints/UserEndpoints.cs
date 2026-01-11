using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Data;
using System.Security.Claims;


namespace Kursserver.Endpoints
{
    [Authorize]
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app) 
        {


            app.MapPost("api/add-user", async ([FromBody] AddUserDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, (int)dto.AuthLevel);
                if (accessCheck != null) return accessCheck;
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
                        db.Permissions.Add(new Permission { UserId = user.Id });
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

            app.MapDelete("api/delete-user/", async ([FromBody] DeleteUserDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var user = await db.Users.FindAsync(dto.Id);
                    if (user == null) return Results.Problem("User not found");
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, (int)user.AuthLevel);
                    if (accessCheck != null) return accessCheck;
                    db.Users.Remove(user);
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to delete user: " + ex.Message, statusCode: 500);
                }
            });

            

            app.MapPut("api/update-user", async ([FromBody] UpdateUserDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, (int)dto.AuthLevel);
                if (accessCheck != null) return accessCheck;
                var user = db.Users.FirstOrDefault(x => x.Id == dto.Id);
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

            app.MapGet("api/fetch-users", async (ApplicationDbContext db, HttpContext context) =>
            {
                var accessCheck = HasAdminPriviligies.IsTeacher(context, 1, 0);
                if (accessCheck != null) return accessCheck;
                try
                {
                    var users = await db.Users.ToListAsync();
                    return Results.Ok(users);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch user: " + ex.Message, statusCode: 500);
                }
            });

            app.MapPut("api/update-activity", async ([FromBody] UpdateActivityDto dto, ApplicationDbContext db, HttpContext context) => {

                var user = db.Users.FirstOrDefault(x => x.Id == dto.Id);
                if (user == null) return Results.NotFound();
                var accessCheck = HasAdminPriviligies.IsTeacher(context, (int)user.AuthLevel);
                if (accessCheck != null) return accessCheck;
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
