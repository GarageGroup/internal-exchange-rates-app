# Журнал изменений инфраструктуры

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
