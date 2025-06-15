using Microsoft.AspNetCore.Authorization;
using RevenueRecognitionAPI.Services;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace RevenueRecognitionAPI.Middleware;

public class BasicAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private const string AuthenticationScheme = "Basic";

    public BasicAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRevenueService revenueService)
    {
        if (ShouldSkipAuthentication(context.Request.Path))
        {
            await _next(context);
            return;
        }
        
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null)
        {
            await _next(context);
            return;
        }
        
        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            await ReturnUnauthorized(context);
            return;
        }

        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(context.Request.Headers["Authorization"]);
            
            if (!AuthenticationScheme.Equals(authHeader.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                await ReturnUnauthorized(context);
                return;
            }

            if (string.IsNullOrEmpty(authHeader.Parameter))
            {
                await ReturnUnauthorized(context);
                return;
            }

            var credentials = ExtractCredentials(authHeader.Parameter);
            if (credentials == null)
            {
                await ReturnUnauthorized(context);
                return;
            }
            
            var isValid = await revenueService.ValidateEmployeeCredentials(credentials.Value.login, credentials.Value.password);
            if (!isValid)
            {
                await ReturnUnauthorized(context);
                return;
            }
            
            var employee = await revenueService.GetEmployeeByLogin(credentials.Value.login);
            if (employee == null)
            {
                await ReturnUnauthorized(context);
                return;
            }
            
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, employee.Login),
                new Claim(ClaimTypes.NameIdentifier, employee.Id.ToString()),
                new Claim(ClaimTypes.Role, employee.Role.ToString()),
                new Claim("FirstName", employee.FirstName),
                new Claim("LastName", employee.LastName)
            };

            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            context.User = principal;

            await _next(context);
        }
        catch
        {
            await ReturnUnauthorized(context);
        }
    }

    private static (string login, string password)? ExtractCredentials(string parameter)
    {
        try
        {
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(parameter));
            var parts = credentials.Split(':', 2);
            
            if (parts.Length == 2)
            {
                return (parts[0], parts[1]);
            }
        }
        catch
        {
        }
        
        return null;
    }

    private static bool ShouldSkipAuthentication(PathString path)
    {
        var pathsToSkip = new[]
        {
            "/swagger",
            "/health",
            "/favicon.ico"
        };

        return pathsToSkip.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task ReturnUnauthorized(HttpContext context)
    {
        context.Response.StatusCode = 401;
        context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Revenue Recognition API\"";
        await context.Response.WriteAsync("Unauthorized");
    }
}