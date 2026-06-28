# Security & Privacy

## Threat Model

FamilyGuard is a cooperative family safety tool, not adversarial anti-tamper software.

**Enforcement boundary:** the Windows account/security model.

| Role | Access Level |
|---|---|
| Parent Windows account | Administrator |
| Child Windows accounts | Standard users |
| DAD settings | Protected by parent PIN |
| Uninstall/disable | Requires Windows administrator |

A knowledgeable Windows administrator can ultimately disable or remove the app. FamilyGuard does not attempt to hide itself, bypass OS controls, or engage in cat-and-mouse behavior.

## What "Tamper Resistance" Means Here

- Standard users cannot change protected policy
- Protected files/settings are ACL'd correctly
- Service restarts crashed agents
- Policy/settings changes are logged
- Uninstall/disable requires Windows admin rights

## Privacy Policy

### DAD Does Not

- Record audio
- Transcribe speech
- Capture screenshots
- Log keystrokes
- Read chat contents or app messages
- Inject into games or modify other processes
- Hide itself from the user
- Transmit any data off-device

### DAD Observes

- Keyboard/mouse/controller idle duration
- Session lock/disconnect/reconnect state
- Default communications microphone mute state
- Policy decisions and actions taken
- Service/agent health events
- User/session identity needed for policy evaluation

This data exists so families can understand what DAD did and why. It is stored locally in SQLite and never transmitted.

## Windows APIs Used

DAD uses only documented, non-intrusive Windows APIs:

| Purpose | API | Library |
|---|---|---|
| Keyboard/mouse idle | `GetLastInputInfo` | user32.dll |
| Tick count (rollover-safe) | `GetTickCount64` | kernel32.dll |
| Controller input | `XInputGetState` | xinput1_4.dll |
| Mic discovery | `IMMDeviceEnumerator::GetDefaultAudioEndpoint` | Core Audio COM |
| Mic mute state | `IAudioEndpointVolume::GetMute` | Core Audio COM |
| Mic mute | `IAudioEndpointVolume::SetMute` | Core Audio COM |
| Session enumeration | `WTSEnumerateSessions` | wtsapi32.dll |
| Session/power events | `Microsoft.Win32.SystemEvents` | .NET BCL |

**No hooks, no DLL injection, no process manipulation.** This design is intended to remain compatible with anti-cheat software.

## PIN Security

- Stored as PBKDF2-HMAC-SHA256 with 16-byte random salt, 210,000 iterations
- Legacy SHA-256 hashes auto-upgrade to PBKDF2 on successful verification
- Rate-limited: 5 failed attempts trigger 15-minute lockout
- Unlock success/failure events logged

## Update Security

- Update manifest URL must use HTTPS
- Download URL in manifest must use HTTPS
- SHA256 hash validated (64-character hex string)
- Binaries signed with Authenticode (SHA256 + DigiCert timestamp)

## Data Directory ACLs

`C:\ProgramData\FamilyGuard\` is configured by the MSI installer with:

| Principal | Access |
|---|---|
| SYSTEM | Full control |
| Administrators | Full control |
| Users | Read + Execute (no write) |

Standard users can read event logs but cannot modify settings, policies, or PIN.
