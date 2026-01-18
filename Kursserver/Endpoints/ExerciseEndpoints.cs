using Kursserver.Dto;
using Kursserver.Models;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace Kursserver.Endpoints
{
    public static class ExerciseEndpoints
    {
        public static void MapExerciseEndpoints(this WebApplication app)
        {
            app.MapGet("api/fetch-exercises", async (ApplicationDbContext db) =>
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

            app.MapPost("api/add-exercise", async (AddExerciseDto dto, ApplicationDbContext db, HttpContext context) =>
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
                        Tags = dto.Tags ?? new List<string>(),
                        Clues = dto.Clues ?? new List<string>()
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

            app.MapDelete("api/delete-exercise/{id}", async (int id, ApplicationDbContext db, HttpContext context) =>
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

            app.MapPut("api/update-exercise", async (UpdateExerciseDto dto, ApplicationDbContext db, HttpContext context) =>
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
                    if (dto.Difficulty != null && exercise.Difficulty != dto.Difficulty) exercise.Difficulty = (int)dto.Difficulty;
                    if (dto.Tags != null && !exercise.Tags.SequenceEqual(dto.Tags)) exercise.Tags = dto.Tags;
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
