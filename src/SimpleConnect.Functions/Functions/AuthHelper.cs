using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using SimpleConnect.Functions.Configuration;
using SimpleConnect.Functions.Middleware;

namespace SimpleConnect.Functions.Functions;

public static class AuthHelper
{
    public static bool IsAdmin(FunctionContext context, AppSettings settings)
    {
        var principal = GetUser(context);
        if (principal is null)
        {
            return false;
        }

        var objectId = principal.FindFirstValue("oid")
            ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");

        if (string.IsNullOrEmpty(objectId))
        {
            return false;
        }

        return settings.AdminObjectIds.Contains(objectId);
    }

    public static ClaimsPrincipal? GetUser(FunctionContext context)
    {
        if (context.Items.TryGetValue(AuthenticationMiddleware.UserContextKey, out var user))
        {
            return user as ClaimsPrincipal;
        }

        return null;
    }
}
