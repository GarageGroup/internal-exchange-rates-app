# Infrastructure and CI/CD

## App Registrations

Для каждой среды нужны две App Registration:

1. **Azure deployment App Registration** — Azure login, установка инфраструктуры,
   публикация Function и будущее обновление APIM.
2. **Dataverse deployment App Registration** — регистрация System Assigned Managed
   Identity функции в Dataverse и назначение ей роли
   `DATAVERSE_DEPLOY_PRINCIPAL_ROLE_NAME`.

Всего нужно четыре App Registration: по две для `Test` и `Prod`. Для самой Azure
Function App Registration не нужна: она работает через `SystemAssignedManagedIdentity`.
Client secrets также не нужны — GitHub Actions использует OIDC.

## Shell-скрипт выдачи прав

Администратор должен заполнить и выполнить этот скрипт отдельно для `Test` и `Prod`.
Перед запуском нужно выполнить `az login`.

```shell
#!/bin/sh
set -eu

# Заполнить перед запуском.
AZURE_DEPLOY_APP_ID="<azure-deployment-app-registration-client-id>"
DATAVERSE_DEPLOY_APP_ID="<dataverse-deployment-app-registration-client-id>"
SUB_ID="<subscription-id>"
RG="rg-internal-exchange-rates-test"

GITHUB_ORG="<github-organization>"
GITHUB_REPO="internal-exchange-rates"
GITHUB_ENVIRONMENT="Test" # Test или Prod, регистр важен
ENVIRONMENT_NAME="test"   # test или prod

# Существующий APIM находится в другой resource group.
APIM_SUB_ID="$SUB_ID"
APIM_RG="<existing-apim-resource-group>"
APIM_NAME="<existing-apim-name>"

GRAPH_API_ID="00000003-0000-0000-c000-000000000000"
APPLICATION_READ_ALL_ROLE_ID="9a5d68dd-52b0-4cc2-bd40-abcf44ac3a30"

# 1. GitHub OIDC для Azure deployment App Registration.
cat > azure-deploy-federated-credential.json <<EOF
{
  "name": "github-${GITHUB_REPO}-${ENVIRONMENT_NAME}",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:${GITHUB_ORG}/${GITHUB_REPO}:environment:${GITHUB_ENVIRONMENT}",
  "audiences": ["api://AzureADTokenExchange"]
}
EOF

az ad app federated-credential create \
  --id "$AZURE_DEPLOY_APP_ID" \
  --parameters @azure-deploy-federated-credential.json

# 2. Деплой ресурсов приложения.
APP_RG_SCOPE="/subscriptions/$SUB_ID/resourceGroups/$RG"

az role assignment create \
  --assignee "$AZURE_DEPLOY_APP_ID" \
  --role "Contributor" \
  --scope "$APP_RG_SCOPE"

# Нужна для назначения Function Managed Identity ролей
# Storage Blob Data Owner и Storage Table Data Contributor.
az role assignment create \
  --assignee "$AZURE_DEPLOY_APP_ID" \
  --role "Role Based Access Control Administrator" \
  --scope "$APP_RG_SCOPE"

# 3. Доступ к существующему APIM.
APIM_SCOPE="/subscriptions/$APIM_SUB_ID/resourceGroups/$APIM_RG/providers/Microsoft.ApiManagement/service/$APIM_NAME"

az role assignment create \
  --assignee "$AZURE_DEPLOY_APP_ID" \
  --role "API Management Service Contributor" \
  --scope "$APIM_SCOPE"

# 4. GitHub OIDC для Dataverse deployment App Registration.
cat > dataverse-deploy-federated-credential.json <<EOF
{
  "name": "github-${GITHUB_REPO}-${ENVIRONMENT_NAME}",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:${GITHUB_ORG}/${GITHUB_REPO}:environment:${GITHUB_ENVIRONMENT}",
  "audiences": ["api://AzureADTokenExchange"]
}
EOF

az ad app federated-credential create \
  --id "$DATAVERSE_DEPLOY_APP_ID" \
  --parameters @dataverse-deploy-federated-credential.json

# 5. Microsoft Graph Application.Read.All для поиска client ID
# Function Managed Identity по её principal ID.
az ad app permission add \
  --id "$DATAVERSE_DEPLOY_APP_ID" \
  --api "$GRAPH_API_ID" \
  --api-permissions "${APPLICATION_READ_ALL_ROLE_ID}=Role"

az ad app permission admin-consent \
  --id "$DATAVERSE_DEPLOY_APP_ID"

rm -f azure-deploy-federated-credential.json
rm -f dataverse-deploy-federated-credential.json
```

Пользователь, выполняющий скрипт, должен иметь права на federated credentials и
Microsoft Graph admin consent, а также `Owner` либо сочетание `User Access
Administrator` и `Contributor` на указанных Azure scopes.

Dataverse deployment App Registration нужно вручную добавить в каждую среду
Dataverse как **Application User**. Она должна иметь роль, разрешающую читать
`systemuser`, `role`, `businessunit`, создавать Application User и назначать ему
security role. Пока отдельная ограниченная роль не подготовлена, можно использовать
`System Administrator`. Это настройка Dataverse-среды, а не Azure RBAC.

Managed Identity функции автоматически добавляет в Dataverse workflow `install.yml`.
Ей назначается прикладная роль с именем из
`DATAVERSE_DEPLOY_PRINCIPAL_ROLE_NAME`.

## GitHub repository variables и secret

Эти значения задаются на уровне repository, поскольку `publish` и `delete` не
выбирают GitHub Environment.

| Имя | Тип | Назначение |
| --- | --- | --- |
| `AZURE_ARTIFACT_NAME` | Repository variable | Базовое имя ZIP, например `internal-exchange-rates` |
| `AZURE_ARTIFACT_CONTAINER_NAME` | Repository variable | Blob container для ZIP-файлов релизов |
| `AZURE_ARTIFACT_ACCOUNT_NAME` | Repository variable | Имя общего Storage Account для CI/CD-артефактов |
| `AZURE_ACCOUNT_KEY_ARTIFACT` | Repository secret | Ключ artifact Storage Account для upload/download/delete |

Этот ключ используется только CI/CD и не передаётся Function. Доступ приложения к
`CurrentRates` и `DailyRates` выполняется через Managed Identity.

## GitHub Environment variables

Создать Environments с точными именами `Test` и `Prod`. В каждом задать свои значения:

| Имя | Обязательно | Назначение |
| --- | --- | --- |
| `DEPLOY_CLIENT_ID` | Да | Client ID Azure deployment App Registration |
| `DEPLOY_TENANT_ID` | Да | Tenant ID для Azure OIDC login |
| `DEPLOY_SUBSCRIPTION_ID` | Да | Subscription ID среды |
| `DEPLOY_DATAVERSE_CLIENT_ID` | Да | Client ID Dataverse deployment App Registration |
| `DEPLOY_DATAVERSE_TENANT_ID` | Да | Tenant ID для Dataverse OIDC login |
| `AZURE_NAME_ROOT` | Да | Общая часть имён, например `internal-exchange-rates` |
| `AZURE_NAME_POSTFIX` | Да | `test` или `prod` |
| `STORAGE_ACCOUNT_NAME` | Да | Точное имя Storage Account приложения: 3-24 строчные латинские буквы и цифры, например `stintexratestest` |
| `DATAVERSE_SERVICE_URL` | Да | URL Dataverse без `/` в конце |
| `DATAVERSE_DEPLOY_PRINCIPAL_ROLE_NAME` | Да | Имя Dataverse-роли для Function Managed Identity; Business Unit определяется по найденной роли |
| `FUNC_INSTANCE_MEMORY` | Нет | Память Flex instance: `512`, `2048` или `4096`; default `2048` |
| `FUNC_MAX_INSTANCE_COUNT` | Нет | Максимальное число Flex instances; default `100` |
| `APIM_RESOURCE_GROUP` | Да для APIM | Resource group существующего APIM |
| `APIM_SERVICE_NAME` | Да для APIM | Имя существующего APIM |

Environment secrets для приложения не требуются.

## Workflow

- `install.yml` создаёт/обновляет инфраструктуру и настраивает Dataverse Application User.
- `publish.yml` собирает ZIP, загружает его в Blob Storage и разворачивает в `Test`.
- `deploy.yml` разворачивает выбранную версию в `Test` или `Prod`.
- `delete.yml` удаляет ZIP релиза из Blob Storage и Git tag.
