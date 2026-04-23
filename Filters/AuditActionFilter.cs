using System.Security.Claims;
using Airport.Models;
using Airport.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace Airport.Filters
{
    public class AuditActionFilter : IAsyncActionFilter
    {
        private readonly AuditLogService _auditLogService;

        public static readonly Dictionary<(string, string, string), string> Descriptions = new()
        {
            { ("Auth", "Login",          "POST"), "Вход в аккаунт" },
            { ("Auth", "Logout",         "POST"), "Выход из аккаунта" },
            { ("Auth", "Register",       "POST"), "Регистрация аккаунта" },
            { ("Auth", "ChangePassword", "POST"), "Изменение пароля" },
            { ("Auth", "UpdateProfile",  "POST"), "Изменение данных аккаунта" },

            { ("Admin", "EditUser", "POST"), "Изменение данных пользователя" },

            { ("Aircraft", "Create",  "POST"), "Добавление самолёта" },
            { ("Aircraft", "Edit",    "POST"), "Изменение самолёта" },
            { ("Aircraft", "Delete",  "POST"), "Удаление самолёта" },

            { ("Flight", "Create", "POST"), "Добавление рейса" },
            { ("Flight", "Edit",   "POST"), "Изменение рейса" },
            { ("Flight", "Delete", "POST"), "Удаление рейса" },

            { ("Landing", "Create", "POST"), "Добавление посадки" },
            { ("Landing", "Edit",   "POST"), "Изменение посадки" },
            { ("Landing", "Delete", "POST"), "Удаление посадки" },

            { ("Ticket", "Create", "POST"), "Покупка билета" },
            { ("Ticket", "Edit",   "POST"), "Изменение билета" },
            { ("Ticket", "Delete", "POST"), "Удаление билета" },

            { ("Home", "SearchFlights", "GET"), "Поиск рейсов" },
        };

        public static string GetDescription(string controller, string action, string method)
        {
            if (Descriptions.TryGetValue((controller, action, method), out var desc))
                return desc;
            return string.Empty;
        }

        public AuditActionFilter(AuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var resultContext = await next();

            try
            {
                var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "";
                var actionName     = context.RouteData.Values["action"]?.ToString() ?? "";
                var httpMethod     = context.HttpContext.Request.Method;

                var description = GetDescription(controllerName, actionName, httpMethod);

                if (string.IsNullOrEmpty(description))
                    return;

                var user = context.HttpContext.User;
                int? userId = null;
                var username = "Аноним";
                var userRole = "Гость";

                if (user.Identity?.IsAuthenticated == true)
                {
                    var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(idClaim, out int parsedId))
                        userId = parsedId;

                    username = user.FindFirst(ClaimTypes.Name)?.Value ?? "Неизвестно";
                    userRole = user.FindFirst(ClaimTypes.Role)?.Value ?? "User";
                }

                var statusCode = 200;
                if (resultContext.Result is StatusCodeResult sc)
                    statusCode = sc.StatusCode;
                else if (resultContext.Result is ObjectResult obj)
                    statusCode = obj.StatusCode ?? 200;
                else if (resultContext.Result is RedirectToActionResult or RedirectResult)
                    statusCode = 302;
                else if (resultContext.Result is NotFoundResult)
                    statusCode = 404;
                else if (resultContext.Result is ForbidResult)
                    statusCode = 403;

                var entry = new AuditLog
                {
                    UserId            = userId,
                    Username          = username,
                    UserRole          = userRole,
                    ActionDescription = description,
                    Controller        = controllerName,
                    Action            = actionName,
                    IpAddress         = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    StatusCode        = statusCode,
                    Timestamp         = DateTime.UtcNow
                };

                await _auditLogService.LogAsync(entry);
            }
            catch
            {
            }
        }
    }
}
