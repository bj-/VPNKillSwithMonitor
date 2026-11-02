# VpnKillSwitch

Стартовый каркас проекта Windows VPN Kill Switch (MVP).

> Важно: это scaffold архитектуры, а не готовый защитный продукт. WFP-модуль,
> универсальное определение VPN endpoint и безопасная загрузочная политика
> оставлены как явно обозначенные реализации следующего этапа. Не используйте
> этот каркас для защиты от утечек до завершения WFP/boot-time интеграционных
> тестов на отдельной виртуальной машине.

## Требования
- Windows 10/11 x64
- .NET 10 SDK
- Visual Studio 2026 или `dotnet` CLI
- Для будущей native WFP-библиотеки: Visual Studio C++ workload / Windows SDK

## Состав
- `VKS.Contracts` — общие DTO, состояния и команды.
- `VKS.Core` — конфигурация и абстракции.
- `VKS.PolicyEngine` — решение о блокировке/разблокировке.
- `VKS.ProcessMonitor` — инвентаризация процессов (первый рабочий адаптер).
- `VKS.VpnMonitor` — начальный монитор интерфейсов; не подтверждает безопасность туннеля.
- `VKS.Wfp` — безопасный stub: не снимает блокировку и сообщает, что enforcement не реализован.
- `VKS.Infrastructure` — JSON-конфигурация и логирование-заготовка.
- `VKS.Service` — Windows Worker Service.
- `VKS.Tray` — минимальный WPF-клиент/placeholder.
- `tests` — unit tests политики.

## Сборка
```powershell
dotnet restore VpnKillSwitch.sln
dotnet build VpnKillSwitch.sln
dotnet test VpnKillSwitch.sln
```

WPF-проект и Windows Service предназначены для сборки на Windows.

## Конфигурация
Скопируйте `config/settings.example.json` в:
`C:\ProgramData\VpnKillSwitch\config\settings.json`

Права на каталог должны быть ограничены SYSTEM и Administrators. В этом каркасе
создание ACL и защищённое обновление конфигурации ещё не реализованы.

## Следующие обязательные этапы
1. Реализовать WFP Provider/Sublayer/filters и проверку фактической политики.
2. Добавить boot-time filters и проверить отсутствие окна утечки при старте.
3. Реализовать VPN adapters, включая endpoint и транспортные исключения.
4. Устранить гонку запуска процессов/разблокировки на уровне enforcement.
5. Добавить авторизацию Named Pipes, installer MSI и интеграционные тесты.
