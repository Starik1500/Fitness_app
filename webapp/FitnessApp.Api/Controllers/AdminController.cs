using FitnessApp.Domain.Entities;
using FitnessApp.Domain.Enums;
using FitnessApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Controllers;

public record CreateExerciseDto(string Name, WorkoutType Category, double BaseMetValue, string Description);
public record CreateWorkoutDto(string Title, WorkoutType Type, DifficultyLevel Difficulty);
public record CreateChallengeDto(string Title, double TargetCalories, int DurationDays);

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    [HttpPost("exercises")]
    public async Task<IActionResult> AddExercise([FromBody] CreateExerciseDto dto)
    {
        var ex = new Exercise
        {
            Name = dto.Name,
            Category = dto.Category,
            BaseMetValue = dto.BaseMetValue,
            Description = dto.Description
        };
        _db.Exercises.Add(ex);
        await _db.SaveChangesAsync();
        return Ok(ex);
    }

    [HttpDelete("exercises/{id}")]
    public async Task<IActionResult> DeleteExercise(Guid id)
    {
        var ex = await _db.Exercises.FindAsync(id);
        if (ex == null) return NotFound();
        _db.Exercises.Remove(ex);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("workouts")]
    public async Task<IActionResult> AddWorkout([FromBody] CreateWorkoutDto dto)
    {
        var wt = new WorkoutTemplate
        {
            Title = dto.Title,
            Type = dto.Type,
            Difficulty = dto.Difficulty,
            IsPublic = true
        };
        _db.WorkoutTemplates.Add(wt);
        await _db.SaveChangesAsync();
        return Ok(wt);
    }

    [HttpDelete("workouts/{id}")]
    public async Task<IActionResult> DeleteWorkout(Guid id)
    {
        var wt = await _db.WorkoutTemplates.FindAsync(id);
        if (wt == null) return NotFound();
        _db.WorkoutTemplates.Remove(wt);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("challenges")]
    public async Task<IActionResult> AddChallenge([FromBody] CreateChallengeDto dto)
    {
        var ch = new Challenge
        {
            Title = dto.Title,
            TargetCalories = dto.TargetCalories,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(dto.DurationDays)
        };
        _db.Challenges.Add(ch);
        await _db.SaveChangesAsync();
        return Ok(ch);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers() =>
        Ok(await _db.Users.Include(u => u.Profile).Select(u => new
        {
            u.Id,
            u.Email,
            Role = u.Role.ToString(),
            u.CreatedAt,
            Weight = u.Profile != null ? u.Profile.CurrentWeightKg : 0
        }).ToListAsync());
}