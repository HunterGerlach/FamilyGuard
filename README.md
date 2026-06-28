# FamilyGuard

**FamilyGuard** is an open-source platform for transparent, policy-driven family computer guidance on Windows. It helps parents define and enforce simple safety rules when they are not nearby.

**DAD** (Digital Activity Defender) is the first tool in the FamilyGuard family. Future tools — including **MOM** and others — will extend the platform with additional guidance capabilities. All tools share FamilyGuard's service infrastructure, policy engine, and data layer.

## What DAD Does

DAD's first release solves one practical problem: **a child walks away while a microphone is still open in a game or voice chat app.**

1. DAD detects that the computer appears unattended (keyboard, mouse, and controller all idle).
2. After the configured timeout (default: 90 seconds), DAD mutes the microphone.
3. A quiet notification explains what happened.
4. When the child returns, they manually unmute. DAD **never auto-unmutes**.

## What DAD Does Not Do

DAD is **not spyware** and does not hide itself.

DAD does **not**: record audio, transcribe speech, capture screenshots, log keystrokes, read chat contents, inject into games, or try to defeat Windows administrators.

See [docs/security.md](docs/security.md) for the full privacy and threat model.

## Quick Start

### Prerequisites

- Windows 10 (22H2+) or Windows 11
- .NET 10 Runtime ([download](https://dotnet.microsoft.com/download/dotnet/10.0))
- Parent account: Windows administrator
- Child accounts: Windows standard users

### Install from MSI

1. Download `FamilyGuard.msi` from [Releases](https://github.com/HunterGerlach/FamilyGuard/releases).
2. Run the installer as administrator.
3. The FamilyGuard service starts automatically and launches a per-user agent for each logged-in session.
4. On first run, DAD prompts the parent to set a PIN to protect settings.

See [docs/installation.md](docs/installation.md) for manual installation, uninstall, and upgrade details.

### Runtime Behavior

Once installed, FamilyGuard runs silently in the background:

- **System tray icon** shows monitoring status:
  - Green — monitoring active, user present
  - Yellow — microphone open and user may be leaving
  - Orange/Red — microphone was auto-muted
  - Gray — service disconnected
- **Right-click the tray icon** to access Status, Settings, Event Log, and manual Mute/Unmute.
- **Settings require the parent PIN** and are rate-limited after failed attempts.
- **Events are logged** to `C:\ProgramData\FamilyGuard\familyguard.db` (SQLite).

See [docs/configuration.md](docs/configuration.md) for timeout tuning, covered users, and notification preferences.

## Building from Source

### Cross-Platform (macOS / Fedora / Linux)

```bash
podman build -f build/Containerfile -t familyguard-build .
```

This builds all cross-platform projects, runs 136 tests, and cross-compiles `FamilyGuard.Service.exe` and `FamilyGuard.Agent.exe` for Windows (win-x64).

### Full Solution (Windows)

```powershell
dotnet build FamilyGuard.sln
dotnet test FamilyGuard.sln
```

The WPF UI project requires Windows for compilation. See [CONTRIBUTING.md](CONTRIBUTING.md) for full development setup.

## Platform Roadmap

FamilyGuard is designed as a multi-tool family guidance platform:

| Tool | Focus | Status |
|---|---|---|
| **DAD** | Digital Activity Defender — endpoint protection (mic safety, presence monitoring) | V1 code-complete |
| **MOM** | Guidance and education (TBD) | Planned |
| Future | Additional family member tools | Planned |

All tools share: FamilyGuard.Service, FamilyGuard.Infrastructure, policy engine, event store, settings, and the WPF tray UI shell. See [docs/platform-roadmap.md](docs/platform-roadmap.md) for the full vision.

## Documentation

| Doc | Contents |
|---|---|
| [Installation](docs/installation.md) | MSI install, manual install, prerequisites, uninstall, upgrade |
| [Configuration](docs/configuration.md) | Settings, PIN, timeout, covered users, notifications |
| [Architecture](docs/architecture.md) | Clean Architecture, patterns, dependency direction, project structure |
| [Security & Privacy](docs/security.md) | Threat model, privacy policy, Windows APIs, anti-cheat compatibility |
| [Platform Roadmap](docs/platform-roadmap.md) | FamilyGuard vision, DAD, MOM, future tools |
| [Troubleshooting](docs/troubleshooting.md) | Common issues, logs, service recovery |
| [Development Notes](docs/development-notes.md) | Unexpected issues encountered during development |
| [Contributing](CONTRIBUTING.md) | Dev setup, PR process, coding standards |

## Tech Stack

- .NET 10 (LTS) / C# / WPF
- SQLite via Microsoft.Data.Sqlite
- xUnit + NSubstitute + Shouldly
- H.NotifyIcon.Wpf, CommunityToolkit.Mvvm
- WiX Toolset v5 (MSI)
- Podman + Red Hat UBI 9 (containerized builds)
- GitHub Actions (CI/CD with Authenticode signing)

## License

MIT — see [LICENSE](LICENSE).
