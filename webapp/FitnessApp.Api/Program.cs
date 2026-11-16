using FitnessApp.Application.Patterns;
using FitnessApp.Application.Services;
using FitnessApp.Domain.Interfaces;
using FitnessApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=fitness.db"));

builder.Services.AddScoped<IWorkoutProgramBuilder, StandardProgramBuilder>();
builder.Services.AddScoped<ProgramDirector>();

builder.Services.AddScoped<ICalorieCalculationStrategy, CardioCalorieStrategy>();
builder.Services.AddScoped<ICalorieCalculationStrategy, StrengthCalorieStrategy>();
builder.Services.AddScoped<ICalorieCalculationStrategy, YogaCalorieStrategy>();

builder.Services.AddScoped<IWorkoutObserver, AchievementObserver>();
builder.Services.AddScoped<WorkoutEventPublisher>();

builder.Services.AddScoped<BaseAnalyticsReportService>();
builder.Services.AddScoped<IAnalyticsReportService>(sp =>
    new CachedAnalyticsReportDecorator(
        sp.GetRequiredService<BaseAnalyticsReportService>(),
        sp.GetRequiredService<IMemoryCache>()));

builder.Services.AddScoped<IWorkoutObserver, ChallengeProgressObserver>();
builder.Services.AddSingleton<NotificationSenderFactory>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();