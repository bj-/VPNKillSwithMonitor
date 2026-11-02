# VPN Kill Switch — технический проект MVP

Подготовим проект так, чтобы его можно было сразу передать в разработку или последовательно реализовывать в Visual Studio.

Я предлагаю сохранить архитектуру с отдельной Windows-службой, WFP-модулем и Tray, но сделать первую версию максимально компактной: без собственного драйвера ядра, с возможностью добавить его позднее.

MVP v0.1

C# / .NET 10

Windows 10/11 x64

## 1. Структура репозитория

Название проекта: `VpnKillSwitch`.

```
VpnKillSwitch/
│
├── VpnKillSwitch.sln
│
├── src/
│   │
│   ├── VKS.Contracts/
│   │   ├── Commands/
│   │   ├── Events/
│   │   ├── Models/
│   │   └── Enums/
│   │
│   ├── VKS.Core/
│   │   ├── State/
│   │   ├── Policies/
│   │   ├── Interfaces/
│   │   └── Exceptions/
│   │
│   ├── VKS.Service/
│   │   ├── Program.cs
│   │   ├── KillSwitchWorker.cs
│   │   ├── ServiceHost.cs
│   │   └── Configuration/
│   │
│   ├── VKS.VpnMonitor/
│   │   ├── IVpnMonitor.cs
│   │   ├── VpnMonitor.cs
│   │   ├── NetworkInterfaceScanner.cs
│   │   ├── RouteScanner.cs
│   │   ├── TunnelHealthChecker.cs
│   │   └── VpnAdapters/
│   │
│   ├── VKS.ProcessMonitor/
│   │   ├── IProcessMonitor.cs
│   │   ├── ProcessMonitor.cs
│   │   ├── ProcessEnumerator.cs
│   │   ├── ProcessEventListener.cs
│   │   └── ProcessMatcher.cs
│   │
│   ├── VKS.Wfp/
│   │   ├── IWfpController.cs
│   │   ├── WfpController.cs
│   │   ├── WfpSession.cs
│   │   ├── WfpProvider.cs
│   │   ├── WfpFilterManager.cs
│   │   └── WfpExceptions.cs
│   │
│   ├── VKS.PolicyEngine/
│   │   ├── PolicyEngine.cs
│   │   ├── UnlockValidator.cs
│   │   ├── NetworkStateCoordinator.cs
│   │   └── StateTransitionValidator.cs
│   │
│   ├── VKS.Infrastructure/
│   │   ├── Configuration/
│   │   ├── Persistence/
│   │   ├── Logging/
│   │   └── IPC/
│   │
│   └── VKS.Tray/
│       ├── App.xaml
│       ├── App.xaml.cs
│       ├── TrayIconManager.cs
│       ├── TrayViewModel.cs
│       ├── ServiceClient.cs
│       ├── Views/
│       └── Resources/
│
├── tests/
│   ├── VKS.Core.Tests/
│   ├── VKS.PolicyEngine.Tests/
│   ├── VKS.VpnMonitor.Tests/
│   ├── VKS.ProcessMonitor.Tests/
│   └── VKS.Integration.Tests/
│
├── installer/
│   └── VKS.Installer/
│
├── docs/
│   ├── ARCHITECTURE.md
│   ├── SECURITY.md
│   ├── WFP.md
│   └── TESTING.md
│
└── README.md
```

## 2. Модель данных

Начнём с общих контрактов, которые используют все модули.

### 2.1. Состояния

```
public enum VpnState
{
    Unknown,
    Disconnected,
    Connecting,
    Connected
}

public enum NetworkProtectionState
{
    Unknown,
    Locked,
    Unlocked,
    Applying
}

public enum ProcessGuardState
{
    Unknown,
    Clear,
    Blocked
}

public enum LockReason
{
    Startup,
    VpnDisconnected,
    ForbiddenProcessRunning,
    ManualLock,
    MonitorFailure,
    ConfigurationError,
    EnforcementFailure
}
```

### 2.2. Общая модель статуса

```
public sealed record SystemStatus
{
    public VpnState Vpn { get; init; }

    public NetworkProtectionState Network { get; init; }

    public ProcessGuardState ProcessGuard { get; init; }

    public LockReason? LockReason { get; init; }

    public IReadOnlyList<ProcessInfo> BlockingProcesses { get; init; }
        = [];

    public DateTimeOffset LastUpdated { get; init; }
}
```

### 2.3. Информация о процессе

```
public sealed record ProcessInfo
{
    public int ProcessId { get; init; }

    public string Name { get; init; } = "";

    public string? ExecutablePath { get; init; }

    public string? UserName { get; init; }

    public DateTimeOffset StartTime { get; init; }
}
```

Важно: `Unknown` должен трактоваться как небезопасное состояние. Нельзя автоматически считать неизвестное состояние разрешающим.

## 3. Интерфейсы модулей

Главный принцип: модули не должны напрямую управлять друг другом. Взаимодействие происходит через интерфейсы и события.

### 3.1. VPN Monitor

```
public interface IVpnMonitor
{
    Task<VpnSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken);

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);

    event EventHandler<VpnStateChangedEventArgs>? StateChanged;
}
```

Модель результата:

```
public sealed record VpnSnapshot
{
    public VpnState State { get; init; }

    public int? InterfaceIndex { get; init; }

    public string? InterfaceName { get; init; }

    public string? RemoteEndpoint { get; init; }

    public bool RouteVerified { get; init; }

    public bool TunnelHealthVerified { get; init; }

    public DateTimeOffset CheckedAt { get; init; }
}
```

Основные методы реализации:

| Метод                          | Назначение               |
| ------------------------------ | ------------------------ |
| `ScanInterfacesAsync()`        | Поиск интерфейсов        |
| `GetActiveRoutesAsync()`       | Получение маршрутов      |
| `DetectVpnCandidatesAsync()`   | Выявление VPN-кандидатов |
| `ResolveTunnelEndpointAsync()` | Определение endpoint     |
| `CheckTunnelHealthAsync()`     | Проверка туннеля         |
| `VerifyProtectedRouteAsync()`  | Проверка маршрута        |

Результатом должен быть не просто факт наличия интерфейса, а подтверждённый защищённый сетевой путь.

Для универсальности рекомендую создать адаптерную архитектуру:

```
public interface IVpnAdapter
{
    string Name { get; }

    bool CanHandle(NetworkInterfaceInfo networkInterface);

    Task<VpnDetectionResult> DetectAsync(
        NetworkInterfaceInfo networkInterface,
        CancellationToken cancellationToken);

    Task<VpnHealthResult> CheckHealthAsync(
        VpnDetectionResult detection,
        CancellationToken cancellationToken);
}
```

В дальнейшем можно добавить:

```
VpnAdapters/
    GenericTunnelAdapter.cs
    WireGuardAdapter.cs
    OpenVpnAdapter.cs
    WindowsVpnAdapter.cs
```

`GenericTunnelAdapter` будет выполнять общую эвристику, специализированные адаптеры — более точную проверку.

### 3.2. Process Monitor

```
public interface IProcessMonitor
{
    Task<IReadOnlyList<ProcessInfo>> GetRunningBlockedProcessesAsync(
        CancellationToken cancellationToken);

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);

    event EventHandler<ProcessChangedEventArgs>? ProcessChanged;
}
```

Внутренняя логика:

```
public sealed class ProcessMonitor : IProcessMonitor
{
    public Task StartAsync(CancellationToken cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken);

    public Task RefreshSnapshotAsync(
        CancellationToken cancellationToken);

    public Task OnProcessStartedAsync(
        ProcessInfo process,
        CancellationToken cancellationToken);

    public Task OnProcessExitedAsync(
        int processId,
        CancellationToken cancellationToken);
}
```

Ключевой момент реализации — идентификация экземпляра процесса.

Нельзя использовать только PID: Windows переиспользует идентификаторы процессов.

В качестве идентификатора экземпляра лучше использовать:

```
public sealed record ProcessInstanceId(
    int ProcessId,
    DateTimeOffset StartTime);
```

Также следует проверять полный путь EXE и правила сопоставления. Для защиты от подмены файла можно дополнительно проверять SHA-256.

### 3.3. WFP Controller

Это единственный компонент, имеющий право изменять системную сетевую политику.

```
public interface IWfpController
{
    Task InitializeAsync(CancellationToken cancellationToken);

    Task ApplyLockAsync(
        LockReason reason,
        CancellationToken cancellationToken);

    Task ApplyUnlockAsync(
        CancellationToken cancellationToken);

    Task<WfpStatus> GetStatusAsync(
        CancellationToken cancellationToken);

    Task RestoreProtectionAsync(
        CancellationToken cancellationToken);
}
```

Внутренняя структура:

```
WfpController
    ├── WfpSession
    ├── WfpProvider
    ├── WfpSublayer
    ├── WfpFilterManager
    ├── WfpTransaction
    └── WfpStatusReader
```

Предлагаю ввести отдельную модель статуса:

```
public sealed record WfpStatus
{
    public bool Initialized { get; init; }

    public bool LockFiltersInstalled { get; init; }

    public bool AllowPolicyInstalled { get; init; }

    public bool EnforcementVerified { get; init; }

    public string? Error { get; init; }
}
```

`ApplyUnlockAsync()` не должен вызываться непосредственно из Tray. Только Policy Engine может инициировать эту операцию после проверки всех условий.

### 3.4. Policy Engine

Главный координатор.

```
public interface IPolicyEngine
{
    Task<SystemStatus> GetStatusAsync(
        CancellationToken cancellationToken);

    Task EvaluateAsync(
        CancellationToken cancellationToken);

    Task RequestLockAsync(
        CancellationToken cancellationToken);

    Task<UnlockResult> RequestUnlockAsync(
        CancellationToken cancellationToken);

    event EventHandler<SystemStatus>? StatusChanged;
}
```

Результат разблокировки:

```
public sealed record UnlockResult
{
    public bool Success { get; init; }

    public string? RejectionReason { get; init; }

    public SystemStatus? Status { get; init; }
}
```

Примеры причин отказа:

```
VPN_NOT_CONNECTED
FORBIDDEN_PROCESS_RUNNING
VPN_HEALTH_CHECK_FAILED
PROCESS_MONITOR_UNAVAILABLE
WFP_NOT_READY
CONFIGURATION_INVALID
```

## 4. Алгоритм Policy Engine

Здесь предлагаю сделать важное уточнение к предыдущему ТЗ.

Чтобы сохранить исходное требование — при отсутствии VPN сеть должна быть заблокирована — рекомендую одинаковое строгое условие для автоматической и ручной разблокировки:

\\[ UnlockAllowed = VPNHealthy \land NoForbiddenProcesses \\]

То есть ручная разблокировка не обходит ни одно правило. Если VPN отключён, кнопка разблокировки не поможет снять блокировку.

Это наиболее безопасная трактовка выбранных требований.

Пример:

```
public async Task EvaluateAsync(
    CancellationToken cancellationToken)
{
    var vpn = await vpnMonitor.GetSnapshotAsync(
        cancellationToken);

    var processes = await processMonitor
        .GetRunningBlockedProcessesAsync(cancellationToken);

    var wfp = await wfpController.GetStatusAsync(
        cancellationToken);

    if (!wfp.EnforcementVerified)
    {
        await wfpController.ApplyLockAsync(
            LockReason.EnforcementFailure,
            cancellationToken);

        return;
    }

    if (vpn.State != VpnState.Connected ||
        !vpn.RouteVerified ||
        !vpn.TunnelHealthVerified)
    {
        await LockAsync(
            LockReason.VpnDisconnected,
            cancellationToken);

        return;
    }

    if (processes.Count > 0)
    {
        await LockAsync(
            LockReason.ForbiddenProcessRunning,
            cancellationToken);

        return;
    }

    await UnlockAsync(cancellationToken);
}
```

Это концептуальный псевдокод. В реальной реализации потребуется дополнительно:

- проверять достоверность и свежесть результатов мониторинга;
- учитывать неизвестные состояния;
- сериализовать переходы состояний;
- применять сетевые изменения транзакционно;
- не разрешать трафик до подтверждения всех условий.

### Защита от гонок

Представим ситуацию:

1. VPN работает.
2. Запрещённых процессов нет.
3. Policy Engine начинает разблокировку.
4. В этот момент запускается запрещённое приложение.

Чтобы не возникало окна утечки, необходима атомарная последовательность:

```
Получить актуальный статус
        ↓
Проверить VPN
        ↓
Проверить процессы
        ↓
Зафиксировать поколение снимка процессов
        ↓
Подготовить WFP-политику
        ↓
Повторно проверить процессы и VPN
        ↓
Применить разрешающую политику
        ↓
Проверить фактическое состояние WFP
```

Одной периодической проверки процессов раз в две секунды для этой задачи недостаточно. Необходима непрерывная регистрация событий создания процессов и механизм синхронизации с разблокировкой.

Для строгой защиты от гонки между созданием процесса и сетевой операцией понадобится дополнительный механизм на уровне ядра или отдельная архитектура предварительной блокировки сетевых соединений. Это нужно проверить на прототипе, прежде чем объявлять защиту от гонок гарантированной.

## 5. WFP: конкретный план реализации

Предлагаю реализовать WFP через отдельную native-библиотеку `VKS.Wfp.Native`.

Причина: Windows Filtering Platform — нативный Windows API, а управление сложными структурами через C# P/Invoke быстро становится трудным для сопровождения.

Структура:

```
VKS.Wfp/
    IWfpController.cs
    WfpController.cs
    WfpPolicyBuilder.cs
    WfpStatusReader.cs

VKS.Wfp.Native/
    WfpSession.cpp
    WfpProvider.cpp
    WfpFilters.cpp
    WfpTransactions.cpp
    WfpNative.h
```

Основные Windows API:

| Функция                  | Назначение            |
| ------------------------ | --------------------- |
| `FwpmEngineOpen0`        | Открытие WFP Engine   |
| `FwpmProviderAdd0`       | Регистрация Provider  |
| `FwpmSubLayerAdd0`       | Регистрация Sublayer  |
| `FwpmFilterAdd0`         | Добавление фильтров   |
| `FwpmFilterDeleteByKey0` | Удаление фильтров     |
| `FwpmTransactionBegin0`  | Начало транзакции     |
| `FwpmTransactionCommit0` | Применение транзакции |
| `FwpmTransactionAbort0`  | Отмена транзакции     |

### 5.1. Наборы фильтров

Разделить фильтры на четыре группы:

| Группа           | Назначение                                |
| ---------------- | ----------------------------------------- |
| `BaseProtection` | Общая блокировка IPv4/IPv6                |
| `VpnTransport`   | Разрешение транспорта VPN                 |
| `ProcessGuard`   | Запрет сетевой активности запрещённых EXE |
| `Management`     | Ограниченные системные исключения         |

Особенно важно: разрешение VPN-транспорта должно быть ограничено конкретными параметрами:

- физическим интерфейсом;
- IP-адресом удалённого endpoint;
- портом;
- протоколом;
- при необходимости — локальным адресом.

Необходимо предусмотреть обновление этих параметров при смене endpoint.

Нельзя делать исключение «разрешить весь трафик процесса VPN-клиента»: некоторые клиенты используют один процесс для туннеля и других сетевых операций.

### 5.2. Принцип применения политики

В заблокированном состоянии:

```
BLOCK:
    All ordinary outbound IPv4
    All ordinary outbound IPv6
    All ordinary inbound IPv4
    All ordinary inbound IPv6

ALLOW:
    Loopback
    Required VPN transport
    Strictly required system traffic
```

В разблокированном состоянии:

```
ALLOW:
    Protected network traffic through VPN

BLOCK:
    Traffic bypassing the protected VPN path
    Traffic from forbidden executable identities
```

Последний пункт — дополнительная защита. Для запрещённых приложений целесообразно заранее установить индивидуальные WFP-фильтры по идентификаторам приложений. Тогда даже между появлением процесса и реакцией Service его сетевые подключения будут запрещены.

При этом нужно учитывать, что WFP-фильтрация на уровне ALE не заменяет пакетную фильтрацию и не решает автоматически все сценарии обхода.

Особенно важная техническая оговорка: нельзя реализовывать защиту как набор обычных правил `Allow VPN / Block Physical`. При определении безопасного маршрута нужно учитывать реальный маршрут, физический транспорт туннеля, DNS, IPv6 и сетевые переходы. Для первого прототипа я рекомендую начать с полной блокировки при неопределённости и лишь после тестирования включить разрешение через VPN.

## 6. State Manager и восстановление

Нужно хранить не только последнее состояние, но и причину, по которой была включена блокировка.

Пример `state.json`:

```
{
  "schemaVersion": 1,
  "networkState": "Locked",
  "lockReason": "VpnDisconnected",
  "manualLockRequested": false,
  "lastKnownVpnState": "Disconnected",
  "updatedAt": "2026-11-02T10:00:00Z"
}
```

Отдельно рекомендую ввести признак `manualLockRequested`.

Его назначение:

- пользователь нажал «Заблокировать»;
- сеть остаётся заблокированной даже после восстановления VPN;
- только явная команда «Разблокировать» снимает ручную блокировку;
- команда разблокировки всё равно проходит все проверки.

Таким образом, ручная блокировка не будет неожиданно отменяться автоматикой.

Состояние WFP должно считаться более авторитетным, чем JSON. Файл состояния используется для восстановления намерений системы, но не для подтверждения реального применения фильтров.

## 7. IPC: формат сообщений

Для первой версии достаточно JSON поверх Named Pipes.

Пример команды:

```
{
  "requestId": "a3f7...",
  "command": "UnlockNetwork"
}
```

Ответ:

```
{
  "requestId": "a3f7...",
  "success": false,
  "errorCode": "FORBIDDEN_PROCESS_RUNNING",
  "message": "Network unlock denied",
  "blockingProcesses": [
    {
      "pid": 1234,
      "name": "app1.exe"
    }
  ]
}
```

Пример контракта:

```
public interface IServiceClient
{
    Task<SystemStatus> GetStatusAsync(
        CancellationToken cancellationToken);

    Task<UnlockResult> UnlockAsync(
        CancellationToken cancellationToken);

    Task LockAsync(
        CancellationToken cancellationToken);
}
```

Для безопасности:

- Pipe создаётся службой.
- Tray не имеет права менять WFP напрямую.
- Проверяется SID пользователя.
- Изменение конфигурации доступно администраторам.
- Команды блокировки доступны обычному пользователю.
- Разблокировка доступна только авторизованным пользователям.
- Все запросы и отказы логируются.

## 8. План разработки по задачам

Вот в каком порядке я рекомендую реализовывать проект.

| Задача  | Результат                    |
| ------- | ---------------------------- |
| VKS-001 | Создание Solution и проектов |
| VKS-002 | Контракты и модели           |
| VKS-003 | State Manager                |
| VKS-004 | Policy Engine                |
| VKS-005 | Process Monitor              |
| VKS-006 | VPN Interface Scanner        |
| VKS-007 | Route Scanner                |
| VKS-008 | Tunnel Health Checker        |
| VKS-009 | WFP Native Library           |
| VKS-010 | WFP Lock Policy              |
| VKS-011 | WFP VPN Transport            |
| VKS-012 | WFP Unlock Policy            |
| VKS-013 | Windows Service              |
| VKS-014 | Named Pipes                  |
| VKS-015 | Tray UI                      |
| VKS-016 | Installer                    |
| VKS-017 | Integration Tests            |
| VKS-018 | Security Tests               |

### Последовательность MVP

1. Прототип 1 — без сетевой блокировки

   Проверить обнаружение VPN, маршрутов и процессов. Реализовать State Machine. На этом этапе приложение только показывает предполагаемые решения.
2. Прототип 2 — WFP Lock

   Реализовать блокировку и снятие блокировки. Тестировать на отдельной виртуальной машине, без основного рабочего подключения.
3. Прототип 3 — автоматический Kill Switch

   Соединить VPN Monitor, Process Monitor и WFP. Проверить потерю VPN и запуск запрещённых процессов.
4. Прототип 4 — Service и Tray

   Добавить IPC, автозапуск, пользовательский интерфейс и настройки.
5. Прототип 5 — Installer

   Установить приложение на чистую Windows, проверить перезагрузку, восстановление и обновление конфигурации.

## 9. Tray UI: минимальный функционал MVP

Для первой версии не нужно делать сложную панель управления.

Достаточно одного окна настроек и контекстного меню.

VPN Kill Switch

Connected

Network protected

VPN tunnel verified

No forbidden processes

Protection

VPN status

Connected

Process guard

Clear

WFP protection

Active

Blocked applications

app1.exe, app2.exe, app3.exe

Lock

Unlock

Settings

Концепция главного окна MVP.

Настройки:

1. Список запрещённых приложений.
2. Добавление приложения через выбор EXE.
3. Удаление приложения.
4. Список обнаруженных VPN-интерфейсов.
5. Интервал проверки.
6. Просмотр журнала.
7. Состояние службы.

## 10. План первого спринта

Я бы начал с реализации следующих компонентов.



Sprint 1

&#x20;Фундамент и логика



| № | Работа                         | Приоритет |
| - | ------------------------------ | --------- |
| 1 | Создать Solution               | Высокий   |
| 2 | Создать VKS.Contracts          | Высокий   |
| 3 | Создать VKS.Core               | Высокий   |
| 4 | Реализовать State Manager      | Высокий   |
| 5 | Реализовать Policy Engine      | Высокий   |
| 6 | Реализовать Process Matcher    | Высокий   |
| 7 | Реализовать Process Enumerator | Высокий   |
| 8 | Написать Unit Tests            | Высокий   |

Definition of Done:

- Все состояния определены.
- Все переходы проверяются тестами.
- Список запрещённых приложений читается из JSON.
- Все экземпляры запрещённых процессов обнаруживаются.
- При неизвестном состоянии система не разрешает разблокировку.
- Ручная разблокировка не обходит правила.
- Решения Policy Engine логируются.

Следующий спринт — обнаружение VPN и WFP.

## 11. Дополнительные требования безопасности

Перед production-релизом обязательно проверить ещё несколько моментов:

1. Защита от выключения службы. Обычный пользователь не должен иметь возможности остановить службу или изменить её конфигурацию.
2. Защита от удаления фильтров. Постоянные WFP-фильтры должны иметь контролируемый жизненный цикл.
3. Обновление программы. При обновлении MSI не должно возникать окна с разрешённой сетью.
4. Secure Boot. Если появится собственный драйвер, потребуется отдельная проверка подписания и совместимости.
5. Локальная безопасность. Loopback не должен автоматически открывать возможность сетевого обхода через локальные прокси.
6. DNS. Необходимо проверять DNS через VPN, включая IPv6 и альтернативные DNS-серверы.
7. Системные туннели. Следует отдельно тестировать VPN с динамической сменой endpoint, split tunneling и несколько одновременно активных VPN.

Особенно важно проверить поведение WFP на этапе загрузки Windows: сохранённые persistent filters и boot-time policy должны быть согласованы так, чтобы между загрузкой сетевого стека и запуском службы не появлялось незащищённого окна. Это отдельный обязательный интеграционный тест, а не предположение архитектуры.

## 12. Итоговые технические решения

Зафиксированная архитектура

Язык

C#

Framework

.NET 10

GUI

WPF

Service

Windows Service / SYSTEM

Network protection

WFP

Process monitoring

ETW + Process APIs

IPC

Named Pipes

Installer

MSI / WiX

Обязательные свойства

- Все пользователи Windows.
- IPv4 + IPv6.
- Автоматический Kill Switch.
- Контроль всех экземпляров запрещённых процессов.
- Безусловный запрет разблокировки при неизвестном состоянии.
- Ручное управление без обхода правил.
- Защита при перезапуске и сбоях.

Моя рекомендация: начать с полноценного исходного кода Sprint 1, а не сразу писать весь проект одним большим блоком. Сначала проверим архитектуру и логику на тестах, затем отдельно реализуем наиболее опасную часть — WFP.

Следующим шагом я предлагаю создать готовый стартовый репозиторий `VpnKillSwitch.sln`: проекты, `.csproj`, DI-регистрацию, модели, интерфейсы, State Manager, Policy Engine и первые Unit Tests. Это будет уже компилируемый фундамент, на который можно последовательно наращивать остальную систему.