using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Agriloco.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DevelopmentOnlyAttribute : Attribute, IAuthorizationFilter, IOrderedFilter
{
    public int Order => -3000;
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
            context.Result = new NotFoundResult();
        else if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            context.Result = new UnauthorizedResult();
    }
}
