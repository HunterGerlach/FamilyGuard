# Installation

## Prerequisites

- Windows 10 (22H2 or later) or Windows 11
- [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (framework-dependent deployment)
- Windows administrator account for installation
- Child accounts configured as Windows standard users

## MSI Installer

1. Download `FamilyGuard.msi` from the [latest release](https://github.com/HunterGerlach/FamilyGuard/releases).
2. Right-click and select **Run as administrator**, or run from an elevated command prompt:
   ```powershell
   msiexec /i FamilyGuard.msi
   ```
3. The installer creates:
   - `C:\Program Files\FamilyGuard\` — executables (Service, Agent, UI)
   - `C:\ProgramData\FamilyGuard\` — data directory (SQLite database, logs)
   - FamilyGuard Windows service (auto-start)
   - Registry key: `HKLM\Software\FamilyGuard`

4. The service starts automatically and launches an agent for each interactive user session.

## Manual Installation (Development)

If building from source without the MSI:

```powershell
# Publish all components
dotnet publish src/FamilyGuard.Service/ -c Release -r win-x64 -o C:\FamilyGuard\service
dotnet publish src/FamilyGuard.Agent/ -c Release -r win-x64 -o C:\FamilyGuard\agent
dotnet publish src/FamilyGuard.UI/ -c Release -r win-x64 -o C:\FamilyGuard\ui

# Create data directory
mkdir C:\ProgramData\FamilyGuard

# Install as Windows service
sc create FamilyGuard binPath= "C:\FamilyGuard\service\FamilyGuard.Service.exe" start= auto
sc description FamilyGuard "DAD - Digital Activity Defender. Family endpoint guidance service."
sc start FamilyGuard
```

## Uninstall

### Via MSI
```powershell
msiexec /x FamilyGuard.msi
```

### Manual
```powershell
sc stop FamilyGuard
sc delete FamilyGuard
Remove-Item -Recurse C:\FamilyGuard
# Optionally remove data (preserves if you plan to reinstall):
# Remove-Item -Recurse C:\ProgramData\FamilyGuard
```

Uninstall requires Windows administrator rights. Standard users cannot remove FamilyGuard.

## Upgrade

The MSI installer supports in-place upgrades. Settings, PIN, policies, and event logs are preserved across upgrades via:

- Data stored in `C:\ProgramData\FamilyGuard\` (separate from program files)
- Schema migrations run automatically on service start
- PIN hash format is forward-compatible (legacy SHA-256 hashes auto-upgrade to PBKDF2)

## File Locations

| Path | Contents |
|---|---|
| `C:\Program Files\FamilyGuard\` | Service, Agent, and UI executables |
| `C:\ProgramData\FamilyGuard\familyguard.db` | SQLite database (events, settings, policies) |
| `HKLM\Software\FamilyGuard` | Registry: data path |
