#!/usr/bin/env bash
set -euo pipefail

required=(APIM_RESOURCE_GROUP APIM_SERVICE_NAME APIM_API_ID APIM_API_PATH APIM_PRODUCT_ID AZURE_RESOURCE_GROUP_NAME AZURE_FUNCTION_NAME)
for variable in "${required[@]}"; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
function_subscription="$(az account show --query id --output tsv --only-show-errors)"
apim_subscription="${APIM_SUBSCRIPTION_ID:-$function_subscription}"
hostname="$(az functionapp show --subscription "$function_subscription" --resource-group "$AZURE_RESOURCE_GROUP_NAME" --name "$AZURE_FUNCTION_NAME" --query defaultHostName --output tsv --only-show-errors)"
[[ -n "$hostname" && "$hostname" != null ]] || { echo 'Function hostname was not found' >&2; exit 1; }
# This is a host-level function key, never the administrative master key.
function_key="$(az functionapp keys list --subscription "$function_subscription" --resource-group "$AZURE_RESOURCE_GROUP_NAME" --name "$AZURE_FUNCTION_NAME" --query functionKeys.default --output tsv --only-show-errors)"
[[ -n "$function_key" && "$function_key" != null ]] || { echo 'Function default key is not available; deploy the Function and retry' >&2; exit 1; }
if [[ -n "${GITHUB_ACTIONS:-}" ]]; then
  echo "::add-mask::$function_key"
fi

umask 077
parameters_file="$(mktemp)"
trap 'rm -f "$parameters_file"' EXIT
# Pass the secret through stdin, not command-line arguments or deployment outputs.
printf '%s' "$function_key" | jq -Rs \
  --arg service "$APIM_SERVICE_NAME" --arg api "$APIM_API_ID" --arg path "$APIM_API_PATH" \
  --arg product "$APIM_PRODUCT_ID" --arg backend "https://${hostname}/api/exchange-rates" \
  '{parameters: {apimServiceName: {value: $service}, apiId: {value: $api}, apiPath: {value: $path}, productId: {value: $product}, backendServiceUrl: {value: $backend}, functionKey: {value: .}}}' > "$parameters_file"
unset function_key

az deployment group create \
  --subscription "$apim_subscription" \
  --resource-group "$APIM_RESOURCE_GROUP" \
  --name "${APIM_API_ID}-configuration" \
  --mode Incremental \
  --template-file "$script_dir/../apim/main.bicep" \
  --parameters "@$parameters_file" \
  --output none --only-show-errors

echo "APIM API '$APIM_API_ID' configured in product '$APIM_PRODUCT_ID'."
