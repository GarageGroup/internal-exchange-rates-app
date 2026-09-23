#!/usr/bin/env bash
set -euo pipefail

required_env_vars=(
  AZURE_RESOURCE_GROUP_NAME
  AZURE_FUNCTION_NAME
  AZURE_STORAGE_ACCOUNT_NAME
  DATAVERSE_SERVICE_URL
  EXCHANGE_RATE_CURRENCY_PAIRS
  CURRENT_RATE_UPDATE_SCHEDULE
  DAILY_RATE_UPDATE_SCHEDULE
)

for env_var in "${required_env_vars[@]}"; do
  if [[ -z "${!env_var:-}" ]]; then
    echo "Missing required environment variable: $env_var" >&2
    exit 1
  fi
done

settings=(
  "Info__DeployDateTime=$(date -u +'%Y-%m-%dT%H:%M:%SZ')"
  "Dataverse__ServiceUrl=${DATAVERSE_SERVICE_URL}"
  "Dataverse__AuthenticationType=SystemAssignedManagedIdentity"
  "ExchangeRates__Storage__ServiceUri=https://${AZURE_STORAGE_ACCOUNT_NAME}.table.core.windows.net"
  "ExchangeRates__Storage__CurrentRatesTableName=CurrentRates"
  "ExchangeRates__Storage__DailyRatesTableName=DailyRates"
  "ExchangeRates__CurrencyPairs=${EXCHANGE_RATE_CURRENCY_PAIRS}"
  "ExchangeRates__CurrentUpdateSchedule=${CURRENT_RATE_UPDATE_SCHEDULE}"
  "ExchangeRates__DailyUpdateSchedule=${DAILY_RATE_UPDATE_SCHEDULE}"
)

az functionapp config appsettings set \
  --resource-group "$AZURE_RESOURCE_GROUP_NAME" \
  --name "$AZURE_FUNCTION_NAME" \
  --settings "${settings[@]}" \
  --output none \
  --only-show-errors

echo "Updated Function App settings for $AZURE_FUNCTION_NAME"
