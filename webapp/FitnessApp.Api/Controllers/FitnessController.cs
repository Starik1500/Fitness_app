using FitnessApp.Application.Patterns;
using FitnessApp.Application.Services;
using FitnessApp.Domain.Entities;
using FitnessApp.Domain.Enums;
using FitnessApp.Domain.Interfaces;
using FitnessApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Controllers;

[ApiController]
[Route("api")]
public class FitnessController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ProgramDirector _director;
    private readonly IEnumerable<ICalorieCalculationStrategy> _strategies;
    private readonly WorkoutEventPublisher _publisher;
    private readonly IAnalyticsReportService _reports;

    public FitnessController(
        AppDbContext db,
        ProgramDirector director,
        IEnumerable<ICalorieCalculationStrategy> strategies,
        WorkoutEventPublisher publisher,
        IAnalyticsReportService reports)
    {
        _db = db;
        _director = director;
        _strategies = strategies;
        _publisher = publisher;
        _reports = reports;
    }

    [HttpPost("programs/generate")]
    public async Task<IActionResult> GenerateProgram(Guid userId, FitnessGoal goal)
    {
        var profile = await _db.UserProfiles.FindAsync(userId);
        if (profile == null) return NotFound("Профіль не знайдено");

        var library = await _db.WorkoutTemplates.Where(w => w.IsPublic).ToListAsync();
        var program = _director.Construct(profile, goal, library);

        _db.UserPrograms.Add(program);
        await _db.SaveChangesAsync();
        return Ok(program);
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> CompleteWorkout(Guid userId, Guid templateId, int durationMinutes)
    {
        var profile = await _db.UserProfiles.FindAsync(userId);
        var template = await _db.WorkoutTemplates.FindAsync(templateId);
        if (profile == null || template == null) return BadRequest("Невірні дані");

        var strategy = _strategies.First(s => s.SupportedType == template.Type);
        var calories = strategy.Calculate(profile.CurrentWeightKg, durationMinutes, baseMet: 6.5);

        var session = new WorkoutSession
        {
            UserId = userId,
            WorkoutTemplateId = templateId,
            DurationMinutes = durationMinutes,
            CaloriesBurned = calories
        };

        _db.WorkoutSessions.Add(session);
        await _db.SaveChangesAsync();

        await _publisher.NotifyCompletedAsync(session);

        return Ok(session);
    }

    [HttpGet("reports/popular-workouts")]
    public async Task<IActionResult> GetPopularWorkouts() => Ok(await _reports.GetPopularWorkoutsAsync());

    [HttpGet("reports/average-activity")]
    public async Task<IActionResult> GetAverageActivity(DateTime from, DateTime to) =>
        Ok(await _reports.GetAverageActivityAsync(from, to));

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users
            .Include(u => u.Profile)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.Role,
                Weight = u.Profile != null ? u.Profile.WeightKg : 0
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("exercises")]
    public async Task<IActionResult> GetExercises()
    {
        var list = await _db.Exercises.ToListAsync();
        return Ok(list);
    }

    [HttpGet("workouts")]
    public async Task<IActionResult> GetWorkouts()
    {
        var list = await _db.WorkoutTemplates.ToListAsync();
        return Ok(list);
    }
}