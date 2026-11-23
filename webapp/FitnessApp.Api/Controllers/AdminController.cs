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
[AdminOnly]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

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

    [HttpPut("users/{id}/promote")]
    public async Task<IActionResult> PromoteUserToAdmin(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound("Користувача не знайдено");

        if (user.Role == UserRole.Admin)
            return BadRequest("Користувач уже має права адміністратора");

        user.Role = UserRole.Admin;
        await _db.SaveChangesAsync();
        return Ok(new { user.Id, user.Email, Role = user.Role.ToString() });
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        if (Guid.TryParse(Request.Headers["X-User-Id"], out var currentAdminId) && currentAdminId == id)
            return BadRequest("Неможливо видалити власний активний акаунт адміністратора");

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound("Користувача не знайдено");

        if (user.Email.Equals("admin@fitness.local", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Системний обліковий запис адміністратора захищено від видалення");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("users/{id}/demote")]
    public async Task<IActionResult> DemoteAdminToUser(Guid id)
    {
        if (Guid.TryParse(Request.Headers["X-User-Id"], out var currentAdminId) && currentAdminId == id)
            return BadRequest("Неможливо понизити власний активний акаунт адміністратора");

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound("Користувача не знайдено");

        if (user.Email.Equals("admin@fitness.local", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Системний обліковий запис адміністратора захищено від пониження ролі");

        if (user.Role == UserRole.User)
            return BadRequest("Користувач уже має базову роль User");

        user.Role = UserRole.User;
        await _db.SaveChangesAsync();
        return Ok(new { user.Id, user.Email, Role = user.Role.ToString() });
    }
}