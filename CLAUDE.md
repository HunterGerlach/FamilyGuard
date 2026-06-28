# CLAUDE.md — FamilyGuard

## Project

**FamilyGuard** is a multi-tool family computer guidance platform. **DAD** (Digital Activity Defender) is the first tool. Future tools include **MOM** (TBD) and others. All tools share the FamilyGuard service, infrastructure, and UI shell.

## Naming

| Context | Use |
|---|---|
| Platform / service / technical | **FamilyGuard** |
| First tool (user-facing) | **DAD** (all caps, never "DaD") |
| Future tools | **MOM**, others (all caps) |
| Namespaces / projects | `FamilyGuard.*` |
| XAML design tokens | `FG` prefix (FGBlue, FGInkBrush) |

## Build

```bash
# Cross-platform build + test + cross-compile (macOS/Fedora/Linux)
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
- Container: Podman + UBI 9 (`ubi9/dotnet-100`). Never Docker.
- SQLitePCLRaw vulnerability suppressed globally (GHSA-2m69-gcr7-jv3q)

## CI

- Linux CI: Podman build — builds all, tests all, cross-publishes win-x64 binaries as artifacts
- Windows CI: Full solution build including WPF UI + integration tests
- Release: On tag push — build, test, sign (Authenticode), WiX MSI, update manifest, GitHub Release

## Code Signing

- Self-signed dev cert in GitHub secrets (`CODE_SIGNING_CERT`, `CODE_SIGNING_PASSWORD`)
- Release workflow signs .exe + MSI with signtool (SHA256 + DigiCert timestamp)
- `.certs/` is gitignored — never commit certificates
- For production: replace secret with CA-issued cert (same workflow)

## Documentation

Detailed docs in `docs/` — see README.md for the index.
