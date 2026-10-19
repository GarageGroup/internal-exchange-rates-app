# Infrastructure and CI/CD

## App Registrations

Для каждой среды нужны две App Registration:

1. **Azure deployment App Registration** — Azure login, установка инфраструктуры,
   публикация Function и обновление APIM.
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
| `EXCHANGE_RATE_CURRENCY_PAIRS` | Да | Пары для timer handlers через `;`: `EUR/USD;EUR/RUB;USD/EUR;USD/RUB;RUB/EUR;RUB/USD` |
| `CURRENT_RATE_UPDATE_SCHEDULE` | Да | NCRONTAB текущих курсов, например `0 */10 * * * *` |
| `DAILY_RATE_UPDATE_SCHEDULE` | Да | NCRONTAB дневных курсов, например `0 0 1 * * *` |
| `DATAVERSE_DEPLOY_PRINCIPAL_ROLE_NAME` | Да | Имя Dataverse-роли для Function Managed Identity; Business Unit определяется по найденной роли |
| `FUNC_INSTANCE_MEMORY` | Нет | Память Flex instance: `512`, `2048` или `4096`; default `2048` |
| `FUNC_MAX_INSTANCE_COUNT` | Нет | Максимальное число Flex instances; default `100` |
| `APIM_RESOURCE_GROUP` | Да для APIM | Resource group существующего APIM |
| `APIM_SERVICE_NAME` | Да для APIM | Имя существующего APIM |
| `APIM_SUBSCRIPTION_ID` | Нет | Subscription существующего APIM; по умолчанию `DEPLOY_SUBSCRIPTION_ID` |

Environment secrets для приложения не требуются.

## Workflow

- `install.yml` создаёт/обновляет инфраструктуру и настраивает Dataverse Application User.
- `publish.yml` собирает ZIP, загружает его в Blob Storage и разворачивает в `Test`.
- `deploy.yml` разворачивает выбранную версию в `Test` или `Prod`.
- `delete.yml` удаляет ZIP релиза из Blob Storage и Git tag.

## APIM: API, Product и update-swagger

Как в Production Calendar, ID API, URL prefix и ID нового Product вычисляются из
существующего `AZURE_NAME_POSTFIX`: `exchange-rates-test` или `exchange-rates-prod`.
Product создаёт Bicep; искать его ID или создавать Product вручную не нужно.
Переменные `APIM_API_ID`, `APIM_API_PATH`, `APIM_PRODUCT_ID` не используются.
Для Test достаточно уже заполненных `APIM_RESOURCE_GROUP`, `APIM_SERVICE_NAME`
и общих deployment-переменных. `APIM_SUBSCRIPTION_ID` нужен только при другой subscription APIM.

`deploy.yml` содержит job `update-swagger`, выполняемый после успешного deployment Function.
При `skip_deploy=true` он также может обновить уже работающую Function; `skip_apim=true`
пропускает только настройку APIM. При неудаче deployment Function APIM не обновляется.
`publish.yml` выполняет те же шаги после автоматического deployment в Test.
`install.yml` проверяет существующий APIM; API настраивается после публикации кода.

По образцу Production Calendar спецификация `src/app/AzureFunc/Swagger/swagger.json`
включена в сборку как embedded resource `ExchangeRates.OpenApi.json`. Function выдаёт её
через защищённый `GET /api/swagger/swagger.json` (`AuthorizationLevel.Function`).
При изменении HTTP-контрактов актуализируйте этот документ; автоматической генерации
из C#-контрактов нет. Swagger endpoint не включён в публичный бизнес-API APIM.

`configure-apim.sh` сначала создаёт/обновляет API, policy и Product. Затем
`update-swagger.sh` получает hostname и ключ Function, скачивает документ по HTTPS
с `x-functions-key` во временный файл и импортирует его через `az apim api import`.
Загрузка повторяется при временной недоступности Function после deployment.
Используется документ из развёрнутого ZIP, а не из checkout runner: rollback не импортирует
контракты более новой версии. Старые ZIP без Swagger endpoint сначала нужно заменить
версией с этим endpoint. Общий Swagger API APIM и отдельная подписка для него не нужны.
Импорт создаёт/обновляет две операции с постоянными operationId:

- `GET /exchange-rates-{test|prod}/current/{baseCurrency}/{quoteCurrency}`;
- `GET /exchange-rates-{test|prod}/daily/{baseCurrency}/{quoteCurrency}/{date}`, дата `yyyy-MM-dd`.

Backend — фактический `defaultHostName` Function с суффиксом `/api/exchange-rates`.
Спецификация описывает параметры, ответы и ошибки. `configure-apim.sh` применяет
`.infra/apim/main.bicep`: секретный Named Value `exchange-rates-{test|prod}-function-key`, policy API,
новый опубликованный Product `Garage Exchange Rates`, привязку API и доступ группы `developers`.
Отображаемое имя API — `Exchange Rates`, без суффикса среды; ID и URL сохраняют test/prod.
Существующие общие API, Product и глобальные policies не изменяются.

Клиент использует APIM subscription key (`Ocp-Apim-Subscription-Key`). Product требует
подписку с одобрением администратора. Подписки пользователей deployment не создаёт.
APIM передаёт Function ключ в `x-functions-key`, перезаписывая входящий заголовок и
удаляя входящий параметр `code` и APIM subscription key из запроса к backend.
Используется `functionKeys.default` — host-level ключ
вызова функций, не административный `_master`. Ключ извлекается из Azure при запуске,
маскируется в Actions, передаётся в Bicep через временный файл и `@secure()` параметр.
После ротации ключа повторите шаги APIM. Новые GitHub secrets для него не требуются.

Методы появляются в Developer Portal из импортированного API; доступность зависит от
группы пользователя и подписки Product. Уже опубликованный портал не перепубликуется
этим workflow: оформление и публикация общего портала остаются отдельным действием.

### Дополнительные права deployment principal

Роль на одном ресурсе APIM из скрипта выше позволяет управлять APIM, но для
`az deployment group create` также нужны права на ARM deployments в его resource group.
Достаточно назначить `API Management Service Contributor` на RG APIM: роль включает
`Microsoft.Resources/deployments/*` и чтение resource group. Ранее назначенная роль
на одном APIM сохраняется. Более широкая роль `Contributor` не требуется.

```shell
az role assignment create \
  --assignee "$AZURE_DEPLOY_APP_ID" \
  --role "API Management Service Contributor" \
  --scope "/subscriptions/$APIM_SUB_ID/resourceGroups/$APIM_RG"
```

На RG Function ранее выданного `Contributor` достаточно для чтения hostname и ключей.
Повторные запуски используют те же API/Product/Named Value и не создают подписки-дубли.
Изменение ID означает создание другого ресурса; старый API/Product автоматически не удаляется.
