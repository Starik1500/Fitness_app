using FitnessApp.Application.Services;
using FitnessApp.Domain.Entities;
using FitnessApp.Domain.Enums;
using FitnessApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<WeightLog> WeightLogs => Set<WeightLog>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<WorkoutTemplate> WorkoutTemplates => Set<WorkoutTemplate>();
    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();
    public DbSet<UserProgram> UserPrograms => Set<UserProgram>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<Challenge> Challenges => Set<Challenge>();
    public DbSet<UserChallenge> UserChallenges => Set<UserChallenge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>().HasKey(p => p.UserId);
        modelBuilder.Entity<WorkoutExercise>().HasKey(we => new { we.WorkoutTemplateId, we.ExerciseId, we.OrderIndex });
        modelBuilder.Entity<UserAchievement>().HasKey(ua => new { ua.UserId, ua.AchievementId });
        modelBuilder.Entity<UserChallenge>().HasKey(uc => new { uc.UserId, uc.ChallengeId });

        var testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = testUserId,
            Email = "user@fitness.local",
            PasswordHash = "demo_hash",
            Role = UserRole.User
        });
        modelBuilder.Entity<UserProfile>().HasData(new UserProfile
        {
            UserId = testUserId,
            Gender = Gender.Male,
            BirthDate = new DateOnly(2002, 5, 15),
            HeightCm = 180,
            CurrentWeightKg = 82
        });

        modelBuilder.Entity<WorkoutTemplate>().HasData(
            new WorkoutTemplate { Id = Guid.Parse("22222222-2222-2222-2222-222222222221"), Title = "Ранкова йога-розминка", Type = WorkoutType.Yoga, Difficulty = DifficultyLevel.Beginner },
            new WorkoutTemplate { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Title = "Інтенсивне кардіо спалювання", Type = WorkoutType.Cardio, Difficulty = DifficultyLevel.Intermediate },
            new WorkoutTemplate { Id = Guid.Parse("22222222-2222-2222-2222-222222222223"), Title = "Базовий силовий фулбоді", Type = WorkoutType.Strength, Difficulty = DifficultyLevel.Beginner }
        );

        modelBuilder.Entity<Achievement>().HasData(
            new Achievement { Id = Guid.Parse("33333333-3333-3333-3333-333333333331"), Code = "FIRST_WORKOUT", Title = "Перший крок (1 тренування)", ConditionThreshold = 1 }
        );
    }
}

public class AchievementObserver : IWorkoutObserver
{
    private readonly AppDbContext _db;
    public AchievementObserver(AppDbContext db) => _db = db;

    public async Task OnWorkoutCompletedAsync(WorkoutSession session)
    {
        var totalSessions = await _db.WorkoutSessions.CountAsync(s => s.UserId == session.UserId);
        var eligible = await _db.Achievements.Where(a => a.ConditionThreshold <= totalSessions).ToListAsync();

        foreach (var ach in eligible)
        {
            var exists = await _db.UserAchievements.AnyAsync(ua => ua.UserId == session.UserId && ua.AchievementId == ach.Id);
            if (!exists)
                _db.UserAchievements.Add(new UserAchievement { UserId = session.UserId, AchievementId = ach.Id });
        }
        await _db.SaveChangesAsync();
    }
}

public class BaseAnalyticsReportService : IAnalyticsReportService
{
    private readonly AppDbContext _db;
    public BaseAnalyticsReportService(AppDbContext db) => _db = db;

    public async Task<List<PopularWorkoutDto>> GetPopularWorkoutsAsync()
    {
        var grouped = await _db.WorkoutSessions
            .GroupBy(s => new { s.WorkoutTemplateId, s.WorkoutTemplate!.Title })
            .Select(g => new { g.Key.Title, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        return grouped.Select(x => new PopularWorkoutDto(x.Title, x.Count)).ToList();
    }

    public async Task<AverageActivityDto> GetAverageActivityAsync(DateTime from, DateTime to)
    {
        var sessions = await _db.WorkoutSessions.Where(s => s.CompletedAt >= from && s.CompletedAt <= to).ToListAsync();
        if (!sessions.Any()) return new AverageActivityDto(0, 0);

        var usersCount = sessions.Select(s => s.UserId).Distinct().Count();
        return new AverageActivityDto(
            Math.Round((double)sessions.Sum(s => s.DurationMinutes) / usersCount, 1),
            Math.Round(sessions.Sum(s => s.CaloriesBurned) / usersCount, 1)
        );
    }
}

public class ChallengeProgressObserver : IWorkoutObserver
{
    private readonly AppDbContext _db;
    public ChallengeProgressObserver(AppDbContext db) => _db = db;

    public async Task OnWorkoutCompletedAsync(WorkoutSession session)
    {
        var activeUserChallenges = await _db.UserChallenges
            .Join(_db.Challenges,
                uc => uc.ChallengeId,
                c => c.Id,
                (uc, c) => new { UserChallenge = uc, Challenge = c })
            .Where(x => x.UserChallenge.UserId == session.UserId
                     && x.Challenge.StartDate <= session.CompletedAt
                     && x.Challenge.EndDate >= session.CompletedAt)
            .Select(x => x.UserChallenge)
            .ToListAsync();

        foreach (var uc in activeUserChallenges)
            uc.CurrentCalories += session.CaloriesBurned;

        if (activeUserChallenges.Any())
            await _db.SaveChangesAsync();
    }
}