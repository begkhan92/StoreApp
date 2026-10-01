# StoreApp: день 1

Razor Pages + htmx + SQLite (WAL) + EF Core + Identity. Три проекта: Domain, Infrastructure, Web.

## Запуск

Открой `StoreApp.sln` в Visual Studio / Rider или используй CLI:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add Initial -p src/StoreApp.Infrastructure -s src/StoreApp.Web -o Data/Migrations

dotnet run --project src/StoreApp.Web
```

Миграции применяются автоматически при старте. Вход: `owner@store.local` / `ChangeMe123` (смени в `appsettings.json` до деплоя).
Проект нацелен на .NET 9 (`Directory.Build.props`, пакеты `9.*`). Для .NET 10 поменяй `net9.0` на `net10.0` и `9.*` на `10.*` в csproj.

## Что есть
- Роли Owner / Manager / Employee / Demo. Открытая регистрация даёт только Demo (лимит 5 регистраций в час с одного IP).
- Аудит-лог: `AuditSaveChangesInterceptor` пишет кто, что и как изменил. Смотреть: `/Admin/Audit`.
- Деньги хранятся как целые сотые (`MoneyConverter`).
- 32 демо-товара, 3 клиента, начальные остатки.
- `/Products`: эталон htmx-поиска (150 мс, частичный ответ, поиск по `SearchText` без учёта регистра для кириллицы).

## Важно
- Код написан без компиляции (в среде не было .NET SDK). Первый `dotnet build` может показать мелкие ошибки.
- Агрегаты по деньгам (SUM) в SQLite с конвертером могут не транслироваться в SQL. Для отчётов на 5-6 дне суммируй в памяти после `ToListAsync()` или проецируй в `long`.
- htmx и Alpine подключены с CDN. Для десктопа скачай в `wwwroot/lib`.

## Локализация
- Языки: ru (по умолчанию), tk, en. Переключатель RU / TM / EN в интерфейсе, выбор хранится в cookie.
- Ключ перевода это русский текст: `@T["Товары"]`. Переводы лежат в `src/StoreApp.Web/Resources/tk.json` и `en.json`. Нет перевода: показывается русский.
- Новая строка: пишешь `@T["Русский текст"]` во view (или `T["..."]` в коде) и добавляешь пару в оба json.
- Туркменский перевод сделан быстро, его должен вычитать носитель языка.
- Данные (названия товаров, категории) не переводятся, они хранятся как введены.
- На Linux-VPS для культуры `tk` нужен ICU (`libicu`), на Ubuntu с .NET он обычно уже стоит.
