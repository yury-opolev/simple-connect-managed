#Requires -Version 7.0
<#
.SYNOPSIS
    Build SimpleConnect (JS interop + Blazor + Functions).

.DESCRIPTION
    Produces a ready-to-deploy out/functions/ directory.
    Does NOT zip or deploy — use deploy.ps1 for that.

    Prerequisites: .NET 8 SDK, Node.js 18+
    Configuration: infra/.env.{environment} with ENTRA_CLIENT_ID

.PARAMETER Environment
    Deployment environment: staging or prod. Prompted if not provided.

.EXAMPLE
    ./build.ps1
    ./build.ps1 -Environment staging
#>

[CmdletBinding()]
param(
    [ValidateSet("staging", "prod")]
    [string]$Environment
)

$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot
$projectRoot = Split-Path $scriptDir -Parent

if ([string]::IsNullOrWhiteSpace($Environment)) {
    $Environment = Read-Host "Environment (staging/prod)"
    if ($Environment -notin @("staging", "prod")) {
        throw "Environment must be 'staging' or 'prod'"
    }
}

# ---------- load .env file ----------------------------------------------------
$envFile = Join-Path $scriptDir ".env.$Environment"
$entraClientId = ""

if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        if ($_ -match '^\s*([A-Z_]+)\s*=\s*"(.*)"\s*$') {
            switch ($Matches[1]) {
                "ENTRA_CLIENT_ID" { $entraClientId = $Matches[2] }
            }
        }
    }
} else {
    throw "Settings file not found: $envFile`nRun setup.ps1 first to create the environment."
}

if ([string]::IsNullOrWhiteSpace($entraClientId)) {
    throw "ENTRA_CLIENT_ID not found in $envFile. Run setup.ps1 first."
}

Write-Host "============================================================"
Write-Host " SimpleConnect - Build ($Environment)"
Write-Host "============================================================"
Write-Host " Entra Client ID: $entraClientId"
Write-Host "============================================================"

# ---------- 1. Build JS interop -----------------------------------------------
Write-Host ""
Write-Host ">>> Building JS interop (ACS Calling SDK)..."
try {
    Push-Location (Join-Path $projectRoot "src/SimpleConnect.JsInterop")
    npm install --silent
    npm run build
    Pop-Location
    Write-Host "  Built acs-interop.js"
} catch {
    Pop-Location -ErrorAction SilentlyContinue
    Write-Host "  FAILED: JS interop build failed" -ForegroundColor Red
    Write-Host "  Manual fix:" -ForegroundColor Yellow
    Write-Host "    cd src/SimpleConnect.JsInterop" -ForegroundColor Yellow
    Write-Host "    npm install && npm run build" -ForegroundColor Yellow
    throw
}

# ---------- 2. Build Blazor client --------------------------------------------
Write-Host ""
Write-Host ">>> Building Blazor client..."
try {
    $clientPublishDir = Join-Path $projectRoot "out/client"
    if (Test-Path $clientPublishDir) { Remove-Item $clientPublishDir -Recurse -Force }
    dotnet publish (Join-Path $projectRoot "src/SimpleConnect.Client") `
        -c Release -o $clientPublishDir --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
    Write-Host "  Blazor client published"
} catch {
    Write-Host "  FAILED: Blazor client build failed" -ForegroundColor Red
    Write-Host "  Manual fix:" -ForegroundColor Yellow
    Write-Host "    dotnet publish src/SimpleConnect.Client -c Release -o out/client" -ForegroundColor Yellow
    throw
}

# ---------- 3. Inject Entra config into publish output ------------------------
Write-Host ""
Write-Host ">>> Injecting Entra configuration..."
try {
    $publishedAppsettings = Join-Path $clientPublishDir "wwwroot/appsettings.json"
    $appsettingsContent = @{
        AzureAd = @{
            Authority = "https://login.microsoftonline.com/consumers"
            ClientId = $entraClientId
            ValidateAuthority = $true
            ApiScope = "api://$entraClientId/access_as_user"
        }
    } | ConvertTo-Json -Depth 3
    Set-Content -Path $publishedAppsettings -Value $appsettingsContent -Encoding UTF8
    Write-Host "  Injected Client ID into publish output"
} catch {
    Write-Host "  FAILED: Could not inject appsettings.json" -ForegroundColor Red
    Write-Host "  Manual fix: edit out/client/wwwroot/appsettings.json with your Entra Client ID" -ForegroundColor Yellow
    throw
}

# ---------- 4. Publish Functions ----------------------------------------------
Write-Host ""
Write-Host ">>> Publishing Functions..."
try {
    $functionsPublishDir = Join-Path $projectRoot "out/functions"
    if (Test-Path $functionsPublishDir) { Remove-Item $functionsPublishDir -Recurse -Force }
    dotnet publish (Join-Path $projectRoot "src/SimpleConnect.Functions") `
        -c Release -o $functionsPublishDir --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
    Write-Host "  Functions published"
} catch {
    Write-Host "  FAILED: Functions publish failed" -ForegroundColor Red
    Write-Host "  Manual fix:" -ForegroundColor Yellow
    Write-Host "    dotnet publish src/SimpleConnect.Functions -c Release -o out/functions" -ForegroundColor Yellow
    throw
}

# ---------- 5. Merge Blazor output into Functions wwwroot ---------------------
Write-Host ""
Write-Host ">>> Merging Blazor assets into Functions output..."
try {
    $blazorWwwroot = Join-Path $clientPublishDir "wwwroot"
    $functionsWwwroot = Join-Path $functionsPublishDir "wwwroot"
    if (Test-Path $blazorWwwroot) {
        & robocopy $blazorWwwroot $functionsWwwroot /E /IS /IT /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -le 7) { $LASTEXITCODE = 0 }
        Write-Host "  Merged"
    }
} catch {
    Write-Host "  FAILED: Could not merge Blazor assets" -ForegroundColor Red
    Write-Host "  Manual fix:" -ForegroundColor Yellow
    Write-Host "    Copy contents of out/client/wwwroot/ into out/functions/wwwroot/" -ForegroundColor Yellow
    throw
}

# ---------- done ---------------------------------------------------------------
Write-Host ""
Write-Host "============================================================"
Write-Host " Build complete!"
Write-Host "============================================================"
Write-Host " Output       : out/functions/"
Write-Host " Entra Client : $entraClientId"
Write-Host ""
Write-Host " Next: ./deploy.ps1 -Environment $Environment"
Write-Host "============================================================"
