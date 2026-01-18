using Azure.Core;
using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;

namespace Kursserver.Endpoints
{
    
    public static class PostEndpoints
    {
        public static void MapPostGetPostEndpoint(this WebApplication app)
        {
            app.MapGet("/api/fetch-posts", async (ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    //var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    //if (accessCheck != null) return accessCheck;
                    var post = await db.Posts.ToListAsync();
                    return Results.Ok(post);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch posts: " + ex.Message, statusCode: 500);
                }
            });
        }

        [Authorize]
        public static void MapPostEndpoints(this WebApplication app)
        {


            app.MapPost("/api/add-posts", async (AddPostDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;
                    var userId = new FromClaims().GetUserId(context);
                    var user = await db.Users.FindAsync(userId);
                    if (user == null) return Results.BadRequest("User not found");
                    var post = new Post
                    {
                        UserId = userId,
                        Html = dto.Html,
                        Delta = dto.Delta,
                        PublishedAt = dto.PublishDate != null ? ((DateTime)dto.PublishDate).Date : DateTime.Now,
                        Pinned = dto.Pinned,
                        Author = user != null ? $"{user.FirstName} {user.LastName}" : ""
                    };
                    db.Posts.Add(post);
                    await db.SaveChangesAsync();
                    return Results.Created($"/api/posts/{post.Id}", post);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to add post: " + ex.Message, statusCode: 500);
                }
            });

            app.MapPut("/api/update-posts", async (UpdatePostDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;
                    var post = await db.Posts.FindAsync(dto.Id);
                    if (post == null) return Results.Problem("Post notfound in database");
                    if (!string.IsNullOrEmpty(dto.Delta)) post.Delta = dto.Delta;
                    if (!string.IsNullOrEmpty(dto.Html)) post.Html = dto.Html;
                    if (dto.Pinned.HasValue) post.Pinned = dto.Pinned.Value;
                    post.PublishedAt = dto.PublishDate != null ? ((DateTime)dto.PublishDate).Date : DateTime.Now;
                    post.UpdatedAt = DateTime.Now;
                    db.Posts.Update(post);
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to update post: " + ex.Message, statusCode: 500);
                }
            });

            app.MapDelete("/api/delete-post/{id}",async (int id, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;
                    var post = await db.Posts.FindAsync(id);
                    if (post == null) return Results.NotFound("Post not found");
                    db.Posts.Remove(post);
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to delete post: " + ex.Message, statusCode: 500);
                }
            });

            app.MapPost("api/upload-image", async (UploadImageDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;
                    // Step 1: Parse the base64 string (format: "data:image/png;base64,actualBase64Data")

                    var base64data = dto.Image.Split(',')[1];
                    var mimeType = dto.Image.Split(',')[0].Split(':')[1].Split(';')[0];

                    // Step 2: Decode base64 to byte array
                    byte[] imageBytes = Convert.FromBase64String(base64data);

                    // Step 3: Determine file extension from MIME type
                    string extension = GetExtensionFromMimeType(mimeType);
                    if (string.IsNullOrEmpty(extension)) return Results.Problem();

                    // Step 4: Generate a unique filename
                    string fileName = Guid.NewGuid().ToString() + extension;
                    string filePath = Path.Combine("wwwroot", "images", fileName);
                    // Ensure the directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    // Step 5: Save the file
                    await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                    // NEW: Return success response with URL
                    string imageUrl = $"{context.Request.Scheme}://{context.Request.Host}/images/{fileName}";
                    Debug.WriteLine(imageUrl);
                    return Results.Ok(new { url = imageUrl });

                }
                catch (Exception ex)
                {
                    return Results.StatusCode(500);
                }
            });
        }

        private static string GetExtensionFromMimeType(string mimeType)
        {
            return mimeType switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                "image/svg+xml" => ".svg",
                _ => ""
            };
        }
    }
}
