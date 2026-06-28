# Contributing to FamilyGuard

## Development Setup

### Cross-Platform (macOS / Fedora / Linux)

1. Install [Podman](https://podman.io/docs/installation)
2. Clone the repo: `git clone git@github.com:HunterGerlach/FamilyGuard.git`
3. Build and test: `podman build -f build/Containerfile -t familyguard-build .`

This builds all cross-platform projects, runs all tests, and cross-compiles Service + Agent for win-x64. No .NET SDK required locally.

### Windows

1. Install [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
2. Clone the repo
3. Build: `dotnet build FamilyGuard.sln`
4. Test: `dotnet test FamilyGuard.sln`

## Project Structure

See [docs/architecture.md](docs/architecture.md) for the full architecture overview.

**Key rule:** dependencies point inward. Domain has zero dependencies. Application depends only on Domain. Infrastructure implements Application ports. Service/Agent/UI are composition roots with DI wiring only.

## Coding Standards

- **TDD:** write failing tests first, then implement
- **Clean Architecture:** strict dependency direction, no shortcuts
- **SOLID principles** throughout
- **.NET conventions:** `TreatWarningsAsErrors`, `Nullable` enabled, latest `LangVersion`
- **Assertions:** Shouldly (MIT). Do not use FluentAssertions (commercial license v8+).
- **Containers:** Podman + UBI 9. Do not use Docker.

## Pull Request Process

1. Create a branch from `main`
2. Write tests for your changes
3. Verify: `podman build -f build/Containerfile -t familyguard-build .`
4. Push and open a PR against `main`
5. Both Linux and Windows CI must pass
6. PRs are reviewed for architecture alignment, test coverage, and naming consistency

## Naming Conventions

| Context | Convention |
|---|---|
| Platform / service / technical | **FamilyGuard** |
| First protection tool (user-facing) | **DAD** (all caps) |
| Future tools | **MOM**, others (all caps) |
| Namespaces / projects | `FamilyGuard.*` |
| XAML design tokens | `FG` prefix (FGBlue, FGInkBrush) |

## Adding a New Policy

1. Define conditions in `FamilyGuard.Domain/Enums/PolicyCondition.cs`
2. Define actions in `FamilyGuard.Domain/Enums/PolicyActionType.cs`
3. Implement the action strategy in `FamilyGuard.Application/Policies/`
4. Add a migration in `FamilyGuard.Infrastructure/Persistence/Migrations.cs`
5. Write tests in `FamilyGuard.Application.Tests/Policies/`

## Releases

Releases are triggered by pushing a tag:

```bash
git tag v0.2.0
git push origin v0.2.0
```

The release workflow builds, tests, signs (Authenticode), packages (WiX MSI), and publishes to GitHub Releases.
