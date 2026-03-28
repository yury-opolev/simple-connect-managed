#!/usr/bin/env bash
set -euo pipefail

###############################################################################
# deploy.sh — Build and deploy code to an existing SimpleConnect environment
#
# Use setup.sh for first-time setup (creates Entra app, resource group, etc.)
# Use this script for subsequent code redeployments.
#
# Usage:
#   ./deploy.sh --env staging
#   ./deploy.sh --env prod
###############################################################################

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

ENVIRONMENT=""
RESOURCE_GROUP=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --env)            ENVIRONMENT="$2"; shift 2 ;;
    --resource-group) RESOURCE_GROUP="$2"; shift 2 ;;
    --help)
      echo "Usage: $0 --env staging|prod [--resource-group <rg-name>]"
      exit 0 ;;
    *)
      echo "ERROR: Unknown argument: $1" >&2; exit 1 ;;
  esac
done

[[ -z "$ENVIRONMENT" ]] && { echo "ERROR: --env is required" >&2; exit 1; }

# ---------- load .env file ----------------------------------------------------
ENV_FILE="$SCRIPT_DIR/.env.$ENVIRONMENT"
FUNCTION_APP_NAME=""

if [[ -f "$ENV_FILE" ]]; then
  while IFS='=' read -r key value; do
    value="${value%\"}"
    value="${value#\"}"
    case "$key" in
      RESOURCE_GROUP)    [[ -z "$RESOURCE_GROUP" ]] && RESOURCE_GROUP="$value" ;;
      FUNCTION_APP_NAME) FUNCTION_APP_NAME="$value" ;;
    esac
  done < <(grep -v '^\s*#' "$ENV_FILE" | grep '=')
else
  echo "ERROR: Settings file not found: $ENV_FILE" >&2
  echo "  Run setup.sh first to create the environment." >&2
  exit 1
fi

[[ -z "$RESOURCE_GROUP" ]] && { echo "ERROR: RESOURCE_GROUP not found in $ENV_FILE and not provided via --resource-group" >&2; exit 1; }
[[ -z "$FUNCTION_APP_NAME" ]] && { echo "ERROR: FUNCTION_APP_NAME not found in $ENV_FILE. Run setup.sh first." >&2; exit 1; }

echo "============================================================"
echo " SimpleConnect — Deploy ($ENVIRONMENT)"
echo "============================================================"
echo " Function App: $FUNCTION_APP_NAME"
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

# ---------- 1. Build ----------------------------------------------------------
CURRENT_STEP="Building application"
MANUAL_HINT="./build.sh --env $ENVIRONMENT"
echo ""
echo ">>> $CURRENT_STEP..."
"$SCRIPT_DIR/build.sh" --env "$ENVIRONMENT"

# ---------- 2. Zip and deploy -------------------------------------------------
CURRENT_STEP="Deploying to Azure"
MANUAL_HINT="cd out/functions && zip -r ../deploy.zip . && az functionapp deployment source config-zip --resource-group $RESOURCE_GROUP --name $FUNCTION_APP_NAME --src out/deploy.zip"
echo ""
echo ">>> Deploying to $FUNCTION_APP_NAME..."
DEPLOY_ZIP="$PROJECT_ROOT/out/deploy.zip"
rm -f "$DEPLOY_ZIP"
cd "$PROJECT_ROOT/out/functions"
zip -r "$DEPLOY_ZIP" . --quiet

az functionapp deployment source config-zip \
  --resource-group "$RESOURCE_GROUP" \
  --name "$FUNCTION_APP_NAME" \
  --src "$DEPLOY_ZIP" \
  --output none

# ---------- done ---------------------------------------------------------------
trap - ERR
echo ""
echo "============================================================"
echo " Deployment complete!"
echo " https://${FUNCTION_APP_NAME}.azurewebsites.net"
echo "============================================================"
