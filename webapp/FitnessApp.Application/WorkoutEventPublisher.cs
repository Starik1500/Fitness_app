using FitnessApp.Domain.Entities;
using FitnessApp.Domain.Interfaces;

namespace FitnessApp.Application.Patterns;

public class WorkoutEventPublisher
{
    private readonly IEnumerable<IWorkoutObserver> _observers;
    public WorkoutEventPublisher(IEnumerable<IWorkoutObserver> observers) => _observers = observers;

    public async Task NotifyCompletedAsync(WorkoutSession session)
    {
        foreach (var observer in _observers)
            await observer.OnWorkoutCompletedAsync(session);
    }
}