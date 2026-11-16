using FitnessApp.Application.Patterns;
using FitnessApp.Domain.Entities;
using FitnessApp.Domain.Enums;
using FitnessApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Controllers;

public record RegisterRequest(string Email, string Password, Gender Gender, DateOnly BirthDate, double HeightCm, double WeightKg);
public record LoginRequest(string Email, string Password);
public record ProfileUpdateRequest(Gender Gender, DateOnly BirthDate, double HeightCm, double WeightKg);
public record ReminderRequest(Guid UserId, string CronExpression, NotificationChannel Channel);

[ApiController]
[Route("api")]
public class AuthAndProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly NotificationSenderFactory _notifFactory;

    public AuthAndProfileController(AppDbContext db, NotificationSenderFactory notifFactory)
    {
        _db = db;
        _notifFactory = notifFactory;
    }

    [HttpPost("auth/register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest("Email вже зареєстрований");

        var user = new User { Email = req.Email, PasswordHash = req.Password, Role = UserRole.User };
        var profile = new UserProfile
        {
            UserId = user.Id,
            Gender = req.Gender,
            BirthDate = req.BirthDate,
            HeightCm = req.HeightCm,
            CurrentWeightKg = req.WeightKg
        };
        var initialWeight = new WeightLog { UserId = user.Id, WeightKg = req.WeightKg };

        _db.Users.Add(user);
        _db.UserProfiles.Add(profile);
        _db.WeightLogs.Add(initialWeight);
        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.Email, Role = user.Role.ToString(), Profile = profile, Bmi = profile.CalculateBMI() });
    }

    [HttpPost("auth/login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users.Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email == req.Email && u.PasswordHash == req.Password);

        if (user == null) return Unauthorized("Невірний email або пароль");

        return Ok(new
        {
            user.Id,
            user.Email,
            Role = user.Role.ToString(),
            Profile = user.Profile,
            Bmi = user.Profile?.CalculateBMI() ?? 0
        });
    }

    [HttpGet("profile/{userId}")]
    public async Task<IActionResult> GetProfile(Guid userId)
    {
        var user = await _db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Profile == null) return NotFound();

        return Ok(new { user.Id, user.Email, Role = user.Role.ToString(), Profile = user.Profile, Bmi = user.Profile.CalculateBMI() });
    }

    [HttpPut("profile/{userId}")]
    public async Task<IActionResult> UpdateProfile(Guid userId, [FromBody] ProfileUpdateRequest req)
    {
        var profile = await _db.UserProfiles.FindAsync(userId);
        if (profile == null) return NotFound();

        profile.Gender = req.Gender;
        profile.BirthDate = req.BirthDate;
        profile.HeightCm = req.HeightCm;
        profile.CurrentWeightKg = req.WeightKg;

        _db.WeightLogs.Add(new WeightLog { UserId = userId, WeightKg = req.WeightKg });
        await _db.SaveChangesAsync();

        return Ok(new { Profile = profile, Bmi = profile.CalculateBMI() });
    }

    [HttpPost("profile/{userId}/weight")]
    public async Task<IActionResult> LogWeight(Guid userId, [FromQuery] double weightKg)
    {
        var profile = await _db.UserProfiles.FindAsync(userId);
        if (profile == null) return NotFound();

        profile.CurrentWeightKg = weightKg;
        var log = new WeightLog { UserId = userId, WeightKg = weightKg };
        _db.WeightLogs.Add(log);
        await _db.SaveChangesAsync();

        return Ok(new { Log = log, NewBmi = profile.CalculateBMI() });
    }

    [HttpGet("reminders/{userId}")]
    public async Task<IActionResult> GetReminders(Guid userId) =>
        Ok(await _db.Reminders.Where(r => r.UserId == userId).ToListAsync());

    [HttpPost("reminders")]
    public async Task<IActionResult> CreateReminder([FromBody] ReminderRequest req)
    {
        var reminder = new Reminder { UserId = req.UserId, CronExpression = req.CronExpression, Channel = req.Channel };
        _db.Reminders.Add(reminder);
        await _db.SaveChangesAsync();

        var sender = _notifFactory.CreateSender(req.Channel);
        var preview = await sender.SendAsync(req.UserId, $"Нагадування заплановано за розкладом: {req.CronExpression}");

        return Ok(new { Reminder = reminder, DeliveryPreview = preview });
    }
}