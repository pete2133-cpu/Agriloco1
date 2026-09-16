using System.Security.Claims;
using Agriloco.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Agriloco.Api.Security;

// Runs before antiforgery/model validation, then checks the bound farm before mutation.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class FarmMapWriteAttribute(string farmArgument = "farmId") : Attribute,
    IAsyncAuthorizationFilter, IAsyncActionFilter, IAsyncPageFilter, IOrderedFilter
{
    public bool IncludeReads { get; set; }
    public bool AllowEditorToken { get; set; }
    public string? Resource { get; set; }
    public int Order => -2000;
    private static bool IsWrite(HttpContext http) =>
        !HttpMethods.IsGet(http.Request.Method) && !HttpMethods.IsHead(http.Request.Method) &&
        !HttpMethods.IsOptions(http.Request.Method);

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!IncludeReads && !IsWrite(context.HttpContext)) return;
        // Accept explicit bearer credentials only on opted-in Unity editor endpoints.
        // A failed bearer token never falls back to an ambient dashboard cookie.
        if (AllowEditorToken && context.HttpContext.Request.Headers.ContainsKey("Authorization"))
        {
            var http = context.HttpContext;
            var header = http.Request.Headers.Authorization.ToString();
            var principal = http.Request.IsHttps && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? await http.RequestServices.GetRequiredService<UnityEditorTokens>().ValidateAsync(header[7..])
                : null;
            if (principal == null) context.Result = new UnauthorizedResult();
            else http.User = principal;
            return; // Ownership is still checked below after model binding.
        }
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            context.Result = new UnauthorizedResult();
        else if (IsWrite(context.HttpContext))
        {
            // Browser cookies authenticate WebGL. Require the browser's Origin as CSRF protection,
            // including for the publish POST, which has no JSON body. Razor also validates tokens.
            var request = context.HttpContext.Request;
            if (!Uri.TryCreate(request.Headers.Origin.ToString(), UriKind.Absolute, out var origin) ||
                !string.Equals(origin.GetLeftPart(UriPartial.Authority),
                    $"{request.Scheme}://{request.Host}", StringComparison.OrdinalIgnoreCase))
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
        }
        return;
    }

    private async Task<IActionResult?> Check(HttpContext http, IDictionary<string, object?> arguments, object? page = null)
    {
        if (!IncludeReads && !IsWrite(http)) return null;
        var parts = farmArgument.Split('.');
        object? value = arguments.FirstOrDefault(x => x.Key.Equals(parts[0], StringComparison.OrdinalIgnoreCase)).Value;
        if (value == null && page != null)
            value = page.GetType().GetProperties().FirstOrDefault(p => p.Name.Equals(parts[0], StringComparison.OrdinalIgnoreCase))?.GetValue(page);
        foreach (var part in parts.Skip(1)) value = value?.GetType().GetProperty(part)?.GetValue(value);
        if (!int.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var memberId)) return new UnauthorizedResult();
        var db = http.RequestServices.GetRequiredService<AgrilocoContext>();
        // Navigation-only farmer pages have no farm-bound data. Still require an active membership.
        if (value == null && page != null)
            value = await db.Members.Where(m => m.Id == memberId && m.IsActive).Select(m => (int?)m.FarmId).FirstOrDefaultAsync();
        if (value is not int farmId || farmId <= 0) return new BadRequestObjectResult("A valid farmId is required.");
        if (Resource == "crop")
            farmId = await db.Crops.Where(c => c.Id == farmId).Select(c => c.FarmId).FirstOrDefaultAsync();
        var allowed = await db.Members.AsNoTracking().AnyAsync(m => m.Id == memberId && m.IsActive &&
            m.FarmId == farmId && m.Farm != null && m.Farm.IsActive);
        return allowed ? null : new StatusCodeResult(StatusCodes.Status403Forbidden);
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        context.Result = await Check(context.HttpContext, context.ActionArguments);
        if (context.Result == null) await next();
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;
    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        context.Result = await Check(context.HttpContext, context.HandlerArguments, context.HandlerInstance);
        if (context.Result == null) await next();
    }
}
