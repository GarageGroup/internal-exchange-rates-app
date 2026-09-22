#!/usr/bin/env bash
set -euo pipefail

# Placeholder: no API, backend, policy or operation is created until endpoint contracts exist.
if [[ -z "${APIM_RESOURCE_GROUP:-}" && -z "${APIM_SERVICE_NAME:-}" ]]; then
  echo 'APIM is not configured; skipping placeholder.'
  exit 0
fi
: "${APIM_RESOURCE_GROUP:?Set APIM_RESOURCE_GROUP and APIM_SERVICE_NAME together}"
: "${APIM_SERVICE_NAME:?Set APIM_RESOURCE_GROUP and APIM_SERVICE_NAME together}"
az apim show \
  --resource-group "$APIM_RESOURCE_GROUP" \
  --name "$APIM_SERVICE_NAME" \
  --output none \
  --only-show-errors
echo 'APIM verified; no methods are created yet.'
