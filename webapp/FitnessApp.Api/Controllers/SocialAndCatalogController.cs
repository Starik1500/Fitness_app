using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Controllers;

[ApiController]
[Route("api")]
public class SocialAndCatalogController : ControllerBase
{
    private readonly AppDbContext _db;
    public SocialAndCatalogController(AppDbContext db) => _db = db;

    [HttpGet("workouts")]
    public async Task<IActionResult> GetWorkouts() =>
        Ok(await _db.WorkoutTemplates.Where(w => w.IsPublic).ToListAsync());

    [HttpGet("exercises")]
    public async Task<IActionResult> GetExercises() =>
        Ok(await _db.Exercises.ToListAsync());

    [HttpGet("programs/{userId}")]
    public async Task<IActionResult> GetUserPrograms(Guid userId) =>
        Ok(await _db.UserPrograms.Include(p => p.Workouts).Where(p => p.UserId == userId).ToListAsync());

    [HttpGet("sessions/{userId}")]
    public async Task<IActionResult> GetUserSessions(Guid userId) =>
        Ok(await _db.WorkoutSessions.Include(s => s.WorkoutTemplate)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CompletedAt)
            .ToListAsync());

    [HttpGet("reports/progress/{userId}")]
    public async Task<IActionResult> GetUserProgressReport(Guid userId)
    {
        var weights = await _db.WeightLogs
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.RecordedAt)
            .Select(w => new { Date = w.RecordedAt.ToString("yyyy-MM-dd"), w.WeightKg })
            .ToListAsync();

        var sessions = await _db.WorkoutSessions
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.CompletedAt)
            .Select(s => new { Date = s.CompletedAt.ToString("yyyy-MM-dd"), s.CaloriesBurned, s.DurationMinutes })
            .ToListAsync();

        return Ok(new { WeightHistory = weights, CalorieHistory = sessions });
    }

    [HttpGet("social/achievements/{userId}")]
    public async Task<IActionResult> GetUserAchievements(Guid userId)
    {
        var unlocked = await _db.UserAchievements
            .Where(ua => ua.UserId == userId)
            .Join(_db.Achievements, ua => ua.AchievementId, a => a.Id,
                (ua, a) => new { a.Id, a.Code, a.Title, ua.UnlockedAt })
            .ToListAsync();

        return Ok(unlocked);
    }

    [HttpGet("social/leaderboard")]
    public async Task<IActionResult> GetLeaderboard()
    {
        var users = await _db.Users.ToListAsync();
        var sessions = await _db.WorkoutSessions.ToListAsync();

        var board = users.Select(u => new
        {
            u.Id,
            Name = u.Email.Split('@')[0],
            TotalCalories = Math.Round(sessions.Where(s => s.UserId == u.Id).Sum(s => s.CaloriesBurned), 1),
            WorkoutsCount = sessions.Count(s => s.UserId == u.Id)
        })
        .OrderByDescending(x => x.TotalCalories)
        .ToList();

        return Ok(board);
    }

    [HttpGet("social/challenges/{userId}")]
    public async Task<IActionResult> GetChallenges(Guid userId)
    {
        var all = await _db.Challenges.ToListAsync();
        var userJoined = await _db.UserChallenges.Where(uc => uc.UserId == userId).ToListAsync();

        var result = all.Select(c =>
        {
            var joined = userJoined.FirstOrDefault(uc => uc.ChallengeId == c.Id);
            return new
            {
                c.Id,
                c.Title,
                c.TargetCalories,
                c.StartDate,
                c.EndDate,
                IsJoined = joined != null,
                CurrentCalories = joined?.CurrentCalories ?? 0
            };
        });

        return Ok(result);
    }

    [HttpPost("social/challenges/{challengeId}/join")]
    public async Task<IActionResult> JoinChallenge(Guid challengeId, [FromQuery] Guid userId)
    {
        var exists = await _db.UserChallenges.AnyAsync(uc => uc.UserId == userId && uc.ChallengeId == challengeId);
        if (exists) return BadRequest("Ви вже берете участь у цьому челенджі");

        _db.UserChallenges.Add(new UserChallenge { UserId = userId, ChallengeId = challengeId, CurrentCalories = 0 });
        await _db.SaveChangesAsync();
        return Ok(new { Message = "Успішно приєднано до челенджу" });
    }
}