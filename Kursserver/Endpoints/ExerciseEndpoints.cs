using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Endpoints
{
    public static class ExerciseEndpoints
    {
        public static void MapExerciseEndpoints(this WebApplication app)
        {
            app.MapGet("api/fetch-exercises", [Authorize] async (ApplicationDbContext db) =>
            {
                try
                {
                    var exercises = await db.Exercises.ToListAsync();
                    return Results.Ok(exercises);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch exercises: " + ex.Message, statusCode: 500);
                }
            });

            app.MapPost("api/add-exercise", [Authorize] async (AddExerciseDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;
                    if (db.Exercises.Any(x => x.Title == dto.Title)) return Results.Problem("A project with that title already exists");
                    var exercise = new Exercise
                    {
                        Title = dto.Title,
                        Description = dto.Description,
                        Javascript = dto.Javascript,
                        Difficulty = dto.Difficulty,
                        ExpectedResult = dto.ExpectedResult,
                        Clues = dto.Clues ?? new List<string>(),
                        ExerciseType = dto.ExerciseType,
                        GoodToKnow = dto.GoodToKnow
                    };
                    db.Exercises.Add(exercise);
                    await db.SaveChangesAsync();
                    return Results.Ok(exercise);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to add exercise: " + ex.Message, statusCode: 500);
                }
            });

            app.MapDelete("api/delete-exercise/{id}", [Authorize] async (int id, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;
                    var exercise = await db.Exercises.FindAsync(id);
                    if (exercise == null) return Results.Problem("Project doesn't exist");
                    db.Exercises.Remove(exercise);
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to delete exercise: " + ex.Message, statusCode: 500);
                }
            });

            app.MapPut("api/update-exercise", [Authorize] async (UpdateExerciseDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                try
                {
                    var accessCheck = HasAdminPriviligies.IsTeacher(context, 1);
                    if (accessCheck != null) return accessCheck;
                    var exercise = await db.Exercises.FindAsync(dto.Id);
                    if (exercise == null) return Results.Problem("Project doesn't exist");

                    if (!string.IsNullOrEmpty(dto.Title) && exercise.Title != dto.Title) exercise.Title = dto.Title;
                    if (!string.IsNullOrEmpty(dto.Description) && exercise.Description != dto.Description) exercise.Description = dto.Description;
                    if (!string.IsNullOrEmpty(dto.Javascript) && exercise.Javascript != dto.Javascript) exercise.Javascript = dto.Javascript;
                    if (!string.IsNullOrEmpty(dto.ExpectedResult) && exercise.ExpectedResult != dto.ExpectedResult) exercise.ExpectedResult = dto.ExpectedResult;
                    if (!string.IsNullOrEmpty(dto.ExerciseType) && exercise.ExerciseType != dto.ExerciseType) exercise.ExerciseType = dto.ExerciseType;
                    if (dto.Difficulty != null && exercise.Difficulty != dto.Difficulty) exercise.Difficulty = (int)dto.Difficulty;
                    if (!string.IsNullOrEmpty(dto.GoodToKnow) && exercise.GoodToKnow != dto.GoodToKnow) exercise.GoodToKnow = dto.GoodToKnow;
                    if (dto.Clues != null && !exercise.Clues.SequenceEqual(dto.Clues)) exercise.Clues = dto.Clues;

                    db.Exercises.Update(exercise);
                    await db.SaveChangesAsync();
                    return Results.Ok();

                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to update exercise: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Student fetches their AI-generated exercise history
            /// CALLS: useExerciseHistory() → exerciseService.fetchExerciseHistory()
            /// </summary>
            app.MapGet("api/exercise-history", [Authorize] async (ApplicationDbContext db, HttpContext context) =>
            {
                var userIdClaim = context.User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                try
                {
                    var history = await db.ExerciseHistories
                        .Where(h => h.UserId == userId)
                        .OrderByDescending(h => h.CreatedAt)
                        .Select(h => new
                        {
                            h.Id, h.Topic, h.Language, h.Difficulty, h.Title,
                            h.Description, h.Example, h.Assumptions, h.FunctionSignature,
                            h.Solution, h.Asserts, h.IsCompleted, h.CreatedAt
                        })
                        .ToListAsync();
                    return Results.Ok(history);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch exercise history: " + ex.Message, statusCode: 500);
                }
            });

            /// <summary>
            /// SCENARIO: Student fetches their AI-generated project history
            /// CALLS: useProjectHistory() → exerciseService.fetchProjectHistory()
            /// </summary>
            app.MapGet("api/project-history", [Authorize] async (ApplicationDbContext db, HttpContext context) =>
            {
                var userIdClaim = context.User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                try
                {
                    var history = await db.ProjectHistories
                        .Where(h => h.UserId == userId)
                        .OrderByDescending(h => h.CreatedAt)
                        .Select(h => new
                        {
                            h.Id, h.TechStack, h.Difficulty, h.Title,
                            h.Description, h.LearningGoals, h.UserStories,
                            h.DesignSpecs, h.AssetsNeeded, h.StarterHtml,
                            h.BonusChallenges, h.SolutionHtml, h.SolutionCss,
                            h.SolutionJs, h.IsCompleted, h.CreatedAt
                        })
                        .ToListAsync();
                    return Results.Ok(history);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to fetch project history: " + ex.Message, statusCode: 500);
                }
            });

            app.MapPost("api/exercise-feedback", [Authorize] async (
                ExerciseFeedbackDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                var userIdClaim = context.User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                try
                {
                    var history = new ExerciseHistory
                    {
                        UserId = userId,
                        Topic = dto.Topic,
                        Language = dto.Language,
                        Difficulty = dto.Difficulty,
                        Title = dto.Title,
                        Description = dto.Description ?? "",
                        Example = dto.Example,
                        Assumptions = dto.Assumptions,
                        FunctionSignature = dto.FunctionSignature,
                        Solution = dto.Solution,
                        Asserts = dto.Asserts,
                        IsCompleted = dto.IsCompleted,
                        IsPositive = dto.IsPositive,
                        FeedbackReason = dto.FeedbackReason,
                        FeedbackComment = dto.FeedbackComment,
                        CreatedAt = DateTime.UtcNow
                    };

                    db.ExerciseHistories.Add(history);
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to save exercise feedback: " + ex.Message, statusCode: 500);
                }
            });

            app.MapPost("api/project-feedback", [Authorize] async (
                ProjectFeedbackDto dto, ApplicationDbContext db, HttpContext context) =>
            {
                var userIdClaim = context.User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                try
                {
                    var history = new ProjectHistory
                    {
                        UserId = userId,
                        TechStack = dto.TechStack,
                        Difficulty = dto.Difficulty,
                        Title = dto.Title,
                        Description = dto.Description ?? "",
                        LearningGoals = dto.LearningGoals,
                        UserStories = dto.UserStories,
                        DesignSpecs = dto.DesignSpecs,
                        AssetsNeeded = dto.AssetsNeeded,
                        StarterHtml = dto.StarterHtml,
                        BonusChallenges = dto.BonusChallenges,
                        SolutionHtml = dto.SolutionHtml,
                        SolutionCss = dto.SolutionCss,
                        SolutionJs = dto.SolutionJs,
                        IsCompleted = dto.IsCompleted,
                        IsPositive = dto.IsPositive,
                        FeedbackReason = dto.FeedbackReason,
                        FeedbackComment = dto.FeedbackComment,
                        CreatedAt = DateTime.UtcNow
                    };

                    db.ProjectHistories.Add(history);
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to save project feedback: " + ex.Message, statusCode: 500);
                }
            });
        }

    }
}
