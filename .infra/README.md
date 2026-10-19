# Infrastructure and CI/CD

## App Registrations

Для каждой среды нужна одна Azure deployment App Registration: установка инфраструктуры,
публикация Function и настройка APIM. Всего две — для Test и Prod.
Function использует SystemAssignedManagedIdentity для Storage. GitHub Actions — OIDC,
client secrets не нужны. Интеграция с Dataverse удалена: сервис его не использует.

## Shell-скрипт выдачи прав

Для Prod заполненный shell-блок выдан администратору в чате. Он создаёт RG приложения при отсутствии, регистрирует providers, настраивает GitHub OIDC для Prod и назначает Contributor/RBAC Administrator на RG приложения и API Management Service Contributor на RG APIM в отдельной subscription. Повторный запуск поддерживается; deployment не выполняется.

Администратор должен заполнить и выполнить этот скрипт отдельно для `Test` и `Prod`.
Перед запуском нужно выполнить `az login`.

```shell
#!/bin/sh
set -eu

# Заполнить перед запуском.
AZURE_DEPLOY_APP_ID="<azure-deployment-app-registration-client-id>"
SUB_ID="<subscription-id>"
RG="rg-internal-exchange-rates-test"

GITHUB_ORG="<github-organization>"
GITHUB_REPO="internal-exchange-rates"
GITHUB_ENVIRONMENT="Test" # Test или Prod, регистр важен
ENVIRONMENT_NAME="test"   # test или prod

# Существующий APIM находится в другой resource group.
APIM_SUB_ID="$SUB_ID"
APIM_RG="<existing-apim-resource-group>"


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
APIM_SCOPE="/subscriptions/$APIM_SUB_ID/resourceGroups/$APIM_RG"

az role assignment create \
  --assignee "$AZURE_DEPLOY_APP_ID" \
  --role "API Management Service Contributor" \
  --scope "$APIM_SCOPE"

rm -f azure-deploy-federated-credential.json
```

Пользователь, выполняющий скрипт, должен иметь права на federated credentials
и назначение Azure RBAC на указанных scopes. Graph Application.Read.All и Dataverse-роли
этому проекту не требуются. Для APIM роль назначается на его resource group,
чтобы разрешить также ARM deployments.

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

После удаления интеграции из GitHub Environments можно удалить старые переменные:
`DEPLOY_DATAVERSE_CLIENT_ID`, `DEPLOY_DATAVERSE_TENANT_ID`, `DATAVERSE_SERVICE_URL`,
`DATAVERSE_DEPLOY_PRINCIPAL_ROLE_NAME`. Текущие workflow их не читают.

### Очистка ранее выданного доступа Dataverse

Изменения репозитория не отзывают ранее выданные роли. Администратору Dataverse нужно
найти Application User Managed Identity этой Function и снять `System Administrator`
и остальные роли, выданные только для ExchangeRates; при отсутствии другого назначения
деактивировать Application User. Аналогично проверить Application User бывшего
Dataverse deployment-приложения. Не изменять Azure deployment-приложение и Managed
Identity Function: они продолжают использоваться для Azure и Storage.

Если Dataverse deployment App Registration выделена только для ExchangeRates,
администратор может отключить её и отозвать прежний Microsoft Graph Application.Read.All
и GitHub federated credential. Сначала проверить зависимости других решений;
автоматического удаления App Registration или общих прав deployment не выполняет.

Следующий deploy удалит `Dataverse__ServiceUrl` и `Dataverse__AuthenticationType`
из настроек Function. Старые Application Users и их роли этим шагом не изменяются.

### Актуальные переменные

19.10.2026 создан GitHub Environment `Prod` и заполнены следующие значения:

| Переменная | Prod |
| --- | --- |
| `APIM_RESOURCE_GROUP` | `rg-integration-platform-prod` |
| `APIM_SERVICE_NAME` | `apim-garage-integration-platform-prod` |
| `APIM_SUBSCRIPTION_ID` | `12495afb-c46a-4d23-9e80-3a1dae5406ff` |
| `DEPLOY_CLIENT_ID` | `a1f416d9-7138-4022-8efd-9ea9c1d5215b` |
| `DEPLOY_TENANT_ID` | `69879a40-68bb-4b30-941b-eb9692ddd9b4` |
| `DEPLOY_SUBSCRIPTION_ID` | `106dd084-8190-453f-87c9-cd2cb714b1d6` |
| `AZURE_NAME_ROOT` | `exchange-rates` |
| `AZURE_NAME_POSTFIX` | `prod` |
| `STORAGE_ACCOUNT_NAME` | `stexchangeratesprod` |
| `EXCHANGE_RATE_CURRENCY_PAIRS` | `EUR/USD;EUR/RUB;USD/EUR;USD/RUB;RUB/EUR;RUB/USD` |
| `CURRENT_RATE_UPDATE_SCHEDULE` | `0 */10 * * * *` |
| `DAILY_RATE_UPDATE_SCHEDULE` | `0 0 1 * * *` |
| `FUNC_INSTANCE_MEMORY` | `2048` |
| `FUNC_MAX_INSTANCE_COUNT` | `100` |

Function/RG: `func-exchange-rates-prod`, `rg-exchange-rates-prod`; регион `northeurope`.
API, URL prefix и Product ID: `exchange-rates-prod`. В обоих subscriptions проверен
одинаковый tenant; APIM найден, имя Storage было свободно на момент проверки.
`APIM_SUBSCRIPTION_ID` уже поддерживается verify/configure/update-swagger и workflow:
выбор subscription Function не меняется, APIM-команды получают отдельный `--subscription`.

Общие artifact-переменные и secret остаются на уровне repository/organization, как для Test.
Список organization variables текущему GitHub-токену недоступен (403), их значения и
доступность для workflow в этой проверке не подтверждались. Секреты не считывались.
Новые Prod secrets не создавались. Сначала администратор выдаёт права, затем после
коммита/push изменений без Dataverse запускаются `install` Prod и `deploy` нужной версии.
GitHub Environment настроен, но Prod deployment пока не выполнен.

Создать Environments с точными именами `Test` и `Prod`. В каждом задать свои значения:

| Имя | Обязательно | Назначение |
| --- | --- | --- |
| `DEPLOY_CLIENT_ID` | Да | Client ID Azure deployment App Registration |
| `DEPLOY_TENANT_ID` | Да | Tenant ID для Azure OIDC login |
| `DEPLOY_SUBSCRIPTION_ID` | Да | Subscription ID среды |
| `AZURE_NAME_ROOT` | Да | Общая часть имён, например `internal-exchange-rates` |
| `AZURE_NAME_POSTFIX` | Да | `test` или `prod` |
| `STORAGE_ACCOUNT_NAME` | Да | Точное имя Storage Account приложения: 3-24 строчные латинские буквы и цифры, например `stintexratestest` |
| `EXCHANGE_RATE_CURRENCY_PAIRS` | Да | Пары для timer handlers через `;`: `EUR/USD;EUR/RUB;USD/EUR;USD/RUB;RUB/EUR;RUB/USD` |
| `CURRENT_RATE_UPDATE_SCHEDULE` | Да | NCRONTAB текущих курсов, например `0 */10 * * * *` |
| `DAILY_RATE_UPDATE_SCHEDULE` | Да | NCRONTAB дневных курсов, например `0 0 1 * * *` |
| `FUNC_INSTANCE_MEMORY` | Нет | Память Flex instance: `512`, `2048` или `4096`; default `2048` |
| `FUNC_MAX_INSTANCE_COUNT` | Нет | Максимальное число Flex instances; default `100` |
| `APIM_RESOURCE_GROUP` | Да для APIM | Resource group существующего APIM |
| `APIM_SERVICE_NAME` | Да для APIM | Имя существующего APIM |
| `APIM_SUBSCRIPTION_ID` | Нет | Subscription существующего APIM; по умолчанию `DEPLOY_SUBSCRIPTION_ID` |

Environment secrets для приложения не требуются.

## Workflow

- `install.yml` создаёт/обновляет инфраструктуру без обращения к Dataverse.
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
