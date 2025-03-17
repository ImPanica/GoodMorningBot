# Инструкция по переносу базы данных

## Вариант 1: Автоматическое создание (рекомендуется)

1. Установите SQL Server LocalDB на новом компьютере
2. Откройте командную строку в папке проекта
3. Выполните команды:
```bash
dotnet ef database update
```
База данных будет создана автоматически с нужной структурой.

## Вариант 2: Перенос существующих данных

### На старом компьютере:

1. Откройте SQL Server Management Studio (SSMS)
2. Подключитесь к LocalDB: `(localdb)\MSSQLLocalDB`
3. Откройте New Query и выполните скрипт из файла `ExportChats.sql`
4. Сохраните результат в файл `ImportChats.sql`

### На новом компьютере:

1. Установите SQL Server LocalDB
2. Выполните `dotnet ef database update` для создания структуры базы
3. Откройте SSMS и подключитесь к LocalDB
4. Выполните сохраненный скрипт `ImportChats.sql`

## Вариант 3: Прямое копирование файлов базы

1. На старом компьютере:
   - Остановите все приложения, использующие базу
   - Скопируйте файлы из:
   ```
   C:\Users\[пользователь]\AppData\Local\Microsoft\Microsoft SQL Server Local DB\Instances\MSSQLLocalDB\GoodMorningBot.mdf
   C:\Users\[пользователь]\AppData\Local\Microsoft\Microsoft SQL Server Local DB\Instances\MSSQLLocalDB\GoodMorningBot_log.ldf
   ```

2. На новом компьютере:
   - Установите SQL Server LocalDB
   - Создайте папку MSSQLLocalDB, если её нет
   - Скопируйте файлы .mdf и .ldf в ту же папку

## Примечание

При переносе на другой компьютер убедитесь, что:
1. Установлен .NET 8.0 SDK
2. Установлен SQL Server LocalDB
3. Настроен файл appsettings.json с правильными ключами API
4. Выполнены все миграции базы данных 