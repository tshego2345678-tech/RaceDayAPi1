using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace RaceDayMvc.Filters;

/// <summary>Session-based guard: login redirect when unauthenticated, role redirect when the role is wrong.</summary>
public class RoleAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string? _role;
    public RoleAuthorizeAttribute(string? role = null) { _role = role; }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var s = context.HttpContext.Session;
        if (string.IsNullOrEmpty(s.GetString("Token")))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }
        if (_role != null && s.GetString("Role") != _role)
        {
            context.Result = s.GetString("Role") == "Organiser"
                ? new RedirectToActionResult("Dashboard", "Organiser", null)
                : new RedirectToActionResult("Index", "Home", null);
        }
    }
}
