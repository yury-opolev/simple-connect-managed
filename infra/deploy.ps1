#Requires -Version 7.0
<#
.SYNOPSIS
    Build and deploy code to an existing SimpleConnect environment.

.DESCRIPTION
    Use setup.ps1 for first-time setup. Use this script for subsequent code redeployments.
    Loads settings from .env.{environment} persisted by setup.ps1.

.PARAMETER Environment
    Deployment environment: staging or prod. Prompted if not provided.

.EXAMPLE
    ./deploy.ps1
    ./deploy.ps1 -Environment staging
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
$resourceGroup = ""
$functionAppName = ""

if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        if ($_ -match '^\s*([A-Z_]+)\s*=\s*"(.*)"\s*$') {
            switch ($Matches[1]) {
                "RESOURCE_GROUP"    { $resourceGroup = $Matches[2] }
                "FUNCTION_APP_NAME" { $functionAppName = $Matches[2] }
            }
        }
    }
} else {
    throw "Settings file not found: $envFile`nRun setup.ps1 first to create the environment."
}

if ([string]::IsNullOrWhiteSpace($resourceGroup)) {
    throw "RESOURCE_GROUP not found in $envFile. Run setup.ps1 first."
}
if ([string]::IsNullOrWhiteSpace($functionAppName)) {
    throw "FUNCTION_APP_NAME not found in $envFile. Run setup.ps1 first."
}

Write-Host "============================================================"
Write-Host " SimpleConnect - Deploy ($Environment)"
Write-Host "============================================================"
Write-Host " Function App: $functionAppName"
Write-Host "============================================================"

# ---------- 1. Build ----------------------------------------------------------
Write-Host ""
Write-Host ">>> Building application..."
try {
    & "$scriptDir/build.ps1" -Environment $Environment
} catch {
    Write-Host "  FAILED: Build failed" -ForegroundColor Red
    Write-Host "  Manual fix:" -ForegroundColor Yellow
    Write-Host "    ./build.ps1 -Environment $Environment" -ForegroundColor Yellow
    throw
}

# ---------- 2. Zip and deploy -------------------------------------------------
Write-Host ""
Write-Host ">>> Deploying to $functionAppName..."
try {
    $functionsPublishDir = Join-Path $projectRoot "out/functions"
    $deployZip = Join-Path $projectRoot "out/deploy.zip"
    if (Test-Path $deployZip) { Remove-Item $deployZip -Force }
    Compress-Archive -Path "$functionsPublishDir/*" -DestinationPath $deployZip

    az functionapp deployment source config-zip `
        --resource-group $resourceGroup `
        --name $functionAppName `
        --src $deployZip `
        --output none
    if ($LASTEXITCODE -ne 0) { throw "az functionapp deploy failed with exit code $LASTEXITCODE" }
} catch {
    Write-Host "  FAILED: Azure deployment failed" -ForegroundColor Red
    Write-Host "  Manual fix:" -ForegroundColor Yellow
    Write-Host "    Compress-Archive -Path 'out/functions/*' -DestinationPath out/deploy.zip" -ForegroundColor Yellow
    Write-Host "    az functionapp deployment source config-zip --resource-group $resourceGroup --name $functionAppName --src out/deploy.zip" -ForegroundColor Yellow
    throw
}

Write-Host ""
Write-Host "============================================================"
Write-Host " Deployment complete!"
Write-Host " https://$functionAppName.azurewebsites.net"
Write-Host "============================================================"
