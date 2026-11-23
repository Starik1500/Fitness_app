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
            !Guid.TryParse(userIdHeader, out var userId))
        {
            context.Result = new UnauthorizedObjectResult("Потрібна авторизація (відсутній заголовок X-User-Id)");
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var user = await db.Users.FindAsync(userId);

        if (user == null || user.Role != UserRole.Admin)
        {
            context.Result = new ObjectResult("Доступ заборонено: потрібні права адміністратора") { StatusCode = 403 };
            return;
        }

        await next();
    }
}