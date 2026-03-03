# PaymentService

### Описание

**PaymentService** — сервис для обработки платежей, построенный на принципах **Clean Architecture**, 
**Domain-Driven Design (DDD)** и паттерна **Result**. Разработка велась по методологии 
**TDD (Test-Driven Development)**, что гарантирует высокую отказоустойчивость и почти полное 
покрытие бизнес-логики тестами. Сервис позволяет пользователям регистрироваться, создавать заказы, 
инициировать и подтверждать платежи. Поддерживает идемпотентность, JWT-аутентификацию и 
ограничение частоты запросов (rate limiting).

### Принятые технические решения:
- **Паттерн Result**: Выбран для явной обработки бизнес-ошибок без использования исключений (exceptions) в логике управления потоком (control flow). Это повышает производительность системы и делает код более предсказуемым и читаемым.

- **Идемпотентность**: Реализована на уровне слоя Application через проверку ключа идемпотентности в заголовке Idempotency-Key. Это гарантирует, что повторные запросы (например, при сбое сети) не приведут к дублированию платежей или заказов.

- **TDD**: Разработка велась через цикл "Red-Green-Refactor", что обеспечило почти 100% покрытие бизнес-логики и надежность при проведении транзакций.

### Стек технологий

| Технология                                 | Назначение |
|--------------------------------------------|---|
| **.NET 10 / ASP.NET Core**                 | Целевой фреймворк и Web API |
| **Entity Framework Core 10**               | ORM, доступ к данным |
| **PostgreSQL** (через Npgsql)              | Основная БД |
| **MediatR**                                | CQRS (команды и запросы) |
| **FluentValidation**                       | Валидация входных данных |
| **JWT Bearer**                             | Аутентификация и авторизация |
| **Polly**                                  | Отказоустойчивость (retry, circuit-breaker) |
| **BCrypt.Net-Next**                        | Хеширование паролей |
| **Swagger / OpenAPI**                      | Интерактивная документация API |
| **xUnit + FluentAssertions + NSubstitute** | Unit-тестирование |
| **Testcontainers.PostgreSql**              | Интеграционные тесты с реальной БД |
| **Docker Compose**                         | Локальная инфраструктура (БД + pgAdmin) |
| **GitHub Actions**                         | CI (сборка и тесты) |

### Структура репозитория

```
PaymentService/
├── src/
│   ├── PaymentService.Api/                  # Точка входа: ASP.NET Core Web API
│   │   ├── Controllers/                     # AuthController, OrdersController, PaymentsController, ConfirmPaymentController
│   │   ├── Common/                          # Rate limiting, extensions
│   │   ├── appsettings.json                 # Конфигурация по умолчанию
│   │   └── Program.cs                       # Точка входа приложения
│   ├── PaymentService.Application/          # Слой приложения: CQRS, use-cases, интерфейсы
│   │   ├── Features/Auth/                   # Регистрация, логин, refresh/revoke токенов
│   │   ├── Features/Orders/                 # Создание и получение заказов
│   │   ├── Features/Payments/               # Создание, подтверждение, список платежей
│   │   ├── Common/                          # Интерфейсы, поведения, утилиты
│   │   │   ├── Behaviors/                   # MediatR-поведения (идемпотентность, валидация)
│   │   │   │   └── Idempotency/             # Поддержка идемпотентности
│   │   │   └── Exceptions/                  # Пользовательские исключения для ошибок приложения
│   │   └── Auth/                            # Интерфейсы JWT, токены
│   ├── PaymentService.Domain/               # Доменный слой: сущности, Value Objects, Result<T>
│   │   ├── Entities/                        # User, Order, Payment
│   │   ├── Enums/                           # Статусы заказов и платежей
│   │   ├── ValueObjects/                    # Обёртки над примитивами
│   │   ├── Events/                          # Доменные события
│   │   └── Common/                          # Result<T>, Error, ErrorType
│   ├── PaymentService.Infrastructure/       # Инфраструктурный слой: EF Core, PostgreSQL, Auth
│   │   ├── Persistence/                     # DbContext, конфигурации, миграции,
│   │   ├── PaymentProvider/                 # Фейковый провайдер платежей
│   │   └── Auth/                            # JWT-сервис, хеширование паролей
├── tests/
│   ├── PaymentService.Domain.Tests/         # Unit-тесты доменного слоя
│   ├── PaymentService.Application.Tests/    # Unit-тесты слоя приложения
│   └── PaymentService.Api.Tests/            # Unit-тесты API
├── docker-compose.yml                       # PostgreSQL + pgAdmin
├── coverage.ps1                             # Скрипт запуска покрытия (Windows/PowerShell)
├── Directory.Packages.props                 # Централизованное управление версиями пакетов
└── PaymentService.slnx                      # Solution-файл
```

### API-эндпоинты

| Метод | Путь | Описание |
|---|---|---|
| `POST` | `/api/v1/auth/register` | Регистрация пользователя |
| `POST` | `/api/v1/auth/login` | Аутентификация |
| `POST` | `/api/v1/auth/refresh` | Обновление access-токена |
| `POST` | `/api/v1/auth/revoke` | Отзыв refresh-токена |
| `GET` | `/api/v1/auth/me` | Информация о текущем пользователе |
| `POST` | `/api/v1/orders` | Создание заказа |
| `GET` | `/api/v1/orders/{id}` | Получение заказа по ID |
| `POST` | `/api/v1/orders/{orderId}/payments` | Инициация платежа |
| `GET` | `/api/v1/orders/{orderId}/payments` | Список платежей по заказу |
| `POST` | `/api/v1/payments/{paymentId}/confirm` | Подтверждение платежа |

### Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) и Docker Compose (для PostgreSQL)
- Или отдельный экземпляр PostgreSQL 15+

### Локальный запуск

#### 1. Клонировать репозиторий

```bash
git clone https://github.com/Shodmon0406/PaymentService.git
cd PaymentService
```

#### 2. Запустить PostgreSQL через Docker Compose

```bash
docker compose up -d
```

Это поднимет:
- **Payment Service** на `localhost:8080`
- **PostgreSQL** на `localhost:5432`
- **pgAdmin** на `http://localhost:5050` (login: `admin@paymentservice.com` / `admin`)

#### 3. Настроить конфигурацию

Создайте файл `src/PaymentService.Api/appsettings.Development.json` (или используйте переменные окружения). Файл уже существует с настройками для разработки. **Для безопасности в production задайте `JwtSettings:SigningKey` через переменную окружения.**

#### 4. Применить миграции БД (автоматически при запуске, но можно сделать вручную)

```bash
dotnet ef database update \
  --project src/PaymentService.Infrastructure \
  --startup-project src/PaymentService.Api
```

#### 5. Собрать и запустить

```bash
# Восстановить пакеты
dotnet restore PaymentService.slnx

# Собрать решение
dotnet build PaymentService.slnx

# Запустить API
dotnet run --project src/PaymentService.Api
```

API будет доступен по адресу `https://localhost:7135` (порт указан в `launchSettings.json`).

### Конфигурация

Все настройки определены в `appsettings.json`.

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=payment_service_postgres;Port=5432;Database=payment_service;User Id=postgres;Password=postgres;"
  },
  "JwtSettings": {
    "Issuer": "PaymentService",
    "Audience": "PaymentService",
    "SigningKey": "Secury_Key_20a9d462_8b34_4ffe_8d0f_8c6b51190d06",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "RateLimiting": {
    "Global": { "PermitLimit": 200, "WindowSeconds": 60, "QueueLimit": 0 },
    "Auth": { "PermitLimit": 10, "WindowSeconds": 60, "QueueLimit": 0 },
    "PaymentConfirm": { "PermitLimit": 30, "WindowSeconds": 60, "QueueLimit": 0 }
  }
}
```

### Запуск тестов

```bash
# Запустить все тесты
dotnet test PaymentService.slnx

# Запустить с отчётом покрытия
dotnet test PaymentService.slnx --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Сгенерировать HTML-отчёт покрытия (требует reportgenerator и PowerShell)
./coverage.ps1
```

> **Примечание:** Интеграционные тесты (`PaymentService.Api.Tests`) используют **Testcontainers** и требуют работающего Docker.

### CI/CD

В проекте настроен GitHub Actions workflow (`.github/workflows/ci.yml`), который запускается при каждом push и pull request в ветки `main` / `master`:

1. Checkout репозитория
2. Установка .NET 10 SDK
3. Восстановление зависимостей
4. Сборка в режиме `Release`
5. Запуск тестов с отчётом покрытия
6. Загрузка результатов тестов как артефакт (хранится 7 дней)
