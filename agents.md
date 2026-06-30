# agents.md — Unexpected Issues Encountered

This file records unexpected issues that required non-obvious solutions during development. It serves as a reference for future development sessions.

## 1. FluentAssertions v8+ Commercial License (2026-06-30)

**Issue:** FluentAssertions v8 changed from MIT to Xceed commercial license. Build output showed licensing warning on every test run.

**Resolution:** Swapped to Shouldly (MIT) across all 4 test projects. Required rewriting all assertion syntax (`.Should().Be()` → `.ShouldBe()`, `.Should().ContainSingle()` → `.ShouldHaveSingleItem()`, etc.)

**Lesson:** Check package licenses before adopting. Shouldly is the preferred assertion library for this project.

## 2. CA1416 Platform Compatibility Analyzer (2026-06-30)

**Issue:** Infrastructure project targets `net10.0` (cross-platform) for cross-compilation, but Windows adapter classes use Windows-only APIs. On Windows CI, CA1416 fired as errors (TreatWarningsAsErrors) on every call to `[SupportedOSPlatform("windows")]` types.

**Resolution:** Added `[SupportedOSPlatform("windows")]` to ALL classes in `Platform/Windows/` AND to the platform registration classes in Service/Agent. Suppressed CA1416 in Integration.Tests project since it only runs on Windows.

**Lesson:** When a cross-platform assembly contains platform-specific code behind conditional compilation, the analyzer still needs `[SupportedOSPlatform]` on every class that touches platform APIs. This includes registration/DI wiring classes.

## 3. WPF + WinForms Namespace Collision (2026-06-30)

**Issue:** Enabling `UseWindowsForms` alongside `UseWPF` caused `Application` to be ambiguous between `System.Windows.Application` and `System.Windows.Forms.Application`. XAML-generated partial class code doesn't respect `using` aliases or `global using` aliases.

**Resolution:** Removed `UseWindowsForms` entirely. Rewrote `TrayIconGenerator` to use pure WPF rendering (`DrawingVisual` + `RenderTargetBitmap`) instead of `System.Drawing`. Fully qualified `System.Windows.Application` in `App.xaml.cs`.

**Lesson:** Never mix `UseWPF` and `UseWindowsForms` in the same project. If you need System.Drawing functionality, use WPF equivalents or put it in a separate assembly.

## 4. Cross-Compilation TFM Requirements (2026-06-30)

**Issue:** Service and Agent initially targeted `net10.0-windows` which cannot be restored or built on Linux. Changing to `net10.0` broke compilation because `Program.cs` directly referenced `Platform.Windows` types.

**Resolution:** Changed TFMs to `net10.0`. Added `WINDOWS` preprocessor constant (defined only on Windows via csproj condition). Wrapped platform registration imports and calls in `#if WINDOWS`. Pass `-r win-x64` only at publish time, not in csproj.

**Lesson:** For cross-compilable executables: target `net10.0`, use `#if WINDOWS` for platform code, pass RID at publish time. Don't set `RuntimeIdentifier` in csproj — it locks restore to that platform.

## 5. SQLitePCLRaw Vulnerability (2026-06-30)

**Issue:** `Microsoft.Data.Sqlite` pulls in `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 which has a known high-severity vulnerability (GHSA-2m69-gcr7-jv3q). With `TreatWarningsAsErrors`, NU1903 failed the build. No upstream fix available.

**Resolution:** Added `NuGetAuditSuppress` in `Directory.Build.props` globally. Pinning a newer version of SQLitePCLRaw didn't help — the vulnerable version is the latest.

**Lesson:** `NuGetAuditSuppress` is the correct mechanism for suppressing specific known vulnerabilities. Put it in `Directory.Build.props` so it applies to all projects including test projects that transitively reference the vulnerable package.
