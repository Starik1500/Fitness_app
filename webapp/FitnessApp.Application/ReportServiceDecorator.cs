using Microsoft.Extensions.Caching.Memory;

namespace FitnessApp.Application.Services;

public record PopularWorkoutDto(string Title, int SessionsCount);
public record AverageActivityDto(double AvgMinutesPerUser, double AvgCaloriesPerUser);

public interface IAnalyticsReportService
{
    Task<List<PopularWorkoutDto>> GetPopularWorkoutsAsync();
    Task<AverageActivityDto> GetAverageActivityAsync(DateTime from, DateTime to);
}

public class CachedAnalyticsReportDecorator : IAnalyticsReportService
{
    private readonly IAnalyticsReportService _inner;
    private readonly IMemoryCache _cache;

    public CachedAnalyticsReportDecorator(IAnalyticsReportService inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<List<PopularWorkoutDto>> GetPopularWorkoutsAsync()
    {
        return (await _cache.GetOrCreateAsync("popular_workouts", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await _inner.GetPopularWorkoutsAsync();
        }))!;
    }

    public Task<AverageActivityDto> GetAverageActivityAsync(DateTime from, DateTime to) =>
        _inner.GetAverageActivityAsync(from, to);
}