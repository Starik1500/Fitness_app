using FitnessApp.Domain.Enums;
using FitnessApp.Domain.Interfaces;

namespace FitnessApp.Application.Patterns;

public class EmailNotificationSender : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.Email;
    public Task<string> SendAsync(Guid userId, string message) =>
        Task.FromResult($"[Email -> User {userId}]: {message}");
}

public class WebPushNotificationSender : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.WebPush;
    public Task<string> SendAsync(Guid userId, string message) =>
        Task.FromResult($"[WebPush -> User {userId}]: {message}");
}

public class NotificationSenderFactory
{
    public INotificationSender CreateSender(NotificationChannel channel) => channel switch
    {
        NotificationChannel.Email => new EmailNotificationSender(),
        NotificationChannel.WebPush => new WebPushNotificationSender(),
        _ => throw new ArgumentOutOfRangeException(nameof(channel), "Невідомий канал сповіщень")
    };
}