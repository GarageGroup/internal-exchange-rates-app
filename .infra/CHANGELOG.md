# Журнал изменений инфраструктуры

## 19.10.2026 — Подготовка GitHub Environment Prod

- Пользователь разрешил заполнить GitHub variables и указал Prod APIM RG/service, deployment client ID и отдельную APIM subscription. Создан Environment Prod, записаны и прочитаны обратно 14 environment variables; фактические значения перечислены в README.
- Read-only Azure: обе subscriptions находятся в tenant 69879a40-68bb-4b30-941b-eb9692ddd9b4, Prod APIM существует в North Europe. Имя stexchangeratesprod свободно. Имя Storage, root/postfix, расписания, валюты и Flex defaults выбраны исполнителем по conventions Test; отдельный APIM_SUBSCRIPTION_ID уже поддерживался, дополнительных изменений workflow для cross-subscription не понадобилось.
- Первоначально подготовлен PowerShell-скрипт; по уточнению пользователя заменён shell-блоком в чате, файл удалён перед коммитом. Скрипт включает: проверка APIM, создание RG приложения при отсутствии, регистрация providers, RBAC и upsert GitHub OIDC. Runtime ресурсы и release не разворачивает.
- Проверки: PowerShell AST parser без ошибок; параметры federated-credential update сверены с установленным Azure CLI; git diff --check. Скрипт администратора не выполнялся, права не выдавались.
- Ограничение: organization variables/secrets не доступны для проверки текущему GitHub-токену (403 на список variables); существующие общие настройки artifacts не изменены. Чтение secret values не выполнялось. Изменения удаления Dataverse сохранены в рабочем дереве, не одобрены автоматически подготовкой Prod.
- Статус: GitHub Prod variables применены по прямому запросу пользователя. Пользователь одобрил локальные изменения командой «коммить» 19.10.2026; они включаются в коммит, хеш фиксируется в координационном CURRENT_STATE.md. Push/CI/CD/Prod deployment агент не выполняет.

## 19.10.2026 — Удаление неиспользуемой интеграции Dataverse

- Пользователь решил отказаться от Dataverse: текущий runtime использует только провайдера курсов и Azure Table Storage. Проверка src не обнаружила обращений к Dataverse.
- Удалены install job grant-dataverse-access, input skip_dataverse, скрипт регистрации Application User, Dataverse env workflow, параметр/настройки Bicep и обязательные переменные shell-скриптов. Локальные настройки очищены без переноса секретов в документы/Git.
- set-function-app-settings.sh при следующем deploy удаляет два старых Dataverse app settings. Azure Managed Identity и Storage RBAC сохранены.
- README: одна Azure deployment App Registration на среду; удалены Dataverse/Graph admin-consent команды. Документированы четыре ненужные GitHub variables и отдельная очистка Application Users, ролей и выделенной Dataverse deployment App Registration администратором.
- Read-only проверка Azure: у Test пока присутствуют Dataverse__ServiceUrl и Dataverse__AuthenticationType. Выданные роли Dataverse не проверены/не сняты; GitHub variables и внешние ресурсы агент не изменял. Удаление кода не означает отзыв доступа.
- Проверки: Bicep build main, bash -n обоих изменённых скриптов, разбор workflow PyYAML и diff check прошли. C# не менялся, сборка не повторялась, тесты не создавались.
- Статус: пользователь одобрил коммит 19.10.2026; push и deployment агент не выполняет. Следующий шаг после публикации — deploy и отдельная административная очистка неиспользуемого Dataverse-доступа.

## 19.10.2026 — Отображаемые названия APIM

- По запросу пользователя API называется `Exchange Rates` без суффикса среды, Product — `Garage Exchange Rates`.
- Обновлены displayName в Bicep и display-name импорта Swagger. ID ресурсов и URL не изменены.
- Проверки: Bicep build, bash -n update-swagger и diff check прошли. Пользователь подтвердил отсутствие суффиксов в Test и Prod и одобрил коммит 19.10.2026. Хеш фиксируется в координационном CURRENT_STATE.md; push и deployment не выполняются.

## 19.10.2026 — Исправление получения hostname Flex Function

- Пользователь сообщил об ошибке CI/CD `Function hostname was not found` в Azure CLI 2.91.0.
- Read-only проверка Azure для func-exchange-rates-test подтвердила: defaultHostName на верхнем уровне null, значение находится в properties.defaultHostName.
- В configure-apim.sh и update-swagger.sh запрос изменён на `properties.defaultHostName || defaultHostName`, как в Production Calendar, с поддержкой обоих форматов ответа.
- Проверки: новый запрос вернул func-exchange-rates-test.azurewebsites.net; bash -n обоих скриптов и git diff --check прошли. Настройка APIM и deployment не запускались, тесты не создавались.
- Review: пользователь одобрил исправление командой «коммить» 19.10.2026; исправление включается в коммит, хеш фиксируется в координационном CURRENT_STATE.md. Push/deployment не выполняются. После отправки исправления нужно запустить новый deploy workflow из обновлённой ветки с skip_deploy=true для версии со Swagger endpoint: rerun старого запуска использует прежний commit.

## 19.10.2026 — Автоматическое именование API и Product

- По замечанию пользователя повторно изучены prepare-apim.sh/update-swagger.sh Production Calendar: имя API, путь и Product там вычисляются по среде, а не запрашиваются отдельными переменными GitHub.
- В ExchangeRates удалены APIM_API_ID, APIM_API_PATH, APIM_PRODUCT_ID из workflow и обязательных входов скриптов. configure-apim/update-swagger используют `exchange-rates-$AZURE_NAME_POSTFIX`; postfix проверяется на test/prod. Product создаётся автоматически с тем же ID. Уже существующие APIM_RESOURCE_GROUP/APIM_SERVICE_NAME и общие переменные deployment достаточны.
- README актуализирован; уточнена достаточная роль API Management Service Contributor на RG APIM. Пользователь сообщил, что администратор уже выдал права; новое назначение не выполнялось и не проверялось в Azure.
- Проверки: bash -n обоих скриптов, разбор workflow PyYAML и git diff --check прошли; ссылки на удалённые переменные в workflow/скриптах отсутствуют. C# и Bicep не менялись, сборка не повторялась. Тесты не создавались.
- Review: пользователь одобрил исправление командой «коммить» 19.10.2026. Изменения включаются в коммит автоматического именования; хеш фиксируется в координационном CURRENT_STATE.md. Push и deployment не выполняются. Предыдущая реализация APIM/Swagger сохранена в a234993.

## 19.10.2026 — Exchange Rates API в существующем APIM

- Запрос пользователя в чате ExchangeRates: отдельный API и новый Product, шаг CI/CD по образцу Timesheet `update-swagger`, доступ к Function по ключу.
- Изучены `.infra/scripts/update-swagger.sh`, `configure-apim.sh`, APIM Bicep и workflow Timesheet API. Использован тот же механизм импорта OpenAPI, источник — файл в ExchangeRates, поскольку HTTP endpoint Swagger в Function отсутствует.
- Добавлены OpenAPI двух GET-операций, импорт с постоянными operationId, Bicep API policy/секретного Named Value/Product/привязок. Function остаётся с `AuthorizationLevel.Function`; backend использует host-level `functionKeys.default`, не `_master`.
- Product опубликован, требует подписку с одобрением, доступен группе `developers`. Общие policies наследуются; существующие API/Product не редактируются. Ротация ключа требует повторного запуска настройки APIM.
- `deploy.yml`: job `update-swagger` после deployment; разрешён запуск при `skip_deploy`, запрещён после failure/cancel deployment. `publish.yml`: те же шаги после deployment Test. `install.yml`: проверка существующего APIM, включая необязательную отдельную subscription.
- `.infra/README.md`: новые environment variables, маршруты, права на ARM deployment в APIM RG, ограничения Developer Portal. Создание новых ресурсов в Azure не выполнялось.
- Проверки 19.10.2026: `az bicep build`, `swagger-cli validate`, `bash -n` (Git Bash) для трёх изменённых/новых скриптов, разбор workflow через PyYAML, `git diff --check`. Тесты не создавались. C# не менялся, сборка solution не повторялась. Полный actionlint не выполнен: исполняемый файл недоступен.
- Статус первоначального варианта: пользователь указал на неполную реализацию update-swagger, вариант не одобрен. Корректировка описана ниже. Коммит, push и deployment не выполнялись.

### Корректировка Swagger по образцу Production Calendar

- Источник решения: замечание пользователя и прямое указание посмотреть Production Calendar. Изучены Function.Swagger.Get.cs, EmbeddedResource в csproj, update-swagger.sh и APIM workflow образца. В образце документ не генерируется из контрактов: это embedded JSON, который выдаёт Function. Прежнее объяснение об обязательной генерации из C# уточнено.
- Документ перенесён из `.infra/apim/openapi.json` в `src/app/AzureFunc/Swagger/swagger.json`, встроен как `ExchangeRates.OpenApi.json`. Добавлен защищённый Function-key endpoint GET /api/swagger/swagger.json. Swagger endpoint не входит в импортируемый бизнес-API.
- update-swagger скачивает документ из развёрнутой Function с ключом и retry, валидирует формат и импортирует в APIM. Checkout runner больше не является источником спецификации: продвижение/откат ZIP использует собственный документ версии. Старые ZIP без Swagger endpoint требуют предварительного обновления.
- Bicep теперь создаёт API до импорта; в deploy/publish сначала Configure API Management, затем Update Swagger. Добавлена проверка отмены job в deploy. APIM subscription key удаляется перед отправкой backend по образцу Calendar.
- Новые проверки 19.10: Release build без ошибок/предупреждений; linux-x64 framework-dependent publish успешен; functions.metadata содержит GetExchangeRatesSwagger, authLevel Function, route swagger/swagger.json. Bicep build, swagger-cli validate нового пути, bash -n обновлённого скрипта, разбор workflow PyYAML прошли. Тесты не создавались, внешние запросы к Azure/Function не выполнялись.
- Review: пользователь одобрил исправленный вариант командой «коммить» 19.10.2026. Изменения включаются в коммит APIM/Swagger; push и deployment не выполняются. Документ JSON нужно поддерживать при изменении HTTP-контрактов вручную. Хеш коммита фиксируется в координационном CURRENT_STATE.md после создания.
