using FitnessApp.Domain.Entities;
using FitnessApp.Domain.Enums;

namespace FitnessApp.Domain.Interfaces;

public interface ICalorieCalculationStrategy
{
    WorkoutType SupportedType { get; }
    double Calculate(double weightKg, int durationMinutes, double baseMet);
}

public interface IWorkoutProgramBuilder
{
    void Reset(Guid userId, FitnessGoal goal);
    void AddWarmup(IEnumerable<WorkoutTemplate> availableTemplates);
    void AddMainWorkouts(IEnumerable<WorkoutTemplate> availableTemplates, double bmi);
    UserProgram Build();
}

public interface IWorkoutObserver
{
    Task OnWorkoutCompletedAsync(WorkoutSession session);
}

public interface INotificationSender
{
    NotificationChannel Channel { get; }
    Task<string> SendAsync(Guid userId, string message);
}