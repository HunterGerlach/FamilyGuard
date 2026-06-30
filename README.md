# DaD — Digital Activity Defender

DaD (Digital Activity Defender) is a transparent Windows background app for families. It helps parents define, enforce, and audit simple computer-usage policies when a parent is not nearby.

The first release focuses on one practical safety problem: **a child walks away while a microphone is still open in a game or voice chat app.**

## Why DaD Exists

Voice chat can stay open after a child leaves the room. DaD reduces that risk by watching for device inactivity and muting the microphone after a parent-configured timeout. It is designed to be visible, understandable, and respectful of privacy.

## What DaD Does

When a child is playing games with voice chat (for example Minecraft, Fortnite, Discord, or Xbox party chat) and walks away:

1. DaD detects that the computer appears unattended by checking keyboard, mouse, controller, and session state.
2. After the configured timeout (default: 90 seconds), DaD mutes the microphone.
3. DaD shows a quiet notification explaining what happened.
4. When the child returns, they manually unmute. DaD **never auto-unmutes**.

## What DaD Does Not Do

DaD is **not spyware** and does not hide itself from the people using the computer.

DaD does **not**:

- Record audio
- Transcribe speech
- Capture screenshots
- Log keystrokes
- Read chat contents or app messages
- Inject into games or modify other processes
- Try to defeat Windows administrators

## User Experience Principles

DaD is built around four product principles:

- **Visible:** the tray icon and status window show what DaD is doing.
- **Explainable:** policy actions are logged as understandable events.
- **Parent-controlled:** protected settings require a parent PIN and are rate-limited after failed attempts.
- **Non-invasive:** DaD uses documented Windows APIs and avoids hooks, DLL injection, and process manipulation.

## Architecture

```text
FamilyGuard.Service     Windows service (LocalSystem), supervises agents
FamilyGuard.Agent       Per-user session agent, monitors presence + mic
FamilyGuard.UI          WPF tray icon + status/settings/event-log windows
```

DaD follows Clean Architecture with strict dependency direction:

- `Domain` — entities, value objects, events, and enums with zero external dependencies
- `Application` — use cases, port interfaces, state machine, and policy engine
- `Infrastructure` — SQLite persistence, Windows API adapters, IPC, and updates
- `Service`, `Agent`, `UI` — composition roots and host-specific wiring

## Windows APIs Used

DaD uses only documented, non-intrusive Windows APIs:

| Purpose | API |
|---|---|
| Keyboard/mouse idle | `GetLastInputInfo` (`user32.dll`) |
| Controller input | `XInputGetState` (`xinput1_4.dll`) |
| Microphone mute state | `IAudioEndpointVolume::GetMute` (Core Audio COM) |
| Microphone mute | `IAudioEndpointVolume::SetMute` (Core Audio COM) |
| Session enumeration | `WTSEnumerateSessions` (`wtsapi32.dll`) |
| Session events | `Microsoft.Win32.SystemEvents` |

Because DaD avoids hooks, DLL injection, and process manipulation, the design is intended to remain compatible with anti-cheat software.

## Security Model

DaD is a cooperative family safety tool, not adversarial anti-tamper software.

Recommended Windows account setup:

- Parent Windows account: administrator
- Child Windows accounts: standard users
- DaD settings: protected by a parent PIN
- Protected data directory: ACL'd so standard users cannot modify policy files
- Uninstall: requires Windows administrator rights

A knowledgeable Windows administrator can disable or remove DaD. DaD does not attempt to hide, persist against administrators, or bypass operating-system controls.

## Privacy and Stored Data

DaD observes and stores operational metadata only:

- Presence/idle duration
- Controller activity state
- Session lock/disconnect state
- Microphone device mute state
- Policy decisions and actions
- Service/agent health events

This metadata exists so families can understand what DaD did and why.

## Development

### Prerequisites

- **Podman** for cross-platform development on Linux/macOS
- **.NET 10 SDK** for local Windows builds and WPF UI work

### Build and Test (Cross-Platform)

```bash
podman build -f build/Containerfile -t familyguard-build .
```

The container build restores dependencies, builds the cross-platform projects, and runs the cross-platform test suite.

### Build Full Solution (Windows)

```powershell
dotnet build FamilyGuard.sln
dotnet test FamilyGuard.sln
```

The WPF UI and Windows platform adapters require Windows for a full local build.

### Project Structure

```text
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
installer/
  Package.wxs                   WiX MSI installer
```

## Tech Stack

- .NET 10 (LTS)
- C# / WPF
- SQLite via Microsoft.Data.Sqlite
- xUnit + NSubstitute + Shouldly
- H.NotifyIcon.Wpf for the system tray
- CommunityToolkit.Mvvm
- WiX Toolset for MSI packaging
- Podman + Red Hat UBI 9 for containerized builds
- GitHub Actions for CI/CD

## License

MIT — see [LICENSE](LICENSE).
