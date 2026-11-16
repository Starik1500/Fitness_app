using FitnessApp.Domain.Entities;
using FitnessApp.Domain.Enums;
using FitnessApp.Domain.Interfaces;

namespace FitnessApp.Application.Patterns;

public class StandardProgramBuilder : IWorkoutProgramBuilder
{
    private UserProgram _program = new();

    public void Reset(Guid userId, FitnessGoal goal)
    {
        _program = new UserProgram
        {
            UserId = userId,
            Goal = goal,
            Title = $"Персональна програма: {goal}",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))
        };
    }

    public void AddWarmup(IEnumerable<WorkoutTemplate> templates)
    {
        var yoga = templates.FirstOrDefault(t => t.Type == WorkoutType.Yoga);
        if (yoga != null) _program.Workouts.Add(yoga);
    }

    public void AddMainWorkouts(IEnumerable<WorkoutTemplate> templates, double bmi)
    {
        var targetType = _program.Goal == FitnessGoal.MuscleGain ? WorkoutType.Strength : WorkoutType.Cardio;
        var filtered = templates
            .Where(t => t.Type == targetType && (bmi <= 28 || t.Difficulty != DifficultyLevel.Pro))
            .Take(2);

        _program.Workouts.AddRange(filtered);
    }

    public UserProgram Build() => _program;
}

public class ProgramDirector
{
    private readonly IWorkoutProgramBuilder _builder;
    public ProgramDirector(IWorkoutProgramBuilder builder) => _builder = builder;

    public UserProgram Construct(UserProfile profile, FitnessGoal goal, IEnumerable<WorkoutTemplate> library)
    {
        _builder.Reset(profile.UserId, goal);
        _builder.AddWarmup(library);
        _builder.AddMainWorkouts(library, profile.CalculateBMI());
        return _builder.Build();
    }
}