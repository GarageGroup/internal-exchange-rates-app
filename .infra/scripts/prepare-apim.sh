#!/usr/bin/env bash
set -euo pipefail

# Install only verifies the existing shared service; deploy/publish configure the API.
if [[ -z "${APIM_RESOURCE_GROUP:-}" && -z "${APIM_SERVICE_NAME:-}" ]]; then
  echo 'APIM is not configured; skipping verification.'
  exit 0
fi
: "${APIM_RESOURCE_GROUP:?Set APIM_RESOURCE_GROUP and APIM_SERVICE_NAME together}"
: "${APIM_SERVICE_NAME:?Set APIM_RESOURCE_GROUP and APIM_SERVICE_NAME together}"
az apim show \
  --subscription "${APIM_SUBSCRIPTION_ID:-$(az account show --query id --output tsv --only-show-errors)}" \
  --resource-group "$APIM_RESOURCE_GROUP" \
  --name "$APIM_SERVICE_NAME" \
  --output none \
  --only-show-errors
echo 'APIM verified; deploy/publish configure the Exchange Rates API.'
