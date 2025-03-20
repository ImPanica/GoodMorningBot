# GoodMorningBot

Telegram бот, который отправляет ежедневные утренние и вечерние сообщения с мотивирующими цитатами и красивыми фотографиями.

## Функциональность

- Отправка ежедневных сообщений в настроенное время
  - Утреннее сообщение в 8:00
  - Вечернее сообщение в 21:00
- Случайные мотивирующие цитаты на русском языке (API Forismatic)
- Красивые фотографии для каждого сообщения (API Unsplash)
- Поддержка групповых чатов и личных сообщений
- Форматированный текст (жирный шрифт, цитаты, курсив)
- Команда `/citation` для получения случайной цитаты в любой момент

## Настройка

1. Создайте файл `appsettings.json` в корневой папке проекта:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=GoodMorningBot;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "BotConfiguration": {
    "TelegramBotToken": "ВАШ_ТОКЕН_БОТА",
    "UnsplashApiKey": "ВАШ_КЛЮЧ_API_UNSPLASH"
  }
}
```

2. Получите необходимые API ключи:
   - Telegram Bot Token: Создайте бота через [@BotFather](https://t.me/BotFather)
   - Unsplash API Key: Зарегистрируйтесь на [Unsplash Developers](https://unsplash.com/developers)

3. Установите .NET 8.0 SDK

4. Запустите миграции базы данных:
```bash
dotnet ef database update
```

## Настройка времени отправки

Время отправки сообщений настраивается через CRON-выражение в файле `Program.cs`. По умолчанию установлено:
- Утреннее сообщение: 8:00 по московскому времени
- Вечернее сообщение: 21:00 по московскому времени

Примеры CRON-выражений:
```csharp
// Каждый день в 9:00
"0 0 8 * * ?"

// Каждый день в 21:00
"0 0 21 * * ?"

// Каждые 30 минут (для тестирования)
"0 */30 * * * ?"

// Каждую минуту (для тестирования)
"0 * * * * ?"
```

Для изменения времени отправки найдите в `Program.cs` метод `ConfigureServices` и измените CRON-выражения:

```csharp
// Утреннее сообщение
q.AddTrigger(opts => opts
    .ForJob(morningJobKey)
    .WithIdentity("MorningMessageTrigger")
    .WithCronSchedule("0 0 8 * * ?", x => x
        .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time")))
);

// Вечернее сообщение
q.AddTrigger(opts => opts
    .ForJob(eveningJobKey)
    .WithIdentity("EveningMessageTrigger")
    .WithCronSchedule("0 0 21 * * ?", x => x
        .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time")))
);
```

## Использование

1. Запустите бот:
```bash
dotnet run
```

2. Добавьте бота в чат или начните с ним личную переписку

3. Доступные команды:
   - `/start` - активировать рассылку в текущем чате
   - `/citation` - получить случайную цитату с фотографией в любой момент

## Формат сообщений

### Утреннее сообщение (9:00):
- **Жирный** текст приветствия "Доброе утро!"
- Случайная красивая фотография с солнышком ☀️
- Цитата дня в формате `цитаты`
- _Курсивом_ выделенное имя автора цитаты

### Вечернее сообщение (21:00):
- **Жирный** текст приветствия "Доброй ночи!"
- Случайная красивая фотография с луной 🌙
- Цитата на ночь в формате `цитаты`
- _Курсивом_ выделенное имя автора цитаты

### Случайная цитата (команда `/citation`):
- Случайная цитата в формате `цитаты`
- Случайная фотография
- _Курсивом_ выделенное имя автора цитаты

## Технические детали

- .NET 8.0
- Entity Framework Core
- SQL Server
- Telegram.Bot
- Quartz.NET для планирования задач
- Forismatic API для цитат
- Unsplash API для фотографий

## Зависимости

Все необходимые пакеты NuGet указаны в файле проекта:
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.EntityFrameworkCore.Design
- Microsoft.Extensions.Configuration.Json
- Microsoft.Extensions.Hosting
- Newtonsoft.Json
- Quartz
- Telegram.Bot 