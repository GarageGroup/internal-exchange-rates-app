#!/usr/bin/env bash
set -euo pipefail

required=(APIM_RESOURCE_GROUP APIM_SERVICE_NAME AZURE_RESOURCE_GROUP_NAME AZURE_FUNCTION_NAME AZURE_NAME_POSTFIX)
for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done
[[ "$AZURE_NAME_POSTFIX" =~ ^(test|prod)$ ]] || { echo 'AZURE_NAME_POSTFIX must be test or prod' >&2; exit 1; }
api_name="exchange-rates-$AZURE_NAME_POSTFIX"

function_subscription="$(az account show --query id --output tsv --only-show-errors)"
apim_subscription="${APIM_SUBSCRIPTION_ID:-$function_subscription}"
hostname="$(az functionapp show --subscription "$function_subscription" --resource-group "$AZURE_RESOURCE_GROUP_NAME" --name "$AZURE_FUNCTION_NAME" --query defaultHostName --output tsv --only-show-errors)"
[[ -n "$hostname" && "$hostname" != null ]] || { echo 'Function hostname was not found' >&2; exit 1; }

function_key="$(az functionapp keys list --subscription "$function_subscription" --resource-group "$AZURE_RESOURCE_GROUP_NAME" --name "$AZURE_FUNCTION_NAME" --query functionKeys.default --output tsv --only-show-errors)"
[[ -n "$function_key" && "$function_key" != null ]] || { echo 'Function default key is not available; check host readiness' >&2; exit 1; }
if [[ "${GITHUB_ACTIONS:-}" == true ]]; then
  echo "::add-mask::$function_key"
fi

umask 077
specification="$(mktemp)"
trap 'rm -f "$specification"; unset function_key' EXIT
# Use the specification embedded in the deployed ZIP, including on version rollback.
curl --silent --show-error --fail --retry 12 --retry-all-errors --retry-delay 10 \
  --connect-timeout 10 --max-time 30 --header "x-functions-key: $function_key" \
  --output "$specification" "https://${hostname}/api/swagger/swagger.json"
unset function_key
jq -e '.openapi == "3.0.3" and (.paths | type == "object")' "$specification" > /dev/null

# API, policy and Product are configured first; stable operation IDs preserve identity.
az apim api import \
  --subscription "$apim_subscription" \
  --resource-group "$APIM_RESOURCE_GROUP" \
  --service-name "$APIM_SERVICE_NAME" \
  --api-id "$api_name" \
  --display-name "Exchange Rates ($AZURE_NAME_POSTFIX)" \
  --path "$api_name" \
  --service-url "https://${hostname}/api/exchange-rates" \
  --protocols https \
  --subscription-required true \
  --specification-format OpenApiJson \
  --specification-path "$specification" \
  --output none --only-show-errors

echo "OpenAPI imported into APIM API '$api_name'."
