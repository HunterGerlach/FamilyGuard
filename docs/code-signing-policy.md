# Code Signing Policy

## Overview

FamilyGuard uses Authenticode code signing for all Windows executables and MSI installers. This ensures that users can verify the software's authenticity and that it has not been tampered with since publication.

Free code signing provided by [SignPath.io](https://signpath.io), certificate by [SignPath Foundation](https://signpath.org).

## Signed Artifacts

All release builds sign the following files:

- `FamilyGuard.Service.exe` — Windows service
- `FamilyGuard.Agent.exe` — Per-user agent
- `FamilyGuard.UI.exe` — WPF tray application
- `FamilyGuard-{version}-{commit}.msi` — Windows Installer package

Signatures use SHA-256 digest with RFC 3161 timestamping via DigiCert.

## Team Roles

| Role | Responsibility | Member |
|---|---|---|
| Author | Writes and modifies source code | Hunter Gerlach (@HunterGerlach) |
| Reviewer | Reviews and approves code changes | Hunter Gerlach (@HunterGerlach) |
| Approver | Authorizes signing of release builds | Hunter Gerlach (@HunterGerlach) |

## Build and Signing Process

1. Code is committed to the `main` branch on GitHub
2. A version tag (e.g., `v0.7.5`) is pushed to trigger the release workflow
3. GitHub Actions builds all projects in a clean environment (`windows-latest`)
4. All tests pass before any signing occurs
5. Executables are signed with the code signing certificate
6. WiX MSI installer is built and signed
7. Signed artifacts are published to GitHub Releases

## Security Controls

- All team members use multi-factor authentication on GitHub
- The signing certificate private key is stored as a GitHub Actions encrypted secret
- Signing only occurs in the GitHub Actions CI/CD pipeline — never on local machines
- All signing events are logged in the GitHub Actions workflow run
- Only tagged releases trigger signing — not pull requests or branch pushes

## Source Code

All signed binaries are built exclusively from the source code at:
https://github.com/HunterGerlach/FamilyGuard

No proprietary or third-party closed-source code is included.

## License

FamilyGuard is licensed under the [MIT License](../LICENSE).
