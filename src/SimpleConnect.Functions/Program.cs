using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleConnect.Functions.Configuration;
using SimpleConnect.Functions.Middleware;
using SimpleConnect.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(builder =>
    {
        builder.UseMiddleware<AuthenticationMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        var configuration = context.Configuration;

        var appSettings = new AppSettings
        {
            AcsConnectionString = configuration["ACS_CONNECTION_STRING"] ?? "",
            StorageConnectionString = configuration["AZURE_STORAGE_CONNECTION"]
                ?? configuration["AzureWebJobsStorage"] ?? "",
            JwtSecret = configuration["JWT_SECRET"] ?? "",
            TokenExpiryHours = int.TryParse(configuration["TOKEN_EXPIRY_HOURS"], out var h) ? h : 24,
            BaseUrl = configuration["BASE_URL"] ?? "",
            EntraTenantId = configuration["ENTRA_TENANT_ID"] ?? "",
            EntraClientId = configuration["ENTRA_CLIENT_ID"] ?? "",
            AdminObjectIds = (configuration["ADMIN_OBJECT_IDS"] ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
        };
        services.AddSingleton(appSettings);

        services.AddSingleton<IInviteTokenService, InviteTokenService>();
        services.AddSingleton<ITokenStoreService, TokenStoreService>();
        services.AddSingleton<IRoomService, RoomService>();
        services.AddSingleton<IRoomStoreService, RoomStoreService>();
    })
    .Build();

host.Run();
