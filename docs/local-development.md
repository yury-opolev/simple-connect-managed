# Local Development

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite) (local storage emulator) or Azure Storage connection string
- An Azure subscription with an Azure Communication Services resource
- An Entra ID (Azure AD) app registration

## Entra ID App Registration

1. Go to [Azure Portal > Entra ID > App registrations > New registration](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/CreateApplicationBlade)
2. Name: `simple-connect` (or your choice)
3. Supported account types: "Accounts in any organizational directory and personal Microsoft accounts"
4. Redirect URI: Platform = **Single-page application (SPA)**, URI = `http://localhost:7071/authentication/login-callback`
5. After creation, note the **Application (client) ID** and **Directory (tenant) ID**
6. Under **Expose an API**:
   - Set Application ID URI to `api://{client-id}`
   - Add a scope: `api://{client-id}/access_as_user` (Admin consent: Yes)
7. Under **Authentication** > Implicit grant: check **ID tokens**
8. For production, add additional redirect URIs for your deployed Function App URL

## Setup

### 1. Configure local settings

```bash
cp src/SimpleConnect.Functions/local.settings.json.template src/SimpleConnect.Functions/local.settings.json
```

Edit `local.settings.json` with your values:
- `ACS_CONNECTION_STRING`: From Azure Portal > your ACS resource > Keys
- `JWT_SECRET`: Any random string, minimum 32 characters
- `ENTRA_TENANT_ID`: Your Azure AD tenant ID
- `ENTRA_CLIENT_ID`: Your app registration client ID

### 2. Update Blazor Entra config

Edit `src/SimpleConnect.Client/wwwroot/appsettings.json`:
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/{YOUR_TENANT_ID}",
    "ClientId": "{YOUR_CLIENT_ID}",
    "ValidateAuthority": true,
    "ApiScope": "api://{YOUR_CLIENT_ID}/access_as_user"
  }
}
```

### 3. Build everything

The build script handles JS interop, Blazor client, and Functions in one step:

```bash
cd infra
./build.sh --env staging      # Bash
./build.ps1 -Environment staging   # PowerShell
```

Or manually:

```bash
cd src/SimpleConnect.JsInterop
npm install
npm run build
```

This outputs `acs-interop.js` to `src/SimpleConnect.Client/wwwroot/js/`.

```bash
dotnet publish src/SimpleConnect.Client -c Release -o out/client
dotnet publish src/SimpleConnect.Functions -c Release -o out/functions
cp -r out/client/wwwroot/* out/functions/wwwroot/
```

### 5. Start Azurite (storage emulator)

```bash
azurite --silent
```

### 6. Run the Functions app

```bash
cd src/SimpleConnect.Functions
func start
```

The app is now available at `http://localhost:7071`.

### 7. Run tests

```bash
dotnet test
```
