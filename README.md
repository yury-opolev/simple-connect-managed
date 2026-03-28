# Simple Connect

Browser-based video calling for families. Create a room, share a link, connect -- no app install needed.

## Why Simple Connect?

- **Zero friction for guests** -- click a link, allow camera, you're in. No accounts, no downloads.
- **Works everywhere** -- desktop browsers, iPhone Safari, Android Chrome.
- **Privacy-first** -- runs on your own Azure subscription. No third-party services see your calls.
- **Dirt cheap** -- Azure consumption tier + ACS at ~$0.004/min/participant. A few calls a week costs $1-3/month.
- **Single-use invite links** -- each link works once, expires automatically, and is bound to a specific room.

## Quick Start

### First-time setup

You need an Azure subscription and three tools installed:
[Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli),
[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0),
[Node.js 18+](https://nodejs.org/).

```bash
az login
cd infra
./setup.sh        # Bash (prompts for environment, region)
./setup.ps1       # PowerShell
```

The script handles everything: Entra ID app registration, Azure resource deployment (ACS, Functions, Storage, Key Vault), code build, and deployment. Settings are saved to `infra/.env.{environment}` for future use.

### Build only (no deployment)

```bash
cd infra
./build.sh --env staging      # Bash
./build.ps1 -Environment staging   # PowerShell
```

Builds JS interop, Blazor client, and Functions into `out/functions/`. Useful for local development or inspecting build output.

### Redeploy after code changes

```bash
cd infra
./deploy.sh --env staging      # Bash (builds + deploys)
./deploy.ps1 -Environment staging   # PowerShell
```

### Deploy from GitHub

Code-only deployments can be triggered from the GitHub **Actions** tab using the **Deploy** workflow. See [Deployment > GitHub Actions](docs/deployment.md#github-actions) for one-time setup instructions.

## Usage

**As host:** sign in with your Microsoft account, create a room, copy the invite links, join the call.

**As guest:** open the invite link, join. That's it.

## Docs

- [Local Development](docs/local-development.md) -- prerequisites, Entra ID setup, running locally
- [Deployment](docs/deployment.md) -- Azure resources, scripts, GitHub Actions
- [Architecture](docs/architecture.md) -- project structure, security model, tech stack details
