using FitnessApp.Application.Patterns;
using FitnessApp.Application.Services;
using FitnessApp.Domain.Entities;
using FitnessApp.Domain.Interfaces;
using FitnessApp.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FitnessTracker API",
        Version = "v1"
    });

    c.AddSecurityDefinition("UserId", new OpenApiSecurityScheme
    {
        Name = "X-User-Id",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Ідентифікатор користувача (GUID)"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("UserId", document)] = []
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
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    db.Database.EnsureCreated();

    var admin = db.Users.FirstOrDefault(u => u.Email == "admin@fitness.local");
    if (admin == null)
    {
        admin = new User
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            Email = "admin@fitness.local",
            Role = FitnessApp.Domain.Enums.UserRole.Admin
        };
        admin.PasswordHash = hasher.HashPassword(admin, "admin123");
        db.Users.Add(admin);
        db.SaveChanges();
    }
    else if (!admin.PasswordHash.StartsWith("AQAAAA"))
    {
        // Автоматично захешує старий пароль адміна, якщо fitness.db вже існувала
        admin.PasswordHash = hasher.HashPassword(admin, admin.PasswordHash);
        db.SaveChanges();
    }

    if (!db.UserProfiles.Any(p => p.UserId == admin.Id))
    {
        db.UserProfiles.Add(new UserProfile
        {
            UserId = admin.Id,
            Gender = FitnessApp.Domain.Enums.Gender.Male,
            BirthDate = new DateOnly(1995, 1, 1),
            HeightCm = 180,
            CurrentWeightKg = 80
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