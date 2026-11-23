using FitnessApp.Application.Patterns;
using FitnessApp.Application.Services;
using FitnessApp.Domain.Interfaces;
using FitnessApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("UserIdAuth", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "X-User-Id",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Введіть GUID користувача (напр. 99999999-9999-9999-9999-999999999999 для Admin)"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "UserIdAuth" }
            },
            Array.Empty<string>()
        }
    });
});
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

    var admin = db.Users.FirstOrDefault(u => u.Email == "admin@fitness.local");
    if (admin == null)
    {
        admin = new FitnessApp.Domain.Entities.User
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            Email = "admin@fitness.local",
            PasswordHash = "admin123", // якщо у AuthController використовується хешування — підстав аналогічний хеш
            Role = FitnessApp.Domain.Enums.UserRole.Admin
        };
        db.Users.Add(admin);
        db.SaveChanges();
    }

    if (!db.UserProfiles.Any(p => p.UserId == admin.Id))
    {
        db.UserProfiles.Add(new FitnessApp.Domain.Entities.UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = admin.Id,
            Gender = FitnessApp.Domain.Enums.Gender.Male,
            BirthDate = new DateTime(1995, 1, 1),
            HeightCm = 180,
            WeightKg = 80
        });
        db.SaveChanges();
    }
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();