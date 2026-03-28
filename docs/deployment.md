# Deployment

## Scripts

| Script | Purpose |
|--------|---------|
| `infra/setup.sh` / `setup.ps1` | First-time setup: creates Entra app, resource group, ARM deployment, builds and deploys code |
| `infra/build.sh` / `build.ps1` | Build only: JS interop, Blazor client, Functions, merged into `out/functions/` |
| `infra/deploy.sh` / `deploy.ps1` | Build + deploy: calls `build.sh` then zips and pushes to Azure |

### First-time setup (local)

```bash
az login
cd infra
./setup.sh        # prompts for environment, region, etc.
```

The setup script:
1. Verifies Azure CLI login
2. Creates or reuses an Entra ID app registration (SPA redirect URIs configured automatically)
3. Gets your user Object ID for admin access
4. Creates the resource group
5. Deploys the ARM template (`azuredeploy.json`)
6. Saves all settings to `.env.{environment}` (gitignored -- never committed)
7. Updates Entra redirect URIs with the deployed URL
8. Calls `build.sh` and `deploy.sh` to build and deploy code

Use `--infra-only` to skip the build/deploy step.

### Build only

```bash
cd infra
./build.sh --env staging
```

Reads `ENTRA_CLIENT_ID` from `.env.{environment}`, builds everything, injects config into the publish output. Produces `out/functions/` ready to deploy. No Azure credentials needed.

### Code-only redeployment

```bash
cd infra
./deploy.sh --env staging
```

Calls `build.sh` internally, then zips `out/functions/` and deploys via `az functionapp deployment source config-zip`.

## Settings Storage

### Local: `.env.{environment}` files

The setup script saves all deployment settings to `infra/.env.{environment}` (e.g., `.env.staging`). These files are **gitignored** and never committed. They contain:

- `ENTRA_CLIENT_ID` -- your Entra app registration ID
- `ADMIN_OBJECT_IDS` -- your Entra user Object IDs
- `RESOURCE_GROUP`, `LOCATION`, `PROJECT_NAME` -- Azure resource identifiers
- `FUNCTION_APP_NAME`, `FUNCTION_APP_URL`, `KEY_VAULT_NAME` -- deployed resource names

The build and deploy scripts read from these files automatically.

### GitHub: Secrets and Variables

For GitHub Actions deployments, the same settings are stored in **GitHub repo Settings > Environments**:

- **Secrets** (encrypted, not visible): `AZURE_CREDENTIALS`, `ADMIN_OBJECT_IDS`
- **Variables** (non-secret config): `ENTRA_CLIENT_ID`, `RESOURCE_GROUP`, `FUNCTION_APP_NAME`, `PROJECT_NAME`, `LOCATION`, `KEY_VAULT_NAME`

Both approaches keep sensitive values out of the repository.

## GitHub Actions

The `.github/workflows/deploy.yml` workflow enables code deployments from the GitHub **Actions** tab.

### One-time setup

1. **Run `setup.sh` locally first** to create the Entra app and Azure resources.

2. **Create a service principal** for GitHub Actions:
   ```bash
   az ad sp create-for-rbac \
     --name "github-simpleconnect" \
     --role contributor \
     --scopes /subscriptions/{SUBSCRIPTION_ID}/resourceGroups/{RESOURCE_GROUP} \
     --json-auth
   ```
   Copy the JSON output.

3. **Configure GitHub Environments** (repo Settings > Environments):

   Create environments `staging` and `prod`, then add:

   | Type | Name | Value |
   |------|------|-------|
   | Secret | `AZURE_CREDENTIALS` | The JSON from step 2 |
   | Secret | `ADMIN_OBJECT_IDS` | Your Entra Object IDs (comma-separated) |
   | Variable | `ENTRA_CLIENT_ID` | From `infra/.env.{env}` |
   | Variable | `RESOURCE_GROUP` | From `infra/.env.{env}` |
   | Variable | `FUNCTION_APP_NAME` | From `infra/.env.{env}` |
   | Variable | `PROJECT_NAME` | From `infra/.env.{env}` |
   | Variable | `LOCATION` | From `infra/.env.{env}` |
   | Variable | `KEY_VAULT_NAME` | From `infra/.env.{env}` |

### Deploying

1. Go to the **Actions** tab in your GitHub repo
2. Select the **Deploy** workflow
3. Click **Run workflow**
4. Choose the environment (staging / prod)
5. Click the green **Run workflow** button

The workflow builds the code and deploys to Azure. A summary with the deployment URL appears in the workflow run.

## Azure Resources

The ARM template (`infra/azuredeploy.json`) creates:

| Resource | SKU | Purpose |
|----------|-----|---------|
| Azure Communication Services | Free | Video/audio calling |
| Storage Account | Standard LRS | Functions runtime + JWT token tracking (Table Storage) |
| App Service Plan | Y1 Dynamic (Consumption) | Scales to zero when idle |
| Azure Functions App | .NET 8 Isolated, Linux | API + static file hosting |
| Key Vault | Standard | Stores the JWT signing secret |
| Application Insights | Free tier (5 GB/month) | Monitoring and telemetry |

Naming convention: `{projectName}-{environment}-{suffix}` (e.g., `simcon-staging-func`).

## Parameters

Parameter files are at `infra/parameters.{environment}.json`:

| Parameter | Default | Description |
|-----------|---------|-------------|
| `projectName` | `simcon` | Resource name prefix |
| `environmentName` | _(required)_ | `staging` or `prod` |
| `acsDataLocation` | `Asia Pacific` | ACS data residency region |
| `tokenExpiryHours` | `24` | JWT invite token lifetime |

The setup script also injects `entraTenantId`, `entraClientId`, and `adminObjectIds` at deploy time.

## Manual Deployment

If you prefer not to use the scripts:

```bash
# 1. Create resource group
az group create --name rg-simcon-staging --location southeastasia

# 2. Deploy ARM template
az deployment group create \
  --resource-group rg-simcon-staging \
  --template-file infra/azuredeploy.json \
  --parameters @infra/parameters.staging.json \
  --parameters entraTenantId="consumers" entraClientId="your-client-id" adminObjectIds="your-object-id"

# 3. Build JS interop
cd src/SimpleConnect.JsInterop && npm install && npm run build && cd ../..

# 4. Publish Blazor client
dotnet publish src/SimpleConnect.Client -c Release -o out/client

# 5. Publish Functions
dotnet publish src/SimpleConnect.Functions -c Release -o out/functions

# 6. Merge Blazor output into Functions wwwroot
cp -r out/client/wwwroot/* out/functions/wwwroot/

# 7. Zip and deploy
cd out/functions && zip -r ../deploy.zip . && cd ../..
az functionapp deployment source config-zip \
  --resource-group rg-simcon-staging \
  --name simcon-staging-func \
  --src out/deploy.zip
```

## Cost Estimate

For family use (a few calls per week, 2-4 participants):

| Service | Cost |
|---------|------|
| Azure Communication Services | ~$0.004/participant/minute = $1-3/month |
| Azure Functions (Consumption) | Free tier (1M executions/month) |
| Azure Storage | Pennies/month |
| Application Insights | Free tier (5 GB/month) |
| **Total** | **~$1-3/month** |
