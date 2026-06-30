# CLAUDE.md — FamilyGuard / DaD

## Project

DaD (Digital Activity Defender) / FamilyGuard — a transparent Windows background app for family computer guidance. V1 feature: auto-mute microphone when child walks away from computer.

## Build

```bash
# Cross-platform build + test (macOS/Fedora/Linux)
podman build -f build/Containerfile -t familyguard-build .

# Full solution build (Windows only)
dotnet build FamilyGuard.sln

# Run tests
dotnet test tests/FamilyGuard.Domain.Tests/
dotnet test tests/FamilyGuard.Application.Tests/
dotnet test tests/FamilyGuard.Infrastructure.Tests/
dotnet test tests/FamilyGuard.Integration.Tests/  # Windows only
```

## Architecture

Clean Architecture with strict dependency direction:
- `Domain` → zero dependencies
- `Application` → Domain only (defines all port interfaces)
- `Infrastructure` → Application + Domain (implements ports)
- `Service/Agent/UI` → composition roots (DI wiring only)

Windows API calls exist ONLY in `Infrastructure/Platform/Windows/` and are:
- Conditionally excluded on non-Windows via `<Compile Remove>`
- Annotated with `[SupportedOSPlatform("windows")]`
- Gated by `#if WINDOWS` in platform registration classes

## Key Conventions

- TFMs: Domain/Application/Infrastructure/Service/Agent use `net10.0`; UI uses `net10.0-windows`
- Cross-compilation: Service + Agent cross-compile for `win-x64` from Linux containers
- Tests: xUnit + NSubstitute + Shouldly (NOT FluentAssertions — commercial license v8+)
- Assertions: Shouldly (MIT). Never use FluentAssertions.
- Container: Podman + UBI 9 (`ubi9/dotnet-100`). Never Docker.
- SQLitePCLRaw vulnerability suppressed globally (GHSA-2m69-gcr7-jv3q) — no upstream fix yet

## CI

- Linux CI: Podman build in GitHub Actions — builds all, tests all, cross-publishes win-x64 binaries
- Windows CI: Full solution build including WPF UI + integration tests
- Release: On tag push — build, test, publish, WiX MSI, update manifest, GitHub Release
