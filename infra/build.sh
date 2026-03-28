#!/usr/bin/env bash
set -euo pipefail

###############################################################################
# build.sh — Build SimpleConnect (JS interop + Blazor + Functions)
#
# Produces a ready-to-deploy out/functions/ directory.
# Does NOT zip or deploy — use deploy.sh for that.
#
# Prerequisites: .NET 8 SDK, Node.js 18+
# Configuration: infra/.env.{environment} with ENTRA_CLIENT_ID
#
# Usage:
#   ./build.sh --env staging
#   ./build.sh --env prod
###############################################################################

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

ENVIRONMENT=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --env) ENVIRONMENT="$2"; shift 2 ;;
    --help)
      echo "Usage: $0 --env staging|prod"
      exit 0 ;;
    *) echo "ERROR: Unknown argument: $1" >&2; exit 1 ;;
  esac
done

if [[ -z "$ENVIRONMENT" ]]; then
  read -rp "Environment (staging/prod): " ENVIRONMENT
  if [[ "$ENVIRONMENT" != "staging" && "$ENVIRONMENT" != "prod" ]]; then
    echo "ERROR: Must be 'staging' or 'prod'" >&2; exit 1
  fi
fi

# ---------- load .env file ----------------------------------------------------
ENV_FILE="$SCRIPT_DIR/.env.$ENVIRONMENT"
ENTRA_CLIENT_ID=""

if [[ -f "$ENV_FILE" ]]; then
  while IFS='=' read -r key value; do
    value="${value%\"}"
    value="${value#\"}"
    case "$key" in
      ENTRA_CLIENT_ID) ENTRA_CLIENT_ID="$value" ;;
    esac
  done < <(grep -v '^\s*#' "$ENV_FILE" | grep '=')
else
  echo "ERROR: Settings file not found: $ENV_FILE" >&2
  echo "  Run setup.sh first to create the environment." >&2
  exit 1
fi

if [[ -z "$ENTRA_CLIENT_ID" ]]; then
  echo "ERROR: ENTRA_CLIENT_ID not found in $ENV_FILE. Run setup.sh first." >&2
  exit 1
fi

echo "============================================================"
echo " SimpleConnect — Build ($ENVIRONMENT)"
echo "============================================================"
echo " Entra Client ID: $ENTRA_CLIENT_ID"
echo "============================================================"

# ---------- error handler -----------------------------------------------------
CURRENT_STEP=""
MANUAL_HINT=""
on_error() {
  echo "" >&2
  echo "FAILED: $CURRENT_STEP" >&2
  if [[ -n "$MANUAL_HINT" ]]; then
    echo "To do this manually:" >&2
    echo "  $MANUAL_HINT" >&2
  fi
  exit 1
}
trap on_error ERR

# ---------- 1. Build JS interop -----------------------------------------------
CURRENT_STEP="Building JS interop"
MANUAL_HINT="cd src/SimpleConnect.JsInterop && npm install && npm run build"
echo ""
echo ">>> $CURRENT_STEP..."
cd "$PROJECT_ROOT/src/SimpleConnect.JsInterop"
npm install --silent
npm run build
echo "  Built acs-interop.js"

# ---------- 2. Build Blazor client --------------------------------------------
CURRENT_STEP="Building Blazor client"
MANUAL_HINT="dotnet publish src/SimpleConnect.Client -c Release -o out/client"
echo ""
echo ">>> $CURRENT_STEP..."
cd "$PROJECT_ROOT"
rm -rf "$PROJECT_ROOT/out/client"
dotnet publish src/SimpleConnect.Client -c Release -o out/client --nologo --verbosity quiet
echo "  Blazor client published"

# ---------- 3. Inject Entra config into publish output ------------------------
CURRENT_STEP="Injecting Entra configuration into publish output"
MANUAL_HINT="Edit out/client/wwwroot/appsettings.json with your Entra Client ID"
echo ""
echo ">>> $CURRENT_STEP..."
cat > "$PROJECT_ROOT/out/client/wwwroot/appsettings.json" << APPSETTINGS_EOF
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/consumers",
    "ClientId": "$ENTRA_CLIENT_ID",
    "ValidateAuthority": true,
    "ApiScope": "api://$ENTRA_CLIENT_ID/access_as_user"
  }
}
APPSETTINGS_EOF
echo "  Injected Client ID into publish output"

# ---------- 4. Publish Functions ----------------------------------------------
CURRENT_STEP="Publishing Functions"
MANUAL_HINT="dotnet publish src/SimpleConnect.Functions -c Release -o out/functions"
echo ""
echo ">>> $CURRENT_STEP..."
rm -rf "$PROJECT_ROOT/out/functions"
dotnet publish src/SimpleConnect.Functions -c Release -o out/functions --nologo --verbosity quiet
echo "  Functions published"

# ---------- 5. Merge Blazor output into Functions wwwroot ---------------------
CURRENT_STEP="Merging Blazor assets into Functions output"
MANUAL_HINT="cp -r out/client/wwwroot/* out/functions/wwwroot/"
echo ""
echo ">>> $CURRENT_STEP..."
cp -r "$PROJECT_ROOT/out/client/wwwroot/"* "$PROJECT_ROOT/out/functions/wwwroot/"
echo "  Merged"

# ---------- done ---------------------------------------------------------------
trap - ERR
echo ""
echo "============================================================"
echo " Build complete!"
echo "============================================================"
echo " Output: out/functions/"
echo " Config: Entra Client ID $ENTRA_CLIENT_ID injected"
echo ""
echo " Next: ./deploy.sh --env $ENVIRONMENT"
echo "============================================================"
