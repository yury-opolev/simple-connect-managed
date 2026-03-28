using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SimpleConnect.Client;
using SimpleConnect.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient with base address pointing to the Functions API
var baseAddress = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });

// Register application services
builder.Services.AddScoped<ICallService, CallService>();
builder.Services.AddScoped<IApiClient, ApiClient>();

// Configure MSAL authentication with Entra ID
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add(
        builder.Configuration["AzureAd:ApiScope"] ?? "api://simple-connect/.default");
    options.ProviderOptions.LoginMode = "redirect";
});

await builder.Build().RunAsync();
