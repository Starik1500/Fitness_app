using FitnessApp.Domain.Enums;
using FitnessApp.Domain.Interfaces;

namespace FitnessApp.Application.Patterns;

public class CardioCalorieStrategy : ICalorieCalculationStrategy
{
    public WorkoutType SupportedType => WorkoutType.Cardio;
    public double Calculate(double weightKg, int durationMinutes, double baseMet) =>
        Math.Round(baseMet * weightKg * (durationMinutes / 60.0) * 1.1, 1);
}

public class StrengthCalorieStrategy : ICalorieCalculationStrategy
{
    public WorkoutType SupportedType => WorkoutType.Strength;
    public double Calculate(double weightKg, int durationMinutes, double baseMet) =>
        Math.Round(baseMet * weightKg * (durationMinutes / 60.0) * 1.05, 1);
}

public class YogaCalorieStrategy : ICalorieCalculationStrategy
{
    public WorkoutType SupportedType => WorkoutType.Yoga;
    public double Calculate(double weightKg, int durationMinutes, double baseMet) =>
        Math.Round(baseMet * weightKg * (durationMinutes / 60.0) * 0.9, 1);
}