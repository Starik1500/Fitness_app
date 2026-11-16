using FitnessApp.Domain.Enums;

namespace FitnessApp.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserProfile? Profile { get; set; }
    public List<WeightLog> WeightLogs { get; set; } = new();
    public List<UserProgram> Programs { get; set; } = new();
    public List<WorkoutSession> Sessions { get; set; } = new();
    public List<UserAchievement> Achievements { get; set; } = new();
    public List<UserChallenge> Challenges { get; set; } = new();
}

public class UserProfile
{
    public Guid UserId { get; set; }
    public Gender Gender { get; set; }
    public DateOnly BirthDate { get; set; }
    public double HeightCm { get; set; }
    public double CurrentWeightKg { get; set; }

    public double CalculateBMI() => Math.Round(CurrentWeightKg / Math.Pow(HeightCm / 100.0, 2), 2);
}

public class WeightLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public double WeightKg { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

public class Exercise
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public WorkoutType Category { get; set; }
    public double BaseMetValue { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class WorkoutTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public WorkoutType Type { get; set; }
    public DifficultyLevel Difficulty { get; set; }
    public bool IsPublic { get; set; } = true;
    public List<WorkoutExercise> Exercises { get; set; } = new();
}

public class WorkoutExercise
{
    public Guid WorkoutTemplateId { get; set; }
    public Guid ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }
    public int Sets { get; set; }
    public int Reps { get; set; }
    public int DurationMinutes { get; set; }
    public int OrderIndex { get; set; }
}

public class UserProgram
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public FitnessGoal Goal { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<WorkoutTemplate> Workouts { get; set; } = new();
}

public class WorkoutSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid WorkoutTemplateId { get; set; }
    public WorkoutTemplate? WorkoutTemplate { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public int DurationMinutes { get; set; }
    public double CaloriesBurned { get; set; }
}

public class Achievement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int ConditionThreshold { get; set; } 
}

public class UserAchievement
{
    public Guid UserId { get; set; }
    public Guid AchievementId { get; set; }
    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
}

public class Challenge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public double TargetCalories { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class UserChallenge
{
    public Guid UserId { get; set; }
    public Guid ChallengeId { get; set; }
    public double CurrentCalories { get; set; }
}

public class Reminder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string CronExpression { get; set; } = "0 18 * * 1,3,5";
    public NotificationChannel Channel { get; set; } = NotificationChannel.WebPush;
    public bool IsActive { get; set; } = true;
}