# Деплой MPIC на сервер (Windows)

## Состав релиза

Приложение публикуется как **framework-dependent** (runtime .NET должен быть установлен на сервере отдельно).

Файлы состояния, которые **не заменяются** при обновлении и должны переживать деплой:

| Файл | Назначение |
|---|---|
| `processed_letters.json` | память об обработанных письмах (пропуск ящиков без новых писем) |
| `notification_state.json` | состояние уведомлений о сбоях/восстановлении |
| `Megaplan/Token/token.json`, `token_exp_at.txt` | кэш токена API Мегаплана |
| `Logs/app_YYYYMMDD.log` | файловые логи |

Все они создаются рядом с `MPIC.exe`, т.к. служба стартует с рабочей директорией = папке приложения (настраивается в `Program.cs`).

## 1. Требования к серверу

- Windows 10/11 или Windows Server 2016+;
- **.NET Runtime 10.0** (x64) — только рантайм, SDK не нужен:
  https://dotnet.microsoft.com/download/dotnet/10.0 (`SDK -> .NET Runtime`).
  Проверка: `dotnet --list-runtimes` должен показывать `Microsoft.NETCore.App 10.x`.
- Сетевой доступ: IMAP (993) к Яндексу, SMTP (465) для уведомлений, HTTPS к API Мегаплана.

## 2. Сборка и публикация (на машине разработки)

```powershell
# из корня репозитория C:\Git\MPIC
dotnet publish .\MPIC\MPIC.csproj -c Release -o .\publish
```

В `.\publish` появится `MPIC.exe` со всеми DLL, `appsettings.json` и ресурсами `Megaplan/Mapping`.

## 3. Первичная установка службы

Скопируйте содержимое `.\publish` на сервер, например в `C:\Services\MPIC\`.

Задайте секреты через переменные окружения машины (пароли не хранятся в `appsettings.json`):

```powershell
[Environment]::SetEnvironmentVariable('MPIC__Username',        '<логин_мегаплан>', 'Machine')
[Environment]::SetEnvironmentVariable('MPIC__Password',        '<пароль_мегаплан>', 'Machine')
[Environment]::SetEnvironmentVariable('MPIC__BaseApUrl',       'https://api.megaplan.ru/api3/', 'Machine')
[Environment]::SetEnvironmentVariable('MPIC__MonitoredMailboxes__0__Password', '<пароль_ящика_1>', 'Machine')
[Environment]::SetEnvironmentVariable('MPIC__MonitoredMailboxes__1__Password', '<пароль_ящика_2>', 'Machine')
[Environment]::SetEnvironmentVariable('MPIC__NotificationSettings__SenderPassword', '<пароль_smtp>', 'Machine')
```

(После добавления переменных перезапустите консоль/службу, чтобы они подхватились.)

Создайте службу Windows (от администратора):

```powershell
New-Service -Name 'MPIC' `
  -BinaryPathName 'C:\Services\MPIC\MPIC.exe' `
  -DisplayName 'MPIC - Mail-CRM Integration Checker' `
  -StartupType Automatic
Start-Service MPIC
```

## 4. Обновление существующей службы

```powershell
Stop-Service MPIC
# удалить старые DLL/EXE, но НЕ трогать:
#   processed_letters.json, notification_state.json, Megaplan\, Logs\
# скопировать содержимое .\publish в C:\Services\MPIC
Start-Service MPIC
```

## 5. Проверка после деплоя

1. Служба запущена: `Get-Service MPIC`.
2. Лог пишется: `C:\Services\MPIC\Logs\app_ГГГГММДД.log`.
3. Первый цикл: в логе появляются строки `--- Начало цикла проверки ---` и по каждому ящику либо
   `Сделка из письма была создана` / фиксация сбоя, либо
   `Письмо от ... уже обработано в предыдущем цикле. Прекращаю цикл работы с ящиком ...`.
4. Создан файл состояния: `C:\Services\MPIC\processed_letters.json` с записями по ящикам.

## 6. Диагностика

- Служба не стартует → журнал событий Windows (`Get-EventLog -LogName Application -Source MPIC -Newest 20` или просмотр Application), ошибки хоста .NET попадают туда.
- Ошибки подключения → проверьте пароли в переменных окружения и доступность `imap.yandex.ru:993`.
- Нужно перепроверить письмо принудительно → остановить службу, удалить запись ящика из `processed_letters.json` (или файл целиком), запустить службу.
