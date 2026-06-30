# DaD — Digital Activity Defender

A transparent Windows background app that lets parents define, enforce, and audit family computer usage policies when parents are not around.

## What It Does

DaD monitors the family computer and automatically enforces policies when children are using it. The first policy: **if a child walks away and leaves the microphone open, DaD auto-mutes it.**

This is **not spyware**. DaD is visible, understandable, and policy-driven. It never records audio, captures screenshots, logs keystrokes, or reads chat contents.

## V1 Feature: Unattended Microphone Protection

When a child is playing games with voice chat (Minecraft, Fortnite, Discord, Xbox party chat) and walks away:

1. DaD detects the computer is unattended (keyboard, mouse, and controller all idle)
2. After the configured timeout (default: 90 seconds), DaD mutes the microphone
3. A quiet notification tells the child what happened
4. When the child returns, they manually unmute — DaD never auto-unmutes

## Architecture

```
FamilyGuard.Service     Windows service (LocalSystem), supervises agents
FamilyGuard.Agent       Per-user session agent, monitors presence + mic
FamilyGuard.UI          WPF tray icon + status/settings windows
```

**Clean Architecture** with strict dependency direction:
- `Domain` — entities, value objects, events (zero dependencies)
- `Application` — use cases, port interfaces, state machine, policy engine
- `Infrastructure` — SQLite persistence, Windows API adapters, IPC
- `Service/Agent/UI` — composition roots (DI wiring only)

## Windows APIs Used

DaD uses only documented, non-intrusive Windows APIs:

| Purpose | API |
|---|---|
| Keyboard/mouse idle | `GetLastInputInfo` (user32.dll) |
| Controller input | `XInputGetState` (xinput1_4.dll) |
| Microphone mute state | `IAudioEndpointVolume::GetMute` (Core Audio COM) |
| Microphone mute | `IAudioEndpointVolume::SetMute` (Core Audio COM) |
| Session enumeration | `WTSEnumerateSessions` (wtsapi32.dll) |
| Session events | `Microsoft.Win32.SystemEvents` |

**No hooks, no DLL injection, no process manipulation.** This design is compatible with anti-cheat software.

## Security Model

This is a cooperative family tool, not adversarial anti-tamper software.

- Parent Windows account: administrator
- Child Windows accounts: standard users
- Settings protected by parent PIN (rate-limited, locked after failed attempts)
- Data directory ACL'd: standard users cannot modify protected files
- Uninstall requires Windows administrator rights

A knowledgeable Windows administrator can disable or remove the app. DaD does not attempt to hide itself or defeat administrators.

## Privacy

DaD **does not**:
- Record audio
- Transcribe speech
- Capture screenshots
- Log keystrokes
- Read chat contents
- Hide from the user

DaD **only observes**:
- Presence/idle duration
- Controller activity state
- Session lock/disconnect state
- Microphone device mute state
- Policy decisions and actions
- Service/agent health

## Development

### Prerequisites

- **Podman** (for cross-platform development on macOS/Fedora)
- **.NET 10 SDK** (for Windows builds)

### Build and Test (Cross-Platform)

```bash
podman build -f build/Containerfile -t familyguard-build .
```

This builds Domain, Application, and Infrastructure in a UBI 9 container and runs all cross-platform tests.

### Build Full Solution (Windows)

```powershell
dotnet build FamilyGuard.sln
dotnet test FamilyGuard.sln
```

### Project Structure

```
src/
  FamilyGuard.Domain/           Entities, enums, value objects, events
  FamilyGuard.Application/      Use cases, ports, state machine, policy engine
  FamilyGuard.Infrastructure/   SQLite, Windows adapters, IPC, updates
  FamilyGuard.Service/          Windows service host
  FamilyGuard.Agent/            Per-user agent process
  FamilyGuard.UI/               WPF tray + windows
tests/
  FamilyGuard.Domain.Tests/
  FamilyGuard.Application.Tests/
  FamilyGuard.Infrastructure.Tests/
  FamilyGuard.Integration.Tests/   (Windows only)
installer/
  Package.wxs                   WiX MSI installer
```

## Tech Stack

- .NET 10 (LTS)
- C# / WPF
- SQLite (via Microsoft.Data.Sqlite)
- xUnit + NSubstitute + Shouldly
- H.NotifyIcon.Wpf (system tray)
- CommunityToolkit.Mvvm
- WiX Toolset (MSI installer)
- Podman + Red Hat UBI 9 (containerized builds)
- GitHub Actions (CI/CD)

## License

MIT — see [LICENSE](LICENSE) file.
