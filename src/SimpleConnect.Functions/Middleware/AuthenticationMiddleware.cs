using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using SimpleConnect.Functions.Configuration;

namespace SimpleConnect.Functions.Middleware;

public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    public const string UserContextKey = "AuthenticatedUser";

    private readonly AppSettings settings;
    private readonly ILogger<AuthenticationMiddleware> logger;
    private readonly JwtSecurityTokenHandler tokenHandler = new();
    private readonly ConfigurationManager<OpenIdConnectConfiguration> configManager;

    public AuthenticationMiddleware(AppSettings settings, ILogger<AuthenticationMiddleware> logger)
    {
        this.settings = settings;
        this.logger = logger;

        var authority = $"https://login.microsoftonline.com/{this.settings.EntraTenantId}/v2.0";
        var metadataUrl = $"{authority}/.well-known/openid-configuration";

        this.configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataUrl,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var requestData = await context.GetHttpRequestDataAsync();
        if (requestData is not null)
        {
            var token = ExtractBearerToken(requestData);
            if (!string.IsNullOrEmpty(token))
            {
                var principal = await this.ValidateTokenAsync(token);
                if (principal is not null)
                {
                    context.Items[UserContextKey] = principal;
                }
            }
        }

        await next(context);
    }

    private async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            var config = await this.configManager.GetConfigurationAsync(CancellationToken.None);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = new[]
                {
                    $"https://login.microsoftonline.com/{this.settings.EntraTenantId}/v2.0",
                    "https://login.microsoftonline.com/9188040d-6c67-4c5b-b112-36a304b66dad/v2.0"
                },
                ValidateAudience = true,
                ValidAudiences = new[]
                {
                    this.settings.EntraClientId,
                    $"api://{this.settings.EntraClientId}"
                },
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = config.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = this.tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (SecurityTokenException ex)
        {
            this.logger.LogWarning("Token validation failed: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error during token validation");
            return null;
        }
    }

    private static string? ExtractBearerToken(
        Microsoft.Azure.Functions.Worker.Http.HttpRequestData request)
    {
        if (!request.Headers.TryGetValues("Authorization", out var authValues))
        {
            return null;
        }

        var authHeader = authValues.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) ||
            !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authHeader["Bearer ".Length..];
    }
}
