using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Services;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Endpoints
{
    public static class GrokEndpoints
    {
        public static void MapGrokEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("api/grok").WithTags("AI");

            // Exercise generation
            group.MapPost("exercise-asserts", [Authorize] async (
                ExerciseRequest request,
                GrokService grokService,
                ApplicationDbContext db,
                HttpContext context) =>
            {
                if (!request.IsValid(out var error))
                {
                    return Results.BadRequest(new ExerciseResponse
                    {
                        Success = false,
                        Error = error
                    });
                }

                try
                {
                    var userId = int.Parse(context.User.FindFirst("id")?.Value ?? "0");
                    if (userId == 0)
                    {
                        return Results.BadRequest(new ExerciseResponse
                        {
                            Success = false,
                            Error = "Invalid user ID."
                        });
                    }

                    var recentExercises = await db.ExerciseHistories
                                .Where(h => h.UserId == userId
                                         && h.Topic == request.Topic
                                         && h.Language == request.Language)
                                .OrderByDescending(h => h.CreatedAt)
                                .Take(10)
                                .Select(h => new { h.Title, h.Description })
                                .ToListAsync();

                    var prompt = ExercisePromptTemplates.GeneratePracticeAsserts(
                        request.Topic,
                        request.Language,
                        request.Difficulty,
                        recentExercises.Cast<object>().ToList()
                    );

                    var result = await grokService.GetCompletionAsync(prompt);

                    var (title, description, example, assumptions, functionSignature, asserts, solution) =
                        ExerciseResponseParser.ParseAssertResponse(result);
                    db.ExerciseHistories.Add(new ExerciseHistory
                    {
                        UserId = userId,
                        Topic = request.Topic,
                        Language = request.Language,
                        Difficulty = request.Difficulty,
                        Title = title,
                        Description = description.Length > 200
                            ? description.Substring(0, 200)
                            : description,
                        CreatedAt = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync();
                    return Results.Ok(new ExerciseResponse
                    {
                        Success = true,
                        Title = title,
                        Description = description,
                        Example = example,
                        Assumptions = assumptions,
                        FunctionSignature = functionSignature,
                        Asserts = asserts,
                        Solution = solution
                    });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new ExerciseResponse
                    {
                        Success = false,
                        Error = "Failed to generate exercise. Please try again."
                    });
                }
            });

            // Project generation
            group.MapPost("project-asserts", [Authorize] async (
                ProjectRequest request,
                GrokService grokService,
                ApplicationDbContext db,
                HttpContext context) =>
            {
                if (!request.IsValid(out var error))
                {
                    return Results.BadRequest(new ProjectResponse
                    {
                        Success = false,
                        Error = error
                    });
                }

                try
                {
                    var userId = int.Parse(context.User.FindFirst("id")?.Value ?? "0");
                    if (userId == 0)
                    {
                        return Results.BadRequest(new ProjectResponse
                        {
                            Success = false,
                            Error = "Invalid user ID."
                        });
                    }
                    var recentProjects = await db.ProjectHistories
                        .Where(h => h.UserId == userId
                                 && h.TechStack == request.TechStack)
                        .OrderByDescending(h => h.CreatedAt)
                        .Take(10)
                        .Select(h => new { h.Title, h.Description })
                        .ToListAsync();

                    var prompt = ProjectPromptTemplates.GenerateWebProject(
                        request.GetNormalizedProjectType(),
                        request.Difficulty,
                        recentProjects.Cast<object>().ToList()
                    );

                    var result = await grokService.GetCompletionAsync(prompt);
                    var parsed = ProjectResponseParser.Parse(result);
                    db.ProjectHistories.Add(new ProjectHistory
                    {
                        UserId = userId,
                        TechStack = request.TechStack ?? "",
                        Difficulty = request.Difficulty,
                        Title = parsed.Title ?? "",
                        Description = parsed.Description?.Length > 200
                            ? parsed.Description.Substring(0, 200)
                            : (parsed.Description ?? ""),
                        CreatedAt = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync();
                    return Results.Ok(parsed);
                }
                catch (Exception ex)
                {
                    return Results.Ok(new ProjectResponse
                    {
                        Success = false,
                        Error = "Failed to generate project. Please try again."
                    });
                }
            });
        }
    }
}
