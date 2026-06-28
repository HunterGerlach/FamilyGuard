# Architecture

## Clean Architecture

FamilyGuard follows Clean Architecture with strict dependency direction. Source code dependencies point inward only.

```
Domain          <- NOTHING (zero project refs, zero NuGet deps)
     ^
Application     <- Domain only (defines all port interfaces)
     ^
Infrastructure  <- Application + Domain (implements ports)
     ^
Service/Agent/UI <- All layers (composition roots, DI wiring only)
```

## Projects

```
src/
  FamilyGuard.Domain/           Entities, enums, value objects, domain events
  FamilyGuard.Application/      Use cases, port interfaces, state machine, policy engine
  FamilyGuard.Infrastructure/   SQLite persistence, Windows adapters, IPC, updates
  FamilyGuard.Service/          Windows service host (composition root)
  FamilyGuard.Agent/            Per-user agent process (composition root)
  FamilyGuard.UI/               WPF tray + windows (composition root)
tests/
  FamilyGuard.Domain.Tests/
  FamilyGuard.Application.Tests/
  FamilyGuard.Infrastructure.Tests/
  FamilyGuard.Integration.Tests/    (Windows only)
```

## Processes

```
FamilyGuard.Service (LocalSystem)
  |-- Supervises per-user agents
  |-- Runs migrations on startup
  |-- Checks for updates periodically
  |
  +-- FamilyGuard.Agent (per user session)
       |-- Monitors presence (keyboard/mouse/controller/session state)
       |-- Evaluates policy rules against current state
       |-- Executes actions (mute mic, notify, log)
       |-- Hosts tray icon and UI windows
```

## Design Patterns

| Pattern | Where | Purpose |
|---|---|---|
| State | PresenceStateMachine | Clean state transitions (Present/LikelyAway/Away/Unknown) |
| Strategy | IPolicyActionStrategy | Extensible actions without modifying the engine |
| Observer | IEventBus / EventBus | Decouple event producers from consumers |
| Repository | IEventStore, ISettingsRepository | Abstract persistence |
| Ports & Adapters | Application/Ports + Infrastructure/Platform | Testable core, swappable infrastructure |
| Message Channel (EIP) | IMessageChannel | Service-Agent IPC abstraction |
| Composition Root | Program.cs in Service/Agent/UI | All DI wiring at outermost layer |

## Cross-Platform Build

Service and Agent target `net10.0` (not `net10.0-windows`) to enable cross-compilation from Linux containers. Windows-specific code uses:

- `#if WINDOWS` conditional compilation for platform registration
- `[SupportedOSPlatform("windows")]` attribute on all Windows adapter classes
- `<Compile Remove="Platform/Windows/**">` on non-Windows builds

The UI project targets `net10.0-windows` and requires Windows for compilation.

## Key Abstractions (Port Interfaces)

| Interface | Layer | Purpose |
|---|---|---|
| IPresenceDetector | Application/Ports/Input | Idle time + controller activity |
| ISessionMonitor | Application/Ports/Input | WTS session enumeration |
| IAgentLifecycleManager | Application/Ports/Input | Launch/stop per-user agents |
| ISystemEventMonitor | Application/Ports/Input | Sleep/wake, lock/unlock, device changes |
| IMicrophoneController | Application/Ports/Output | Read/set mic mute state |
| IEventStore | Application/Ports/Output | Structured event persistence |
| ISettingsRepository | Application/Ports/Output | Protected settings + PIN |
| IPolicyRepository | Application/Ports/Output | Policy rule CRUD |
| INotificationSender | Application/Ports/Output | Tray/toast notifications |
| IUpdateChecker | Application/Ports/Output | Update manifest checking |
