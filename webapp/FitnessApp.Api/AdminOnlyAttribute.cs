using FitnessApp.Domain.Enums;
using FitnessApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FitnessApp.Api;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminOnlyAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
            !Guid.TryParse(userIdHeader.ToString().Trim('"'), out var userId))
        {
            context.Result = new UnauthorizedObjectResult("Не передано коректний заголовок X-User-Id");
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var user = await db.Users.FindAsync(userId);

        if (user == null)
        {
            context.Result = new UnauthorizedObjectResult($"Користувача з ID {userId} не знайдено в БД");
            return;
        }

        if (user.Role != UserRole.Admin)
        {
            context.Result = new ObjectResult($"Доступ заборонено для {user.Email}: роль {user.Role}") { StatusCode = 403 };
            return;
        }

        await next();
    }
}