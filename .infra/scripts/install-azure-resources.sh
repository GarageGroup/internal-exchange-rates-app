#!/usr/bin/env bash
set -euo pipefail

for variable in AZURE_NAME_ROOT AZURE_NAME_POSTFIX AZURE_STORAGE_ACCOUNT_NAME DATAVERSE_SERVICE_URL; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done
env_name="$AZURE_NAME_POSTFIX"
[[ "$env_name" =~ ^[a-z0-9-]+$ ]] || { echo 'Invalid AZURE_NAME_POSTFIX' >&2; exit 1; }
[[ "$AZURE_STORAGE_ACCOUNT_NAME" =~ ^[a-z0-9]{3,24}$ ]] || {
  echo 'AZURE_STORAGE_ACCOUNT_NAME must contain 3-24 lowercase letters and digits' >&2
  exit 1
}
[[ "$DATAVERSE_SERVICE_URL" =~ ^https://[a-zA-Z0-9.-]+$ ]] || { echo 'Invalid DATAVERSE_SERVICE_URL' >&2; exit 1; }
rg="rg-${AZURE_NAME_ROOT}-${env_name}"
if [[ "$(az group exists -n "$rg" -o tsv --only-show-errors)" == false ]]; then
  az group create -n "$rg" -l northeurope --tags "application=$AZURE_NAME_ROOT" "environment=$env_name" -o none --only-show-errors
fi
[[ "$(az group show -n "$rg" --query location -o tsv --only-show-errors)" == northeurope ]] || { echo 'Resource group must be in North Europe' >&2; exit 1; }
for provider in Microsoft.Web Microsoft.Storage Microsoft.Insights Microsoft.OperationalInsights; do
  [[ "$(az provider show -n "$provider" --query registrationState -o tsv --only-show-errors)" == Registered ]] || { echo "Provider must be registered: $provider" >&2; exit 1; }
done
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
outputs="$(az deployment group create -g "$rg" -n "exchange-rates-$env_name" --mode Incremental --template-file "$script_dir/../main.bicep" \
  --parameters nameRoot="$AZURE_NAME_ROOT" environmentName="$env_name" storageAccountName="$AZURE_STORAGE_ACCOUNT_NAME" dataverseServiceUrl="$DATAVERSE_SERVICE_URL" \
  instanceMemoryMB="${FUNC_INSTANCE_MEMORY:-2048}" maximumInstanceCount="${FUNC_MAX_INSTANCE_COUNT:-100}" \
  --query properties.outputs -o json --only-show-errors)"
function_name="$(jq -er '.functionAppName.value' <<< "$outputs")"
principal_id="$(jq -er '.functionPrincipalId.value' <<< "$outputs")"
if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  printf 'resource_group=%s\nfunction_name=%s\nfunction_principal_id=%s\n' "$rg" "$function_name" "$principal_id" >> "$GITHUB_OUTPUT"
fi
