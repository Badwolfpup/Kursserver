using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Endpoints
{
    [Authorize]
    public static class ProjectEndpoints
    {
        public static void MapProjectEndpoints(this WebApplication app)
        {
            app.MapGet("api/fetch-projects", [Authorize] async (ApplicationDbContext db) =>
            {
                try
                {
                    var projects = await db.Projects.ToListAsync();
                    return Results.Ok(projects);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch projects: " + ex.Message, statusCode: 500);
                }
            });

            app.MapPost("api/add-project", [Authorize] async (AddProjectDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;
                    if (db.Projects.Any(x => x.Title == dto.Title)) return Results.Problem("A project with that title already exists");
                    var project = new Project
                    {
                        Title = dto.Title,
                        Description = dto.Description,
                        Html = dto.Html,
                        Css = dto.Css,
                        Javascript = dto.Javascript,
                        Difficulty = dto.Difficulty,
                        ProjectType = dto.ProjectType
                    };
                    db.Projects.Add(project);
                    await db.SaveChangesAsync();
                    return Results.Ok(project);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to add project: " + ex.Message, statusCode: 500);
                }
            });

            app.MapDelete("api/delete-project/{id}", [Authorize] async (int id, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;
                    var project = await db.Projects.FindAsync(id);
                    if (project == null) return Results.Problem("Project doesn't exist");
                    db.Projects.Remove(project);
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to delete project: " + ex.Message, statusCode: 500);
                }
            });

            app.MapPut("api/update-project", [Authorize] async (UpdateProjectDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;
                    var project = await db.Projects.FindAsync(dto.Id);
                    if (project == null) return Results.Problem("Project doesn't exist");

                    if (!string.IsNullOrEmpty(dto.Title) && project.Title != dto.Title) project.Title = dto.Title;
                    if (!string.IsNullOrEmpty(dto.Description) && project.Description != dto.Description) project.Description = dto.Description;
                    if (!string.IsNullOrEmpty(dto.Html) && project.Html != dto.Html) project.Html = dto.Html;
                    if (!string.IsNullOrEmpty(dto.Css) && project.Css != dto.Css) project.Css = dto.Css;
                    if (!string.IsNullOrEmpty(dto.Javascript) && project.Javascript != dto.Javascript) project.Javascript = dto.Javascript;
                    if (!string.IsNullOrEmpty(dto.ProjectType) && project.ProjectType != dto.ProjectType) project.ProjectType = dto.ProjectType;
                    if (dto.Difficulty != null && project.Difficulty != dto.Difficulty) project.Difficulty = (int)dto.Difficulty;

                    db.Projects.Update(project);
                    await db.SaveChangesAsync();
                    return Results.Ok();

                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to update project: " + ex.Message, statusCode: 500);
                }
            });
        }
    }
}
