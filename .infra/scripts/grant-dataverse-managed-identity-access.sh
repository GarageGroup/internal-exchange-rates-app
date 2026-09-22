#!/usr/bin/env bash
# Creates/updates the Dataverse Application User for the Function system identity.
set -euo pipefail

for variable in DATAVERSE_SERVICE_URL FUNCTION_PRINCIPAL_ID DATAVERSE_BUSINESS_UNIT_ID DATAVERSE_DEPLOY_PRINCIPAL_ROLE_NAME; do
  [[ -n "${!variable:-}" ]] || { echo "Missing $variable" >&2; exit 1; }
done
url="${DATAVERSE_SERVICE_URL%/}"
client_id="$(az ad sp show --id "$FUNCTION_PRINCIPAL_ID" --query appId -o tsv --only-show-errors)"
[[ -n "$client_id" ]] || { echo 'Cannot resolve Function managed identity appId' >&2; exit 1; }
token="$(az account get-access-token --resource "$url" --query accessToken -o tsv --only-show-errors)"
api="$url/api/data/v9.2"
headers=(-H "Authorization: Bearer $token" -H 'Accept: application/json' -H 'Content-Type: application/json' -H 'OData-Version: 4.0')

encoded_role_name="$(jq -rn --arg value "$DATAVERSE_DEPLOY_PRINCIPAL_ROLE_NAME" '$value|@uri')"
role_query="roles?%24select=roleid,name,_businessunitid_value&%24filter=name%20eq%20%27${encoded_role_name}%27"
matching_roles="$(curl --fail-with-body -sS "${headers[@]}" "$api/$role_query" \
  | jq --arg business_unit_id "$DATAVERSE_BUSINESS_UNIT_ID" \
    '[.value[] | select(((._businessunitid_value // "") | ascii_downcase) == ($business_unit_id | ascii_downcase))]')"
[[ "$(jq length <<< "$matching_roles")" == 1 ]] || {
  echo "Cannot uniquely resolve Dataverse role '$DATAVERSE_DEPLOY_PRINCIPAL_ROLE_NAME' in Business Unit '$DATAVERSE_BUSINESS_UNIT_ID'" >&2
  exit 1
}
role_id="$(jq -er '.[0].roleid' <<< "$matching_roles")"

query="systemusers?%24select=systemuserid,_businessunitid_value,isdisabled&%24filter=applicationid%20eq%20${client_id}"
user="$(curl --fail-with-body -sS "${headers[@]}" "$api/$query" | jq '.value')"
count="$(jq length <<< "$user")"
if [[ "$count" == 0 ]]; then
  curl --fail-with-body -sS -X POST "${headers[@]}" "$api/systemusers" \
    --data "{\"applicationid\":\"$client_id\",\"azureactivedirectoryobjectid\":\"$FUNCTION_PRINCIPAL_ID\",\"businessunitid@odata.bind\":\"/businessunits($DATAVERSE_BUSINESS_UNIT_ID)\"}" >/dev/null
  for _ in {1..12}; do
    sleep 5
    user="$(curl --fail-with-body -sS "${headers[@]}" "$api/$query" | jq '.value')"
    [[ "$(jq length <<< "$user")" == 1 ]] && break
  done
fi
[[ "$(jq length <<< "$user")" == 1 ]] || { echo 'Dataverse Application User was not created; rerun after propagation' >&2; exit 1; }
user_id="$(jq -r '.[0].systemuserid' <<< "$user")"
[[ "$(jq -r '.[0].isdisabled' <<< "$user")" != true ]] || { echo 'Dataverse Application User is disabled' >&2; exit 1; }
[[ "$(jq -r '.[0]._businessunitid_value' <<< "$user" | tr '[:upper:]' '[:lower:]')" == "${DATAVERSE_BUSINESS_UNIT_ID,,}" ]] || { echo 'Existing Application User belongs to another Business Unit' >&2; exit 1; }

roles="$(curl --fail-with-body -sS "${headers[@]}" "$api/systemusers($user_id)/systemuserroles_association?%24select=roleid")"
if ! jq -e --arg role "$role_id" '.value[] | select(.roleid == $role)' <<< "$roles" >/dev/null; then
  curl --fail-with-body -sS -X POST "${headers[@]}" "$api/systemusers($user_id)/systemuserroles_association/\$ref" \
    --data "{\"@odata.id\":\"$api/roles($role_id)\"}" >/dev/null
fi
echo "Dataverse role '$DATAVERSE_DEPLOY_PRINCIPAL_ROLE_NAME' assigned to Function managed identity."
