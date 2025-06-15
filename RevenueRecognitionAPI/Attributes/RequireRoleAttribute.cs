using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RevenueRecognitionAPI.Models;
using System.Security.Claims;

namespace RevenueRecognitionAPI.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly EmployeeRole _requiredRole;

    public RequireRoleAttribute(EmployeeRole requiredRole)
    {
        _requiredRole = requiredRole;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var roleClaimValue = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(roleClaimValue) || 
            !Enum.TryParse<EmployeeRole>(roleClaimValue, out var userRole))
        {
            context.Result = new ForbidResult();
            return;
        }
        
        if (_requiredRole == EmployeeRole.Admin && userRole != EmployeeRole.Admin)
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}

public class RequireAdminAttribute : RequireRoleAttribute
{
    public RequireAdminAttribute() : base(EmployeeRole.Admin)
    {
    }
}

public class RequireUserAttribute : RequireRoleAttribute
{
    public RequireUserAttribute() : base(EmployeeRole.StandardUser)
    {
    }
}