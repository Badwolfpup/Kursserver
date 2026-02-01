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
        }

    }
}
